using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ECIS.Core;
namespace ECIS.Client.WEIS
{
    public class SequenceParamRaw
    {
        public string rigTypes { set; get; }
        public string rigNames { set; get; }
        public string wellNames { set; get; }
        public int historicalData { set; get; }
        public int isCalendarModeSameMode { set; get; }

        public SequenceParamRaw()
        {
            rigTypes = "";
            rigNames = "";
            wellNames = "";
            historicalData = 3;
            isCalendarModeSameMode = 0;
        }
    }

    public class SequenceParam
    {
        public List<string> rigTypes { set; get; }
        public List<string> rigNames { set; get; }
        public List<string> wellNames { set; get; }
        public int historicalData { set; get; }
        public bool isCalendarModeSameMode { set; get; }
    }

    public class SequenceData
    {
        public string RigName { set; get; }
        public List<WellActivity> Activities { set; get; }
        public List<WellActivityUpdate> ActivitiySequences { set; get; }
        public DateRange DateRange { set; get; }
    }

    public class SequenceMargin
    {
        public int Left { set; get; }
        public int Width { set; get; }
    }

    public class SequenceSaveParam
    {
        public string ActivityType { set; get; }
        public DateTime? AfterDateFinish { set; get; }
        public DateTime? AfterDateStart { set; get; }
        public DateTime? OriginalDateFinish { set; get; }
        public DateTime? OriginalDateStart { set; get; }
        public int PhaseNo { set; get; }
        public string Rig { set; get; }
        public string UARigSequenceId { set; get; }
        public string Well { set; get; }
        public string ToRig { set; get; }
    }
}
