using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Projections.MSSQL
{
    public class MSSQLView : IView
    {
        public MSSQLView()
            : this(new ViewCheckpoint(), new JObject())
        {
            IsNew = true;
        }

        public MSSQLView(ViewCheckpoint logicalCheckpoint, JObject payload)
        {
            Payload = payload;
            LogicalCheckpoint = logicalCheckpoint;
            IsNew = false;
        }

        [JsonProperty("logicalCheckpoint")] public ViewCheckpoint LogicalCheckpoint { get; set; }

        [JsonProperty("payload")] public JObject Payload { get; set; }

        public bool IsNew { get; }
     
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