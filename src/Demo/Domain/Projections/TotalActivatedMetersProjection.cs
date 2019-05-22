using Demo.Domain.Events;
using Projections;

namespace ConsoleUI
{
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