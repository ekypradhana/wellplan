using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ECIS.Client.WEIS
{
    public class WellDrillData
    {
        public string Identifier { get; set; }
        public double Days { get; set; }
        public double Cost { get; set; }
        public string Comment { get; set; }
    }

    public class WellDrillDataPercent : WellDrillData
    {
        public double PercentTime { get; set; }
        public double PercentCost { get; set; }
    }
}