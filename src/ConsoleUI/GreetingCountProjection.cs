using Projections;

namespace ConsoleUI
{
    public class GreetingCountProjection : Projection<GreetingCountView>
    {
        public GreetingCountProjection()
        {
            RegisterHandler<GreetedEvent>(WhenGreeted);
        }

        private void WhenGreeted(GreetedEvent greeted, GreetingCountView view)
        {
            view.Count += 1;
        }
    }
}