using System;

namespace EventStore
{
    public interface IEvent
    {
        DateTime Timestamp { get; }
    }
}