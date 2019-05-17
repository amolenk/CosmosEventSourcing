using EventStore;
using Projections;

namespace ConsoleUI
{
    public class GreetingCountByRegionProjection : Projection<GreetingCountView>
    {
        public GreetingCountByRegionProjection()
        {
            RegisterHandler<GreetedEvent>(WhenGreeted);
        }

        public override string GetViewName(IEvent @event)
        {
            return $"Greetings-{((GreetedEvent)@event).Region}";
        }

        private void WhenGreeted(GreetedEvent greeted, GreetingCountView view)
        {
            view.Count += 1;
        }
    }
}