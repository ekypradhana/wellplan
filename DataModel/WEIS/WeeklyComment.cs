using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using ECIS.Core;

namespace ECIS.Client.WEIS
{
    public class WeeklyComment
    {
        public string ExecutiveSummary { get; set; }
        public string OperationSummary { get; set; }
        public string PlannedOperations { get; set; }
        public double LEDays { get; set; }
        public double LECost { get; set; }
        public bool Last7DaysSupplement { get; set; }
        public string SupplementReason { get; set; }
    }
}