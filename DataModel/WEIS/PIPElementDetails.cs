
using ECIS.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS
{
    public class PIPElementDetails
    {
        public string _id { get; set; }
        public string SequenceId { get; set; }
        public int PhaseNo { get; set; }
        public string ActivityType { get; set; }
        public string WellName { get; set; }
        public string Title { get; set; }
        public string Completion { get; set; }
        public string Classification { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodFinish { get; set; }
        public List<string> AssignTOOps { get; set; }
        public double DaysPlanImprovement { get; set; }
        public double DaysPlanRisk { get; set; }
        public double DaysActualImprovement { get; set; }
        public double DaysActualRisk { get; set; }
        public double DaysLastWeekImprovement { get; set; }
        public double DaysLastWeekRisk { get; set; }
        public double DaysCurrentWeekImprovement { get; set; }
        public double DaysCurrentWeekRisk { get; set; }
        public double CostPlanImprovement { get; set; }
        public double CostPlanRisk { get; set; }
        public double CostActualImprovement { get; set; }
        public double CostActualRisk { get; set; }
        public double CostCurrentWeekImprovement { get; set; }
        public double CostCurrentWeekRisk { get; set; }
        public double CostLastWeekImprovement { get; set; }
        public double CostLastWeekRisk { get; set; }

           public double LEDays { get; set; }
        public double LECost { get; set; }
         

        public  PIPElementDetails()
        {
            AssignTOOps = new List<string>();
        }


    }
}
