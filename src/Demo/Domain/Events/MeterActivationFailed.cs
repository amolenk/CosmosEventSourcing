using System;
using EventStore;

namespace Demo.Domain.Events
{
    public class MeterActivationFailed : EventBase
    {
        public string ActivationCode { get; set; }
    }
}