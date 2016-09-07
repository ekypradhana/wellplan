using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS
{
    public class StandardPmasterReport : ECISModel
    {
        public override string TableName
        {
            get { return "StandardPmasterReport"; }
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
        public List<ListAnnualy> Annually { get; set; }
    }

    //public class AnnuallyStandatPMaster
    //{
    //    public int year { get; set; }
    //    public double value { get; set; }
    //}



    public class PMasterAttribute
    {
        public double AvgShellShare { get; set; }
        public string Unit { get; set; }
    }



    public class PMasterDetails
    {
        public int DateId { get; set; }
        public string Type { get; set; }
        public double value { get; set; }
        public double valueSS { get; set; }
        //public double valueEDM { get; set; }
        //public double valueRT { get; set; }
        public bool isZeroVal { get; set; }
        public string attribute { get; set; }

    }

    public class PValueDetail : PMasterDetails
    {
        public double WithSSValue { get; set; }
    }



    
}
