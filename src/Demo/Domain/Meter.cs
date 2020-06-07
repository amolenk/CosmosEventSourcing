using System;
using System.Collections.Generic;
using Demo.Domain.Events;
using EventStore;

namespace Demo.Domain
{
    public class Meter
    {
        public string MeterId { get; private set; }

        public string PostalCode { get; private set; }

        public string HouseNumber { get; private set; }

        public bool IsActivated { get; private set; }

        public int FailedActivationAttempts { get; private set; }

        public DateTime? LatestReadingDate { get; private set; }

        internal int Version { get; private set; }

        internal List<IEvent> Changes { get; }

        private string _activationCode;

        public Meter(string meterId, string postalCode, string houseNumber, string activationCode)
        {
            Changes = new List<IEvent>();
            
            Apply(new MeterRegistered
            {
                MeterId = meterId,
                PostalCode = postalCode,
                HouseNumber = houseNumber,
                ActivationCode = activationCode
            });
        }

        public Meter(IEnumerable<IEvent> events)
        {
            Changes = new List<IEvent>();

            foreach (var @event in events)
            { 
                Mutate(@event);
                Version += 1;
            }
        }

        public bool Activate(string activationCode)
        {
            if (IsActivated) throw new InvalidOperationException("Already activated.");

            if (activationCode == _activationCode)
            {
                Apply(new MeterActivated());
                return true;
            }
            else
            {
                Apply(new MeterActivationFailed { ActivationCode = activationCode });
                return false;
            }
        }

        private void Apply(IEvent @event)
        {
            Changes.Add(@event);
            Mutate(@event);
        }

        private void Mutate(IEvent @event)
        {
            ((dynamic)this).When((dynamic)@event);
        }

        private void When(MeterRegistered @event)
        {
            MeterId = @event.MeterId;
            PostalCode = @event.PostalCode;
            HouseNumber = @event.HouseNumber;
            _activationCode = @event.ActivationCode;
        }

        private void When(MeterActivated @event)
        {
            IsActivated = true;
        }

        private void When(MeterReadingsCollected @event)
        {
            if (LatestReadingDate == null || @event.Date > LatestReadingDate)
            {
                LatestReadingDate = @event.Date;
            }
        }

        private void When(MeterActivationFailed @event)
        {
            FailedActivationAttempts += 1;
        }

        #region Snapshot Functionality
        
        public Meter(MeterSnapshot snapshot, int version, IEnumerable<IEvent> events)
        {
            MeterId = snapshot.Id;
            PostalCode = snapshot.PostalCode;
            HouseNumber = snapshot.HouseNumber;
            IsActivated = snapshot.IsActivated;
            FailedActivationAttempts = snapshot.FailedActivationAttempts;
            LatestReadingDate = snapshot.LatestReadingDate;
            _activationCode = snapshot.ActivationCode;
            Changes = new List<IEvent>();
            Version = version;

            foreach (var @event in events)
            { 
                Mutate(@event);
                Version += 1;
            }
        }

        public MeterSnapshot GetSnapshot()
        {
            return new MeterSnapshot(
                MeterId,
                PostalCode,
                HouseNumber,
                IsActivated,
                FailedActivationAttempts,
                LatestReadingDate,
                _activationCode);
        }

        #endregion
    }
}