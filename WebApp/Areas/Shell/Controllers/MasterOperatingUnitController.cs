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
    public class MasterOperatingUnitController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Master Operating Unit";
            return View("../MasterRegion/Index");
        }

        public JsonResult Populate(string Keyword = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return MasterOperatingUnit.Populate<MasterOperatingUnit>(new MasterRigNameController().Like(Keyword)).OrderBy(d => d._id)
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
                var d = MasterOperatingUnit.Get<MasterOperatingUnit>(id);
                d.Name = Convert.ToString(d._id);
                return d;
            });
        }

        public JsonResult Save(string Name)
        {
            try
            {
                if (MasterOperatingUnit.Populate<MasterOperatingUnit>(new MasterRigNameController().Like(Name)).Count > 0)
                {
                    return Json(new { Success = false, Message = "Name already exists" }, JsonRequestBehavior.AllowGet);
                }

                new MasterOperatingUnit() { _id = Name }.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult Update(List<MasterOperatingUnit> updated)
        {
            try
            {
                updated = (updated == null ? new List<MasterOperatingUnit>() : updated);
                var counter = 0;
                var success = false;
                var message = "";

                foreach (var each in updated)
                {
                    string id = Convert.ToString(each._id);
                    string name = Convert.ToString(each.Name);

                    var activities = WellActivity.Populate<WellActivity>(Query.EQ("OperatingUnit", id));
                    if (activities.Count > 0) continue;

                    var rigNames = MasterOperatingUnit.Populate<MasterOperatingUnit>(Query.EQ("_id", id));
                    if (rigNames.Count == 0) continue;

                    var rigNamesUsingThisNewName = MasterOperatingUnit.Populate<MasterOperatingUnit>(Query.EQ("_id", name));
                    if (rigNamesUsingThisNewName.Count > 0) continue;

                    DataHelper.Delete(each.TableName, Query.EQ("_id", id));
                    new MasterOperatingUnit() { _id = name }.Save();

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

                var activities = WellActivity.Populate<WellActivity>(Query.EQ("OperatingUnit", _id));
                if (activities.Count > 0)
                {
                    success = false;
                    message = "Data that used in activity cannot be deleted";
                }
                else
                {
                    DataHelper.Delete(new MasterOperatingUnit().TableName, Query.EQ("_id", _id));
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