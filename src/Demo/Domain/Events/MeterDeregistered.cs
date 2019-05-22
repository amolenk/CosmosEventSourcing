using System;
using EventStore;

namespace Demo.Domain.Events
{
    public class MeterDeregistered : EventBase
    {
        public string MeterId { get; set; }
    }
}