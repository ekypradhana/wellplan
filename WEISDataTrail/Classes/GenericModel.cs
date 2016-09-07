using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECIS.Client.WEIS;
using System.Reflection;
using ECIS.Core;


using MongoDB;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WEISDataTrail
{
    [BsonIgnoreExtraElements]
    public class GenericModel<T>
    {
        private string TableName { get; set; }
        public string Title { get; set; }
        public object _id { get; set; }
        //public DateTime LastUpdate { get; set; }

        public T obj;
        public DateTime TrailDate { get; private set; }

        public string DateId { get; private set; }


        public GenericModel(T ob)
        {
            obj = ob;
            this.TrailDate = DateTime.Now;
            DateId = this.TrailDate.ToString("yyyyMMdd");
            DefaultConstruct(obj);
            this._id = _id.ToString() + "-" + TrailDate.ToString("yyyyMMdd");
        }
        public GenericModel(T ob, string TableName, DateTime TrailDate)
        {
            obj = ob;
            if (ob.GetType().Name == "BsonDocument")
            {
                this._id = BsonHelper.GetString(ob.ToBsonDocument(), "_id") + "-" + TrailDate.ToString("yyyyMMdd");
                this.TableName = TableName + "_tr";
                //this.LastUpdate = DateTime.Now;
                this.TrailDate = TrailDate;
                DateId = this.TrailDate.ToString("yyyyMMdd");

                this.SaveTrail();
            }
            else
                throw new Exception();
        }
        private void DefaultConstruct(T ob, string TableName = "")
        {
            if (TableName == "")
            {
                PropertyInfo pTable = obj.GetType().GetProperty("TableName");
                this.TableName = pTable.GetValue(obj, null) == null ? "" : pTable.GetValue(obj, null).ToString();
            }
            else
            {
                this.TableName = TableName;
            }

            this.TableName = this.TableName + "_tr";

            PropertyInfo pTitle = obj.GetType().GetProperty("Title");
            this.Title = pTitle.GetValue(obj, null) == null ? "" : pTitle.GetValue(obj, null).ToString();

            PropertyInfo p_id = obj.GetType().GetProperty("_id");
            this._id = p_id.GetValue(obj, null);

            PropertyInfo pLast = obj.GetType().GetProperty("LastUpdate");
            //this.LastUpdate = Convert.ToDateTime(pLast.GetValue(obj, null));

        }

        public void SaveTrail()
        {
            MongoCollection<BsonDocument> mongoCollection = (MongoCollection<BsonDocument>)ExtendedMDB.GetDb(
                System.Configuration.ConfigurationManager.AppSettings["ServerHost2"],
                System.Configuration.ConfigurationManager.AppSettings["ServerDb2"]
                ).GetCollection<BsonDocument>(this.TableName);
            mongoCollection.Save(this.ToBsonDocument());
        }
    }
}
