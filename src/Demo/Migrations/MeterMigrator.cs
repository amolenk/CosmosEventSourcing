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
            var retVal = new MonitorRegistered(e.MeterId, e.PostalCode, e.HouseNumber, e.ActivationCode);
            retVal.Timestamp = e.Timestamp;
            return retVal;
        }

        private IEvent Map(MeterActivated e)
        {
            var retVal = new MonitorActivated();
            retVal.Timestamp = e.Timestamp;
            return retVal;
        }

        private IEvent Map(MeterDeregistered e)
        {
            var retVal = new MonitorDeregistered(e.MeterId);
            retVal.Timestamp = e.Timestamp;
            return retVal;
        }

        private IEvent Map(MeterActivationFailed e)
        {
            var retVal = new MonitorActivationFailed(e.ActivationCode);
            retVal.Timestamp = e.Timestamp;
            return retVal;
        }

        private IEvent Map(MeterReadingsCollected e)
        {
            var monitorReadings = 
                e.Readings.Select(t => new MonitorReading(t.Timestamp, t.Value)).ToList();
            var retVal = new MonitorReadingsCollected(e.Date, monitorReadings.ToArray());
            retVal.Timestamp = e.Timestamp;
            return retVal;
        }
       
    }
}