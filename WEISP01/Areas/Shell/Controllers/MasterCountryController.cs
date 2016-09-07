using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.IO;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth(WEISRoles = "Administrators")]
    public class MasterCountryController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Master Country";
            return View("../MasterCountry/Index");
        }

        public JsonResult Populate(string Keyword = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var check = MasterCountry.Get<MasterCountry>(Query.EQ("_id", "United States"));
                if (check==null || check.Code==null || check.Currency==null || check.Continent==null)
                {
                    BatchCountries();
                }
                var data =  MasterCountry.Populate<MasterCountry>(new MasterCountryController().Like(Keyword)).OrderBy(d => d._id)
                    .Select(d =>
                    {
                        d.Name = Convert.ToString(d._id);
                        d.Continent = d.Continent;
                        d.Currency = d.Currency;
                        d.Code = d.Code;
                        return d;
                    });
                return data;
            });
        }

        public IMongoQuery Like(string Keyword = null)
        {
            Keyword = (Keyword == null ? "" : Keyword).Trim();
            if (Keyword.Equals("")) return null;

            var queries = new List<IMongoQuery>();
            queries.Add(Query.Matches("_id", new BsonRegularExpression(new Regex(Keyword.ToString().ToLower(), RegexOptions.IgnoreCase))));
            queries.Add(Query.Matches("name", new BsonRegularExpression(new Regex(Keyword.ToString().ToLower(), RegexOptions.IgnoreCase))));

            return Query.Or(queries.ToArray());
        }

        public JsonResult Get(object id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var d = MasterCountry.Get<MasterCountry>(id);
                d.Name = Convert.ToString(d._id);
                return d;
            });
        }

        public JsonResult Save(string Name,string Continent,string Currency,string Code)
        {
            try
            {
                if (MasterCountry.Populate<MasterCountry>(new MasterCountryController().Like(Name)).Count > 0)
                {
                    return Json(new { Success = false, Message = "Name already exists" }, JsonRequestBehavior.AllowGet);
                }

                new MasterCountry() { _id = Name, Name = Name,Continent = Continent,Currency = Currency,Code = Code }.Save();

                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult Update(List<MasterCountry> updated)
        {
            try
            {
                updated = (updated == null ? new List<MasterCountry>() : updated);
                var counter = 0;
                var success = false;
                var message = "";

                foreach (var each in updated)
                {
                    string id = Convert.ToString(each._id);
                    string name = Convert.ToString(each.Name);
                    string continent = Convert.ToString(each.Continent);
                    string currency = Convert.ToString(each.Currency);
                    string code = Convert.ToString(each.Code);

                    var LoBUsingThisNewName = MasterCountry.Populate<MasterCountry>(Query.EQ("_id", name));
                    DataHelper.Delete(each.TableName, Query.EQ("_id", id));
                    new MasterCountry() { _id = name, Name = name, Continent = continent, Currency = currency, Code = code }.Save();
                    
                    counter++;
                }

                success = true;
                message = "Data updated!";

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

                DataHelper.Delete(new MasterCountry().TableName, Query.EQ("_id", _id));
                success = true;

                return Json(new { Success = success, Message = message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
            };
        }

        public JsonResult getCountry()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var data = DataHelper.Populate("_temp_countries");
                //var mydata = data.Select(
                //    d=>
                //    {
                //        return new
                //        {
                //            _id = d.GetString("name"),
                //            name = d.GetString("name"),
                //            phone = d.GetString("phone"),
                //            continent = d.GetString("continent"),
                //            capital = d.GetString("capital"),
                //            currency = d.GetString("currency"),
                //            code = d.GetString("code")
                //        };

                //    }  
                //).OrderBy(x=>x.name);
                //ri.Data = mydata;
                foreach (var d in data)
                {
                    new MasterCountry()
                    {
                        _id = d.GetString("name"),
                        Name = d.GetString("name"),
                        Phone = d.GetString("phone"),
                        Continent = d.GetString("continent"),
                        Capital = d.GetString("capital"),
                        Currency = d.GetString("currency"),
                        Code = d.GetString("code")
                    }.Save();
                }
                ri.Data = "OK";
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            ri.CalcLapseTime();
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult BatchCountries()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                string takeJson = Path.Combine(Server.MapPath("~/App_Data/Temp"), "countries.json");

                string Json = System.IO.File.ReadAllText(takeJson);  
                JavaScriptSerializer ser = new JavaScriptSerializer();
                var resp = ser.Deserialize<List<TempCountry>>(Json);
                foreach (var r in resp)
                {
                    var c = new MasterCountry();
                    c._id = r.name;
                    c.Name = r.name;
                    c.Phone = r.phone;
                    c.Continent = r.continent;
                    c.Capital = r.capital;
                    c.Currency = r.currency;
                    c.Code = r.code;
                    c.Save();
                }
                ri.Data = "OK";
            }
            catch (Exception e)
            {

                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }
    }

    internal class TempCountry
    {
        public string name { get; set; }
        public string phone { get; set; }
        public string continent { get; set; }
        public string capital { get; set; }
        public string currency { get; set; }
        public string code { get; set; }
    }
}
 