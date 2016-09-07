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
    public class RigEscalationController : Controller
    {
        //
        // GET: /Shell/RigEscalation/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult UpdateRigEscalation(List<Dictionary<string, object>> changes)
        {
            var ri = new ResultInfo();
            try
            {

                foreach (var c in changes)
                {
                    var doc = c.ToBsonDocument();
                    var title = doc.GetString("Title");

                    var t = RigEscalationFactor.Get<RigEscalationFactor>(Query.EQ("Title", title));
                    foreach (var val in t.Value)
                    {
                        val.Value = Convert.ToDouble(doc.GetString("Year_" + val.Year.ToString()));
                    }

                    t.Save();
                }

                //changes.ForEach(d =>
                //{
                //    var doc = d.ToBsonDocument();
                //    var rigRate = RigRates.Get<RigRates>(Query.EQ("_id", doc.GetString("_id")));
                //    if (rigRate.Value == null)
                //    {
                //        rigRate.Value = new List<FYExpenseProfile>();
                //    }

                //    foreach (var each in doc)
                //    {
                //        if (each.Name.Contains("Year_"))
                //        {
                //            var year = Convert.ToInt32(each.Name.Replace("Year_", ""));

                //            var rigValue = rigRate.Value.FirstOrDefault(e => e.Year == year);
                //            if (rigValue == null)
                //            {
                //                rigRate.Value.Add(new FYExpenseProfile()
                //                {
                //                    Year = year,
                //                    Value = Convert.ToDouble(each.Value)
                //                });
                //            }
                //            else
                //            {
                //                rigValue.Value = Convert.ToDouble(each.Value);
                //            }
                //        }
                //        else if (each.Name == "Title")
                //        {
                //            rigRate.Title = Convert.ToString(each.Value);
                //        }
                //    }

                //    rigRate.Delete();
                //    rigRate.Save();
                //});
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult DeleteRigEscalation(string id)
        {
            var ri = new ResultInfo();
            try
            {
                var rigRate = RigEscalationFactor.Get<RigEscalationFactor>(Query.EQ("_id", id));
                if (rigRate != null)
                {
                    rigRate.Delete();
                }
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetRigEscalationData(string keyword = "")
        {
            var ri = new ResultInfo();
            try
            {
                var now = DateTime.Now;
                var minYear = now.Year;
                var maxYear = now.Year;

                IMongoQuery query = null;
                var rigs = RigEscalationFactor.Populate<RigEscalationFactor>(query);

                if (rigs.Any())
                {
                    minYear = rigs.FirstOrDefault().Value.DefaultIfEmpty(new FYExpenseProfile()).Min(d => d.Year);
                    maxYear = rigs.FirstOrDefault().Value.DefaultIfEmpty(new FYExpenseProfile()).Max(d => d.Year);
                }

                var result = rigs.Select(d =>
                {
                    var each = new { Title = d.Title }.ToBsonDocument();

                    for (var i = minYear; i <= maxYear; i++)
                    {
                        var value = d.Value.FirstOrDefault(e => e.Year == i) ?? new FYExpenseProfile();
                        each.Set("_id", Convert.ToString(d._id));
                        each.Set("Year_" + i, value.Value);
                    }

                    return each;
                }).OrderBy(d => d.GetString("Title"));

                ri.Data = result.Select(d => d.ToDictionary());
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult AddRigEscalationYear()
        {
            var ri = new ResultInfo();
            try
            {
                var sampleData = RigEscalationFactor.Populate<RigEscalationFactor>();
                if (sampleData != null)
                {

                    var maxYear = sampleData.FirstOrDefault().Value.Max(d => d.Year);

                    foreach (var data in sampleData)
                    {
                        FYExpenseProfile d = new FYExpenseProfile();
                        d.Value = 90;
                        d.ValueType = "Percentage";
                        d.Year = maxYear + 1;
                        d.Title = "FY" + new DateTime(maxYear + 1, 1, 1).ToString("yy");
                        data.Value.Add(d);

                        data.Save();
                    }


                }
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult AddRigEscalation(Dictionary<string, object> insert)
        {
            var ri = new ResultInfo();
            try
            {
                var doc = insert.ToBsonDocument();

                var rignm = doc.GetString("Title");

                RigEscalationFactor rr = new RigEscalationFactor();
                rr.Title = rignm;
                List<FYExpenseProfile> vals = new List<FYExpenseProfile>();

                var data = RigEscalationFactor.Populate<RigEscalationFactor>();
                var max = data.FirstOrDefault().Value.Max(x => x.Year);
                var min = data.FirstOrDefault().Value.Min(x => x.Year);

                for (int i = min; i <= max; i++)
                {
                    vals.Add(new FYExpenseProfile
                    {
                        Title = "FY" + i.ToString(),
                        Value = 0,
                        ValueType = "Percentage",
                        Year = i

                    });
                }
                rr.Value = vals;

                rr.Save();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }
    }
}