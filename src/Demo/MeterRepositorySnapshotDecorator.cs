using System.Threading.Tasks;
using Demo.Domain;
using EventStore;

namespace Demo
{
    public class MeterRepositorySnapshotDecorator : IMeterRepository
    {
        private readonly IEventStore _eventStore;
        private readonly ISnapshotStore _snapshotStore;
        private readonly IMeterRepository _innerMeterRepository;

        public MeterRepositorySnapshotDecorator(IEventStore eventStore, ISnapshotStore snapshotStore,
            IMeterRepository innerMeterRepository)
        {
            _eventStore = eventStore;
            _snapshotStore = snapshotStore;
            _innerMeterRepository = innerMeterRepository;
        }

        public async Task<Meter> LoadMeterAsync(string id)
        {
            var streamId = $"meter:{id}";

            var snapshot = await _snapshotStore.LoadSnapshotAsync(streamId);
            if (snapshot != null)
            {
                var streamTail = await _eventStore.LoadStreamAsync(streamId, snapshot.Version + 1);

                return new Meter(
                    snapshot.SnapshotData.ToObject<MeterSnapshot>(),
                    snapshot.Version,
                    streamTail.Events);
            }
            else
            {
                return await _innerMeterRepository.LoadMeterAsync(id);
            }
        }

        public Task<bool> SaveMeterAsync(Meter meter)
        {
            return _innerMeterRepository.SaveMeterAsync(meter);
        }
    }
}