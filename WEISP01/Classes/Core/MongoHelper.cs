using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using MongoDB.Bson.Serialization;

namespace ECIS.Core
{
    public class MongoHelper<T> where T : class
    {
        public MongoCollection<T> Collection { get; private set; }

        public MongoHelper()
        {
            var cs = "mongodb://localhost";
            var conn = new MongoClient(cs);
            var db = conn.GetServer().GetDatabase("ecisdb");
            Collection = db.GetCollection<T>(typeof(T).Name+"s");
        }
    }
}