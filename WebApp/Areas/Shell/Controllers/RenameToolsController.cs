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
    [ECAuth(WEISRoles = "Administrators")]
    public class RenameToolsController : Controller
    {
        public ActionResult Well()
        {
            ViewBag.Title = "Rename Tools - Well";
            ViewBag.Prefix = "Well";
            ViewBag.GridColumnField = "WellName";
            ViewBag.GridColumnTitle = "Well Name";
            return View("../RenameTools/Index");
        }

        public IMongoQuery Like(string what, string Keyword = null)
        {
            Keyword = (Keyword == null ? "" : Keyword).Trim();
            if (Keyword.Equals("")) return null;

            var queries = new List<IMongoQuery>();
            queries.Add(Query.Matches(what, new BsonRegularExpression(new Regex(Keyword.ToString().ToLower(), RegexOptions.IgnoreCase))));

            return Query.Or(queries.ToArray());
        }

        public JsonResult WellPopulate(string Keyword = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var queries = new List<IMongoQuery>();
                queries.Add(Query.NE("IsVirtualWell", true));

                var like = Like("_id", Keyword);
                if (like != null) queries.Add(like);

                var query = Query.And(queries.ToArray());

                return WellInfo.Populate<WellInfo>(query)
                    .Select(d => new { _id = d._id, WellName = d._id })
                    .OrderBy(d => d.WellName);
            });
        }

        public JsonResult WellUpdate(List<Dictionary<string, object>> updated = null)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                updated = (updated == null ? new List<Dictionary<string, object>>() : updated);

                var ok = true;

                foreach (var well in updated)
                {
                    var wellName = well["WellName"].ToString().Trim();

                    if (WellInfo.Populate<WellInfo>(Query.EQ("_id", wellName)).Count() > 0)
                    {
                        ok = false;
                        break;
                    }
                }

                if (!ok)
                {
                    ri.Result = "NOK";
                    ri.Message = "Failed to save. There are already well with same name";
                    return MvcTools.ToJsonResult(ri);
                }

                foreach (var well in updated)
                {
                    var originalWellName = well["_id"].ToString();
                    var updatedWellName = well["WellName"].ToString().Trim();

                    foreach (var wellInfo in WellInfo.Populate<WellInfo>(Query.EQ("_id", originalWellName)))
                    {
                        wellInfo.Delete();
                        wellInfo._id = updatedWellName;
                        wellInfo.Save();
                    }

                    foreach (var activity in WellActivity.Populate<WellActivity>(Query.EQ("WellName", originalWellName)))
                    {
                        activity.Delete();
                        activity.WellName = updatedWellName;
                        activity.Save();
                    }

                    foreach (var activityUpdate in WellActivityUpdate.Populate<WellActivityUpdate>(Query.EQ("WellName", originalWellName)))
                    {
                        activityUpdate.Delete();
                        activityUpdate.WellName = updatedWellName;
                        activityUpdate.Save();
                    }

                    foreach (var pip in WellPIP.Populate<WellPIP>(Query.EQ("WellName", originalWellName)))
                    {
                        pip.Delete();
                        pip.WellName = updatedWellName;
                        pip.Save();
                    }

                    foreach (var activityDocument in WellActivityDocument.Populate<WellActivityDocument>(Query.EQ("WellName", originalWellName)))
                    {
                        activityDocument.Delete();
                        activityDocument.WellName = updatedWellName;
                        activityDocument.Save();
                    }

                    foreach (var actual in WellActivityActual.Populate<WellActivityActual>(Query.EQ("WellName", originalWellName)))
                    {
                        actual.Delete();
                        actual.WellName = updatedWellName;
                        actual.Save();
                    }

                    foreach (var person in WEISPerson.Populate<WEISPerson>(Query.EQ("WellName", originalWellName)))
                    {
                        person.Delete();
                        person.WellName = updatedWellName;
                        person.Save();
                    }

                    foreach (var junction in WellJunction.Populate<WellJunction>(Query.EQ("OUR_WELL_ID", originalWellName)))
                    {
                        junction.Delete();
                        junction.OUR_WELL_ID = updatedWellName;
                        junction.Save();
                    }
                }
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public ActionResult Rig()
        {
            ViewBag.Title = "Rename Tools - Rig";
            ViewBag.Prefix = "Rig";
            ViewBag.GridColumnField = "RigName";
            ViewBag.GridColumnTitle = "Rig Name";
            return View("../RenameTools/Index");
        }

        public JsonResult RigPopulate(string Keyword = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return MasterRigName.Populate<MasterRigName>(Like("_id", Keyword))
                    .Select(d => new { _id = d._id, RigName = d._id })
                    .OrderBy(d => d.RigName);
            });
        }

        public JsonResult RigUpdate(List<Dictionary<string, object>> updated = null)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                updated = (updated == null ? new List<Dictionary<string, object>>() : updated);

                var ok = true;

                foreach (var rig in updated)
                {
                    var rigName = rig["RigName"].ToString().Trim();
                    if (MasterRigName.Populate<MasterRigName>(Query.EQ("_id", rigName)).Count() > 0)
                    {
                        ok = false;
                        break;
                    }
                }

                if (!ok)
                {
                    ri.Result = "NOK";
                    ri.Message = "Failed to save. There are already rig with same name";
                    return MvcTools.ToJsonResult(ri);
                }

                foreach (var rig in updated)
                {
                    var originalRigName = rig["_id"].ToString();
                    var updatedRigName = rig["RigName"].ToString().Trim();

                    foreach (var rigName in MasterRigName.Populate<MasterRigName>(Query.EQ("_id", originalRigName)))
                    {
                        rigName.Delete();
                        rigName._id = updatedRigName;
                        rigName.Save();
                    }

                    foreach (var activity in WellActivity.Populate<WellActivity>(Query.EQ("RigName", originalRigName)))
                    {
                        activity.Delete();
                        activity.RigName = updatedRigName;
                        activity.Save();
                    }
                }
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public ActionResult Sequence()
        {
            return View();
        }

        public JsonResult SequencePopulate(string Keyword = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                IMongoQuery query = null;
                Keyword = (Keyword == null ? "" : Keyword);

                if (!Keyword.Equals(""))
                {
                    var queries = new IMongoQuery[] { Like("RigName", Keyword), Like("WellName", Keyword), Like("UARigSequenceId", Keyword) };
                    query = Query.Or(queries);
                }

                return WellActivity.Populate<WellActivity>(query)
                    .Select(d => new
                    {
                        RigName = d.RigName,
                        WellName = d.WellName,
                        UARigSequenceId = d.UARigSequenceId,
                        OldUARigSequenceId = d.UARigSequenceId
                    });
            });
        }

        public JsonResult SequenceUpdate(List<Dictionary<string, object>> updated = null)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                updated = (updated == null ? new List<Dictionary<string, object>>() : updated);

                foreach (var rig in updated)
                {
                    var RigName = rig["RigName"].ToString();
                    var WellName = rig["WellName"].ToString();
                    var originalUARigSequenceId = rig["OldUARigSequenceId"].ToString();
                    var updatedUARigSequenceId = rig["UARigSequenceId"].ToString().Trim();

                    var queryForActivity = Query.And(Query.EQ("WellName", WellName), Query.EQ("UARigSequenceId", originalUARigSequenceId));
                    foreach (var activity in WellActivity.Populate<WellActivity>(queryForActivity))
                    {
                        activity.Delete();
                        activity.UARigSequenceId = updatedUARigSequenceId;
                        activity.Save();
                    }

                    var queryForActivityActual = Query.And(Query.EQ("WellName", WellName), Query.EQ("SequenceId", originalUARigSequenceId));
                    foreach (var activityActual in WellActivityActual.Populate<WellActivityActual>(queryForActivityActual))
                    {
                        activityActual.Delete();
                        activityActual.SequenceId = updatedUARigSequenceId;
                        activityActual.Save();
                    }

                    foreach (var activityUpdate in WellActivityUpdate.Populate<WellActivityUpdate>(queryForActivityActual))
                    {
                        activityUpdate.Delete();
                        activityUpdate.SequenceId = updatedUARigSequenceId;
                        activityUpdate.Save();
                    }

                    foreach (var person in WEISPerson.Populate<WEISPerson>(queryForActivityActual))
                    {
                        person.Delete();
                        person.SequenceId = updatedUARigSequenceId;
                        person.Save();
                    }

                    foreach (var pip in WellPIP.Populate<WellPIP>(queryForActivityActual))
                    {
                        pip.Delete();
                        pip.SequenceId = updatedUARigSequenceId;
                        pip.Save();
                    }
                }
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }
    }
}