using ECIS.Core;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ECIS.Client.WEIS
{
    public class BizPlanPeriodBucket
    {

        public double WorkingInterest { get; set; }

        public string Key { get; set; }
        public string Type { get; set; }
        public DateRange Period { get; set; }

        public double LCCost { get; set; }

        public double MeanDays { get; set; }
        public double RigCost { get; set; }
        public double MaterialCost { get; set; }
        public double ServiceCost { get; set; }
        public double TroubleFreeCost { get; set; }
        public double NPTCost { get; set; }
        public double TECOPCost { get; set; }
        public double EscCostRig { get; set; }
        public double EscCostServices { get; set; }
        public double EscCostMaterial { get; set; }
        public double EscCostTotal { get; set; }
        public double MeanCostEDM { get; set; }
        public double CSOCost { get; set; }
        public double InflationCost { get; set; }
        public double MeanCostRealTerm { get; set; }
        public double MeanCostMOD { get; set; }
        public double ShellShare { get; set; }


        public double MeanCostEDMSS { get; set; }
        public double MeanCostRealTermSS { get; set; }
        public double MeanCostMODSS { get; set; }

        public double MeanCostEDMRigRate { get; set; }

        public BizPlanPeriodBucket()
        {
            Period = new DateRange();
        }

    }
}
