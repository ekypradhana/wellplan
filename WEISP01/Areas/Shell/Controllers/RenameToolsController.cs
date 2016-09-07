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
                        if (activity.OPHistories != null && activity.OPHistories.Count() > 0)
                        {
                            foreach (var h in activity.OPHistories)
                            {
                                h.WellName = updatedWellName;
                            }
                        }
                        activity.Save();

                    }

                    foreach (var activityUpdate in WellActivityUpdate.Populate<WellActivityUpdate>(Query.EQ("WellName", originalWellName)))
                    {
                        activityUpdate.Delete();
                        activityUpdate.WellName = updatedWellName;

                        var idbefore = activityUpdate._id.ToString();

                        var _id = String.Format("W{0}S{1}A{2}D{3:yyyyMMdd}",
                                   activityUpdate.WellName.Replace(" ", "").Replace("-", ""),
                                   activityUpdate.SequenceId,
                                   activityUpdate.Phase.ActivityType.Replace(" ", "").Replace("-", ""),
                                   activityUpdate.UpdateVersion);

                        activityUpdate._id = _id;
                        activityUpdate.Save();

                        // updating Refference 2 comments
                        var datas = WEISComment.Populate<WEISComment>(
                            Query.And(
                            Query.EQ("ReferenceType", "WeeklyReport"),
                            Query.EQ("Reference1", idbefore)
                            ));

                        foreach(var t in datas)
                        {
                            t.Reference1 = _id;
                            t.Save();
                        }

                        // documents
                        foreach (var activityDocument in WellActivityDocument.Populate<WellActivityDocument>(Query.EQ("WellName", originalWellName)))
                        {
                            activityDocument.Delete();
                            activityDocument.WellName = updatedWellName;
                            activityDocument.ActivityUpdateId = activityUpdate._id.ToString();
                            activityDocument.Save();
                        }
                    }

                    foreach (var pip in WellPIP.Populate<WellPIP>(Query.EQ("WellName", originalWellName)))
                    {
                        pip.Delete();
                        pip.WellName = updatedWellName;
                        var idbefore = pip._id.ToString();

                        var _id = String.Format("W{0}S{1}P{2}", pip.WellName, pip.SequenceId, pip.PhaseNo);
                        pip._id = _id;
                        pip.Save();

                        // updating Refference 2 comments
                        var datas = WEISComment.Populate<WEISComment>(
                            Query.And(
                            Query.EQ("ReferenceType", "WellPIP"),
                            Query.EQ("Reference1", idbefore)
                            ));

                        foreach (var t in datas)
                        {
                            t.Reference1 = _id;
                            t.Save();
                        }
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
                        var _id = String.Format("W{0}S{1}A{2}", person.WellName.Replace(" ", ""), person.SequenceId, person.ActivityType.Replace(" ", ""));
                        person._id = _id;
                        person.Save();
                    }

                    foreach (var junction in WellJunction.Populate<WellJunction>(Query.EQ("OUR_WELL_ID", originalWellName)))
                    {
                        junction.Delete();
                        junction.OUR_WELL_ID = updatedWellName;
                        junction.Save();
                    }

                    // new function
                    foreach (var pi in WellActivityPhaseInfo.Populate<WellActivityPhaseInfo>(Query.EQ("WellName", originalWellName)))
                    {
                        pi.Delete();
                        pi.WellName = updatedWellName;

                        var _id = String.Format("WA{0}W{1}S{2}P{3}O{4}E{5}", pi.WellActivityId, pi.WellName, pi.SequenceId, pi.PhaseNo, pi.OPType, pi.ActivityType);
                        pi._id = _id;
                        pi.Save();
                    }

                    foreach (var mon in WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.EQ("WellName", originalWellName)))
                    {
                        var idbefore = mon._id.ToString();
                        mon.Delete();
                        mon.WellName = updatedWellName;
                        var _id = String.Format("W{0}S{1}A{2}D{3:yyyyMMdd}",
                                    mon.WellName.Replace(" ", "").Replace("-", ""),
                                    mon.SequenceId,
                                    mon.Phase.ActivityType.Replace(" ", "").Replace("-", ""),
                                    mon.UpdateVersion);
                        mon._id = _id;

                        DataHelper.Save(mon.TableName, mon.ToBsonDocument());

                        // updating Refference 2 comments
                        var datas = WEISComment.Populate<WEISComment>(
                            Query.And(
                            Query.EQ("ReferenceType", "MonthlyReport"),
                            Query.EQ("Reference2", idbefore)
                            ));
                        //var datenewref1 = mon.UpdateVersion.ToString("yyyyMMdd");
                        var newref1 = mon.WellName + "||" + mon.Phase.ActivityType + "||" + mon.SequenceId;
                        foreach (var t in datas)
                        {
                            t.Reference1 = newref1;
                            t.Reference2 = _id;
                            t.Save();
                        }

                    }

                    foreach (var aloc in BizPlanAllocation.Populate<BizPlanAllocation>(Query.EQ("WellName", originalWellName)))
                    {
                        aloc.Delete();
                        aloc.WellName = updatedWellName;

                        var _id = String.Format("W{0}A{1}R{2}O{3}S{4}", aloc.WellName, aloc.ActivityType, aloc.RigName, aloc.SaveToOP,aloc.UARigSequenceId);
                        aloc._id = _id;
                        aloc.Save();
                    }

                    foreach (var biz in BizPlanActivity.Populate<BizPlanActivity>(Query.EQ("WellName", originalWellName)))
                    {
                        biz.Delete();
                        biz.WellName = updatedWellName;

                        if (biz.OPHistories != null && biz.OPHistories.Count() > 0)
                        {
                            foreach (var h in biz.OPHistories)
                            {
                                h.WellName = updatedWellName;
                            }
                        }

                        if (biz.Phases != null && biz.Phases.Count() > 0)
                        {
                            foreach (var h in biz.Phases)
                            {
                                if (h.Allocation != null)
                                {
                                    h.Allocation.WellName = updatedWellName;
                                    var _id = String.Format("W{0}A{1}R{2}O{3}S{4}", h.Allocation.WellName, h.Allocation.ActivityType, 
                                        h.Allocation.RigName, h.Allocation.SaveToOP, h.Allocation.UARigSequenceId);
                                    h.Allocation._id = _id;

                                }
                                if (h.PhaseInfo != null)
                                {
                                    h.PhaseInfo.WellName = updatedWellName;
                                    var _id = String.Format("WA{0}W{1}S{2}P{3}O{4}E{5}", h.PhaseInfo.WellActivityId, h.PhaseInfo.WellName, h.PhaseInfo.SequenceId, h.PhaseInfo.PhaseNo, h.PhaseInfo.OPType, h.PhaseInfo.ActivityType);
                                    h.PhaseInfo._id = _id;

                                }
                            }
                        }
                        DataHelper.Save( biz.TableName, biz.ToBsonDocument());
                        biz.Save();
                    }

                    foreach (var log in DataHelper.Populate("LogLatestLS", Query.EQ("Well_Name", originalWellName)))
                    {
                        DataHelper.Delete("LogLatestLS", Query.EQ("Well_Name", originalWellName)); /// log.Delete();
                        log.Set("Well_Name", updatedWellName);

                        DataHelper.Save("LogLatestLS", log);
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
                        rigName.Name = updatedRigName;
                        rigName.Save();
                    }

                    foreach (var activity in WellActivity.Populate<WellActivity>(Query.EQ("RigName", originalRigName)))
                    {
                        activity.Delete();
                        activity.RigName = updatedRigName;

                        if (activity.OPHistories != null && activity.OPHistories.Count() > 0)
                        {
                            foreach (var h in activity.OPHistories)
                            {
                                h.RigName = updatedRigName;
                            }
                        }

                        activity.Save();
                    }


                    foreach (var pip in WellPIP.Populate<WellPIP>(Query.EQ("WellName", originalRigName + "_CR")))
                    {
                        pip.Delete();
                        pip.WellName = updatedRigName + "_CR";

                        var _id = String.Format("W{0}S{1}P{2}", pip.WellName, pip.SequenceId, pip.PhaseNo);
                        pip._id = _id;
                        pip.Save();
                    }

                    foreach (var pi in WellActivityPhaseInfo.Populate<WellActivityPhaseInfo>(Query.EQ("RigName", originalRigName)))
                    {
                        pi.Delete();
                        pi.RigName = updatedRigName;
                        pi.Save();
                    }


                    foreach (var aloc in BizPlanAllocation.Populate<BizPlanAllocation>(Query.EQ("RigName", originalRigName)))
                    {
                        aloc.Delete();
                        aloc.RigName = updatedRigName;

                        var _id = String.Format("W{0}A{1}R{2}O{3}S{4}", aloc.WellName, aloc.ActivityType, aloc.RigName, aloc.SaveToOP, aloc.UARigSequenceId);
                        aloc._id = _id;
                        aloc.Save();
                    }

                    foreach (var biz in BizPlanActivity.Populate<BizPlanActivity>(Query.EQ("RigName", originalRigName)))
                    {
                        biz.Delete();
                        biz.RigName = updatedRigName;

                        if (biz.OPHistories != null && biz.OPHistories.Count() > 0)
                        {
                            foreach (var h in biz.OPHistories)
                            {
                                h.RigName = updatedRigName;
                            }
                        }

                        if (biz.Phases != null && biz.Phases.Count() > 0)
                        {
                            foreach (var h in biz.Phases)
                            {
                                if (h.Allocation != null)
                                {
                                    h.Allocation.RigName = updatedRigName;
                                    var _id = String.Format("W{0}A{1}R{2}O{3}S{4}", h.Allocation.WellName, h.Allocation.ActivityType,
                                        h.Allocation.RigName, h.Allocation.SaveToOP, h.Allocation.UARigSequenceId);
                                    h.Allocation._id = _id;

                                }
                                if (h.PhaseInfo != null)
                                {
                                    h.PhaseInfo.RigName = updatedRigName;
                                    var _id = String.Format("WA{0}W{1}S{2}P{3}O{4}E{5}", h.PhaseInfo.WellActivityId, h.PhaseInfo.WellName, h.PhaseInfo.SequenceId, h.PhaseInfo.PhaseNo, h.PhaseInfo.OPType, h.PhaseInfo.ActivityType);
                                    h.PhaseInfo._id = _id;

                                }
                                if(h.Estimate != null)
                                {
                                    h.Estimate.RigName = updatedRigName;
                                }
                            }
                        }
                        DataHelper.Save(biz.TableName, biz.ToBsonDocument());
                        biz.Save();
                    }


                    foreach (var log in DataHelper.Populate("LogLatestLS", Query.EQ("Rig_Name", originalRigName)))
                    {
                        DataHelper.Delete("LogLatestLS", Query.EQ("Rig_Name", originalRigName)); /// log.Delete();
                        log.Set("Rig_Name", updatedRigName);
                        DataHelper.Save("LogLatestLS", log);
                    }

                    foreach (var log in RigRatesNew.Populate<RigRatesNew>(Query.EQ("Title", originalRigName)))
                    {
                        log.Delete();
                        log.Title = updatedRigName;
                        var _id = String.Format("T{0}", log.Title);
                        log._id = _id;
                        log.Save();
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

                        if (activity.OPHistories != null && activity.OPHistories.Count() > 0)
                        {
                            foreach (var h in activity.OPHistories)
                            {
                                h.UARigSequenceId = updatedUARigSequenceId;
                            }
                        }
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
                        var idbefore = activityUpdate._id.ToString();
                        var _id = String.Format("W{0}S{1}A{2}D{3:yyyyMMdd}",
                                   activityUpdate.WellName.Replace(" ", "").Replace("-", ""),
                                   activityUpdate.SequenceId,
                                   activityUpdate.Phase.ActivityType.Replace(" ", "").Replace("-", ""),
                                   activityUpdate.UpdateVersion);

                        activityUpdate._id = _id;
                        activityUpdate.Save();

                        // updating Refference 2 comments
                        var datas = WEISComment.Populate<WEISComment>(
                            Query.And(
                            Query.EQ("ReferenceType", "WeeklyReport"),
                            Query.EQ("Reference1", idbefore)
                            ));

                        foreach (var t in datas)
                        {
                            t.Reference1 = _id;
                            t.Save();
                        }
                    }

                    foreach (var person in WEISPerson.Populate<WEISPerson>(queryForActivityActual))
                    {
                        person.Delete();
                        //person.SequenceId = updatedUARigSequenceId;
                        //person.Save();

                        person.SequenceId = updatedUARigSequenceId;
                        var _id = String.Format("W{0}S{1}A{2}", person.WellName.Replace(" ", ""), person.SequenceId, person.ActivityType.Replace(" ", ""));
                        person._id = _id;
                        person.Save();
                    }

                    foreach (var pip in WellPIP.Populate<WellPIP>(queryForActivityActual))
                    {
                        //pip.Delete();
                        //pip.SequenceId = updatedUARigSequenceId;
                        //pip.Save();

                        pip.Delete();
                        pip.SequenceId = updatedUARigSequenceId;
                        var idbefore = pip._id.ToString();

                        var _id = String.Format("W{0}S{1}P{2}", pip.WellName, pip.SequenceId, pip.PhaseNo);
                        pip._id = _id;
                        pip.Save();

                        // updating Refference 2 comments
                        var datas = WEISComment.Populate<WEISComment>(
                            Query.And(
                            Query.EQ("ReferenceType", "WellPIP"),
                            Query.EQ("Reference1", idbefore)
                            ));

                        foreach (var t in datas)
                        {
                            t.Reference1 = _id;
                            t.Save();
                        }
                    }

                    // new function
                    queryForActivityActual = Query.And(Query.EQ("WellName", WellName), Query.EQ("SequenceId", originalUARigSequenceId));
                    foreach (var pi in WellActivityPhaseInfo.Populate<WellActivityPhaseInfo>(queryForActivityActual))
                    {
                        pi.Delete();
                        pi.SequenceId = updatedUARigSequenceId;

                        var _id = String.Format("WA{0}W{1}S{2}P{3}O{4}E{5}", pi.WellActivityId, pi.WellName, pi.SequenceId, pi.PhaseNo, pi.OPType, pi.ActivityType);
                        pi._id = _id;
                        pi.Save();
                    }

                    foreach (var activityUpdate in WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(queryForActivityActual))
                    {
                        activityUpdate.Delete();
                        activityUpdate.SequenceId = updatedUARigSequenceId;
                        var idbefore = activityUpdate._id.ToString();
                        var _id = String.Format("W{0}S{1}A{2}D{3:yyyyMMdd}",
                                        activityUpdate.WellName.Replace(" ", "").Replace("-", ""),
                                        activityUpdate.SequenceId,
                                        activityUpdate.Phase.ActivityType.Replace(" ", "").Replace("-", ""),
                                        activityUpdate.UpdateVersion);

                        activityUpdate._id = _id;

                        DataHelper.Save(activityUpdate.TableName, activityUpdate.ToBsonDocument());
                        //activityUpdate.Save();

                        // updating Refference 2 comments
                        var datas = WEISComment.Populate<WEISComment>(
                            Query.And(
                            Query.EQ("ReferenceType", "MonthlyReport"),
                            Query.EQ("Reference1", idbefore)
                            ));

                        foreach (var t in datas)
                        {
                            t.Reference1 = _id;
                            t.Save();
                        }
                    }

                    foreach (var aloc in BizPlanAllocation.Populate<BizPlanAllocation>(queryForActivity))
                    {
                        aloc.Delete();
                        aloc.UARigSequenceId = updatedUARigSequenceId;

                        var _id = String.Format("W{0}A{1}R{2}O{3}S{4}", aloc.WellName, aloc.ActivityType, aloc.RigName, aloc.SaveToOP, aloc.UARigSequenceId);
                        aloc._id = _id;
                        aloc.Save();
                    }

                    foreach (var biz in BizPlanActivity.Populate<BizPlanActivity>(queryForActivity))
                    {
                        biz.Delete();
                        biz.UARigSequenceId = updatedUARigSequenceId;

                        if (biz.OPHistories != null && biz.OPHistories.Count() > 0)
                        {
                            foreach (var h in biz.OPHistories)
                            {
                                h.UARigSequenceId = updatedUARigSequenceId;
                            }
                        }

                        if (biz.Phases != null && biz.Phases.Count() > 0)
                        {
                            foreach (var h in biz.Phases)
                            {
                                if (h.Allocation != null)
                                {
                                    h.Allocation.UARigSequenceId = updatedUARigSequenceId;
                                    var _id = String.Format("W{0}A{1}R{2}O{3}S{4}", h.Allocation.WellName, h.Allocation.ActivityType,
                                        h.Allocation.RigName, h.Allocation.SaveToOP, h.Allocation.UARigSequenceId);
                                    h.Allocation._id = _id;

                                }
                                if (h.PhaseInfo != null)
                                {
                                    h.PhaseInfo.SequenceId = updatedUARigSequenceId;
                                    var _id = String.Format("WA{0}W{1}S{2}P{3}O{4}E{5}", h.PhaseInfo.WellActivityId, h.PhaseInfo.WellName, h.PhaseInfo.SequenceId, h.PhaseInfo.PhaseNo, h.PhaseInfo.OPType, h.PhaseInfo.ActivityType);
                                    h.PhaseInfo._id = _id;

                                }
                            }
                        }
                        DataHelper.Save(biz.TableName, biz.ToBsonDocument());
                        biz.Save();
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