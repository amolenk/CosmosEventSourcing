using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventStore
{
    public interface IEventStore
    {
        Task<EventStream> LoadStreamAsync(string streamId);

        Task<EventStream> LoadStreamAsync(string streamId, int fromVersion);
  
        Task<bool> AppendToStreamAsync(
            string streamId,
            int expectedVersion,
            IEnumerable<IEvent> events);
    }
}