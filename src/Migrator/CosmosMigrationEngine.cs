using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore;
using Microsoft.Azure.Cosmos;

namespace Migrator
{
    public class CosmosMigrationEngine : IMigrationEngine
    {
        private readonly IEventTypeResolver _eventTypeResolver;
        private readonly string _endpointUrl;
        private readonly string _authorizationKey;
        private readonly string _databaseId;
        private readonly string _originalEventContainerId;
        private readonly string _outputEventContainerId;
        private readonly string _leaseContainerId;
        private readonly List<IMigrator> _migrators;
        private ChangeFeedProcessor _changeFeedProcessor;

        public CosmosMigrationEngine(
            IEventTypeResolver eventTypeResolver,
            string endpointUrl,
            string authorizationKey,
            string databaseId,
            string originalEventContainerId,
            string outputEventContainerId,
            string leaseContainerId)
        {
            _eventTypeResolver = eventTypeResolver;
            _endpointUrl = endpointUrl;
            _authorizationKey = authorizationKey;
            _databaseId = databaseId;
            _originalEventContainerId = originalEventContainerId;
            _outputEventContainerId = outputEventContainerId;
            _leaseContainerId = leaseContainerId;
            _migrators = new List<IMigrator>();
        }

        public void RegisterMigration(IMigrator migrator)
        {
            _migrators.Add(migrator);
        }

        public Task StartAsync(string instanceName)
        {
            CosmosClient client = new CosmosClient(_endpointUrl, _authorizationKey);

            Container eventContainer = client.GetContainer(_databaseId, _originalEventContainerId);
            Container leaseContainer = client.GetContainer(_databaseId, _leaseContainerId);

            _changeFeedProcessor = eventContainer
                .GetChangeFeedProcessorBuilder<Change>("CosmosDBMigrations", HandleChangesAsync)
                .WithInstanceName(instanceName)
                .WithLeaseContainer(leaseContainer)
                .WithStartTime(new DateTime(2020, 5, 1, 0, 0, 0, DateTimeKind.Utc))
                .Build();

            return _changeFeedProcessor.StartAsync();
        }

        public Task StopAsync()
        {
            return _changeFeedProcessor.StopAsync();
        }

        private CosmosEventStore GetOutputEventStore()
        {
            var eventStore =
                new CosmosEventStore(_eventTypeResolver, _endpointUrl, _authorizationKey, _databaseId,
                    _outputEventContainerId);
            return eventStore;
        }

        private async Task HandleChangesAsync(IReadOnlyCollection<Change> changes, CancellationToken cancellationToken)
        {
            var outputEventStore = GetOutputEventStore();
            foreach (var change in changes)
            {
                var @event = change.GetEvent(_eventTypeResolver);

                var subscribedMigrator = _migrators
                    .SingleOrDefault(m => m.IsSubscribedTo(@event));


                IEvent newEvent = subscribedMigrator != null
                    ? subscribedMigrator.Transform(@event)
                    : @event;
                string newStreamId = subscribedMigrator != null
                    ? subscribedMigrator.GetNewStreamId(change.StreamInfo.Id)
                    : change.StreamInfo.Id;


                var saved = await outputEventStore.AppendToStreamAsync(
                    newStreamId,
                    change.StreamInfo.Version - 1,
                    new[] { newEvent });

                if (!saved)
                {
                    var existingStream = await outputEventStore.LoadStreamAsync(newStreamId, change.StreamInfo.Version);
                    if (!existingStream.Events.Any())
                    {
                        // If it didn't save and it isn't already there,
                        // I want to stop now and find out why.
                        throw new ApplicationException(
                            "Could not save migrated item even though it didn't already exist.");
                    }
                }
            }
        }
    }
}