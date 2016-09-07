using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using ECIS.Core;
using ECIS.Biz.Common;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;

namespace ECIS.Client.WEIS
{
    [BsonIgnoreExtraElements]
    public class MasterRigName : ECISModel
    {
        public override string TableName
        {
            get { return "WEISRigNames"; }
        }

        public string Name { set; get; }
        [BsonIgnore]
        public int CountStream { get; set; }
        public string RigType { set; get; }

        public bool isOfficeLocation { get; set; }

        [BsonIgnore]
        public List<WEISStream> Streams { get; set; }

        public override MongoDB.Bson.BsonDocument PreSave(MongoDB.Bson.BsonDocument doc, string[] references = null)
        {

            if (doc.GetString("Name").Equals("BsonNull"))
                doc.Set("Name", doc.GetString("_id"));
            this._id = String.Format("{0}", doc.GetString("Name"));
            return doc;
        }

        public static List<MasterRigName> GetOnlyRigWithStream()
        {

            string aggregateCond1 = "{ $group: { '_id': '$RigName'}}";
            List<BsonDocument> pipes = new List<BsonDocument>();
            pipes.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));

            //var res = DataHelper.Aggregate("WEISStreams", pipes);
            //var r = res.Select(x => BsonHelper.GetString(x,"_id")).ToList();
            //var y = Query.In("_id", new BsonArray(r.ToArray()));
            var ys = MasterRigName.Populate<MasterRigName>();

            foreach (var x in ys)
            {
                x.Streams = new List<WEISStream>();
                var str = WEISStream.GetStreams(x._id.ToString());
                foreach (var s in str)
                {
                    x.Streams.Add(s);
                }

            }
            return ys;

        }

        public override void PostGet()
        {
            if (this.Name == null || this.Name.Equals(""))
                this.Name = this._id.ToString();

            //this.isOfficeLocation = this.ToBsonDocument().GetBool("isOfficeLocation") == false ? false : true;

            this.Streams = WEISStream.GetStreams(this.Name);

            base.PostGet();
        }


    }

    [BsonIgnoreExtraElements]
    public class MasterRegion : ECISModel
    {
        public override string TableName
        {
            get { return "WEISRegions"; }
        }

        public string Name { set; get; }
    }

    [BsonIgnoreExtraElements]
    public class MasterOperatingUnit : ECISModel
    {
        public override string TableName
        {
            get { return "WEISOperatingUnits"; }
        }

        public string Name { set; get; }
    }

    [BsonIgnoreExtraElements]
    public class MasterRigType : ECISModel
    {
        public override string TableName
        {
            get { return "WEISRigTypes"; }
        }

        public string Name { set; get; }
    }

    [BsonIgnoreExtraElements]
    public class MasterProject : ECISModel
    {
        public override string TableName
        {
            get { return "WEISProjectNames"; }
        }

        public string Name { set; get; }
    }

    [BsonIgnoreExtraElements]
    public class MasterAssetName : ECISModel
    {
        public override string TableName
        {
            get { return "WEISAssetNames"; }
        }

        public string Name { set; get; }
    }

    [BsonIgnoreExtraElements]
    public class MasterActivity : ECISModel
    {
        public override string TableName
        {
            get { return "WEISActivities"; }
        }

        //public string _id { set; get; }
        //public DateTime LastUpdate { set; get; }
        public string EDMActivityId { set; get; }
        public string ActivityCategory { set; get; }


    }

    [BsonIgnoreExtraElements]
    public class MasterFundingType : ECISModel
    {
        public override string TableName
        {
            get { return "WEISFundingTypes"; }
        }

        public string Name { set; get; }

        public static void LoadManualFundingType()
        {
            DataHelper.Delete("WEISFundingTypes");
            List<BsonDocument> docs = new List<BsonDocument>();

            BsonDocument bs1 = new BsonDocument();
            bs1.Set("_id", "EXPEX");
            bs1.Set("Name", "EXPEX");
            bs1.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
            docs.Add(bs1);

            BsonDocument bs2 = new BsonDocument();
            bs2.Set("_id", "CAPEX");
            bs2.Set("Name", "CAPEX");
            bs2.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
            docs.Add(bs2);

            BsonDocument bs3 = new BsonDocument();
            bs3.Set("_id", "ABEX");
            bs3.Set("Name", "ABEX");
            bs3.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
            docs.Add(bs3);

            BsonDocument bs4 = new BsonDocument();
            bs4.Set("_id", "OPEX");
            bs4.Set("Name", "OPEX");
            bs4.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
            docs.Add(bs4);

            BsonDocument bs5 = new BsonDocument();
            bs5.Set("_id", "C2E");
            bs5.Set("Name", "C2E");
            bs5.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
            docs.Add(bs5);

            BsonDocument bs6 = new BsonDocument();
            bs6.Set("_id", "EXPEX SUCCESS");
            bs6.Set("Name", "EXPEX SUCCESS");
            bs6.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
            docs.Add(bs6);

            DataHelper.Save("WEISFundingTypes", docs);
        }
        public static string CreateCollectionFundingType()
        {
            DataHelper.Delete("WEISFundingTypes");
            LoadManualFundingType();
            #region old
            //            List<BsonDocument> docs = new List<BsonDocument>();
            //            string aggregateCond1 = "{ $unwind: '$Phases' }";
            //            string projectio = @"{$project :  { 
            //                'FundingType' : '$Phases.FundingType'
            //                } }";
            //            string grp = "{ $group: { _id : '$FundingType' }  }";
            //            List<BsonDocument> pipelines = new List<BsonDocument>();
            //            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
            //            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(projectio));
            //            pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(grp));
            //            List<BsonDocument> aggregates = DataHelper.Aggregate(new WellActivity().TableName, pipelines);
            //            if (!aggregates.Any())
            //                LoadManualFundingType();
            //            else
            //            {
            //                foreach (var aggregate in aggregates) //.Where(x=> !(BsonHelper.GetString(x,"_id").Equals(null)))
            //                {
            //                    string FundingType = BsonHelper.GetString(aggregate, "_id");
            //                    if (FundingType != "BsonNull")
            //                    {
            //                        BsonDocument bs = new BsonDocument();
            //                        bs.Set("_id", FundingType);
            //                        bs.Set("Name", FundingType);
            //                        bs.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
            //                        docs.Add(bs);
            //                    }
            //                }
            //                DataHelper.Save("WEISFundingTypes", docs);
            //            }
            #endregion
            return "Saved";
        }
    }

    [BsonIgnoreExtraElements]
    public class MasterOP : ECISModel
    {
        public override string TableName
        {
            get { return "WEISOPs"; }
        }

        public string Name { set; get; }
        public static string ActiveOP(){
            var config = new BsonDocument();

                Config.Populate<Config>(Query.EQ("ConfigModule", "BaseOPDefault")).ForEach(d =>
                {
                    var doc = d.ToBsonDocument();
                    config.Set(doc.GetString("_id"), doc.Get("ConfigValue"));
                });

                return config.ToString();
        }
    }

    [BsonIgnoreExtraElements]
    public class MasterCountry : ECISModel
    {
        public override string TableName
        {
            get { return "WEISCountries"; }
        }

        public string Name { get; set; }
        [BsonIgnore]
        public string Phone { get; set; }
        public string Continent { get; set; }
        [BsonIgnore]
        public string Capital { get; set; }
        public string Currency { get; set; }
        public string Code { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class MasterRFMs : ECISModel
    {
        public override string TableName
        {
            get { return "WEISRFMs"; }
        }
    }

    [BsonIgnoreExtraElements]
    public class LineOfBusiness : ECISModel
    {
        public override string TableName
        {
            get { return "WEISLineOfBusiness"; }
        }

        public string Name { set; get; }
    }
}
