using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EventStore
{
    public class Snapshot
    {
        [JsonProperty("id")]
        public string StreamId { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("snapshotData")]
        public JObject SnapshotData { get; set; }
    }
}