using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace CosmosValidate
{
    public class CosmosValidatorEngine : IValidatorEngine
    {
        private readonly IEventTypeResolver _eventTypeResolver;
        private readonly string _endpointUrl;
        private readonly string _authorizationKey;
        private readonly string _databaseId;
        private readonly string _originalEventContainerId;
        private readonly string _migratedEventContainerId;
        private readonly string _validationSuccessContainerId;
        private readonly string _validationFailureContainerId;
        private readonly string _leaseContainerId;
        private readonly List<IValidator> _validators;
        private ChangeFeedProcessor _changeFeedProcessor;


        public CosmosValidatorEngine(
            IEventTypeResolver eventTypeResolver,
            string endpointUrl,
            string authorizationKey,
            string databaseId,
            string originalEventContainerId,
            string migratedEventContainerId,
            string validationSuccessContainerId,
            string validationFailureContainerId,
            string leaseContainerId)
        {
            _eventTypeResolver = eventTypeResolver;
            _endpointUrl = endpointUrl;
            _authorizationKey = authorizationKey;
            _databaseId = databaseId;
            _originalEventContainerId = originalEventContainerId;
            _migratedEventContainerId = migratedEventContainerId;
            _validationSuccessContainerId = validationSuccessContainerId;
            _validationFailureContainerId = validationFailureContainerId;
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
                .GetChangeFeedProcessorBuilder<Change>("CosmosDBComparer", HandleChangesAsync)
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

        private CosmosEventStore GetMigratedEventStore()
        {
            var eventStore =
                new CosmosEventStore(_eventTypeResolver, _endpointUrl, _authorizationKey, _databaseId,
                    _migratedEventContainerId);
            return eventStore;
        }

        private CosmosEventStore GetValidationSuccessEventStore()
        {
            var eventStore =
                new CosmosEventStore(_eventTypeResolver, _endpointUrl, _authorizationKey, _databaseId,
                    _validationSuccessContainerId);
            return eventStore;
        }

        private CosmosEventStore GetValidationFailureEventStore()
        {
            var eventStore =
                new CosmosEventStore(_eventTypeResolver, _endpointUrl, _authorizationKey, _databaseId,
                    _validationFailureContainerId);
            return eventStore;
        }

        private async Task HandleChangesAsync(IReadOnlyCollection<Change> changes, CancellationToken cancellationToken)
        {
            var migratedEventStore = GetMigratedEventStore();
            var successEventStore = GetValidationSuccessEventStore();
            var failureEventStore = GetValidationFailureEventStore();


            foreach (var change in changes)
            {
                var @event = change.GetEvent(_eventTypeResolver);

                var validator = _validators
                    .SingleOrDefault(m => m.IsSubscribedTo(@event));

                EventWrapper expectedEventWrapper = validator != null
                    ? validator.GetExpectedValue(@event, change)
                    : change;

                EventWrapper actualEventWrapper;
                try
                {
                    actualEventWrapper = await migratedEventStore.LoadEventWrapperAsync(expectedEventWrapper.Id);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(
                        $"Error when trying to load eventWrapper with Id" +
                        $" {expectedEventWrapper.Id}. {ex.Message} + {ex.InnerException?.Message}");
                }

                var completedEvent = new ValidationCompletedEvent()
                {
                    OriginalId = expectedEventWrapper.Id,
                    ComparableId = actualEventWrapper.Id,
                    OriginalEventData = expectedEventWrapper.EventData.ToString(Formatting.None),
                    ComparableEventData = actualEventWrapper.EventData.ToString(Formatting.None),
                    OriginalEventType = expectedEventWrapper.EventType,
                    ComparableEventType = actualEventWrapper.EventType,
                };
                bool saved;
                if (!Validator.Equals(expectedEventWrapper, actualEventWrapper))
                {
                    saved = await failureEventStore.AppendToStreamAsync(change.StreamInfo.Id,
                        change.StreamInfo.Version - 1,
                        new[] { completedEvent });
                }
                else
                {
                    saved = await successEventStore.AppendToStreamAsync(change.StreamInfo.Id,
                        change.StreamInfo.Version - 1,
                        new[] { completedEvent });
                }

                if (!saved)
                    throw new ApplicationException("Failed to save.");
            }
        }
    }
}