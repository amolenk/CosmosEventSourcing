using System;
using EventStore;

namespace Demo.Domain.Events
{
    public class MonitorReadingsCollected : EventBase
    {
        public DateTime Date { get;  }

        public MonitorReading[] Readings { get; }

        public MonitorReadingsCollected(DateTime date, MonitorReading[] readings)
        {
            Date = date;
            Readings = readings;
        }
    }

    public class MonitorReading
    {
        public DateTime Timestamp { get;  }

        public int Value { get; }

        public MonitorReading(DateTime timestamp, int value)
        {
            Timestamp = timestamp;
            Value = value;
        }
    }
}