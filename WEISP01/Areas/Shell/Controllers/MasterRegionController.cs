﻿using System;
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
    public class MasterRegionController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Master Region";
            return View("../MasterRegion/Index");
        }

        public JsonResult Populate(string Keyword = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return MasterRegion.Populate<MasterRegion>(new MasterRigNameController().Like(Keyword)).OrderBy(d => d._id)
                    .Select(d =>
                    {
                        d.Name = Convert.ToString(d._id);
                        return d;
                    });
            });
        }

        public JsonResult Get(object id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var d = MasterRegion.Get<MasterRegion>(id);
                d.Name = Convert.ToString(d._id);
                return d;
            });
        }

        public JsonResult Save(string Name)
        {
            try
            {
                if (MasterRegion.Populate<MasterRegion>(new MasterRigNameController().Like(Name)).Count > 0)
                {
                    return Json(new { Success = false, Message = "Name already exists" }, JsonRequestBehavior.AllowGet);
                }

                new MasterRegion() { _id = Name }.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult Update(List<MasterRegion> updated)
        {
            try
            {
                updated = (updated == null ? new List<MasterRegion>() : updated);
                var counter = 0;
                var success = false;
                var message = "";

                foreach (var each in updated)
                {
                    string id = Convert.ToString(each._id);
                    string name = Convert.ToString(each.Name);

                    var activities = WellActivity.Populate<WellActivity>(Query.EQ("Region", id));
                    if (activities.Count > 0) continue;

                    var rigNames = MasterRegion.Populate<MasterRegion>(Query.EQ("_id", id));
                    if (rigNames.Count == 0) continue;

                    var rigNamesUsingThisNewName = MasterRegion.Populate<MasterRegion>(Query.EQ("_id", name));
                    if (rigNamesUsingThisNewName.Count > 0) continue;

                    DataHelper.Delete(each.TableName, Query.EQ("_id", id));
                    new MasterRegion() { _id = name }.Save();

                    counter++;
                }

                if (counter == 0)
                {
                    success = false;
                    message = "Data that used in activity cannot be updated";
                }
                else if (counter == updated.Count)
                {
                    success = true;
                    message = "Data updated!";
                }
                else
                {
                    success = false;
                    message = "Some data failed to be updated because used on activity";
                }

                return Json(new { Success = success, Message = message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult Delete(string _id)
        {
            try
            {
                var success = false;
                var message = "";

                var activities = WellActivity.Populate<WellActivity>(Query.EQ("Region", _id));
                if (activities.Count > 0)
                {
                    success = false;
                    message = "Data that used in activity cannot be deleted";
                }
                else
                {
                    DataHelper.Delete(new MasterRegion().TableName, Query.EQ("_id", _id));
                    success = true;
                }

                return Json(new { Success = success, Message = message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }
    }
}