using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using System.Globalization;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using System.Collections.Specialized;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class ComparisonController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetRigsInRegion(string region)
        {
            try
            {
                var data = WellActivity.Populate<WellActivity>(Query.EQ("Region", region)).GroupBy(d => d.RigName).Select(d => d.Key).OrderBy(d => d);
                return Json(new { Success = true, Data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.StackTrace }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetWellsInRig(string rigname)
        {
            try
            {
                var data = WellActivity.Populate<WellActivity>(Query.EQ("RigName", rigname)).GroupBy(d => d.WellName).Select(d => d.Key).OrderBy(d => d);
                return Json(new { Success = true, Data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.StackTrace }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetChartData(ComparisonDataParam param)
        {
            param.SidebarPicked = (param.SidebarPicked == null ? new List<string>() : param.SidebarPicked);
            param.DataPointPicked = (param.DataPointPicked == null ? new List<string>() : param.DataPointPicked);

            try
            {
                var data = new List<BsonDocument>();

                if (param.TabSelected.Equals("Regional"))
                    data = GetChartDataGroupedBy(param, "Region");
                else if (param.TabSelected.Equals("Rig"))
                    data = GetChartDataGroupedBy(param, "RigName");
                else if (param.TabSelected.Equals("Well"))
                    data = GetChartDataGroupedBy(param, "WellName");

                var seriesRAW = param.DataPointPicked.Where(d => !new string[] { "Cost", "Days", "Cost / Days" }.Contains(d));
                var series = new List<string>();

                if (param.DataPointPicked.Contains("Days"))
                    series = series.Concat(seriesRAW.Select(d => (d + "_Days")).ToList()).ToList();
                if (param.DataPointPicked.Contains("Cost"))
                    series = series.Concat(seriesRAW.Select(d => (d + "_Cost")).ToList()).ToList();
                if (param.DataPointPicked.Contains("Cost / Days"))
                    series = series.Concat(seriesRAW.Select(d => (d + "_CostPerDays")).ToList()).ToList();

                var result = new
                {
                    Data = DataHelper.ToDictionaryArray(data),
                    Series = series
                };

                return Json(new { Success = true, Data = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.StackTrace }, JsonRequestBehavior.AllowGet);
            }
        }

        private List<BsonDocument> GetChartDataGroupedBy(ComparisonDataParam param, string what)
        {
            if (param.SidebarPicked.Count < 1 && param.DataPointPicked.Count < 1)
            {
                return new List<BsonDocument>();
            }

            try
            {
                var query = Query.In(what, new BsonArray(param.SidebarPicked));
                return WellActivity.Populate<WellActivity>(query)
                    .GroupBy(d => d.ToBsonDocument().GetString(what))
                    .Select(d =>
                    {
                        var e = new BsonDocument();
                        e.Set("DataPoint", d.Key);

                        SetProperties(e, d, param, "Plan", "OP");
                        SetProperties(e, d, param, "AFE");
                        SetProperties(e, d, param, "OP", "LS");
                        SetProperties(e, d, param, "LE");

                        return e;
                    })
                    .OrderBy(d => d.GetString("DataPoint"))
                    .ToList();
            }
            catch (Exception e)
            {
                return new List<BsonDocument>();
            }
        }

        private void SetProperties(BsonDocument doc, IGrouping<string, WellActivity> activities, ComparisonDataParam param, string field, string what = null)
        {
            what = (what == null ? field : what);

            if (!param.DataPointPicked.Contains(what))
                return;

            if (param.DataPointPicked.Contains("Cost"))
                doc.Set(what + "_Cost", activities.Sum(d => d.Phases.Sum(e => e.ToBsonDocument().Get(field).ToBsonDocument().GetDouble("Cost"))) / param.Divider);
            if (param.DataPointPicked.Contains("Days"))
                doc.Set(what + "_Days", activities.Sum(d => d.Phases.Sum(e => e.ToBsonDocument().Get(field).ToBsonDocument().GetDouble("Days"))));
            if (param.DataPointPicked.Contains("Cost / Days"))
            {
                var cost = activities.Sum(d => d.Phases.Sum(e => e.ToBsonDocument().Get(field).ToBsonDocument().GetDouble("Cost")));
                var days = activities.Sum(d => d.Phases.Sum(e => e.ToBsonDocument().Get(field).ToBsonDocument().GetDouble("Days")));
                doc.Set(what + "_CostPerDays", cost / param.Divider / days);
            }
        }
	}

    public class ComparisonDataParam
    {
        public string TabSelected { set; get; }
        public List<string> SidebarPicked { set; get; }
        public List<string> DataPointPicked { set; get; }
        public long Divider { set; get; }
    }
}