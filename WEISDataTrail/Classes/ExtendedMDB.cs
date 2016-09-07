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

namespace WEISDataTrail
{
    /// <summary>
    /// please use : ServerHost2
    /// please use : ServerDb2
    /// please use : in app config
    /// </summary>
    public class ExtendedMDB
    {
        public static MongoDatabase GetDb()
        {
            if (ExtendedMDB._db == null)
                ExtendedMDB._db = new MongoClient(string.Format("mongodb://{0}",
                    (object)ConfigurationManager.AppSettings["ServerHost2"])).GetServer().GetDatabase(ConfigurationManager.AppSettings["ServerDb2"]);
            return ExtendedMDB._db;
        }

        internal static MongoDatabase _db;
        internal static MongoDatabase GetDb(string connection, string database)
        {
            if (_db == null)
            {
                var connectionString = connection;
                var client = new MongoClient("mongodb://" + connectionString);//+ ":27017");
                MongoServer server = client.GetServer();
                MongoDatabase t = server.GetDatabase(database);
                _db = t;
            }
            return _db;
        }
        public static List<string> GetCollectionName(string colNamePrefix = "")
        {
            string host = System.Configuration.ConfigurationManager.AppSettings["ServerHost2"];
            string db = System.Configuration.ConfigurationManager.AppSettings["ServerDb2"];
            MongoDatabase mongo = GetDb(host, db);

            List<string> res = new List<string>();

            foreach (string t in mongo.GetCollectionNames().ToList())
            {
                if (t.Contains(colNamePrefix))
                {
                    res.Add(t);
                }
            }

            return res;
        }
        public static long GetCountCollections(string tableName)
        {
            string host = System.Configuration.ConfigurationManager.AppSettings["ServerHost2"];
            string db = System.Configuration.ConfigurationManager.AppSettings["ServerDb2"];
            MongoDatabase mongo = GetDb(host, db);

            var collection = mongo.GetCollection<Type>(tableName);
            var cursor = collection.Count();
            return cursor;
        }

        public static List<BsonDocument> Populate(string memoryId, IMongoQuery q = null, int take = 0, int skip = 0, IEnumerable<string> fields = null, SortByBuilder sort = null, string collectionName = "", bool memoryObject = false, bool forceReadDb = false)
        {
            List<BsonDocument> list = new List<BsonDocument>();
            if (collectionName.Equals(""))
                collectionName = memoryId;
            if (list.Count == 0)
            {
                MongoCursor<BsonDocument> mongoCursor = q == null ? ((MongoCollection<BsonDocument>)ExtendedMDB.GetDb().GetCollection<BsonDocument>(collectionName)).FindAll() :
                    ((MongoCollection<BsonDocument>)ExtendedMDB.GetDb().GetCollection<BsonDocument>(collectionName)).Find(q);
                if (fields != null && Enumerable.Count<string>(fields) > 0)
                    mongoCursor.SetFields(Enumerable.ToArray<string>(fields));
                if (sort != null)
                    mongoCursor.SetSortOrder((IMongoSortBy)sort);
                if (take == 0)
                {
                    list = Enumerable.ToList<BsonDocument>((IEnumerable<BsonDocument>)Queryable.AsQueryable<BsonDocument>((IEnumerable<BsonDocument>)mongoCursor));
                }
                else
                {
                    mongoCursor.SetSkip(skip);
                    mongoCursor.SetLimit(take);
                    return Enumerable.ToList<BsonDocument>((IEnumerable<BsonDocument>)mongoCursor);
                }
            }
            return list;
        }
        public static List<T> Populate<T>(string memoryId, IMongoQuery q = null, int take = 0, int skip = 0, IEnumerable<string> fields = null, SortByBuilder sort = null, string collectionName = "", bool memoryObject = false, bool forceReadDb = false)
        {
            List<BsonDocument> list1 = ExtendedMDB.Populate(memoryId, q, take, skip, fields, sort, collectionName, memoryObject, forceReadDb);
            List<T> list2 = new List<T>();
            using (List<BsonDocument>.Enumerator enumerator = list1.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    BsonDocument current = enumerator.Current;
                    try
                    {
                        list2.Add(BsonSerializer.Deserialize<T>(current));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Unable to deserialize BsonDocument => " + JsonConvert.SerializeObject((object)current.ToDictionary()));
                    }
                }
            }
            return list2;
        }
        public static List<BsonDocument> Aggregate(string collectionName, List<BsonDocument> pipelines)
        {
            AggregateArgs args = new AggregateArgs();
            args.Pipeline = ((IEnumerable<BsonDocument>)pipelines);
            return ExtendedMDB.Aggregate(collectionName, args);
        }
        public static List<BsonDocument> Aggregate(string collectionId, AggregateArgs args)
        {
            args.AllowDiskUse = (new bool?(true));
            return Enumerable.ToList<BsonDocument>(((MongoCollection)ExtendedMDB.GetDb().GetCollection(collectionId)).Aggregate(args));
        }



        public static void Save(string collectionName, BsonDocument data)
        {

            MongoCollection<BsonDocument> mongoCollection = (MongoCollection<BsonDocument>)ExtendedMDB.GetDb().GetCollection<BsonDocument>(collectionName);
            mongoCollection.Save(data);
            data = null;
        }

        public static void Delete(string collectionName, IMongoQuery q = null)
        {
            if (q == null)
                ((MongoCollection)ExtendedMDB.GetDb().GetCollection(collectionName)).RemoveAll();
            else
                ((MongoCollection)ExtendedMDB.GetDb().GetCollection(collectionName)).Remove(q);
        }

    }
}
