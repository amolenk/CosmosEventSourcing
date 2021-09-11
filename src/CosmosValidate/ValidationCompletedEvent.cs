using System;
using EventStore;

namespace CosmosValidate
{

        public class ValidationCompletedEvent : IEvent
        {
            public DateTime Timestamp { get; set; } =  DateTime.UtcNow;
            public string OriginalId { get; set; }
            public string ComparableId { get; set; }
            public string OriginalEventData { get; set; }
            public string ComparableEventData { get; set; }
            public string OriginalEventType { get; set; }
            public string ComparableEventType { get; set; }
            
        }
 
    }
