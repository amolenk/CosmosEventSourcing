using System;
using EventStore;

namespace Demo.Domain.Events
{
    public class MeterRegistered : EventBase
    {
        public string MeterId { get; set; }

        public string PostalCode { get; set; }

        public string HouseNumber { get; set; }

        public string ActivationCode { get; set; }
    }
}