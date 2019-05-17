using System;
using EventStore;

namespace ConsoleUI
{
    public class GreetedEvent : IEvent
    {
        public DateTime Timestamp { get; set; }
        
        public string Message { get; set; }

        public string Region { get; set; }
    }
}