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
                try
                {
                    var eventType = Type.GetType(document.GetPropertyValue<string>("clrtype"));

                    foreach (var projection in _projections)
                    {
                        if (!projection.CanHandle(eventType))
                        {
                            continue;
                        }

                        var @event = (IEvent)document.GetPropertyValue<JObject>("payload").ToObject(eventType);   
                        var viewName = projection.GetViewName(@event);

                        var handled = false;
                        while (!handled)
                        {
                            var view = await _viewRepository.LoadViewAsync(viewName);
                            if (view.IsNewerThanCheckpoint(context.PartitionKeyRangeId, document))
                            {
                                Console.WriteLine($"[{viewName}] Projecting event {document.Id}.");

                                projection.Apply(@event, view);
                            
                                view.UpdateCheckpoint(context.PartitionKeyRangeId, document);

                                handled = await _viewRepository.SaveViewAsync(viewName, view);
                            }
                            else
                            {
                                Console.WriteLine($"[{viewName}] Event {document.Id} already projected.");
                                handled = true;
                            }

                            if (!handled)
                            {
                                System.Console.WriteLine("Failed, trying again!");
                                await Task.Delay(1000);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }
}