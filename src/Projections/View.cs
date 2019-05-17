using System;
using System.Collections.Generic;
using EventStore;
using Microsoft.Azure.Documents;
using Newtonsoft.Json.Linq;

namespace Projections
{
    public class View : IView
    {
        public View()
            : this(new Dictionary<string, ViewPartitionCheckpoint>(), new JObject(), null)
        {
        }

        public View(Dictionary<string, ViewPartitionCheckpoint> partitionCheckpoints, JObject payload, string etag)
        {
            Payload = payload;
            PartitionCheckpoints = partitionCheckpoints;
            Etag = etag;
        }

        public Dictionary<string, ViewPartitionCheckpoint> PartitionCheckpoints { get; }

        public JObject Payload { get; set; }

        public string Etag { get;set; }

        public bool IsNewerThanCheckpoint(string partitionKeyRangeId, Document document)
        {
            if (PartitionCheckpoints.ContainsKey(partitionKeyRangeId))
            {
                var checkpoint = PartitionCheckpoints[partitionKeyRangeId];

                if (document.Timestamp == checkpoint.Timestamp)
                {
                    return !checkpoint.DocumentIds.Contains(document.Id);
                }

                return document.Timestamp > checkpoint.Timestamp;
            }

            return true;
        }

        public void UpdateCheckpoint(string partitionKeyRangeId, Document document)
        {
            if (!PartitionCheckpoints.ContainsKey(partitionKeyRangeId))
            {
                PartitionCheckpoints.Add(partitionKeyRangeId, new ViewPartitionCheckpoint());   
            }

            var checkpoint = PartitionCheckpoints[partitionKeyRangeId];

            if (document.Timestamp != checkpoint.Timestamp)
            {
                checkpoint.Timestamp = document.Timestamp;
                checkpoint.DocumentIds.Clear();
            }

            checkpoint.DocumentIds.Add(document.Id);
        }
    }
}