using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Projections
{
    public class View : IView
    {
        public View()
            : this(-1, new JObject(), null)
        {
        }

        public View(long logicalSequenceNumber, JObject payload, string etag)
        {
            Payload = payload;
            LogicalSequenceNumber = logicalSequenceNumber;
            Etag = etag;
        }

        [JsonProperty("logicalSequenceNumber")]
        public long LogicalSequenceNumber { get; set; }

        [JsonProperty("payload")]
        public JObject Payload { get; set; }

        [JsonProperty("_etag")]
        public string Etag { get;set; }
    }
}