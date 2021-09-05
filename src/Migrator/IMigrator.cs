using EventStore;

namespace Migrator
{
    public interface IMigrator
    {
        bool IsSubscribedTo(IEvent @event);
        string GetNewStreamId(string originalStreamId);
        /// <summary>
        /// Returns a new Event which was derived from the input Event.
        /// If nothing should be changed, it can return the input event.
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        IEvent Transform(IEvent @event);
    }
}