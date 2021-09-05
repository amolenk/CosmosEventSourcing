using EventStore;
using Newtonsoft.Json;

namespace Migrator
{
    public class Change : EventWrapper
    {
        [JsonProperty("_lsn")]
        public long LogicalSequenceNumber { get; set; }
    }
}