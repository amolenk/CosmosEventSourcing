
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
           // RegisterTransformer<MeterRegistered>();
            
           RegisterTransformer<MeterRegistered>(Transform);
           RegisterTransformer<MeterActivated>(Transform);
           RegisterTransformer<MeterDeregistered>(Transform);
           RegisterTransformer<MeterActivationFailed>(Transform);
           RegisterTransformer<MeterReadingsCollected>(Transform);
            
            
        }

        private EventWrapper DefaultTransform(IEvent e, EventWrapper original)
        {
            var expectedStreamId = GetNewEventId(original.StreamInfo.Id);
            var expectedValue =this.GetEventWrapper(expectedStreamId, original.StreamInfo.Version, e);
            return expectedValue;
        }
        public override string GetNewEventId(string originalStreamId)
        {
            return originalStreamId.Replace("meter", "monitor");
        }
        private EventWrapper Transform(MeterRegistered e, EventWrapper original)
        {
            return DefaultTransform(e, original);
        }

        private EventWrapper Transform(MeterActivated e, EventWrapper original)
        {
            return DefaultTransform(e, original);
        }

        private EventWrapper Transform(MeterDeregistered e, EventWrapper original)
        {
            return DefaultTransform(e, original);
        }

        private EventWrapper Transform(MeterActivationFailed e, EventWrapper original)
        {
            return DefaultTransform(e, original);
        }

        private EventWrapper Transform(MeterReadingsCollected e, EventWrapper original)
        {
            return DefaultTransform(e, original);
        }
       
    }
}