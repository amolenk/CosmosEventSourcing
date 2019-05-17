using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.ChangeFeedProcessor;
using Microsoft.Azure.Documents.ChangeFeedProcessor.PartitionManagement;

namespace Projections
{
    public class CosmosDBProjectionEngine
    {
        private readonly string _endpointUri;
        private readonly string _authKey;
        private readonly string _database;
        private readonly string _eventContainer;
        private readonly string _leaseContainer;
        private readonly string _viewContainer;
        private readonly List<IProjection> _projections;

        private IChangeFeedProcessor _changeFeedProcessor;

        public CosmosDBProjectionEngine(string endpointUri, string authKey, string database,
            string eventContainer = "events", string leaseContainer = "leases", string viewContainer = "views")
        {
            _authKey = authKey;
            _endpointUri = endpointUri;
            _database = database;
            _eventContainer = eventContainer;
            _leaseContainer = leaseContainer;
            _viewContainer = viewContainer;
            _projections = new List<IProjection>();
        }

        public void RegisterProjection(IProjection projection)
        {
            _projections.Add(projection);
        }

        public async Task StartAsync()
        {
            var feedCollectionInfo = new DocumentCollectionInfo
            {
                DatabaseName = _database,
                CollectionName = _eventContainer,
                Uri = new Uri(_endpointUri),
                MasterKey = _authKey
            };

            var leaseCollectionInfo = new DocumentCollectionInfo
            {
                DatabaseName = _database,
                CollectionName = _leaseContainer,
                Uri = new Uri(_endpointUri),
                MasterKey = _authKey
            };

            var viewRepository = new CosmosDBViewRepository(_endpointUri, _authKey, _database);

            var builder = new ChangeFeedProcessorBuilder();
            _changeFeedProcessor = await builder
                .WithHostName("ProjectionHost")
                .WithFeedCollection(feedCollectionInfo)
                .WithLeaseCollection(leaseCollectionInfo)
                .WithObserverFactory(new EventObserverFactory(_projections, viewRepository))
                .WithProcessorOptions(new ChangeFeedProcessorOptions {
                    StartFromBeginning = true
                })
                .BuildAsync();
      
            await _changeFeedProcessor.StartAsync();
        }

        public Task StopAsync()
        {
            return _changeFeedProcessor.StopAsync();
        }
    }
}