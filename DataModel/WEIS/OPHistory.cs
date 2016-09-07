using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using ECIS.Core;
using ECIS.Biz.Common;

using Newtonsoft.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization.Attributes;


namespace ECIS.Client.WEIS
{
    [BsonIgnoreExtraElements]
    public class OPHistory
    {
        // OP14, OP15, OP16
        public string Type { get; set; }
        public string WellName { get; set; }
        public string ActivityType { get; set; }
        public string UARigSequenceId { get; set; }
        public string RigName { get; set; }
        public int PhaseNo { get; set; }

        public WellDrillData CalcOP { get; set; }
        public DateRange CalcOPSchedule { get; set; }

        public WellDrillData LE { get; set; }
        public DateRange LESchedule { get; set; }

        public WellDrillData LWE { get; set; }
        public DateRange LWESchedule { get; set; }

        public WellDrillData LME { get; set; }
        public DateRange LMESchedule { get; set; }

        // OP
        public WellDrillData Plan { get; set; }
        public DateRange PlanSchedule { get; set; }

        // LS
        public WellDrillData OP { get; set; }
        public DateRange PhSchedule { get; set; }

        public WellDrillData AFE { get; set; }
        public WellDrillData TQ { get; set; }
        public WellDrillData Target { get; set; }
        public WellDrillData Actual { get; set; }
        public WellDrillData BIC { get; set; }

        public OPHistory()
        {
            Plan = new WellDrillData();
            PlanSchedule = new DateRange();
            CalcOP = new WellDrillData();
            CalcOPSchedule = new DateRange();
            OP = new WellDrillData();
            PhSchedule = new DateRange();
            LE = new WellDrillData();
            LESchedule = new DateRange();
            LWE = new WellDrillData();
            LWESchedule = new DateRange();
            LME = new WellDrillData();
            LMESchedule = new DateRange();
            AFE = new WellDrillData();
            TQ = new WellDrillData();
            Actual = new WellDrillData();
            Target = new WellDrillData();
            BIC = new WellDrillData();
        }
    }
}
