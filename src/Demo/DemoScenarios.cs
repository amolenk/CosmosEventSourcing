using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConsoleUI;
using Demo.Domain;
using Demo.Domain.Events;
using EventStore;
using Microsoft.Azure.Documents.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Projections;

namespace Demo
{
    [TestClass]
    public class DemoScenarios 
    {
        private const string EndpointUri = "https://eventstore.documents.azure.com:443/";
        private const string Database = "eventsdb";

        private static readonly string AuthKey = Environment.GetEnvironmentVariable("COSMOSDB_EVENT_SOURCING_KEY");

        [TestMethod]
        public async Task SC01_AddNewStreamAsync()
        {
            var eventStore = new CosmosDBEventStore(EndpointUri, AuthKey, Database);
            await eventStore.MigrateAsync();

            var meterRegistered = new MeterRegistered
            {
                MeterId = "1",
                PostalCode = "1852ER",
                HouseNumber = "25",
                ActivationCode = "supersecret"
            };

            var streamId = $"meter:{meterRegistered.MeterId}";

            var succes = await eventStore.AppendToStreamAsync(streamId, 0, new IEvent[]
            {
                meterRegistered
            });

            Assert.IsTrue(succes, "Unexpected stream version encountered.");
        }

        [TestMethod]
        public async Task SC02_AppendToExistingStreamAsync()
        {
            var eventStore = new CosmosDBEventStore(EndpointUri, AuthKey, Database);

            var meterActivated = new MeterActivated();
            var meterReadingsCollected = new MeterReadingsCollected
            {
                Date = DateTime.Today,
                Readings = GenerateMeterReadings(DateTime.Today).ToArray()
            };

            var succes = await eventStore.AppendToStreamAsync("meter:1", 1, new IEvent[]
            {
                meterActivated,
                meterReadingsCollected
            });

            Assert.IsTrue(succes, "Unexpected stream version encountered.");
        }

        [TestMethod]
        public async Task SC03_ReadFromStreamAsync()
        {
            var eventStore = new CosmosDBEventStore(EndpointUri, AuthKey, Database);

            var stream = await eventStore.LoadStreamAsync("meter:1");

            Assert.AreEqual(3, stream.Events.Count(), "Stream meter:1 should contain 3 events at this point.");
        }

        [TestMethod]
        public async Task SC04_NewAggregateAsync()
        {
            var eventStore = new CosmosDBEventStore(EndpointUri, AuthKey, Database);
            
            // Request parameters
            var meterId = "2";
            var postalCode = "111 64";
            var houseNumber = "4";
            var activationCode = "supersecret";

            var meter = new Meter(meterId, postalCode, houseNumber, activationCode);

            var repository = new MeterRepository(eventStore);
            var succes = await repository.SaveMeterAsync(meter);

            Assert.IsTrue(succes, "Unexpected stream version encountered.");
        }

        [TestMethod]
        public async Task SC05_UpdateAggregateAsync()
        {
            var eventStore = new CosmosDBEventStore(EndpointUri, AuthKey, Database);

            // Request parameters
            var meterId = "2";
            var activationCode = "supersecret";

            var repository = new MeterRepository(eventStore);

            var meter = await repository.LoadMeterAsync(meterId);

            meter.Activate(activationCode);

            var succes = await repository.SaveMeterAsync(meter);

            Assert.IsTrue(succes, "Unexpected stream version encountered.");
        }

        [TestMethod]
        public Task SC06_GenerateMeterReadingsAsync()
        {
            // Generate some readings for the next 14 days.
            return Task.WhenAll(
                AppendReadingsCollectedEvents("meter:1", DateTime.Today.AddDays(1), 14),
                AppendReadingsCollectedEvents("meter:2", DateTime.Today.AddDays(1), 14));
        }

        [TestMethod]
        public async Task SC07_RunProjectionsAsync()
        {
            var projectionEngine = new CosmosDBProjectionEngine(EndpointUri, AuthKey, Database);

            projectionEngine.RegisterProjection(new TotalActivatedMetersProjection());
            projectionEngine.RegisterProjection(new DailyTotalsByWeekProjection());

            await projectionEngine.StartAsync();

            await Task.Delay(-1);
        }

        #region Helper Methods

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
