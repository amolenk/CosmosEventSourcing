using System;
using System.Threading.Tasks;
using EventStore;
using Projections;

namespace ConsoleUI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var endpointUri = "https://eventstore.documents.azure.com:443/";
            var key = Environment.GetEnvironmentVariable("COSMOSDB_EVENT_SOURCING_KEY");
            var database = "eventsdb";

            var eventStore = await CosmosDBEventStore.CreateAsync(endpointUri, key, database);

            var projectionEngine = new CosmosDBProjectionEngine(endpointUri, key, database);
            projectionEngine.RegisterProjection(new GreetingCountProjection());
            //projectionEngine.RegisterProjection(new GreetingCountByRegionProjection());

            await projectionEngine.StartAsync();

            Console.WriteLine("Change Feed Processor started. Press <Enter> key to stop...");

            // var stream = await eventStore.LoadStreamAsync("Test");

            // await eventStore.AppendToStreamAsync("Test", stream.Version, new IEvent[]
            // {
            //     new GreetedEvent { Message = "Hello, world!", Region = "World" },
            //     new GreetedEvent { Message = "Hello, mars!", Region = "Mars" }
            // });


            Console.ReadLine();

            await projectionEngine.StopAsync();
        }
    }
}
