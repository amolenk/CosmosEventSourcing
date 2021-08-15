using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Projections.Cosmos
{
    public class CosmosView : IView
    {
        public CosmosView()
            : this(new ViewCheckpoint(), new JObject(), null)
        {
        }

        public CosmosView(ViewCheckpoint logicalCheckpoint, JObject payload, string etag)
        {
            Payload = payload;
            LogicalCheckpoint = logicalCheckpoint;
            Etag = etag;
        }

        [JsonProperty("logicalCheckpoint")]
        public ViewCheckpoint LogicalCheckpoint { get; set; }
        
        [JsonProperty("payload")]
        public JObject Payload { get; set; }

        [JsonProperty("_etag")]
        public string Etag { get;set; }

        public bool IsNew { get; set; }

        public bool IsNewerThanCheckpoint(Change change)
        {
            if (change.LogicalSequenceNumber == LogicalCheckpoint.LogicalSequenceNumber)
            {
                return !LogicalCheckpoint.ItemIds.Contains(change.Id);
            }

            return change.LogicalSequenceNumber > LogicalCheckpoint.LogicalSequenceNumber;
        }

        public void UpdateCheckpoint(Change change)
        {
            if (change.LogicalSequenceNumber != LogicalCheckpoint.LogicalSequenceNumber)
            {
                LogicalCheckpoint.LogicalSequenceNumber = change.LogicalSequenceNumber;
                LogicalCheckpoint.ItemIds.Clear();
            }

            LogicalCheckpoint.ItemIds.Add(change.Id);
        }
    }
}