using EventStore;

namespace CosmosValidate
{
    public interface IValidator
    {
        bool IsSubscribedTo(IEvent @event);
        string GetNewEventId(string originalId);
     
        EventWrapper Transform(EventWrapper originalEvent);

       // bool Equals(EventWrapper original, EventWrapper duplicate);
       // bool Validate(IEvent e, EventWrapper originalEvent, EventWrapper newEvent);
    }
}