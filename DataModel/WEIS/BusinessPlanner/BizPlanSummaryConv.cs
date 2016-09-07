using ECIS.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS
{
    public class BizPlanSummaryConv
    {
        public string WellName { get; set; }
        public string ActivityType { get; set; }
        public string RigName { get; set; }
        public DateRange EventPeriod { get; set; }
        public string ExchangeRate { get; set; }
        public double  RateVsUSD { get; set; }

        public double MeanCostMOD { get; set; }
        public double ShellShareMOD { get; set; }
        public double MeanCostRealTerm { get; set; }
        public double RigRate { get; set; }

        public double TroubleFreeCost { get; set; }
        public double NPTCost { get; set; }
        public double TECOPCost { get; set; }
        public double MeanCostEDM { get; set; }
        public double ShellShare { get; set; }
        public double SpreadRate { get; set; }
        public double SpreadRateTotal { get; set; }
        public double BurnRate { get; set; }
        public double MeanTime { get; set; }
        
        
            
        
    }
}
