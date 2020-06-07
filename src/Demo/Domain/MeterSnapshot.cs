using System;
using Newtonsoft.Json;

namespace Demo.Domain
{
    public class MeterSnapshot
    {
        internal MeterSnapshot(string meterId, string postalCode, string houseNumber, bool isActivated,
            int failedActivationAttempts, DateTime? latestReadingDate, string activationCode)
        {
            Id = meterId;
            PostalCode = postalCode;
            HouseNumber = houseNumber;
            IsActivated = isActivated;
            FailedActivationAttempts = failedActivationAttempts;
            LatestReadingDate = latestReadingDate;
            ActivationCode = activationCode;
        }

        [JsonConstructor]
        private MeterSnapshot()
        {
        }

        [JsonProperty("id")]
        public string Id { get; private set; }

        [JsonProperty("postalCode")]
        public string PostalCode { get; private set; }

        [JsonProperty("houseNumber")]
        public string HouseNumber { get; private set; }

        [JsonProperty("isActivated")]
        public bool IsActivated { get; private set; }

        [JsonProperty("failedActivationAttempts")]
        public int FailedActivationAttempts { get; private set; }

        [JsonProperty("latestReadingDate")]
        public DateTime? LatestReadingDate { get; private set; }

        [JsonProperty("activationCode")]
        public string ActivationCode { get; private set; }
    }
}