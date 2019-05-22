using System.Linq;
using System.Threading.Tasks;
using Demo.Domain;
using EventStore;

namespace Demo
{
    public class MeterRepository : IMeterRepository
    {
        private readonly IEventStore _eventStore;

        public MeterRepository(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task<Meter> LoadMeterAsync(string id)
        {
            var streamId = $"meter:{id}";

            var stream = await _eventStore.LoadStreamAsync(streamId);
        
            return new Meter(stream.Events);
        }

        public async Task<bool> SaveMeterAsync(Meter meter)
        {
            if (meter.Changes.Any())
            {
                var streamId = $"meter:{meter.MeterId}";

                return await _eventStore.AppendToStreamAsync(
                    streamId,
                    meter.Version,
                    meter.Changes);
            }

            return true;
        }
    }
}