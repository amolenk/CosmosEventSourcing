using System;

namespace EventStore
{
    public interface IEventTypeResolver
    {
        Type GetEventType(string typeName);
    }
}