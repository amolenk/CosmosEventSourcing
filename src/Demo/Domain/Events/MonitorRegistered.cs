using System;
using EventStore;

namespace Demo.Domain.Events
{
    public class MonitorRegistered : EventBase
    {
        public string MonitorId { get; }

        public string PostalCode { get; }

        public string HouseNumber { get; }

        public string ActivationCode { get; }

        public MonitorRegistered(string monitorId, string postalCode, string houseNumber, string activationCode)
        {
            MonitorId = monitorId;
            PostalCode = postalCode;
            HouseNumber = houseNumber;
            ActivationCode = activationCode;

        }
    }
}