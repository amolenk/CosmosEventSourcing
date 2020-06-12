using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Demo.Domain;
using Demo.Domain.Events;
using Demo.Domain.Projections;
using EventStore;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Projections;

namespace Demo
{
    [TestClass]
    public class DemoScenarios : IEventTypeResolver
    {
        private const string EndpointUrl = "https://cosmoseventsourcing.documents.azure.com:443/";
        private static readonly string AuthorizationKey = Environment.GetEnvironmentVariable("COSMOSDB_EVENT_SOURCING_KEY");
        private const string DatabaseId = "mydatabase";

        public Type GetEventType(string typeName)
        {
            return Type.GetType($"Demo.Domain.Events.{typeName}, Demo");
        }

        [TestMethod]
        public async Task SC00_MigrateDB()
        {
            CosmosClient client = new CosmosClient(EndpointUrl, AuthorizationKey);
            
            await client.CreateDatabaseIfNotExistsAsync(DatabaseId, ThroughputProperties.CreateManualThroughput(400));
            Database database = client.GetDatabase(DatabaseId);

            await database.DefineContainer("events", "/stream/id").CreateIfNotExistsAsync();
            await database.DefineContainer("leases", "/id").CreateIfNotExistsAsync();
            await database.DefineContainer("views", "/id").CreateIfNotExistsAsync();
            await database.DefineContainer("snapshots", "/id").CreateIfNotExistsAsync();

            StoredProcedureProperties storedProcedureProperties = new StoredProcedureProperties
            {
                Id = "spAppendToStream",
                Body = File.ReadAllText("js/spAppendToStream.js")
            };

            Container eventsContainer = database.GetContainer("events");
            try
            {
                await eventsContainer.Scripts.DeleteStoredProcedureAsync("spAppendToStream");
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Stored procedure didn't exist yet.
            } 
            await eventsContainer.Scripts.CreateStoredProcedureAsync(storedProcedureProperties);
        }

        [TestMethod]
        public async Task SC01_CreateStreamAsync()
        {
            IEventStore eventStore = new CosmosEventStore(this, EndpointUrl, AuthorizationKey, DatabaseId);

            var meterRegistered = new MeterRegistered
            {
                MeterId = "87000001",
                PostalCode = "1000 AA",
                HouseNumber = "25",
                ActivationCode = "542-484"
            };

            var meterActivated = new MeterActivated();

            // 1. Add a new stream.
            var streamId = $"meter:{meterRegistered.MeterId}";

            var succes = await eventStore.AppendToStreamAsync(
                streamId,
                0,
                new IEvent[] { meterRegistered, meterActivated });

            Assert.IsTrue(succes, "Unexpected stream version encountered.");
        }

        [TestMethod]
        public async Task SC02_AppendToExistingStreamAsync()
        {
            IEventStore eventStore = new CosmosEventStore(this, EndpointUrl, AuthorizationKey, DatabaseId);

            var streamId = $"meter:87000001";
            var stream = await eventStore.LoadStreamAsync(streamId);

            var readingsCollected = new MeterReadingsCollected
            {
                Date = new DateTime(2020, 4, 30),
                Readings = GenerateMeterReadings(new DateTime(2020, 4, 30)).ToArray()
            };
                
            var succes = await eventStore.AppendToStreamAsync(
                streamId,
                stream.Version,
                new IEvent[] { readingsCollected });

            Assert.IsTrue(succes, "Unexpected stream version encountered.");
        }

        [TestMethod]
        public async Task SC03_DomainAddAsync()
        {
            IEventStore eventStore = new CosmosEventStore(this, EndpointUrl, AuthorizationKey, DatabaseId);
            
            // Request parameters.
            var meterId = "87000002";
            var postalCode = "9999 BB";
            var houseNumber = "4";
            var activationCode = "745-195";

            // New domain object.
            var meter = new Meter(meterId, postalCode, houseNumber, activationCode);

            var repository = new MeterRepository(eventStore);
            var succes = await repository.SaveMeterAsync(meter);

            Assert.IsTrue(succes, "Unexpected stream version encountered.");
        }

        [TestMethod]
        public async Task SC04_DomainUpdateAsync()
        {
            IEventStore eventStore = new CosmosEventStore(this, EndpointUrl, AuthorizationKey, DatabaseId);
            
            // Request parameters.
            var meterId = "87000002";
            var activationCode = "745-195";

            // Load domain object.
            var repository = new MeterRepository(eventStore);
            var meter = await repository.LoadMeterAsync(meterId);

            // Call business logic on domain object.
            meter.Activate(activationCode);

            var succes = await repository.SaveMeterAsync(meter);

            Assert.IsTrue(succes, "Unexpected stream version encountered.");
        }

        [TestMethod]
        public Task SC05A_GenerateMeterReadingsAsync()
        {
            // Generate some readings.
            return Task.WhenAll(
                AppendReadingsCollectedEvents("meter:87000001", new DateTime(2020, 5, 1), 14),
                AppendReadingsCollectedEvents("meter:87000002", new DateTime(2020, 5, 1), 14));
        }

        [TestMethod]
        public async Task SC05B_RunProjectionsAsync()
        {
            IViewRepository viewRepository = new CosmosViewRepository(EndpointUrl, AuthorizationKey, DatabaseId);
            IProjectionEngine projectionEngine = new CosmosProjectionEngine(this, viewRepository, EndpointUrl, AuthorizationKey, DatabaseId);

            projectionEngine.RegisterProjection(new TotalActivatedMetersProjection());
            projectionEngine.RegisterProjection(new DailyTotalsByWeekProjection());

            await projectionEngine.StartAsync("TestInstance");

            await Task.Delay(-1);
        }

        [TestMethod]
        public async Task SC06A_SaveSnapshotAsync()
        {
            IEventStore eventStore = new CosmosEventStore(this, EndpointUrl, AuthorizationKey, DatabaseId);
            ISnapshotStore snapshotStore = new CosmosSnapshotStore(EndpointUrl, AuthorizationKey, DatabaseId);
            
            // Request parameters.
            var meterId = "87000001";

            // Load domain object.
            var repository = new MeterRepository(eventStore);
            var meter = await repository.LoadMeterAsync(meterId);

            var snapshot = meter.GetSnapshot();

            await snapshotStore.SaveSnapshotAsync($"meter:{meterId}", meter.Version, snapshot);
        }

        [TestMethod]
        public Task SC06B_GenerateMeterReadingsAsync()
        {
            // Generate some more readings.
            return AppendReadingsCollectedEvents("meter:87000001", new DateTime(2020, 5, 15), 3);
        }

        [TestMethod]
        public async Task SC06C_LoadSnapshotAsync()
        {
            IEventStore eventStore = new CosmosEventStore(this, EndpointUrl, AuthorizationKey, DatabaseId);
            ISnapshotStore snapshotStore = new CosmosSnapshotStore(EndpointUrl, AuthorizationKey, DatabaseId);
            
            var repository = new MeterRepositorySnapshotDecorator(
                eventStore,
                snapshotStore,
                new MeterRepository(eventStore));

            // Request parameters.
            var meterId = "87000001";

            // Load domain object.
            var meter = await repository.LoadMeterAsync(meterId);

            Assert.AreEqual(20, meter.Version);
        }

        #region Helper Methods

        private async Task AppendReadingsCollectedEvents(string streamId, DateTime fromDate, int dayCount)
        {
            IEventStore eventStore = new CosmosEventStore(this, EndpointUrl, AuthorizationKey, DatabaseId);

            var stream = await eventStore.LoadStreamAsync(streamId);

            // Only append more ReadingsCollected events if we haven't done this before.
            // This can be detected by looking at the stream version.
            var events = Enumerable.Range(0, dayCount - 1)
                .Select(i => new MeterReadingsCollected
                {
                    Date = fromDate.AddDays(i),
                    Readings = GenerateMeterReadings(fromDate.AddDays(i)).ToArray()
                })
                .ToList();
            
            await eventStore.AppendToStreamAsync(streamId, stream.Version, events);

            // Wait a little while before adding the last one.
            // This ensures that the last event will be at a different timestamp
            // which is better for explaining the mechanism.
            await Task.Delay(TimeSpan.FromSeconds(2));

            await eventStore.AppendToStreamAsync(streamId, stream.Version + events.Count, new IEvent[]
            {
                new MeterReadingsCollected
                {
                    Date = fromDate.AddDays(dayCount - 1),
                    Readings = GenerateMeterReadings(fromDate.AddDays(dayCount - 1)).ToArray()
                }
            });
        }
        private static readonly Random _rand = new Random();
        private IEnumerable<MeterReading> GenerateMeterReadings(DateTime date)
        {
            for (int i = 1; i <= 96; i++)
            {
                yield return new MeterReading
                {
                    Timestamp = date.AddMinutes(i * 15),
                    Value = _rand.Next(0, 4)
                };
            }
        }

        #endregion
    }
}
