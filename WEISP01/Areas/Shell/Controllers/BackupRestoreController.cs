using ECIS.Core;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using ECIS.Client.WEIS;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ECIS.Core.DataSerializer;
using System.Text.RegularExpressions;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth(WEISRoles = "Administrators")]
    public class BackupRestoreController : Controller
    {
        //
        // GET: /Shell/BackupRestore/
        public ActionResult Index()
        {
            return View();
        }

          internal MongoDatabase _db;
        internal MongoDatabase GetDb(string connection, string database)
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

        public List<string> GetCollectionName()
        {
            string host = System.Configuration.ConfigurationSettings.AppSettings["ServerHost"];
            string db = System.Configuration.ConfigurationSettings.AppSettings["ServerDb"];
            MongoDatabase mongo = GetDb(host, db);

            List<string> res = new List<string>();

            foreach (string t in mongo.GetCollectionNames().ToList())
            {
                if (t.Contains("_WellActivity_"))
                {
                    res.Add(t);
                }
            }

            return res;
        }


        public JsonResult Backup(string id)
        {
            try
            {

                string host = System.Configuration.ConfigurationSettings.AppSettings["ServerHost"];
                string db = System.Configuration.ConfigurationSettings.AppSettings["ServerDb"];
                MongoDatabase mongo = GetDb(host, db);

                string backupname = "_WellActivity_" + DateTime.Now.ToString("yyyyMMddhhmm") + "_" + id;

                mongo.CreateCollection(backupname);
                var source = mongo.GetCollection("WEISWellActivities");
                var dest = mongo.GetCollection(backupname);
                dest.InsertBatch(source.FindAll());

                DataHelper.Delete("BackupListWellActivity", Query.EQ("CollectionName", backupname));
                DataHelper.Save("BackupListWellActivity",
                    new BsonDocument()
                    .Set("CollectionName", backupname)
                    .Set("User", WebTools.LoginUser.UserName)
                    .Set("Date", DateTime.Now)
                    );

                return Json(new { Data = "", Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public JsonResult DeleteBackupMass(DateTime start, DateTime finish)
        {
            try
            {

                string host = System.Configuration.ConfigurationSettings.AppSettings["ServerHost"];
                string db = System.Configuration.ConfigurationSettings.AppSettings["ServerDb"];
                MongoDatabase mongo = GetDb(host, db);
                //mongo.DropCollection(id);

                List<string> dateRangeStr = new List<string>();
                while ((start = start.AddDays(1)) <= finish)
                {
                    dateRangeStr.Add(start.ToString("yyyyMMdd"));
                    // do your thing
                }
                List<string> CollectionsName = mongo.GetCollectionNames().ToList();
                List<string> listdelete = new List<string>();
                foreach (string c in CollectionsName)
                {
                    if (c.Contains("_WellActivity_"))
                    {
                        if (dateRangeStr.Contains(c.Substring(14, 8)))
                        {
                           // listdelete.Add(c);
                            mongo.DropCollection(c);
                        }
                    }
                }


          

                return Json(new { Data = "", Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult DeleteBackup(string id)
        {
            try
            {
                string host = System.Configuration.ConfigurationSettings.AppSettings["ServerHost"];
                string db = System.Configuration.ConfigurationSettings.AppSettings["ServerDb"];
                MongoDatabase mongo = GetDb(host, db);

                mongo.DropCollection(id);

                return Json(new { Data = "", Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult Restore(string id)
        {
            try
            {

                string host = System.Configuration.ConfigurationSettings.AppSettings["ServerHost"];
                string db = System.Configuration.ConfigurationSettings.AppSettings["ServerDb"];
                MongoDatabase mongo = GetDb(host, db);

                mongo.DropCollection("WEISWellActivities");
                mongo.RenameCollection(id, "WEISWellActivities");

                return Json(new { Data = "", Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Data = ex.InnerException, Result = "NOK", Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}