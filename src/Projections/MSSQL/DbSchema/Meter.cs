using System;
using System.Collections.Generic;

#nullable disable

namespace Projections.MSSQL.DbSchema
{
    public partial class Meter
    {
        public string MeterId { get; set; }
        public string PostalCode { get; set; }
        public string HouseNumber { get; set; }
        public bool? IsActivated { get; set; }
        public int? FailedActivationAttempts { get; set; }
        public DateTime? LatestReadingDate { get; set; }
        public double LogicalCheckPointLsn { get; set; }
        public string LogicalCheckPointItemIds { get; set; }
    }
}
