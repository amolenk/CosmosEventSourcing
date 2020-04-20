using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EventStore
{
    public class CosmosDBEventStore : IEventStore
    {
        private readonly DocumentClient _client;
        private readonly string _database;
        private readonly string _container;

        public CosmosDBEventStore(string endpointUri, string authKey, string database,
            string container = "events")
        {
            _client = new DocumentClient(new Uri(endpointUri), authKey);
            _database = database;
            _container = container;
        }

        public async Task<EventStream> LoadStreamAsync(string id)
        {
            var uri = UriFactory.CreateDocumentCollectionUri(_database, _container);

            var queryable = _client.CreateDocumentQuery(uri, new SqlQuerySpec { 
                QueryText = "SELECT * FROM events e WHERE e.stream.id = @streamId ORDER BY e.stream.version",
                Parameters = new SqlParameterCollection()  { 
                    new SqlParameter("@streamId", id) 
                }
            }).AsDocumentQuery();

            int version = 0;
            var events = new List<IEvent>();

            while(queryable.HasMoreResults)
            {
            	var page = await queryable.ExecuteNextAsync();
                foreach (var item in page)
                {
                    version = item.stream.version;

                    events.Add(DeserializeEvent(item));
                }
            }

            return new EventStream(id, version, events);
        }

        public async Task<bool> AppendToStreamAsync(string streamId, int expectedVersion, IEnumerable<IEvent> events)
        {
            // Serialize events to single JSON array to pass to stored procedure.
            var json = SerializeEvents(streamId, expectedVersion, events);

            // Call store procedure to bulk insert events (only if the expected version matches).
            var uri = UriFactory.CreateStoredProcedureUri(_database, _container, "spAppendToStream");
            var options = new RequestOptions { PartitionKey = new PartitionKey(streamId) };
            var result = await _client.ExecuteStoredProcedureAsync<bool>(uri, options, streamId, expectedVersion, json);
            
            return result.Response;
        }

        private static string SerializeEvents(string streamId, int expectedVersion, IEnumerable<IEvent> events)
        {
            var items = events.Select(e => new
            {
                id = $"{streamId}:{++expectedVersion}:{e.GetType().Name}",
                stream = new
                {
                    id = streamId,
                    version = expectedVersion
                },
                eventType = e.GetType().Name,
                payload = e
            });

            return JsonConvert.SerializeObject(items);
        }

        private static IEvent DeserializeEvent(dynamic item)
        {
            var eventType = Type.GetType($"Demo.Domain.Events.{item.eventType}, Demo");
            
            return JObject.FromObject(item.payload).ToObject(eventType);
        }
    }
}