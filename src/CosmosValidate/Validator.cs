using System;
using System.Collections.Generic;
using EventStore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CosmosValidate
{
    public abstract class Validator : IValidator
    {
        private Dictionary<string, Func<IEvent, EventWrapper, EventWrapper>> _transformers;

        public Validator()
        {
            _transformers = new Dictionary<string, Func<IEvent, EventWrapper, EventWrapper>>();
        }
 
        
        public bool IsSubscribedTo(IEvent e) => _transformers.ContainsKey(e.GetType().Name);
       

        public EventWrapper GetExpectedValue(IEvent e, EventWrapper eventWrapper)
        {
            if (_transformers.TryGetValue(eventWrapper.EventType, out Func<IEvent, EventWrapper, EventWrapper> handler))
            {
                return handler(e, eventWrapper);
            }

            return eventWrapper;
        }

        public static bool Equals(EventWrapper expected, EventWrapper actual)
        {
         
            return expected.Id == actual.Id
                   && expected.EventData.ToString(Formatting.None) == actual.EventData.ToString(Formatting.None)
                   && expected.EventType == actual.EventType;
        }
 
        protected void RegisterTransformer<TEvent>(Func<TEvent, EventWrapper, EventWrapper> handler)
            where TEvent : IEvent
        {
            _transformers[typeof(TEvent).Name] = (e,ew) => handler((TEvent)e, ew);
        }

        protected EventWrapper CreateEventWrapper(string id, string streamId, int version, IEvent eventToWrap)
        {
            var retVal = new EventWrapper
            {
                Id = id,
                StreamInfo = new StreamInfo
                {
                    Id = streamId,
                    Version = version
                },
                EventType = eventToWrap.GetType().Name,
                EventData = JObject.FromObject(eventToWrap)
            };
            return retVal;
        }
    }
}