using EventStore;
using Newtonsoft.Json;

namespace Projections
{
    public class Change : EventWrapper
    {
        [JsonProperty("_lsn")]
        public long LogicalSequenceNumber { get; set; }
    }
}