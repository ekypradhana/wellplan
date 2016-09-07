using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ECIS.Biz.Common;

namespace ECIS.Client.WEIS
{
    public class WEISStream : ECISModel
    {
        public override string TableName
        {
            get { return "WEISStreams"; }
        }

        public WEISStream()
        {
            Provider = "";
            Title = "";
            Streamer = "";
            File = "";
            EDMRigName = "";
            isActive = true;
            RigName = "";
        }

        public string Provider { get; set; }
        public string Streamer { get; set; }
        public string File { get; set; }
        public string EDMRigName { get; set; }
        public bool isActive { get; set; }
        public string RigName { get; set; }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            var res = DataHelper.Populate("WEISRigNames", Query.EQ("_id", BsonHelper.GetString(doc, "RigName")));
            if (res != null && res.Count() > 0)
                return base.PreSave(doc, references);
            else
                throw new Exception("Rig Name doesnt exist in WEISRigNames Table");
        }

        public override void PostDelete()
        {
           DataHelper.Delete( "WEISStreamConfigs", Query.EQ("StreamId", Convert.ToInt32 (this._id)));
        }

        public static List<WEISStream> GetStreams(string RigName)
        {
            return DataHelper.Populate<WEISStream>("WEISStreams", Query.EQ("RigName", RigName));
        }

    }


    public class WEISStreamConfig : ECISModel
    {
        // id User + ID WeisStream
        public string UserName { get; set; }
        public int StreamId { get; set; }
        public int POSOrder { get; set; }

        public override string TableName
        {
            get { return "WEISStreamConfigs"; }
        }
        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            _id = String.Format("S{0}U{1}O{2}", UserName.Replace(" ", ""), StreamId, POSOrder);
            return this.ToBsonDocument();
        }

        public static void SetStreamAppereances(string UserLogin, Dictionary<int, int> KeyStreamIdValuePosOrders)
        {
            DataHelper.Delete("WEISStreamConfigs", Query.EQ("UserName", UserLogin));
            foreach (var t in KeyStreamIdValuePosOrders)
            {
                WEISStreamConfig con = new WEISStreamConfig();
                con.UserName = UserLogin;
                con.StreamId = t.Key;
                con.POSOrder = t.Value;
                con.Save();
            }
        }

        public static List<BsonDocument> GetStreamAppereances(string UserLogin)
        {
            var datas = DataHelper.Populate("WEISStreamConfigs", Query.EQ("UserName", UserLogin));
            if (datas != null && datas.Count() > 0)
            {
                List<BsonDocument> docs = new List<BsonDocument>();

                foreach (var t in datas)
                {
                    var restem = DataHelper.Populate("WEISStreams", Query.EQ("_id", Convert.ToInt32(BsonHelper.GetString(t, "StreamId"))));
                    if (restem != null && restem.Count() > 0)
                    {
                        restem.FirstOrDefault().Set("POSOrder", t.GetInt32("POSOrder"));



                        docs.Add(restem.FirstOrDefault());
                    }
                }

                return docs;
            }
            else
                return null;
        }

        public static List<BsonDocument> GetStreamAppereancesTree(string UserLogin)
        {
            var docs = GetStreamAppereances(UserLogin);

            if (docs != null && docs.Count > 0)
            {
                foreach (var t in docs)
                {
                    t.Set("text", t.GetString("Title"));
                    t.Set("id", t.GetInt32("_id"));
                }
                return docs;
            }
            else
                return new List<BsonDocument>();
            
        }

    }


}
