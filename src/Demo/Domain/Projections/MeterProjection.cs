using System;
using Demo.Domain.Events;
using EventStore;
using Projections;

namespace Demo.Domain.Projections
{
    public class MeterProjection
    {
        public string MeterId { get; set; }

        public string PostalCode { get; set; }

        public string HouseNumber { get; set; }

        public bool IsActivated { get; set; }
        public bool IsRegistered { get; set; }

        public int FailedActivationAttempts { get; set; }

        public DateTime? LatestReadingDate { get; set; }
        
    }

    public class MeterProjector : Projection<MeterProjection>
    {
        public MeterProjector()
        {
            RegisterHandler<MeterRegistered>(WhenMeterRegistered);
            RegisterHandler<MeterActivated>(WhenMeterActivated);
            RegisterHandler<MeterDeregistered>(WhenMeterDeregistered);
            RegisterHandler<MeterActivationFailed>(WhenMeterActivationFailed);
            RegisterHandler<MeterReadingsCollected>(WhenMeterReadingsCollected);
        }

        public override string GetViewName(string streamId, IEvent @event)
        {
            return streamId;
        }

        private void WhenMeterRegistered(MeterRegistered e, MeterProjection view)
        {
            view.MeterId = e.MeterId;
            view.PostalCode = e.PostalCode;
            view.HouseNumber = e.HouseNumber;
            view.IsActivated = false; // Not active until it is activated.
            view.IsRegistered = true;
        }

        private void WhenMeterActivated(MeterActivated e, MeterProjection view)
        {
            view.IsActivated = true;
        }

        private void WhenMeterDeregistered(MeterDeregistered e, MeterProjection view)
        {
            view.IsRegistered = false;
        }

        private void WhenMeterActivationFailed(MeterActivationFailed e, MeterProjection view)
        {
            view.FailedActivationAttempts++;
        }

        private void WhenMeterReadingsCollected(MeterReadingsCollected e, MeterProjection view)
        {
            view.LatestReadingDate = e.Date;
        }
    }
}