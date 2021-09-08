using System;
using System.Collections.Generic;
using EventStore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CosmosValidate
{
    public abstract class Validator : IValidator
    {
        private Dictionary<Type, Func<EventWrapper, EventWrapper>> _transformers;
       // private Dictionary<Type, Func<EventWrapper, EventWrapper, bool>> _validators;

        public Validator()
        {
            _transformers = new Dictionary<Type, Func<EventWrapper, EventWrapper>>();
           // _validators = new Dictionary<Type, Func<EventWrapper, EventWrapper, bool>>();
        }

        public abstract string GetNewEventId(string originalId);


        // public override string GetNewEventId(string originalStreamId)
        // {
        //     return originalStreamId.Replace("meter", "monitor");
        // }
        public bool IsSubscribedTo(IEvent e) => _transformers.ContainsKey(e.GetType());

        public EventWrapper Transform(EventWrapper e)
        {
            if (_transformers.TryGetValue(e.GetType(), out Func<EventWrapper, EventWrapper> handler))
            {
                return handler(e);
            }

            return e;
        }
        // public bool Validate(IEvent e, EventWrapper originalEvent, EventWrapper duplicateEvent)
        // {
        //     if (_validators.TryGetValue(e.GetType(), out Func<EventWrapper, EventWrapper, bool> handler))
        //     {
        //         return handler(originalEvent, duplicateEvent);
        //     }
        //
        //     return DefaultValidator(originalEvent, duplicateEvent);
        // }

        /// <summary>
        /// Expects exact match on Id, EventData.ToString
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <returns></returns>
        // public static bool DefaultValidator(EventWrapper expected, EventWrapper actual)
        // {
        //     return expected.Id == actual.Id
        //            && expected.EventData.ToString(Formatting.None) == actual.EventData.ToString(Formatting.None)
        //            && expected.EventType == actual.EventType;
        // }
        public static bool Equals(EventWrapper expected, EventWrapper actual)
        {
            return expected.Id == actual.Id
                   && expected.EventData.ToString(Formatting.None) == actual.EventData.ToString(Formatting.None)
                   && expected.EventType == actual.EventType;
        }

        protected void RegisterTransformer<TEvent>(Func<EventWrapper, EventWrapper> handler)
            where TEvent : IEvent
        {
            _transformers[typeof(TEvent)] = (e) => handler((EventWrapper)e);
        }

        // protected void RegisterValidator<TEvent>(Func<EventWrapper, EventWrapper, bool> handler)
        //     where TEvent : IEvent
        //
        // {
        //     _validators[typeof(TEvent)] = handler;
        // }

        // protected void RegisterValidator<TEvent>(Func<EventWrapper, EventWrapper, bool> handler)
        //     where TEvent: IEvent
        //
        // {
        //     _validators[typeof(TEvent)] = handler ;
        // }
        protected EventWrapper GetEventWrapper(string streamId, int version, IEvent e)
        {
            var retVal = new EventWrapper
            {
                Id = $"{streamId}:{version}",
                StreamInfo = new StreamInfo
                {
                    Id = streamId,
                    Version = version
                },
                EventType = e.GetType().Name,
                EventData = JObject.FromObject(e)
            };
            return retVal;
        }
    }
}