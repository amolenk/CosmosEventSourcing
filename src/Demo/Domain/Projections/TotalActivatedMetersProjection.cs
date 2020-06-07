using Demo.Domain.Events;
using Projections;

namespace Demo.Domain.Projections
{
    public class TotalActivatedMeters
    {
        public int Count { get; set; } = 0;
    }

    public class TotalActivatedMetersProjection : Projection<TotalActivatedMeters>
    {
        public TotalActivatedMetersProjection()
        {
            RegisterHandler<MeterActivated>(WhenActivated);
            RegisterHandler<MeterDeregistered>(WhenDeregistered);
        }

        private void WhenActivated(MeterActivated meterActivated, TotalActivatedMeters view)
        {
            view.Count += 1;
        }

        private void WhenDeregistered(MeterDeregistered meterDeregistered, TotalActivatedMeters view)
        {
            view.Count -= 1;
        }
    }
}