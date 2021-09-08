using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore;
using Microsoft.Azure.Cosmos;

namespace CosmosValidate
{
    public class CosmosValidatorEngine : IValidatorEngine
    {
        private readonly IEventTypeResolver _eventTypeResolver;
        private readonly string _endpointUrl;
        private readonly string _authorizationKey;
        private readonly string _databaseId;
        private readonly string _originalEventContainerId;
        private readonly string _outputEventContainerId;
        private readonly string _leaseContainerId;
        private readonly List<IValidator> _validators;
        private ChangeFeedProcessor _changeFeedProcessor;

        public CosmosValidatorEngine(
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
            _validators = new List<IValidator>();
        }

        public void RegisterComparer(IValidator validator)
        {
            _validators.Add(validator);
        }
 

        public Task StartAsync(string instanceName)
        {
            CosmosClient client = new CosmosClient(_endpointUrl, _authorizationKey);

            Container eventContainer = client.GetContainer(_databaseId, _originalEventContainerId);
            Container leaseContainer = client.GetContainer(_databaseId, _leaseContainerId);

            _changeFeedProcessor = eventContainer
                .GetChangeFeedProcessorBuilder<Change>("CosmosDBCompares", HandleChangesAsync)
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
  
                var validator = _validators
                    .SingleOrDefault(m => m.IsSubscribedTo(@event));
                
                EventWrapper expectedEventWrapper = validator != null
                    ? validator.Transform(change)
                    : change;
                
                EventWrapper actualEventWrapper;
                try
                {
                    actualEventWrapper = await outputEventStore.LoadEventWrapperAsync(expectedEventWrapper.Id);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(
                        $"Error when trying to load eventWrapper with Id" +
                        $" {expectedEventWrapper.Id}. {ex.Message} + {ex.InnerException?.Message}");
                }

                if (!Validator.Equals(expectedEventWrapper, actualEventWrapper))
                {
                    throw new ApplicationException($"Failed to match {change.Id} from original container with" +
                                                   $" {expectedEventWrapper.Id} on new container");
                }
            }
        }
    }
}