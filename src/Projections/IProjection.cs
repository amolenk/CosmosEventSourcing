using System;
using System.Collections.Generic;
using EventStore;

namespace Projections
{
    public interface IProjection
    {
        bool CanHandle(IEvent @event);

        string GetViewName(string streamId, IEvent @event);

        void Apply(IEvent @event, IView view);
    }
}