using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using ECIS.Core;
using ECIS.Biz.Common;

using Newtonsoft.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization.Attributes;

namespace ECIS.Client.WEIS
{
    public class PlanValue
    {
        public int DateId { get; set; }
        public DateRange DateRange { get; set; }
        public double Days { get; set; }
        public double Cost { get; set; }
        public string Title { get; set; }
        public string PlanId { get; set; }
    }
    public class PlanSimulationBucket : ECISModel
    {
        

        public override string TableName
        {
            get { return  "WEISPlanSimulationBuckets"; }
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            this._id = String.Format("W{0}E{1}R{2}S{3}Seq{4}", WellName.Replace(" ", ""), ActivityType.Replace(" ", ""), RigName.Replace(" ", ""), SimulationId.ToString(), UARigSequenceId);
            return this.ToBsonDocument();
        }


        [BsonIgnore]
        public int Counter { get; set; }

        public string SimulationId { get; set; }
        public string WellName { get; set; }
        public string RigName { get; set; }
        public string LevelOfEstimate { get; set; }
        public string ExType { get; set; }
        public string ActivityType { get; set; }
        public bool isNewWell { get; set; }

        public string UARigSequenceId { get; set; }

        private DateRange _OPSchedule, _LESchedule, _PhSchedule, _SimSchedule;
        public DateRange OPSchedule
        {
            get
            {
                if (_OPSchedule == null) _OPSchedule = new DateRange();
                return _OPSchedule;
            }
            set { _OPSchedule = value; }
        }
        public DateRange LESchedule
        {
            get
            {
                if (_LESchedule == null) _LESchedule = new DateRange();
                return _LESchedule;
            }
            set { _LESchedule = value; }
        }
        public DateRange LSSchedule
        {
            get
            {
                if (_PhSchedule == null) _PhSchedule = new DateRange();
                return _PhSchedule;
            }

            set
            {
                _PhSchedule = value;
            }
        }
        public DateRange SimSchedule
        {
            get
            {
                if (_SimSchedule == null) _SimSchedule = new DateRange();
                return _SimSchedule;
            }

            set
            {
                _SimSchedule = value;
            }
        }


        private WellDrillData _ls, _le, _op, _sim;
        public WellDrillData LS
        {
            get
            {
                if (_ls == null) _ls = new WellDrillData();
                return _ls;
            }
            set
            {
                _ls = value;
            }
        }
        public WellDrillData OP
        {
            get
            {
                if (_op == null) _op = new WellDrillData();
                return _op;
            }
            set
            {
                _op = value;
            }
        }
        public WellDrillData LE
        {
            get
            {
                if (_le == null) _le = new WellDrillData();
                return _le;
            }
            set
            {
                _le = value;
            }
        }
        public WellDrillData Sim
        {
            get
            {
                if (_sim == null) _sim = new WellDrillData();
                return _sim;
            }
            set
            {
                _sim = value;
            }
        }

        public List<PlanValue> Values { get; set; }
        public List<PlanValue> SimulationValues { get; set; }

        public PlanSimulationBucket()
        {
            Values = new List<PlanValue>();
            SimulationValues = new List<PlanValue>();
        }

        public void GenerateBucket(BsonDocument planSimulationUnwind)
        {
            OPSchedule = BsonHelper.Deserialize<DateRange>(planSimulationUnwind.GetDoc("CalcOPSchedule"));
            LESchedule = BsonHelper.Deserialize<DateRange>(planSimulationUnwind.GetDoc("LESchedule"));
            LSSchedule = BsonHelper.Deserialize<DateRange>(planSimulationUnwind.GetDoc("PlanSchedule"));
            SimulationId = planSimulationUnwind.GetString("SimulationId");
            WellName = planSimulationUnwind.GetString("WellName");
            RigName = planSimulationUnwind.GetString("RigName");
            ActivityType = planSimulationUnwind.GetString("ActivityType");
            UARigSequenceId = planSimulationUnwind.GetString("UARigSequenceId");
            OP = BsonHelper.Deserialize<WellDrillData>(planSimulationUnwind.GetDoc("CalcOP"));
            LE = BsonHelper.Deserialize<WellDrillData>(planSimulationUnwind.GetDoc("LE"));
            LS = BsonHelper.Deserialize<WellDrillData>(planSimulationUnwind.GetDoc("Plan"));

            PlanSimulationHeader header = PlanSimulationHeader.Get<PlanSimulationHeader>(Query.EQ("_id", SimulationId));
            string copyFrom = "";
            if(header != null)
            {
                copyFrom = header.CopyFrom.Contains("OP") ? "OP" : header.CopyFrom.Contains("LE") ? "LE" : header.CopyFrom.Contains("LS") ? "LS" : "";
            }

            if(OPSchedule.Start.Year != 1900 && OPSchedule.Finish.Year != 1900)
            {
                var fbp = DateRangeToMonth.NumDaysPerYear2(OPSchedule);
                foreach (var y in fbp)
                {
                    PlanValue vls = new PlanValue();

                    vls.DateId = y.Key;
                    vls.DateRange = y.Value.FirstOrDefault().Key;
                    vls.Days = y.Value.FirstOrDefault().Value;
                    vls.Cost = Tools.Div(vls.Days, OPSchedule.Days + 1) * OP.Cost;
                    vls.Title = "OP";
                    Values.Add(vls);
                }
                if(copyFrom.Equals("OP"))
                {
                    Sim = OP;
                    SimSchedule = OPSchedule;
                    SimulationValues = Values.Where(x => x.Title.Equals("OP")).ToList();
                }
            }

            if (LESchedule.Start.Year != 1900 && LESchedule.Finish.Year != 1900)
            {
                var fbp = DateRangeToMonth.NumDaysPerYear2(LESchedule);
                foreach (var y in fbp)
                {
                    PlanValue vls = new PlanValue();

                    vls.DateId = y.Key;
                    vls.DateRange = y.Value.FirstOrDefault().Key;
                    vls.Days = y.Value.FirstOrDefault().Value;
                    vls.Cost = Tools.Div(vls.Days, LESchedule.Days + 1) * LE.Cost;
                    vls.Title = "LE";
                    Values.Add(vls);
                }
                if (copyFrom.Equals("LE"))
                {
                    Sim = LE;
                    SimSchedule = LESchedule;
                    SimulationValues = Values.Where(x => x.Title.Equals("LE")).ToList();

                }
            }

            if (LSSchedule.Start.Year != 1900 && LSSchedule.Finish.Year != 1900)
            {
                var fbp = DateRangeToMonth.NumDaysPerYear2(LSSchedule);
                foreach (var y in fbp)
                {
                    PlanValue vls = new PlanValue();

                    vls.DateId = y.Key;
                    vls.DateRange = y.Value.FirstOrDefault().Key;
                    vls.Days = y.Value.FirstOrDefault().Value;
                    vls.Cost = Tools.Div(vls.Days, LSSchedule.Days + 1) * LS.Cost;
                    vls.Title = "LS";
                    Values.Add(vls);
                }
                if (copyFrom.Equals("LS"))
                {
                    Sim = LS;
                    SimSchedule = LSSchedule;
                    SimulationValues = Values.Where(x => x.Title.Equals("LS")).ToList();

                }
            }
            
           
        }

        public void GenerateBucket(WellActivityPhase phase, string simulationId, string wellName,  string rigName, string sequenceId)
        {
            OPSchedule = phase.CalcOPSchedule;
            LESchedule = phase.LESchedule; //BsonHelper.Deserialize<DateRange>(planSimulationUnwind.GetDoc("LESchedule"));
            LSSchedule = phase.PhSchedule; //BsonHelper.Deserialize<DateRange>(planSimulationUnwind.GetDoc("PlanSchedule"));
            SimulationId = simulationId; // planSimulationUnwind.GetString("SimulationId");
            WellName = wellName;// planSimulationUnwind.GetString("WellName");
            RigName = rigName; //planSimulationUnwind.GetString("RigName");
            ActivityType = phase.ActivityType; // planSimulationUnwind.GetString("ActivityType");
            UARigSequenceId = sequenceId;
            OP = phase.CalcOP; //BsonHelper.Deserialize<WellDrillData>(planSimulationUnwind.GetDoc("CalcOP"));
            LE = phase.LE; // // BsonHelper.Deserialize<WellDrillData>(planSimulationUnwind.GetDoc("LE"));
            LS = phase.Plan;// BsonHelper.Deserialize<WellDrillData>(planSimulationUnwind.GetDoc("Plan"));

            PlanSimulationHeader header = PlanSimulationHeader.Get<PlanSimulationHeader>(Query.EQ("_id", SimulationId));
            string copyFrom = "";
            if (header != null)
            {
                copyFrom = header.CopyFrom.Contains("OP") ? "OP" : header.CopyFrom.Contains("LE") ? "LE" : header.CopyFrom.Contains("LS") ? "LS" : "";
            }

            if (OPSchedule.Start.Year != 1900 && OPSchedule.Finish.Year != 1900)
            {
                var fbp = DateRangeToMonth.NumDaysPerYear2(OPSchedule);
                foreach (var y in fbp)
                {
                    PlanValue vls = new PlanValue();

                    vls.DateId = y.Key;
                    vls.DateRange = y.Value.FirstOrDefault().Key;
                    vls.Days = y.Value.FirstOrDefault().Value;
                    vls.Cost = Tools.Div(vls.Days, OPSchedule.Days + 1) * OP.Cost;
                    vls.Title = "OP";
                    Values.Add(vls);
                }
                if (copyFrom.Equals("OP"))
                {
                    Sim = OP;
                    SimSchedule = OPSchedule;
                    SimulationValues = Values.Where(x => x.Title.Equals("OP")).ToList();
                }
            }

            if (LESchedule.Start.Year != 1900 && LESchedule.Finish.Year != 1900)
            {
                var fbp = DateRangeToMonth.NumDaysPerYear2(LESchedule);
                foreach (var y in fbp)
                {
                    PlanValue vls = new PlanValue();

                    vls.DateId = y.Key;
                    vls.DateRange = y.Value.FirstOrDefault().Key;
                    vls.Days = y.Value.FirstOrDefault().Value;
                    vls.Cost = Tools.Div(vls.Days, LESchedule.Days + 1) * LE.Cost;
                    vls.Title = "LE";
                    Values.Add(vls);
                }
                if (copyFrom.Equals("LE"))
                {
                    Sim = LE;
                    SimSchedule = LESchedule;
                    SimulationValues = Values.Where(x => x.Title.Equals("LE")).ToList();

                }
            }

            if (LSSchedule.Start.Year != 1900 && LSSchedule.Finish.Year != 1900)
            {
                var fbp = DateRangeToMonth.NumDaysPerYear2(LSSchedule);
                foreach (var y in fbp)
                {
                    PlanValue vls = new PlanValue();

                    vls.DateId = y.Key;
                    vls.DateRange = y.Value.FirstOrDefault().Key;
                    vls.Days = y.Value.FirstOrDefault().Value;
                    vls.Cost = Tools.Div(vls.Days, LSSchedule.Days + 1) * LS.Cost;
                    vls.Title = "LS";
                    Values.Add(vls);
                }
                if (copyFrom.Equals("LS"))
                {
                    Sim = LS;
                    SimSchedule = LSSchedule;
                    SimulationValues = Values.Where(x => x.Title.Equals("LS")).ToList();

                }
            }


        }

     
    }
}
