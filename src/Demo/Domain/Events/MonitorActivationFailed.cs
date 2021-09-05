using System;
using EventStore;

namespace Demo.Domain.Events
{
    public class MonitorActivationFailed : EventBase
    {
        public string ActivationCode { get; }

        public MonitorActivationFailed(string activationCode)
        {
            ActivationCode = activationCode;
        }
    }
}