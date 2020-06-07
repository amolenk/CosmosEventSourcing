using System.Threading.Tasks;

namespace EventStore
{
    public interface ISnapshotStore
    {
        Task<Snapshot> LoadSnapshotAsync(string streamId);
  
        Task SaveSnapshotAsync(string streamId, int version, object snapshot);
    }
}