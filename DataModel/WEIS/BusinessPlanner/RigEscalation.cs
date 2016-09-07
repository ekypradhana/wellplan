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
    public class RigEscalation : ECISModel
    {

        public override string TableName
        {
            get { return "WEISRigEscalation"; }
        }
        public string RigName { get; set; }
        public double Value { get; set; }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            this._id = String.Format("{0}", RigName.Replace(" ","") );
            doc = this.ToBsonDocument();
            return doc;
        }
    }
}
