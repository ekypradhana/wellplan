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
    public class ProjectReferenceFactor : ECISModel
    {
        public override string TableName
        {
            get { return "WEISProjectReferenceFactor"; }
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = String.Format("P{0}", this.ProjectName);

            return this.ToBsonDocument();
        }

        public string ProjectName { set; get; }
        public List<string> ReferenceFactorModels { set; get; }
    }

    public class CountryReferenceFactor : ECISModel
    {
        public override string TableName
        {
            get { return "WEISCountryReferenceFactor"; }
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = String.Format("P{0}", this.Country);

            return this.ToBsonDocument();
        }

        public string Country { set; get; }
        public List<string> ReferenceFactorModels { set; get; }
        public CountryReferenceFactor()
        {
            ReferenceFactorModels = new List<string>();
        }
    }

}
