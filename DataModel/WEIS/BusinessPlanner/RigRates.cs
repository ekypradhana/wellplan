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
    #region Rig Rates
    public class RigRates : ECISModel
    {
        public override string TableName
        {
            get { return "WEISRigRates"; }
        }
        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = String.Format("T{0}", this.Title);

            return this.ToBsonDocument();
        }
        public List<FYExpenseProfile> Value { get; set; }
        public RigRates()
        {
            Value = new List<FYExpenseProfile>();
        }
    }
    #endregion
}
