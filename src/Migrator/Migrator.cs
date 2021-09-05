using System;
using System.Collections.Generic;
using EventStore;

namespace Migrator
{
    public abstract class Migrator : IMigrator
    {
        private Dictionary<Type, Func<IEvent, IEvent>> _handlers;

        public Migrator()
        {
            _handlers = new Dictionary<Type, Func<IEvent, IEvent>>();
        }

        public abstract string GetNewStreamId(string originalStreamId);

        public bool IsSubscribedTo(IEvent e) => _handlers.ContainsKey(e.GetType());

        public IEvent Transform(IEvent e)
        {
            if (_handlers.TryGetValue(e.GetType(), out Func<IEvent, IEvent> handler))
            {
                return handler(e);
            }

            return e;
        }

        protected void RegisterHandler<TEvent>(Func<TEvent, IEvent> handler)
            where TEvent : IEvent
        {
            _handlers[typeof(TEvent)] = new Func<IEvent, IEvent>((e) => handler((TEvent)e));
        }
    }
}