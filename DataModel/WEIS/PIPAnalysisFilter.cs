using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECIS.Core;
using ECIS.Client.WEIS;
namespace ECIS.Client.WEIS
{
    public class PIPAnalysisFilter
    {
        public string TakeDataFor { get; set; }// total || risk || opportunity
        public string PIPType { get; set; }// rig pip scm || well pip
        public List<string> ProjectNames { set; get; }
        public List<string> Regions { set; get; }
        public List<string> RigNames { set; get; }
        public List<string> WellNames { set; get; }
        public List<string> Activities { set; get; }
        public List<string> ActivitiesCategories { set; get; }
        public List<string> Classifications { set; get; }
        public List<string> OPs { set; get; }
        public List<string> ExType { set; get; }
        public string OPRelation { set; get; }

        public DateTime PeriodStart { set; get; }
        public DateTime PeriodFinish { set; get; }

        public string GroupBy { get; set; }
        public string DataPoint { get; set; }


        public PIPAnalysisFilter()
        {
            Regions = new List<string>();
            RigNames = new List<string>();
            WellNames = new List<string>();
            Activities = new List<string>();
            ActivitiesCategories = new List<string>();
            Classifications = new List<string>();
            OPs = new List<string>();
            ExType = new List<string>();

            PeriodStart = new DateTime();
            PeriodFinish = new DateTime();
            GroupBy = "";
            DataPoint = "";
        }
    }

    public class PIPAnalysis
    {
        public string Type { get; set; }
        public bool isRealized { get; set; }
        public string Title { get; set; }
        public double LECost { get; set; }
        public double LEDays { get; set; }
        public double LECostPerDay { get; set; }
        public double PlanCost { get; set; }
        public double PlanDays { get; set; }
        public double PlanCostPerDay { get; set; }

    }
    public class ScatterValue
    {
        public double ValueX { get; set; }
        public double ValueLECost { get; set; }
        public double ValueLEDays { get; set; }
        public double ValueCostPerDays { get; set; }
    }
    public class PIPAnalysisScatter
    {
        public string Status { get; set; }
        public ScatterValue Value { get; set; }
        public DateRange Period { get; set; }
        public string WellName { get; set; }
        public string Title { get; set; }
        public string RigName { get; set; }
        public string ActivityType { get; set; }
        public string Completion { get; set; }

        public PIPAnalysisScatter()
        {
            Value = new ScatterValue();
            Period = new DateRange();
        }

    }
}
