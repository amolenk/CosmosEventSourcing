using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace EventStore
{
    public class CosmosSnapshotStore : ISnapshotStore
    {
        private readonly CosmosClient _client;
        private readonly string _databaseId;
        private readonly string _containerId;

        public CosmosSnapshotStore(string endpointUrl, string authorizationKey,
            string databaseId, string containerId = "snapshots")
        {
            _client = new CosmosClient(endpointUrl, authorizationKey);
            _databaseId = databaseId;
            _containerId = containerId;
        }

        public async Task<Snapshot> LoadSnapshotAsync(string streamId)
        {
            Container container = _client.GetContainer(_databaseId, _containerId);

            PartitionKey partitionKey = new PartitionKey(streamId);

            var response = await container.ReadItemAsync<Snapshot>(streamId, partitionKey);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return response.Resource;
            }

            return null;
        }

        public async Task SaveSnapshotAsync(string streamId, int version, object snapshot)
        {
            Container container = _client.GetContainer(_databaseId, _containerId);

            PartitionKey partitionKey = new PartitionKey(streamId);

            await container.UpsertItemAsync(new Snapshot
            {
                StreamId = streamId,
                Version = version,
                SnapshotData = JObject.FromObject(snapshot)
            }, partitionKey);
        }
    }
}