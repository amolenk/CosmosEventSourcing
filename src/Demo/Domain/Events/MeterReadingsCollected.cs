using System;
using EventStore;

namespace Demo.Domain.Events
{
    public class MeterReadingsCollected : EventBase
    {
        public DateTime Date { get; set; }

        public MeterReading[] Readings { get; set; }
    }

    public class MeterReading
    {
        public DateTime Timestamp { get; set; }

        public int Value { get; set; }
    }
}