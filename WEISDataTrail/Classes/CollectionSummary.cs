using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MongoDB.Bson;
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Newtonsoft.Json;

using System.Configuration;
using ECIS.Client.WEIS;
using ECIS.Core;
namespace WEISDataTrail
{
    public class CollectionSummary : ECISModel
    {
        public override string TableName
        {
            get { return "WEISCollectionSummary_tr"; }
        }

        public DateTime TrailDate { get; set; }
        public int DateId { get; private set; }


        private DateIsland _DateIsland;
        public DateIsland DateIsland
        {
            get
            {
                if (_DateIsland == null) _DateIsland = new DateIsland();
                return _DateIsland;
            }
            set { _DateIsland = value; }
        }

        public List<MasterSummary> Masters { get; set; }

        public TranscSummary PIP { get; set; }
        public TranscSummary RigPIP { get; set; }
        public TranscSummary MonthlyReport { get; set; }
        public TranscSummary WeeklyReport { get; set; }
        public TranscSummary BizPlan { get; set; }
        public TranscSummary WellPlan { get; set; }

        public CollectionSummary()
        {
            Masters = new List<MasterSummary>();
            PIP = new TranscSummary("WEISWellPIPs");
            RigPIP = new TranscSummary("WEISWellPIPs");
            MonthlyReport = new TranscSummary("WEISWellActivityUpdatesMonthly");
            WeeklyReport = new TranscSummary("WEISWellActivityUpdates");
            BizPlan = new TranscSummary("WEISBizPlanActivities");
            WellPlan = new TranscSummary("WEISWellActivities");

        }

        public override MongoDB.Bson.BsonDocument PreSave(MongoDB.Bson.BsonDocument doc, string[] references = null)
        {
            if (references != null && references.Any() && references.Contains("dummy"))
            {
                this._id = references[1];
                this.LastUpdate = DateTime.ParseExact(this._id.ToString(), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                this.DateId = Convert.ToInt32(this._id);
                DateIsland = new DateIsland(DateTime.ParseExact(this._id.ToString(), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture));
                return this.ToBsonDocument();
            }
            else
            {
                this._id = DateTime.Now.ToString("yyyyMMdd");
                this.LastUpdate = DateTime.Now;
                this.DateId = Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd"));
                DateIsland = new DateIsland(TrailDate);
                return this.ToBsonDocument();
            }
        }

        public void SaveSummary()
        {
            PreSave(this.ToBsonDocument());
            ExtendedMDB.Save(this.TableName, this.ToBsonDocument());
        }
    }


    public class TranscSummary
    {
        public string TableName { get; set; }
        public int Count { get; set; }
        public List<SummaryDetailPIP> Details { get; set; }
        public TranscSummary(string tableName)
        {
            Details = new List<SummaryDetailPIP>();
            this.TableName = tableName;
        }
    }

    public class MasterSummary
    {
        public string TableName { get; set; }
        public int Count { get; set; }
        public List<string> Datas { get; set; }
    }

    public class SummaryDetail
    {
        // current OP from WellPlan [OP14,OP15]
        public string OPType { get; set; }
        public WellDrillData OP { get; set; }
        public WellDrillData LE { get; set; }
        public WellDrillData LS { get; set; }
        public WellDrillData MLE { get; set; }
        public WellDrillData AFE { get; set; }

        public SummaryDetail PrevOP { get; set; }

        public SummaryDetail()
        {
            //PrevOP = new SummaryDetail();
            OP = new WellDrillData();
            LE = new WellDrillData();
            LS = new WellDrillData();
            MLE = new WellDrillData();
            AFE = new WellDrillData();
        }

    }


    public class SummaryDetailPIP : SummaryDetail
    {
        // current OP from WellPlan [OP14,OP15]
        public int ReliazedPIP { get; set; }
        public int UnReliazedPIP { get; set; }
        public WellDrillData OPRealized { get; set; }
        public WellDrillData LERealized { get; set; }

        public WellDrillData OPUnRealized { get; set; }
        public WellDrillData LEUnRealized { get; set; }


        public SummaryDetailPIP()
        {
            //PrevOP = new SummaryDetail();
            OPRealized = new WellDrillData();
            LERealized = new WellDrillData();

            OPUnRealized = new WellDrillData();
            LEUnRealized = new WellDrillData();
           
        }

    }
}
