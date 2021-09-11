using EventStore;

namespace CosmosValidate
{
    public interface IValidator
    {
        bool IsSubscribedTo(IEvent @event);

        EventWrapper GetExpectedValue(IEvent e, EventWrapper eventWrapper);
 
    }
}