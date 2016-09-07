
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ECIS.Client.WEIS
{
    public class BizPlanActivityExpense : ECISModel
    {
        public BizPlanActivityExpense()
        {
            FYExpenseProfiles = new List<FYExpenseProfile>();
        }
        public override string TableName
        {
            get { return "WEISBizPlanActivityExpense"; }
        }

        public string RigName { get; set; }
        public string WellName { get; set; }
        public string SequenceId { get; set; }
        private DateRange _LESchedule { get; set; }
        public DateRange LESchedule
        {
            get {
                if (_LESchedule == null) _LESchedule = new DateRange();
                return _LESchedule; 
            }
            set { _LESchedule = value; }
        }
        public double Duration { get; set; }
        public double MeanCostEDM { get; set; }
        public double MeanCostMOD { get; set; }
        public double ShellShare { get; set; }
        public bool isDraft { get; set; }
       
        public override BsonDocument PreSave(BsonDocument doc, string[] references=null)
        {
            doc = base.PreSave(doc);
            this._id = String.Format("W{0}S{1}R{2}", WellName, SequenceId, RigName);
            return this.ToBsonDocument();
        }

        public List<FYExpenseProfile> FYExpenseProfiles { get; set; }
    }
    public class FYExpenseProfile
    {
        public string Title { get; set; }
        public int Year { get; set; }
        public double Value { get; set; }
        public string ValueType { get; set; }
    }

}




