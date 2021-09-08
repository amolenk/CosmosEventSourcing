using System;
using System.Collections.Generic;
using System.Linq;
using Demo.Domain.Events;
using EventStore;
using Projections;

namespace Demo.Migrations
{
   
    
    public class MeterMigrator : Migrator.Migrator
    {
        public MeterMigrator()
        {
            RegisterHandler<MeterRegistered>(Map);
            RegisterHandler<MeterActivated>(Map);
            RegisterHandler<MeterDeregistered>(Map);
            RegisterHandler<MeterActivationFailed>(Map);
            RegisterHandler<MeterReadingsCollected>(Map);
        }

        public override string GetNewStreamId(string originalStreamId)
        {
            return originalStreamId.Replace("meter", "monitor");
        }

        private IEvent Map(MeterRegistered e)
        {
            return new MonitorRegistered(e.MeterId, e.PostalCode, e.HouseNumber, e.ActivationCode);
        }

        private IEvent Map(MeterActivated e)
        {
            return new MonitorActivated();

        }

        private IEvent Map(MeterDeregistered e)
        {
            return new MonitorDeregistered(e.MeterId);
        }

        private IEvent Map(MeterActivationFailed e)
        {
            return new MonitorActivationFailed(e.ActivationCode);
        }

        private IEvent Map(MeterReadingsCollected e)
        {
            var monitorReadings = 
                e.Readings.Select(t => new MonitorReading(t.Timestamp, t.Value)).ToList();
            return new MonitorReadingsCollected(e.Date, monitorReadings.ToArray());
        }
       
    }
}