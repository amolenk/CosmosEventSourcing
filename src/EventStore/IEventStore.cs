using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventStore
{
    public interface IEventStore
    {
        Task<EventStream> LoadStreamAsync(string id);
  
        Task<bool> AppendToStreamAsync(
            string id,
            int expectedVersion,
            IEnumerable<IEvent> events);
    }
}