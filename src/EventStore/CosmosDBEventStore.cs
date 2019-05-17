using System;
using System.Collections.Generic;
using System.IO;
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

        private CosmosDBEventStore(DocumentClient client, string database, string container)
        {
            _client = client;
            _database = database;
            _container = container;
        }

        public static async Task<CosmosDBEventStore> CreateAsync(string endpointUri, string authKey, string database,
        string container = "events")
        {
            var client = new DocumentClient(new Uri(endpointUri), authKey);

            await new Migration(client, database, container).RunAsync();

            return new CosmosDBEventStore(client, database, container);
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

                    var eventType = Type.GetType(item.clrtype);
                    events.Add(JObject.FromObject(item.payload).ToObject(eventType));
                }
            }

            return new EventStream(id, version, events);
        }

        public async Task<bool> AppendToStreamAsync(string id, int expectedVersion, IEnumerable<IEvent> events)
        {
            var wrappedEventVersion = expectedVersion + 1;
            var wrappedEvents = events.Select(e => new
            {
                stream = new
                {
                    id = id,
                    version = wrappedEventVersion++
                },
                clrtype = e.GetType(),
                payload = e
            });

            var uri = UriFactory.CreateStoredProcedureUri(_database, _container, "spAppendToStream");
            var eventsJson = JsonConvert.SerializeObject(wrappedEvents);

            var options = new RequestOptions { PartitionKey = new PartitionKey(id) };
            var result = await _client.ExecuteStoredProcedureAsync<bool>(uri, options, id, expectedVersion, eventsJson);
            
            return result.Response;
        }
    }
}