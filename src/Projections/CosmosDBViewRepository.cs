using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using EventStore;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json.Linq;

namespace Projections
{
    public class CosmosDBViewRepository : IViewRepository
    {
        private readonly DocumentClient _client;
        private readonly string _database;
        private readonly string _container;

        public CosmosDBViewRepository(string endpointUri, string authKey, string database,
            string container = "views")
        {
            _client = new DocumentClient(new Uri(endpointUri), authKey);
            _database = database;
            _container = container;
        }

        public async Task<View> LoadViewAsync(string name)
        {
            Uri uri = UriFactory.CreateDocumentUri(_database, _container, name);
            var partitionKey = new PartitionKey(name);

            try
            {
                var response = await _client.ReadDocumentAsync(uri, new RequestOptions { PartitionKey = partitionKey });
                var document = response.Resource;

                return new View(
                    document.GetPropertyValue<Dictionary<string, ViewPartitionCheckpoint>>("checkpoints"),
                    document.GetPropertyValue<JObject>("payload"),
                    document.ETag);

            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return new View();
            }
        }

        public async Task<bool> SaveViewAsync(string name, View view)
        {
            Uri uri = UriFactory.CreateDocumentCollectionUri(_database, _container);
            var partitionKey = new PartitionKey(name);

            var document = new 
            {
                id = name,
                checkpoints = view.PartitionCheckpoints,
                payload = view.Payload
            };

            var accessCondition = new AccessCondition { Condition = view.Etag, Type = AccessConditionType.IfMatch }; 

            try
            {
                await _client.UpsertDocumentAsync(uri, document, new RequestOptions
                { 
                    PartitionKey = partitionKey,
                    AccessCondition = accessCondition
                });

                return true;
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                return false;
            }
        }
    }
}