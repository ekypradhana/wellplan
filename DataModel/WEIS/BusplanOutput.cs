using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS
{
    public class BusplanOutput
    {
        public int _id { get; set; }
        public string Status { get; set; }
        public string Region { get; set; }
        public string OperatingUnit { get; set; }
        public string UARigSequenceId { get; set; }
        public string RigType { get; set; }
        public string RigName { get; set; }
        public string ProjectName { get; set; }
        public string WellName { get; set; }
        public int PhaseNo { get; set; }
        public string ActivityType { get; set; }
        public string AssetName { get; set; }
        public string NonOP { get; set; }
        public double WorkingInterest { get; set; }
        public string FirmOrOption { get; set; }
        public double PsDuration { get; set; }

        public string SaveToOP { get; set; }

        public double PlanDuration { get; set; }
        public double PlanCost { get; set; }

        public double OpsCost { get; set; }
        public DateTime OpsStart { get; set; }
        public DateTime OpsFinish { get; set; }
        public double OpsDuration { get; set; }

        public double PhDuration { get; set; }
        public string UARigDescription { get; set; }
        public double PhRiskDuration { get; set; }
        public DateTime PhStartForFilter { get; set; }
        public DateTime PhFinishForFilter { get; set; }
        public DateTime PsStartForFilter { get; set; }
        public DateTime PsFinishForFilter { get; set; }


        public DateTime PsStart { get; set; }
        public DateTime PsFinish { get; set; }

        public DateTime PhCost { get; set; }
        public DateTime AFEStart { get; set; }
        public DateTime AFEFinish { get; set; }
        public double AFEDuration { get; set; }
        public double AFECost { get; set; }
        public DateTime LEStart { get; set; }
        public DateTime LEFinish { get; set; }
        public double LEDuration { get; set; }
        public double LECost { get; set; }
        public bool VirtualPhase { get; set; }
        public bool ShiftFutureEventDate { get; set; }
        public double ShellShare { get; set; }
        public string LSInfo { get; set; }
        public List<object> OPEstimate { get; set; }

        public OPEstimates PrevOPEst { get; set; }
        public OPEstimates NextOPEst { get; set; }

        public BusplanOutput()
        {
            PrevOPEst = new OPEstimates();
            NextOPEst = new OPEstimates();
        }
    }


}
