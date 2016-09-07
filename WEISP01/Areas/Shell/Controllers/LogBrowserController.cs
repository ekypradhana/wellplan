using ECIS.Client.WEIS;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class LogBrowserController : Controller
    {
        //
        // GET: /Shell/LogBrowser/
        public ActionResult Index()
        {
            ViewBag.Title = "Log Browser";
            return View();
        }

        public JsonResult LoadGridData(DateTime start, DateTime finish, List<string> UserName = null, Int32 Skip = 0, Int32 Take = 0)
        {
            List<BsonDocument> bdocs = new List<BsonDocument>();
            IMongoQuery q = null;

            finish = finish.AddDays(1);

            List<WEISUserActivityLog> res = new List<WEISUserActivityLog>();
            if (UserName == null )
            {
                q = Query.And(Query.LT("LastUpdate", new DateTime(finish.Year, finish.Month, finish.Day)), Query.GTE("LastUpdate", new DateTime(start.Year, start.Month, start.Day)));
                res = WEISUserActivityLog.Populate<WEISUserActivityLog>(q);
            }
            else
            {
                q = Query.And(
                    Query.LT("LastUpdate", new DateTime(finish.Year, finish.Month, finish.Day)),
                    Query.GTE("LastUpdate", new DateTime(start.Year, start.Month, start.Day)),
                    Query.In("UserName", new BsonArray(UserName))
                    );
                res = WEISUserActivityLog.Populate<WEISUserActivityLog>(q);
            }



            List<string > show = new List<string>();
            show.Add("LastUpdate");
            show.Add("UserName");

            foreach (var r in res)
            {
                bdocs.Add(r.ToBsonDocument());
            }

            var datasall = BsonHelper.Unwind(bdocs, "Logs", "", show);

            return Json(new { Data = DataHelper.ToDictionaryArray(datasall.Skip(Skip).Take(Take)).ToList(), Total = datasall.Count, Result = "OK", Message = "" }, JsonRequestBehavior.AllowGet);
        }

    }
}