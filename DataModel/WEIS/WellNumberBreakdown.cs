using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ECIS.Client.WEIS
{
    public class WellNumberBreakdown
    {
        public double TroubleFree { get; set; }
        public double Trouble { get; set; }
        public double Contingency { get; set; }
        public double EscalationInflation { get; set; }
        public double CSO { get; set; }
        public double OtherAmount1 { get; set; }
        public double OtherAmount2 { get; set; }
        public double RefAmount1 { get; set; }
        public double RefAmount2 { get; set; }
        public double Total
        {
            get
            {
                return TroubleFree + Trouble 
                    + Contingency 
                    + EscalationInflation 
                    + OtherAmount1 + OtherAmount2;
            }
        }
        public double BurnRate { get; set; }
    }
}