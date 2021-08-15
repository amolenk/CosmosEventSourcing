using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Projections.Cosmos
{
    public class CosmosViewRepository : IViewRepository
    {
        private readonly CosmosClient _client;
        private readonly string _databaseId;
        private readonly string _containerId;

        public CosmosViewRepository(
            string endpointUrl, string authorizationKey, string databaseId,
            string containerId = "views")
        {
            _client = new CosmosClient(endpointUrl, authorizationKey);
            _databaseId = databaseId;
            _containerId = containerId;
        }

        public async Task<IView> LoadViewAsync(string name)
        {
            var container = _client.GetContainer(_databaseId, _containerId);
            var partitionKey = new PartitionKey(name);

            try
            {
                var response = await container.ReadItemAsync<CosmosView>(name, partitionKey);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return new CosmosView();
            }
        }

        public async Task<bool> SaveViewAsync(string name, IView view)
        {
            var cView = (CosmosView) view;
            var container = _client.GetContainer(_databaseId, _containerId);
            var partitionKey = new PartitionKey(name);

            
            var item = new 
            {
                id = name,
                logicalCheckpoint = cView.LogicalCheckpoint,
                payload = cView.Payload
            };

            try
            {
                await container.UpsertItemAsync(item, partitionKey, new ItemRequestOptions
                {
                    IfMatchEtag = cView.Etag
                });

                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                return false;
            }
        }
    }
}