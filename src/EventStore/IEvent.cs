using System;
using System.Threading.Tasks;

namespace EventStore
{
    public interface IEvent
    {
        DateTime Timestamp { get; }
    }
}