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
using ECIS.Biz.Common;

namespace ECIS.Client.WEIS
{
    public class RigEscalationFactor : ECISModel
    {
        public override string TableName
        {
            get { return "WEISRigEscalation"; }
        }
        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = String.Format("{0}", this.Title);

            return this.ToBsonDocument();
        }
        public List<FYExpenseProfile> Value { get; set; }
        public RigEscalationFactor()
        {
            Value = new List<FYExpenseProfile>();
        }
    }
}
