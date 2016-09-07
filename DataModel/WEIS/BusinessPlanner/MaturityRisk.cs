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
    public class MaturityRiskMatrix : ECISModel
    {
        public string BaseOP { get; set; }
        public int Year { get; set; }
        public double NPTCost { get; set; }
        public double NPTTime { get; set; }
        public double TECOPCost { get; set; }
        public double TECOPTime { get; set; }

        public override string TableName
        {
            get { return "WEISMaturityRisk"; }
        }
        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = String.Format("T{0}Y{1}BS{2}", this.Title.Replace(" ",""),Year,BaseOP);
            return this.ToBsonDocument();
        }

    }
}
