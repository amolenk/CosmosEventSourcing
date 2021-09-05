using System;
using EventStore;

namespace Demo.Domain.Events
{
    public class MonitorDeregistered : EventBase
    {
        public string MonitorId { get; }

        public MonitorDeregistered(string monitorId)
        {
            MonitorId = monitorId;
        }
    }
}