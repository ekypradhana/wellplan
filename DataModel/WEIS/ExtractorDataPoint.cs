using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS
{
    public enum WAUPhaseDataPoint
    {
        SumOfTQ,	
	    AvgOfTQ,
	    SumOfAFE,	
	    AvgOfAFE,
	    SumOfOP,
	    AvgOfOP,
	    SumOfLE,	
	    AvgOfLE
    }
    public enum WAUElementDataPoint
    {
        SumOfDaysPlanImprovement,	
	    AvgOfDaysPlanImprovement,
	    SumOfDaysPlanRisk,	
	    AvgOfDaysPlanRisk,
	    SumOfDaysActualImprovement,	
	    AvgOfDaysActualImprovement,
	    SumOfDaysActualRisk,	
	    AvgOfDaysActualRisk,
	    SumOfDaysLastWeekImprovement,	
	    AvgOfDaysLastWeekImprovement,
	    SumOfDaysLastWeekRisk,	
	    AvgOfDaysLastWeekRisk,
	    SumOfDaysCurrentWeekImprovement,	
	    AvgOfDaysCurrentWeekImprovement,
	    SumOfDaysCurrentWeekRisk,	
	    AvgOfDaysCurrentWeekRisk,
	    SumOfCostPlanImprovement,	
	    AvgOfCostPlanImprovement,
	    SumOfCostPlanRisk,	
	    AvgOfCostPlanRisk,
	    SumOfCostActualImprovement,	
	    AvgOfCostActualImprovement,
	    SumOfCostActualRisk,	
	    AvgOfCostActualRisk,
	    SumOfCostCurrentWeekImprovement,	
	    AvgOfCostCurrentWeekImprovement,
	    SumOfCostCurrentWeekRisk,	
	    AvgOfCostCurrentWeekRisk,
	    SumOfCostLastWeekImprovement,	
	    AvgOfCostLastWeekImprovement,
	    SumOfCostLastWeekRisk,	
	    AvgOfCostLastWeekRisk
    }
    public enum WAUElementAllocationDataPoint
    {
        SumOfDaysPlanImprovement,	
	    AvgOfDaysPlanImprovement,
	    SumOfDaysPlanRisk,	
	    AvgOfDaysPlanRisk,
	    SumOfCostPlanImprovement,	
	    AvgOfCostPlanImprovement,
	    SumOfCostPlanRisk,	
	    AvgOfCostPlanRisk,
	    SumOfLEDays,	
	    AvgOfLEDays,
	    SumOfLECost,
	    AvgOfLECost
    }
}
