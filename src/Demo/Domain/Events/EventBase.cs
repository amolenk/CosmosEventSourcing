using System;
using System.Diagnostics;
using EventStore;

namespace Demo.Domain.Events
{
    [DebuggerStepThrough]
    public abstract class EventBase : IEvent
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}