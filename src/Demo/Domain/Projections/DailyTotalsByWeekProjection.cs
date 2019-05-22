using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Demo.Domain.Events;
using EventStore;
using Projections;

namespace ConsoleUI
{
    public class DailyTotalsByWeek
    {
        public Dictionary<DateTime, int> Consumption { get; } = new Dictionary<DateTime, int>();
    }

    public class DailyTotalsByWeekProjection : Projection<DailyTotalsByWeek>
    {
        public DailyTotalsByWeekProjection()
        {
            RegisterHandler<MeterReadingsCollected>(WhenReadingsCollected);
        }

        public override string GetViewName(string streamId, IEvent @event)
        {
            var meterId = streamId.Substring(streamId.IndexOf(':') + 1);
            var weekNumber = GetIso8601WeekOfYear(((MeterReadingsCollected)@event).Date);

            return $"DailyTotalsByWeek:{meterId}:wk{weekNumber:00}";
        }

        private void WhenReadingsCollected(MeterReadingsCollected meterReadingsCollected, DailyTotalsByWeek view)
        {
            view.Consumption[meterReadingsCollected.Date] = meterReadingsCollected.Readings.Sum(r => r.Value);
        }

        // This presumes that weeks start with Monday.
        // Week 1 is the 1st week of the year with a Thursday in it.
        public static int GetIso8601WeekOfYear(DateTime time)
        {
            // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
            // be the same week# as whatever Thursday, Friday or Saturday are,
            // and we always get those right
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            // Return the week of our adjusted day
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}