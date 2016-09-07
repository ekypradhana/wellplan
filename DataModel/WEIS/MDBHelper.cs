using ECIS.Core;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ECIS.Client.WEIS;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Text.RegularExpressions;

namespace ECIS.Client.WEIS
{
    public static class MDBHelper
    {

        internal static MongoDatabase _db;
        internal static MongoDatabase GetDb(string connection, string database)
        {
            if (_db == null)
            {
                var connectionString = connection;
                var client = new MongoClient("mongodb://" + connectionString );
                MongoServer server = client.GetServer();
                MongoDatabase t = server.GetDatabase(database);
                _db = t;
            }
            return _db;
        }
        public static void DuplicateAndSave(string collectionName, string destinationName)
        {
            string host = System.Configuration.ConfigurationSettings.AppSettings["ServerHost"];
            string db = System.Configuration.ConfigurationSettings.AppSettings["ServerDb"];
            MongoDatabase mongo = GetDb(host, db);
            mongo.CreateCollection(destinationName);
            var source = mongo.GetCollection(collectionName);
            var dest = mongo.GetCollection(destinationName);
            dest.InsertBatch(source.FindAll());
        }

       
    }
}
