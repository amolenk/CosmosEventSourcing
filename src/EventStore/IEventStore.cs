using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventStore
{
    public interface IEventStore
    {
        Task<EventStream> LoadStreamAsync(string streamId);
  
        Task<bool> AppendToStreamAsync(
            string streamId,
            int expectedVersion,
            IEnumerable<IEvent> events);
    }
}