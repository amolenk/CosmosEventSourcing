using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor.FeedProcessing;
using Newtonsoft.Json.Linq;

namespace Projections
{
    public class EventObserver : IChangeFeedObserver
    {
        private readonly List<IProjection> _projections;
        private readonly IViewRepository _viewRepository;

        public EventObserver(List<IProjection> projections, IViewRepository viewRepostory)
        {
            _projections = projections;
            _viewRepository = viewRepostory;
        }

        public Task OpenAsync(IChangeFeedObserverContext context)
        {
            return Task.CompletedTask;
        }

        public Task CloseAsync(IChangeFeedObserverContext context, ChangeFeedObserverCloseReason reason)
        {
            return Task.CompletedTask;
        }

        public async Task ProcessChangesAsync(IChangeFeedObserverContext context, IReadOnlyList<Document> documents, CancellationToken cancellationToken)
        {
            foreach (var document in documents)
            {
                var @event = DeserializeEvent(document);

                foreach (var projection in _projections)
                {
                    if (!projection.CanHandle(@event))
                    {
                        continue;
                    }

                    var streamInfo = document.GetPropertyValue<JObject>("stream");
                    var viewName = projection.GetViewName(streamInfo["id"].Value<string>(), @event);

                    var handled = false;
                    while (!handled)
                    {
                        var view = await _viewRepository.LoadViewAsync(viewName);
                        if (view.IsNewerThanCheckpoint(context.PartitionKeyRangeId, document))
                        {
                            projection.Apply(@event, view);
                        
                            view.UpdateCheckpoint(context.PartitionKeyRangeId, document);

                            handled = await _viewRepository.SaveViewAsync(viewName, view);
                        }
                        else
                        {
                            // Already handled.
                            handled = true;
                        }

                        if (!handled)
                        {
                            // Oh noos! Somebody changed the view in the meantime, let's wait and try again.
                            await Task.Delay(500);
                        }
                    }
                }
            }
        }

        private static IEvent DeserializeEvent(Document document)
        {
            var eventType = Type.GetType($"Demo.Domain.Events.{document.GetPropertyValue<string>("eventType")}, Demo");

            return (IEvent)document.GetPropertyValue<JObject>("payload").ToObject(eventType);
        }
    }
}