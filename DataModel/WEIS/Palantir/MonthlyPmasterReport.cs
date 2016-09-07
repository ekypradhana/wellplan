using ECIS.Core;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS
{
    public class MonthlyPmasterReport : ECISModel
    {
        public override string TableName
        {
            get { return "MonthlyPmasterReport"; }
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            var newId = SequenceNo.Get(this.TableName).ClaimAsInt();
            _id = newId;
            return this.ToBsonDocument();
        }

        public string ReportingEntity { get; set; }
        public string PlanningEntity { get; set; }
        public string PlanningEntityID { get; set; }
        public string ActivityEntity { get; set; }
        public string ActivityEntityID { get; set; }
        public string ProbabilityofSuccess { get; set; }
        public string AverageShellShare { get; set; }
        public string Unit { get; set; }
        public string PMasterField { get; set; }
        public string PMasterReference { get; set; }
        public List<ListMonthly> Monthly { get; set; }
        public List<ListAnnualy> Annually { get; set; }
    }

    //public class ListMonthlyPMaster
    //{
    //    public int month { get; set; }
    //    public int year { get; set; }
    //    public double value { get; set; }
    //}

    //public class ListAnnuallyPMaster
    //{
    //    public int year { get; set; }
    //    public double value { get; set; }
    //}
}
