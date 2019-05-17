using System;
using System.Collections.Generic;
using EventStore;

namespace Projections
{
    public interface IProjection
    {
        bool CanHandle(Type eventType);

        string GetViewName(IEvent @event);

        void Apply(IEvent @event, IView view);
    }
}