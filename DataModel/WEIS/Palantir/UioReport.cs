using ECIS.Core;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS
{
    public class UioReport  : ECISModel
    {
        public override string TableName
        {
            get { return "UIOP1Report"; }
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            var newId = SequenceNo.Get(this.TableName).ClaimAsInt();
            _id = newId;
            return this.ToBsonDocument();
        }
        
        public string ProjectName { get; set; }
        public string PMasterAEName { get; set; }
        public string Hub { get; set; }
        public string Field { get; set; }
        public double EquityAcreage { get; set; }
        public string ORSNumber { get; set; }
        public string LineItemType { get; set; }
        public string WellName { get; set; }
        public string WellType { get; set; }
        public double WellDuration;
        private DateTime _wellstart;
        public DateTime WellStartDate
        {
            get { return _wellstart; }
            set { _wellstart = Tools.ToUTC(value); }
        }
        public string ETSD { get; set; }
        public string RigName { get; set; }
        private DateTime _startDate;
        public DateTime RigContractStartDate
        {
            get { return _startDate; }
            set { _startDate = Tools.ToUTC(value); }
        }
        private DateTime _endDate;
        public DateTime RigContractEndDate
        {
            get { return _endDate; }
            set { _endDate = Tools.ToUTC(value); }
        }
        public string Unit { get; set; }
        public string CostCategory { get; set; }
        public List<ListMonthly> Monthly { get; set; }
        public List<ListAnnualy> Annual { get; set; }
    }

    public class ListMonthly
    {
        //public int id { get; set; }
        public int month { get; set; }
        public int year { get; set; }
        public double value { get; set; }
    }

    public class ListAnnualy
    {
        public int year { get; set; }
        public double value { get; set; }
    }

    public class UIOGridReport 
    {
        public string TopHeader { get; set; }
        //public int month1 { get; set; }
        public List<string> month { get; set; }
        public List<double> MonthValue { get; set; } 

    }
}
