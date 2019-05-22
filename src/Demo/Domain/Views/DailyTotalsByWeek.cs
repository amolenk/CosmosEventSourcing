using System;
using System.Collections.Generic;
using Projections;

namespace ConsoleUI
{
    public class DailyTotalsByWeek
    {
        public Dictionary<DateTime, int> Consumption { get; } = new Dictionary<DateTime, int>();
    }
}