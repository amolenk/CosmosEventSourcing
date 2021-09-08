using EventStore;
using Newtonsoft.Json;

namespace CosmosValidate
{
    public class Change : EventWrapper
    {
        [JsonProperty("_lsn")]
        public long LogicalSequenceNumber { get; set; }
    }
}