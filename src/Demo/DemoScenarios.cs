using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ConsoleUI;
using Demo.Domain;
using Demo.Domain.Events;
using EventStore;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Projections;

namespace Demo
{
    [TestClass]
    public class DemoScenarios 
    {
        private const string EndpointUri = "https://cosmoseventstore.documents.azure.com:443/";
        private const string Database = "mydatabase";

        private static readonly string AuthKey = Environment.GetEnvironmentVariable("COSMOSDB_EVENT_SOURCING_KEY");

        [TestMethod]
        public async Task SC00_MigrateDB()
        {
            DocumentCollection eventsContainer = new DocumentCollection();
            eventsContainer.Id = "events";
            eventsContainer.PartitionKey.Paths.Add("/stream/id");

            DocumentCollection leasesContainer = new DocumentCollection();
            leasesContainer.Id = "leases";
            leasesContainer.PartitionKey.Paths.Add("/id");

            DocumentCollection viewsContainer = new DocumentCollection();
            viewsContainer.Id = "views";
            viewsContainer.PartitionKey.Paths.Add("/id");

            StoredProcedure appendToStreamsSproc = new StoredProcedure();
            appendToStreamsSproc.Id = "spAppendToStream";
            appendToStreamsSproc.Body = File.ReadAllText("js/spAppendToStream.js");

            using (var client = new DocumentClient(new Uri(EndpointUri), AuthKey))
            {
                await ResetContainerAsync(client, eventsContainer);
                await ResetContainerAsync(client, leasesContainer);
                await ResetContainerAsync(client, viewsContainer);

                await client.CreateStoredProcedureAsync(
                    UriFactory.CreateDocumentCollectionUri(Database, eventsContainer.Id),
                    appendToStreamsSproc);
            }
        }

        [TestMethod]
        public async Task SC01_CreateStreamAsync()
        {
            var eventStore = new CosmosDBEventStore(EndpointUri, AuthKey, Database);

            var meterRegistered = new MeterRegistered
            {
                MeterId = "87000001",
                PostalCode = "1000 AA",
                HouseNumber = "25",
                ActivationCode = "supersecret"
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
            var eventStore = new CosmosDBEventStore(EndpointUri, AuthKey, Database);

            var streamId = $"meter:87000001";
            var stream = await eventStore.LoadStreamAsync(streamId);

            var readingsCollected = new MeterReadingsCollected
            {
                Date = DateTime.Today,
                Readings = GenerateMeterReadings(DateTime.Today).ToArray()
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
            var eventStore = new CosmosDBEventStore(EndpointUri, AuthKey, Database);
            
            // Request parameters.
            var meterId = "87000002";
            var postalCode = "9999 BB";
            var houseNumber = "4";
            var activationCode = "supersecret";

            // New domain object.
            var meter = new Meter(meterId, postalCode, houseNumber, activationCode);

            var repository = new MeterRepository(eventStore);
            var succes = await repository.SaveMeterAsync(meter);

            Assert.IsTrue(succes, "Unexpected stream version encountered.");
        }

        [TestMethod]
        public async Task SC04_DomainUpdateAsync()
        {
            var eventStore = new CosmosDBEventStore(EndpointUri, AuthKey, Database);
            
            // Request parameters.
            var meterId = "87000002";
            var activationCode = "supersecret";

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
            // Generate some readings for the next 14 days.
            return Task.WhenAll(
                AppendReadingsCollectedEvents("meter:87000001", DateTime.Today.AddDays(1), 14),
                AppendReadingsCollectedEvents("meter:87000002", DateTime.Today.AddDays(1), 14));
        }

        [TestMethod]
        public async Task SC05B_RunProjectionsAsync()
        {
            var projectionEngine = new CosmosDBProjectionEngine(EndpointUri, AuthKey, Database);

            projectionEngine.RegisterProjection(new TotalActivatedMetersProjection());
            projectionEngine.RegisterProjection(new DailyTotalsByWeekProjection());

            await projectionEngine.StartAsync();

            await Task.Delay(-1);
        }

        #region Helper Methods

        private async Task ResetContainerAsync(DocumentClient client, DocumentCollection container)
        {
            try
            {
                await client.DeleteDocumentCollectionAsync(
                    UriFactory.CreateDocumentCollectionUri(Database, container.Id));
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound) 
            {
                // Container didn't exist yet.
            }

            await client.CreateDocumentCollectionAsync(
                UriFactory.CreateDatabaseUri(Database), container);
        }
        private async Task AppendReadingsCollectedEvents(string streamId, DateTime fromDate, int dayCount)
        {
            var eventStore = new CosmosDBEventStore(EndpointUri, AuthKey, Database);

            var stream = await eventStore.LoadStreamAsync(streamId);

            // Only append more ReadingsCollected events if we haven't done this before.
            // This can be detected by looking at the stream version.
            if (stream.Version <= 3)
            {
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
                        Date = fromDate.AddDays(dayCount),
                        Readings = GenerateMeterReadings(fromDate.AddDays(dayCount)).ToArray()
                    }
                });
            }
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
