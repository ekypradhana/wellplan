using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class AdminWellController : Controller
    {
        // GET: Shell/AdminWell
        public ActionResult Index()
        {
            return View();
        }

        private IMongoQuery QueryContains(string w, string s)
        {
            return Query.Matches(w, new BsonRegularExpression(new Regex(s.ToLower(), RegexOptions.IgnoreCase)));
        }

        public JsonResult Search(string projectName = "", string rigName = "", string wellName = "")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                List<IMongoQuery> queries = new List<IMongoQuery>();
                queries.Add(Query.NE("IsVirtualWell", true));

                //if (!rigName.Trim().Equals("")) queries.Add(QueryContains("", rigName));
                //if (!projectName.Trim().Equals("")) queries.Add(QueryContains("", projectName));
                if (!wellName.Trim().Equals("")) queries.Add(QueryContains("_id", wellName));

                var query = (queries.Count > 0 ? Query.And(queries.ToArray()) : null);

                var ret = WellInfo.Populate<WellInfo>(query).OrderBy(d => d._id);
                return ret;
            });
        }
        public JsonResult Get(object id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return WellInfo.Get<WellInfo>(id);
            });
        }

        public JsonResult Save(WellInfo w)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                w.Save();
                return w;
            });
        }
    }
}