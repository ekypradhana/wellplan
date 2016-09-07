using ECIS.AppServer.Areas.WebMenu.Models;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Client.WEIS;

namespace ECIS.AppServer.Areas.WebMenu.Controllers
{
    public class MenuController : Controller
    {

        [ECAuth(WEISRoles = "Administrators")]
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetMax()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var menus = DataHelper.Populate<Menu>("Main_Menu");
                //string pMenu; string pSubMenu; int presMenu; int presSubMenu;
                //var data = MacroEconomic.Populate<MacroEconomic>(Query.EQ("Country", "Indonesia"));
                //var biz = menus.FirstOrDefault(f => f._id.Any());
                //var maxRecord = biz.Order(new Menu()).Max(f => f._id);
                //var maxYear = biz.ExchangeRate.AnnualValues.DefaultIfEmpty(new AnnualHelper()).Max(f => f.Year);
                var submenus = new Menu();
                var LastRec = menus.LastOrDefault(f => f._id.Any());
                int TotData = 0; int TotDataMenu = 0; int TotDataSubmenu = 0; string pTotData;
                TotDataMenu = menus.Count();
                foreach (var menu in menus)
                {
                    TotDataSubmenu = TotDataSubmenu + menu.Submenus.Count();
                }
                TotData = TotDataMenu + TotDataSubmenu;
                pTotData = TotData.ToString();
                if (pTotData.Length == 1)
                {
                    pTotData = "M0000" + pTotData;
                }
                else if (pTotData.Length == 2)
                {
                    pTotData = "M000" + pTotData;
                }
                else if (pTotData.Length == 3)
                {
                    pTotData = "M00" + pTotData;
                }
                else if (pTotData.Length == 2)
                {
                    pTotData = "M0" + pTotData;
                }
                else
                {
                    pTotData = "M" + pTotData;
                }

                ri.Data = pTotData;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri.Data);
        }

        #region  Save
        public JsonResult Save(Dictionary<string, string> FormSubmit)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var collection_name = FormSubmit["collection_name"];
                var parent_id = FormSubmit["menu_parent"];
                var topparent_id = FormSubmit["menu_topparent"];
                var menu_id = FormSubmit["menu_id"];
                var menu_order = 1000;
                if (!int.TryParse(FormSubmit["menu_order"], out menu_order)) menu_order = 1000;
                var mode = "";
                Menu menu = new Menu();
                if (menu_id == "")
                {
                    menu._id = "M" + SequenceNo.Get(collection_name).ClaimAsInt().ToString("D5"); 
                    //menu._id = FormSubmit["MaxValue"];
                }
                else
                {
                    mode = "edit";
                    menu._id = menu_id;

                }
                menu.Title = FormSubmit["menu_title"];
                menu.Url = FormSubmit["menu_url"];
                menu.Order = menu_order;
                if (parent_id == "")
                {
                    if (mode == "")
                    {
                        DataHelper.Save(collection_name, menu.ToBsonDocument());
                    }
                    else
                    {
                        var existingMenu = DataHelper.Populate<Menu>(collection_name, Query.EQ("_id", menu._id)).FirstOrDefault();
                        existingMenu.Title = menu.Title;
                        existingMenu.Url = menu.Url;
                        existingMenu.Order = menu.Order;
                        DataHelper.Save(collection_name, existingMenu.ToBsonDocument());
                    }
                }
                else
                {
                    var parent = DataHelper.Populate<Menu>(collection_name, Query.EQ("_id", topparent_id)).FirstOrDefault();
                    bool isAdded = false;
                    if (parent._id == parent_id && mode == "")
                    {
                        parent.Submenus.Add(menu);
                        isAdded = true;
                    }
                    else if (parent.Submenus.Count > 0 && isAdded == false)
                    {
                        isAdded = LoopSubmenus(parent_id, menu, parent.Submenus, mode);
                    }
                    if (isAdded)
                    {
                        DataHelper.Save(collection_name, parent.ToBsonDocument());
                    }
                }
                ri.Data = "Menu Saved";
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public bool LoopSubmenus(string parent_id, Menu menu, List<Menu> submenus, string mode)
        {
            var isAdded = false;
            foreach (var sub in submenus)
            {
                if (mode != "" && sub._id == menu._id)
                {
                    sub.Title = menu.Title;
                    sub.Url = menu.Url;
                    sub.Order = menu.Order;
                    isAdded = true;
                    break;
                }
                if (sub._id == parent_id && isAdded == false)
                {
                    if (mode == "")
                    {
                        sub.Submenus.Add(menu);
                        isAdded = true;
                    }
                    else
                    {
                        foreach (var obj in sub.Submenus)
                        {
                            obj.Title = menu.Title;
                            obj.Url = menu.Url;
                            obj.Order = menu.Order;
                            isAdded = true;
                            break;
                        }
                    }
                    break;
                }
                else if (sub.Submenus.Count > 0 && isAdded == false)
                {
                    isAdded = LoopSubmenus(parent_id, menu, sub.Submenus, mode);
                }

            }
            return isAdded;
        }

        #endregion
        #region Remove
        public JsonResult Remove(string id, string collection_name)
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var menus = DataHelper.Populate<Menu>(collection_name);
                foreach (var menu in menus)
                {
                    if (menu._id == id)
                    {
                        DataHelper.Delete(collection_name, Query.EQ("_id", id));
                        break;
                    }
                    if (menu.Submenus.Count > 0)
                    {
                        bool isRemoved = LoopRemoveSubmenus(id, menu.Submenus);
                        if (isRemoved)
                        {
                            DataHelper.Save(collection_name, menu.ToBsonDocument());
                        }
                    }
                }
                ri.Data = "Menu Removed";
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public bool LoopRemoveSubmenus(string id, List<Menu> submenus)
        {
            bool isRemoved = false;
            foreach (var sub in submenus)
            {
                if (sub._id == id)
                {
                    submenus.Remove(sub);
                    isRemoved = true;
                    break;
                }
                else if (sub.Submenus.Count > 0 && isRemoved == false)
                {
                    isRemoved = LoopRemoveSubmenus(id, sub.Submenus);
                }
            }
            return isRemoved;
        }

        #endregion
        #region Get
        public JsonResult GetMenu(string collection_name)
        {
            var roles = WEISPerson.GetRolesByEmail(WebTools.LoginUser.Email);

            ResultInfo ri = new ResultInfo();
            try
            {
                var result = DataHelper.Populate<Menu>(collection_name)
                    .Where(d =>
                    {
                        if (roles.Where(e => e.ToLower().Contains("admin") || e.ToLower().Contains("app-support")).Count() == 0)
                        {
                            if (d.Title.ToLower().Contains("admin")
                                //|| d.Title.ToLower().Contains("business")
                                )
                            {
                                return false;
                            }
                        }
                        return true;
                    })
                    .OrderBy(d => d.Order)
                    .ThenBy(d => d.Title)
                    .Select(d =>
                    {
                        d.RecursiveOrder();
                        return d;
                    })
                    .ToList<Menu>();

                ri.Data = result;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        #endregion

    }
}