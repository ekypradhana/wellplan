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
    public class MasterThemeController : Controller
    {
        // GET: Shell/AdminWell
        public ActionResult Index()
        {
            return View();
        }
        public JsonResult Populate()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return WellPIPThemes.Populate<WellPIPThemes>();
            });
        }
        public JsonResult Get(object id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return WellPIPThemes.Get<WellPIPThemes>(id);
            });
        }

        public JsonResult Save(string Name)
        {
            try
            {

                WellPIPThemes wpc = new WellPIPThemes();
                wpc.Name = Name;
                wpc.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }
        public JsonResult Update(List<WellPIPThemes> updated)
        {
            try
            {
                IMongoQuery q = null;
                foreach (var x in updated)
                {
                    //select old name first
                    q = Query.EQ("_id", Convert.ToInt32(x._id));
                    WellPIPThemes wpc = WellPIPThemes.Get<WellPIPThemes>(q);

                    string OldName = wpc.Name;

                    q = Query.EQ("Elements.Theme", OldName);
                    WellPIP wp = WellPIP.Get<WellPIP>(q);
                    if (wp == null)
                    {
                        x.Save();
                    }
                }
                return Json(new { Success = true, data = updated }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }
        public JsonResult Delete(int _id)
        {
            try
            {
                IMongoQuery q = null;
                q = Query.EQ("_id", _id);
                WellPIPThemes wpc = WellPIPThemes.Get<WellPIPThemes>(q);

                string OldName = wpc.Name;

                q = Query.EQ("Elements.Theme", OldName);
                WellPIP wp = WellPIP.Get<WellPIP>(q);
                if (wp == null)
                {
                    DataHelper.Delete(wpc.TableName, Query.EQ("_id", _id));
                    return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    throw new Exception("Classification cannot be delete because it's already used!");
                }
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

    }
}