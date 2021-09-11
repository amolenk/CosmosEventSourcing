using System.Linq;
using CosmosValidate;
using Demo.Domain.Events;
using EventStore;

namespace Demo.Migrations
{
    public class MeterValidator : Validator
    {
        public MeterValidator()
        {
            RegisterTransformer<MeterRegistered>(Transformer);
            RegisterTransformer<MeterActivated>(Transformer);
            RegisterTransformer<MeterDeregistered>(Transformer);
            RegisterTransformer<MeterActivationFailed>(Transformer);
            RegisterTransformer<MeterReadingsCollected>(Transformer);
        }
        
        public string ReplaceMeterWithMonitor(string originalStreamId)
        {
            return originalStreamId.Replace("meter", "monitor")
                .Replace("Meter", "Monitor");
        }
        
        private EventWrapper Transformer(MeterRegistered e, EventWrapper original)
        {
            var newStreamId = ReplaceMeterWithMonitor(original.StreamInfo.Id);
            var newEntityId = ReplaceMeterWithMonitor(e.MeterId);
            var newEventId = $"{newStreamId}:{original.StreamInfo.Version}";
             
             var newEvent = new MonitorRegistered(newEntityId, e.PostalCode, e.HouseNumber, e.ActivationCode);
             newEvent.Timestamp = e.Timestamp;
      
            var expectedValue = CreateEventWrapper(newEventId, newStreamId, original.StreamInfo.Version,  
                newEvent);
            return expectedValue;
        }
        private EventWrapper Transformer(MeterActivated e, EventWrapper original)
        {
            var newStreamId = ReplaceMeterWithMonitor(original.StreamInfo.Id);
            var newEventId = $"{newStreamId}:{original.StreamInfo.Version}";

            var newEvent = new MonitorActivated();
            newEvent.Timestamp = e.Timestamp;

            var expectedValue = CreateEventWrapper(newEventId, newStreamId, original.StreamInfo.Version, 
                newEvent);
            return expectedValue;
        }
        private EventWrapper Transformer(MeterDeregistered e, EventWrapper original)
        {
            var newStreamId = ReplaceMeterWithMonitor(original.StreamInfo.Id);
            var newEventId = $"{newStreamId}:{original.StreamInfo.Version}";
            var newEntityId = ReplaceMeterWithMonitor(e.MeterId);

            var newEvent = new MonitorDeregistered(newEntityId);
            newEvent.Timestamp = e.Timestamp;

            var expectedValue = CreateEventWrapper(newEventId, newStreamId, original.StreamInfo.Version,  
                newEvent);
            return expectedValue;
        }
        private EventWrapper Transformer(MeterActivationFailed e, EventWrapper original)
        {
            var newStreamId = ReplaceMeterWithMonitor(original.StreamInfo.Id);
            var newEventId = $"{newStreamId}:{original.StreamInfo.Version}";

            var newEvent = new MonitorActivationFailed(e.ActivationCode);
            newEvent.Timestamp = e.Timestamp;

            var expectedValue = CreateEventWrapper(newEventId, newStreamId, original.StreamInfo.Version,  
                newEvent);
            return expectedValue;
        }
        
        private EventWrapper Transformer(MeterReadingsCollected e, EventWrapper original)
        {
            var newStreamId = ReplaceMeterWithMonitor(original.StreamInfo.Id);
            var newEventId = $"{newStreamId}:{original.StreamInfo.Version}";

            var monitorReadings = 
                e.Readings.Select(x => new MonitorReading(x.Timestamp, x.Value));

            var newEvent = new MonitorReadingsCollected(e.Date, monitorReadings.ToArray());
            newEvent.Timestamp = e.Timestamp;

            var expectedValue = CreateEventWrapper(newEventId, newStreamId, original.StreamInfo.Version,  
                newEvent);
            return expectedValue;
        }
        
    }
}