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
    public class BizMaster : ECISModel
    {

        public override string TableName
        {
            get { return "WEISBizMasters"; }
        }
        public BizMaster()
        {
            Country = "";
            RigName = "";
            ActivityType = "";

            Inflation = new Inflation();
            MarketEscalation = new MarketEscalation();
            GlobalMarketEscalation = new GlobalMarketEscalation();
            MaturityRisks = new List<MaturityRiskMatrix>();
            LongLead = new LongLead();
        }

                

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = String.Format("C{0}R{1}A{2}",
                Country.Replace(" ", ""),
                RigName.Replace(" ", ""),
                ActivityType.Replace(" ", "")
                );
            return this.ToBsonDocument();
        }

        public string RigName { get; set; }
        public string ActivityType { get; set; }
        public string Country { get; set; }

        public Inflation Inflation { get; set; }
        public MarketEscalation MarketEscalation { get; set; }
        public GlobalMarketEscalation GlobalMarketEscalation { get; set; }
        public List<MaturityRiskMatrix> MaturityRisks { get; set; }
        public CSO CSO { get; set; }
        public LongLead LongLead { get; set; }
        public RigRates RigRate { get; set; }

    }

    public class LongLeadElements
    {
        public string Title { set; get; }
        public LongLead Elements { set; get; }
    }


}
