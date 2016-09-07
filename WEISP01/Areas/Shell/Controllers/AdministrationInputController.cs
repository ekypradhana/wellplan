using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
using ECIS.Biz.Common;
using System.Text;
using System.Text.RegularExpressions;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    //Business Plan => Administration Input
    public class AdministrationInputController : Controller
    {
        //
        // GET: /Shell/AdministrationInput/
        public ActionResult Index()
        {
            //return View();
            return RedirectToAction("Index", "AdministrationInput/EconomicInflation");
        }

        public ActionResult EconomicInflation()
        {
            return View();
        }

        public ActionResult MarketEscalationFactor()
        {
            return View();
        }

        public ActionResult MaturityRiskMatrix()
        {
            return View();
        }

        public ActionResult CapitalizedStaffOverhead()
        {
            return View();
        }

        public ActionResult LongLeadItems()
        {
            return View();
        }

        public ActionResult RigRate()
        {
            return View();
            //return View("RigRateView");
        }

        public JsonResult UpdateRigRate(List<Dictionary<string, object>> changes)
        {
            var ri = new ResultInfo();
            try
            {

                foreach (var c in changes)
                {
                    var doc = c.ToBsonDocument();
                    var title = doc.GetString("Title");

                    var t = RigRates.Get<RigRates>(Query.EQ("Title", title));
                    foreach (var val in t.Value)
                    {
                        val.Value = Convert.ToDouble(doc.GetString("Year_" + val.Year.ToString())) * 1000;
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

        public JsonResult DeleteRigRate(string id)
        {
            var ri = new ResultInfo();
            try
            {
                var rigRate = RigRatesNew.Get<RigRatesNew>(Query.EQ("_id", id));
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

        public JsonResult GetRigRateData(string keyword = "")
        {
            var ri = new ResultInfo();
            try
            {
                var now = DateTime.Now;
                var minYear = now.Year;
                var maxYear = now.Year;

                IMongoQuery query = null;
                var rigs = RigRates.Populate<RigRates>(query);

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
                        each.Set("Year_" + i, value.Value / 1000);
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

        public JsonResult GetRigRateData2(string type = "")
        {
            type = (type ?? "");

            var ri = new ResultInfo();
            try
            {
                var now = DateTime.Now;
                var minYear = now.Year;
                var maxYear = now.Year;

                IMongoQuery query = null;
                var rigs = RigRatesNew.Populate<RigRatesNew>(query)
                    .Select(d =>
                    {
                        if (d.Values == null)
                            d.Values = new List<RigRateValue>();

                        if (!type.Equals(""))
                            d.Values = d.Values.Where(e => type.Equals(e.Type)).ToList();

                        return d;
                    });

                var max = rigs.Max(d => d.Values.Count);
                rigs = rigs.Select(d =>
                {
                    while (d.Values.Count < max)
                        d.Values.Add(new RigRateValue() { Type = "", Period = new DateRange() });

                    return d;
                }).ToList();

                ri.Data = rigs.OrderBy(d => d.Title);
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        //Update Rig

        public JsonResult ViewEditRate2(string id)
        {
            var ri = new ResultInfo();
            try
            {
                IMongoQuery query = Query.EQ("_id", id);
                var rigs = RigRatesNew.Get<RigRatesNew>(query);
                if (rigs != null)
                {
                    foreach (var vl in rigs.Values)
                    {
                        vl.Period.Start = Tools.ToUTC(vl.Period.Start, true);
                        vl.Period.Finish = Tools.ToUTC(vl.Period.Finish, true);
                    }
                }
                ri.Data = rigs;
            }
            catch (Exception)
            {

                throw;
            }
            return MvcTools.ToJsonResult(ri);
        }

        private BsonDocument ValidatePeriod(RigRatesNew RigRate, string Type)
        {
            var values = RigRate.Values
                .Where(d => Type.Equals(d.Type))
                //.OrderBy(d => d.Period.Start)
                //.ThenBy(d => d.Period.Finish)
                .ToList();

            if (values.Count == 0)
            {
                return new
                {
                    Success = true,
                    Message = ""
                }.ToBsonDocument();
            }

            var isFine = true;

            var currentPeriodStart = values[0].Period.Start;
            var currentPeriodFinish = values[0].Period.Finish;
            var prevPeriodStart = values[0].Period.Start;
            var prevPeriodFinish = values[0].Period.Finish;

            for (var i = 0; i < values.Count; i++)
            {
                var d = values[i];

                if (i == 0 || !isFine)
                {
                    continue;
                }

                prevPeriodStart = currentPeriodStart;
                prevPeriodFinish = currentPeriodFinish;

                var diff = (d.Period.Start - prevPeriodFinish).Days;

                currentPeriodStart = d.Period.Start;
                currentPeriodFinish = d.Period.Finish;

                if (diff != 1)
                {
                    isFine = false;
                }
            }

            if (!isFine)
            {
                var message = "";

                var finalDiff = (currentPeriodStart - prevPeriodFinish).Days;
                if (finalDiff != 0)
                    message = (Math.Abs(finalDiff) - 1) + " days, ";

                var firstPeriod = prevPeriodStart.ToString("dd-MMM-yyyy") + " ~ " + prevPeriodFinish.ToString("dd-MMM-yyyy");
                var secondPeriod = currentPeriodStart.ToString("dd-MMM-yyyy") + " ~ " + currentPeriodFinish.ToString("dd-MMM-yyyy");

                if (finalDiff > 0)
                    message = "There is gap " + message + " on " + firstPeriod + " and " + secondPeriod + ", type " + Type;
                else
                    message = "Period is crossed " + message + "on " + firstPeriod + " and " + secondPeriod + ", type " + Type;

                return new
                {
                    Success = false,
                    Message = message
                }.ToBsonDocument();
            }

            return new
            {
                Success = true,
                Message = ""
            }.ToBsonDocument();
        }

        public JsonResult SaveRigRate(bool isNewData, RigRatesNew RigRate)
        {
            var ri = new ResultInfo();
            try
            {
                //(RigRate.Values ?? new List<RigRateValue>()).ForEach(d =>
                //{
                //    d.Period = new DateRange()
                //    {
                //        Start = Tools.ToUTC((d.Period ?? new DateRange()).Start),
                //        Finish = Tools.ToUTC((d.Period ?? new DateRange()).Finish)
                //    };
                //});

                var validatorActive = ValidatePeriod(RigRate, "ACTIVE");
                if (!validatorActive.GetBool("Success"))
                {
                    ri.PushException(new Exception(validatorActive.GetString("Message")));
                    return MvcTools.ToJsonResult(ri);
                }

                var validatorIdle = ValidatePeriod(RigRate, "IDLE");
                if (!validatorIdle.GetBool("Success"))
                {
                    ri.PushException(new Exception(validatorIdle.GetString("Message")));
                    return MvcTools.ToJsonResult(ri);
                }

                if (RigRate.Values == null)
                    RigRate.Values = new List<RigRateValue>();

                RigRate.Values.ForEach(d =>
                {
                    d.Period = new DateRange()
                    {
                        Start = Tools.ToUTC((d.Period ?? new DateRange()).Start),
                        Finish = Tools.ToUTC((d.Period ?? new DateRange()).Finish)
                    };
                });

                RigRate.Save();

                AdministrationInput.RecalculateBusPlan();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RemoveRigRatePeriod(string id, int index)
        {
            var ri = new ResultInfo();
            try
            {
                var rigRate = RigRatesNew.Get<RigRatesNew>(Query.EQ("_id", id));
                if (rigRate != null)
                {
                    rigRate.Values.RemoveAt(index);
                    rigRate.Save();
                }

                ri.Data = rigRate;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetProrateAllocation(string Type, string Keyword = "")
        {
            Keyword = (Keyword ?? "");

            var ri = new ResultInfo();
            try
            {
                #region auto populate default value for non exist rigRate
                var rignameexis = RigRatesNew.Populate<RigRatesNew>(null, sort: new SortByBuilder().Ascending("RigName"));
                var rignamerate = rignameexis.Select(x => x.Title);
                var rignamesall = DataHelper.Populate("WEISRigNames").Select(x => BsonHelper.GetString(x, "_id"));

                List<string> rigNotAvail = new List<string>();
                foreach (var t in rignamesall)
                {
                    if (!rignamerate.Contains(t))
                        rigNotAvail.Add(t);
                }

                if (rigNotAvail.Count > 0)
                {
                    foreach (var t in rigNotAvail)
                    {
                        RigRatesNew rn = new RigRatesNew();
                        rn.Title = t;
                        rn.Values = new List<RigRateValue>();

                        // active 
                        RigRateValue rv = new RigRateValue();
                        rv.Period = new DateRange(new DateTime(2015, 1, 1), new DateTime(2030, 1, 1));
                        rv.Type = "ACTIVE";
                        rv.Value = 0;
                        rv.ValueType = "Absolute";
                        rn.Values.Add(rv);

                        // active 
                        rv = new RigRateValue();
                        rv.Period = new DateRange(new DateTime(2015, 1, 1), new DateTime(2030, 1, 1));
                        rv.Type = "IDLE";
                        rv.Value = 0;
                        rv.ValueType = "Absolute";
                        rn.Values.Add(rv);

                        rn.Save();
                    }


                }

                #endregion

                var result = RigRatesNew.Populate<RigRatesNew>(null, sort: new SortByBuilder().Ascending("RigName"))
                    .Where(x=>x.Title !=null)
                    .Select(d =>
                    {
                        var doc = new { _id = d._id.ToString(), Title = d.Title }.ToBsonDocument();
                        
                        var values = d.Calculate(Type);

                        foreach (KeyValuePair<int, double> each in values)
                        {
                            doc.Set("Year_" + each.Key, each.Value);
                        }
                         
                        return doc.ToDictionary();
                    });

                var now = DateTime.Now;

                var minYear = result.Min(d => d.Keys
                    .Where(e => e.Contains("Year_"))
                    .DefaultIfEmpty("Year_" + now.Year)
                    .Min(e => Convert.ToDouble(e.Replace("Year_", ""))));

                var maxYear = result.Max(d => d.Keys
                    .Where(e => e.Contains("Year_"))
                    .DefaultIfEmpty("Year_" + now.Year)
                    .Max(e => Convert.ToDouble(e.Replace("Year_", ""))));

                result = result.Select(d =>
                {
                    for (var i = minYear; i <= maxYear; i++)
                    {
                        var key = "Year_" + i;
                        if (!d.ContainsKey(key))
                        {
                            d[key] = 0.0;
                        }
                    }

                    return d;
                })
                .Where(d =>
                {
                    if (Keyword.Trim() != "")
                    {
                        return Convert.ToString(d["Title"]).ToLower().Contains(Keyword.ToLower());
                    }

                    return true;
                })
                .OrderBy(d => d["Title"]).ToList();

                ri.Data = new
                {
                    Items = result,
                    YearMax = maxYear,
                    YearMin = minYear
                };
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public ActionResult RigRateView()
        {
            return View();
        }

        public JsonResult AddRigRateYear()
        {
            var ri = new ResultInfo();
            try
            {
                var sampleData = RigRates.Populate<RigRates>();
                if (sampleData != null)
                {

                    var maxYear = sampleData.FirstOrDefault().Value.Max(d => d.Year);

                    foreach (var data in sampleData)
                    {
                        FYExpenseProfile d = new FYExpenseProfile();
                        d.Value = 0;
                        d.ValueType = "Absolute";
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

        public JsonResult AddRigRate(Dictionary<string, object> insert)
        {
            var ri = new ResultInfo();
            try
            {
                var doc = insert.ToBsonDocument();

                var rignm = doc.GetString("Title");

                RigRates rr = new RigRates();
                rr.Title = rignm;
                List<FYExpenseProfile> vals = new List<FYExpenseProfile>();

                var data = RigRates.Populate<RigRates>();
                var max = data.FirstOrDefault().Value.Max(x => x.Year);
                var min = data.FirstOrDefault().Value.Min(x => x.Year);

                for (int i = min; i <= max; i++)
                {
                    vals.Add(new FYExpenseProfile
                    {
                        Title = "FY" + i.ToString(),
                        Value = 0,
                        ValueType = "Absolute",
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

        public JsonResult GetCapitalizedStaffOverhead(string Country)
        {
            var ri = new ResultInfo();

            //try
            //{
            //    var query = Query.EQ("Country", Country);
            //    var data = MacroEconomic.Populate<MacroEconomic>(query);
            //    if (data.Any())
            //    {
            //        var totalCSO = data.Max(d => d.CSO.CSOValues.Count);
            //        var items = data.Where(d => d.CSO.CSOValues.Count < totalCSO);

            //        foreach (var item in items)
            //        {
            //            for (var i = 0; i < totalCSO; i++)
            //            {
            //                if (item.CSO.CSOValues == null)
            //                {
            //                    item.CSO.CSOValues = new List<FYExpenseProfile>();
            //                }

            //                item.CSO.CSOValues.Add(new FYExpenseProfile());
            //            }
            //        }
            //    }
            //    ri.Data = data;
            //}
            //catch (Exception e)
            //{
            //    ri.PushException(e);
            //}

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult SaveDummyLongLeads(string Title)
        {
            var ri = new ResultInfo();

            try
            {
                LongLead l = new LongLead();
                l.Title = "Completion";

                FYLongLead f = new FYLongLead();
                f.MonthRequiredValue = 0;
                f.MonthRequiredValueType = "Percentage";
                f.Year = 2015;
                f.Title = "FY15";
                f.TangibleValue = 0;
                f.TangibleValueType = "Percentage";
                l.Details.Add(f);

                l.Save();

                MvcTools.ToJsonResult(ri);
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetLongLeadItems(string Title)
        {
            var ri = new ResultInfo();

            try
            {

                var t = LongLead.Populate<LongLead>();

                ri.Data = t;

            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult UpdateInlineLongLead(List<LongLead> updates)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                if (updates.Any())
                {
                    updates.ForEach(e =>
                    {
                        e.Save();
                    });

                    AdministrationInput.RecalculateBusPlan();
                }

                return "OK";
            });
        }

        public JsonResult AddAnnualValues(string id, String Title, Int32 Year, double Value)
        {
            var ri = new ResultInfo();
            //try
            //{
            //    MacroEconomic.Populate<MacroEconomic>().ForEach(d =>
            //    {
            //        var annualValues = GetCSOValuesOf(d);
            //        var maxYear = annualValues.DefaultIfEmpty(new FYExpenseProfile()).Max(e => e.Year);
            //        //var annual = new FYExpenseProfile() { Title = Title, Year = Year, Value = Value };
            //        //annualValues.Add(annual);

            //        if (maxYear < (Year - 1))
            //        {
            //            for (; maxYear < (Year - 1); maxYear++)
            //            {
            //                var annual = new FYExpenseProfile() { Title = Title, Year = Year, Value = Value };
            //                annualValues.Add(annual);
            //            }
            //        }

            //        if (maxYear < Year)
            //        {
            //            var annual = new FYExpenseProfile() { Title = Title, Year = Year, Value = Value };
            //            if (Convert.ToString(d._id).Equals(id))
            //            {
            //                annual.Value = Value;
            //            }
            //            annualValues.Add(annual);
            //        }

            //        d.Save();
            //    });

            //    ri.Data = true;
            //}
            //catch (Exception e)
            //{
            //    ri.PushException(e);
            //}

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult LongLeadItemsAddAnnualValues()
        {
            var ri = new ResultInfo();

            try
            {
                var sampleData = LongLead.Populate<LongLead>();
                if (sampleData != null)
                {

                    var maxYear = sampleData.FirstOrDefault().Details.Max(d => d.Year);

                    foreach (var data in sampleData)
                    {

                        FYLongLead d = new FYLongLead();
                        d.Year = maxYear + 1;
                        d.Title = "FY" + new DateTime(maxYear + 1, 1, 1).ToString("yy");
                        data.Details.Add(d);
                        data.Save();
                    }


                }
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            AdministrationInput.RecalculateBusPlan();

            return MvcTools.ToJsonResult(ri);

        }


        public JsonResult UpdateInline(List<MacroEconomic> updates)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                if (updates.Any())
                {
                    updates.ForEach(e =>
                    {
                        e.Save();
                    });
                }

                return "OK";
            });
        }

        public JsonResult UpdateLongLead(List<LongLead> updates)
        {

            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                List<LongLead> mtr = new List<LongLead>();
                List<string> titles = new List<string>();
                titles.Add("Drilling");
                titles.Add("Completion");
                titles.Add("Abondoment");


                foreach (var item in titles)
                {
                    var mtrx = new LongLead()
                    {
                        Title = item
                    };
                    mtrx.Save();
                }


                //var exp = BsonSerializer.Deserialize<FYExpenseProfile>();
                //var raw = ActivityExpense.Populate<ActivityExpense>(Query.In("_id", new BsonArray(WellActivityIds)));
                ////var rawActivity = WellActivity.Populate<WellActivity>(Query.In("_id", new BsonArray(WellActivityIds)));
                //exp.Save();
                //exp.Save();
                return "OK";
            });
        }

        public JsonResult UpdateRigName(List<RigRates> updates)
        {

            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                List<RigRates> rig = new List<RigRates>();
                List<string> titles = new List<string>();
                titles.Add("Deepwater Nautelius");

                foreach (var item in titles)
                {
                    var mtrx = new RigRates()
                    {
                        //Title = item
                    };
                    // mtrx.Save();
                }
                return "OK";
            });
        }

        //private List<FYExpenseProfile> GetCSOValuesOf(MacroEconomic me)
        //{
        //    return me.CSO.CSOValues;
        //}

        private List<FYExpenseProfile> GetLongLeadValuesOfTangible(LongLead me)
        {
            return null;// me.Tangible;
        }
        private List<FYExpenseProfile> GetLongLeadValuesOfMonthRequired(LongLead me)
        {
            return null;//  me.MonthRequired;
        }

        public JsonResult GetData(string country)
        {
            var ri = new ResultInfo();

            try
            {
                IMongoQuery query = null;
                List<IMongoQuery> queries = new List<IMongoQuery>();
                queries.Add(Query.Matches("Country",
                        new BsonRegularExpression(new Regex(country.ToLower(),
                            RegexOptions.IgnoreCase))));
                query = Query.Or(queries.ToArray());
                var data = MacroEconomic.Populate<MacroEconomic>(query, sort: SortBy.Ascending("Country"));

                if (data != null)
                {
                    if (data.FirstOrDefault().Inflation == null || data.FirstOrDefault().Inflation.AnnualValues == null || data.FirstOrDefault().Inflation.AnnualValues.Count <= 0)
                    {
                        AnnualHelper a = new AnnualHelper();
                        a.Year = 1995;
                        a.Value = 0;
                        a.Desc = "";
                        a.Source = "";
                        data.FirstOrDefault().Inflation.AnnualValues = new List<AnnualHelper>();
                        data.FirstOrDefault().Inflation.AnnualValues.Add(a);
                        data.FirstOrDefault().Save();
                        data = MacroEconomic.Populate<MacroEconomic>(query, sort: SortBy.Ascending("Country"));

                    }
                }

                ri.Data = data;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        private List<AnnualHelper> GetAnnualValuesOf(MacroEconomic me, string Type = "GDP", string InterestRatesType = "short")
        {
            if (Type.ToLower() == "gdp")
            {
                return me.GDP.AnnualValues;
            }
            else if (Type.ToLower() == "inflation")
            {
                return me.Inflation.AnnualValues;
            }
            else if (Type.ToLower() == "exchangerates")
            {
                return me.ExchangeRate.AnnualValues;
            }
            else if (Type.ToLower() == "interestrates" && InterestRatesType.ToLower() == "short")
            {
                return me.InterestRateShort.AnnualValues;
            }
            else
            {
                return me.InterestRateLong.AnnualValues;
            }
        }

        public JsonResult AddAnnualValues(string id, Int32 Year, double Value, string Source = "", string Desc = "", string Type = "GDP", string InterestRatesType = "short")
        {
            var ri = new ResultInfo();
            try
            {
                MacroEconomic.Populate<MacroEconomic>().ForEach(d =>
                {
                    var annualValues = GetAnnualValuesOf(d, Type, InterestRatesType);
                    var maxYear = annualValues.DefaultIfEmpty(new AnnualHelper()).Max(e => e.Year);

                    if (maxYear < (Year - 1))
                    {
                        for (; maxYear < (Year - 1); maxYear++)
                        {
                            var annual = new AnnualHelper() { Year = Year, Value = 0, Source = Source, Desc = Desc };
                            annualValues.Add(annual);
                        }
                    }

                    if (maxYear < Year)
                    {
                        var annual = new AnnualHelper() { Year = Year, Value = 0, Source = Source, Desc = Desc };
                        if (Convert.ToString(d._id).Equals(id))
                        {
                            annual.Value = Value;
                        }
                        annualValues.Add(annual);
                    }

                    d.Save();
                });

                ri.Data = true;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }


        public JsonResult UpdateInlineMaturity(List<MaturityRiskMatrix> updates)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                if (updates.Any())
                {
                    updates.ForEach(e =>
                    {
                        e.Save();
                    });

                    AdministrationInput.RecalculateBusPlan();
                }

                return "OK";
            });
        }

        public JsonResult GetDataMaturityRisk()
        {
            var ri = new ResultInfo();

            try
            {
                var data = MacroEconomic.Populate<MaturityRiskMatrix>(null, sort: SortBy.Ascending("_id"));

                if (data == null || data.Count() <= 0)
                {
                    List<MaturityRiskMatrix> ris = new List<Client.WEIS.MaturityRiskMatrix>();
                    List<string> titles = new List<string>();
                    titles.Add("Type 0");
                    titles.Add("Type 1");
                    titles.Add("Type 2");
                    titles.Add("Type 3");
                    titles.Add("Type 4");

                    foreach (var item in titles)
                    {
                        var mtrx = new MaturityRiskMatrix()
                        {
                            Title = item
                        };
                        ris.Add(mtrx);
                        mtrx.Save();
                    }
                    ri.Data = ris;// new List<MaturityRiskMatrix>();
                }
                else
                    ri.Data = data;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }


        public JsonResult GetMaturityRiskNewVersion()
        {
            var ri = new ResultInfo();

            try
            {
                var data = new List<BindingMatrix>();
                var GetData = MacroEconomic.Populate<MaturityRiskMatrix>(null, sort: SortBy.Ascending("_id"))
                    .Select(x => x.Title).Distinct();
                var GetDetail = MacroEconomic.Populate<MaturityRiskMatrix>(null, sort: SortBy.Ascending("_id"));
                var TotalYear = MacroEconomic.Populate<MaturityRiskMatrix>(null, sort: SortBy.Ascending("_id"))
                    .FirstOrDefault();

                var MaturyList = new List<BindingMatrix>();
                if (TotalYear.BaseOP == null)
                {
                    DataHelper.Delete("WEISMaturityRisk");
                    var o = 13;
                    for (var i = 0; i < 3; i++)
                    {
                        o++; var y = 2014 + i;
                        for (var j = 0; j < 2; j++)
                        {
                            foreach (var newyear in GetDetail)
                            {
                                var newdata = new MaturityRiskMatrix()
                                {
                                    _id = newyear._id,
                                    Title = newyear.Title,
                                    NPTTime = newyear.NPTTime,
                                    NPTCost = newyear.NPTCost,
                                    TECOPTime = newyear.TECOPTime,
                                    TECOPCost = newyear.TECOPCost,
                                    Year = y,
                                    BaseOP = "OP" + o.ToString()
                                };
                                newdata.Save();
                            }
                            y++;
                        }

                    }

                }
                var GetDetail2 = MacroEconomic.Populate<MaturityRiskMatrix>(null, sort: SortBy.Descending("BaseOP", "Year"));
                foreach (var r in GetData)
                {
                    var mtx = new List<MaturityRiskMatrix>();
                    var eachData = new BindingMatrix();
                    eachData.Title = r;
                    foreach (var d in GetDetail2.Where(x => x.Title.Equals(r)))
                    {
                        if (d.Title.Equals(r))
                        {
                            mtx.Add(new MaturityRiskMatrix() { BaseOP = d.BaseOP, Year = d.Year, NPTTime = d.NPTTime, NPTCost = d.NPTCost, TECOPTime = d.TECOPTime, TECOPCost = d.TECOPCost, _id = d._id });
                        }
                    }
                    eachData.Detail = mtx;
                    MaturyList.Add(eachData);
                }
                ri.Data = MaturyList;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult SaveNewMaturity(List<MaturityRiskMatrix> updates)
        {
            var ri = new ResultInfo();
            var response = "NOK";
            var message = "";
            try
            {
                IMongoQuery query = null;
                List<IMongoQuery> queries = new List<IMongoQuery>();
                queries.Add(Query.EQ("Year", updates.FirstOrDefault().Year));
                queries.Add(Query.EQ("BaseOP", updates.FirstOrDefault().BaseOP));
                query = Query.And(queries.ToArray());
                var CheckId = MacroEconomic.Populate<MaturityRiskMatrix>(query).FirstOrDefault();
                if (CheckId == null)
                {
                    foreach (var r in updates)
                    {
                        response = "OK";
                        var newdata = new MaturityRiskMatrix()
                        {
                            Title = r.Title,
                            NPTTime = r.NPTTime,
                            NPTCost = r.NPTCost,
                            TECOPTime = r.TECOPTime,
                            TECOPCost = r.TECOPCost,
                            Year = r.Year,
                            BaseOP = r.BaseOP
                        };
                        newdata.Save();
                        ri.Result = response;
                        ri.Data = "";
                    }
                }
                else
                {
                    response = "NOK";
                    message = "Base OP: " + updates.FirstOrDefault().BaseOP + " and Year: " + updates.FirstOrDefault().Year + " already exist";
                    throw new Exception(message);
                }

            }
            catch (Exception e)
            {
                ri.Result = response;
                ri.Message = e.Message;
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetMaturityLastYear()
        {
            var ri = new ResultInfo();
            try
            {
                var Get = MacroEconomic.Populate<MaturityRiskMatrix>(null, sort: SortBy.Descending("Year"))
                    .Select(x => x.Year).Distinct().FirstOrDefault();
                ri.Data = Get;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetMaturityYears(int y)
        {
            var ri = new ResultInfo();
            try
            {
                var data = MacroEconomic.Get<MaturityRiskMatrix>(Query.EQ("Year", y));
                ri.Data = data;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult SaveMaturityRisk()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                List<MaturityRiskMatrix> mtr = new List<MaturityRiskMatrix>();
                List<string> titles = new List<string>();
                titles.Add("Type 0");
                titles.Add("Type 1");
                titles.Add("Type 2");
                titles.Add("Type 4");

                foreach (var item in titles)
                {
                    var mtrx = new MaturityRiskMatrix()
                    {
                        Title = item
                    };
                    mtrx.Save();
                }


                //var exp = BsonSerializer.Deserialize<ActivityExpense>(wauDoc);
                //var raw = ActivityExpense.Populate<ActivityExpense>(Query.In("_id", new BsonArray(WellActivityIds)));
                ////var rawActivity = WellActivity.Populate<WellActivity>(Query.In("_id", new BsonArray(WellActivityIds)));
                ////wau.Save();
                //exp.Save();
                return "OK";
            });
        }

        public JsonResult DeleteMaturyRiskByBaseOPYear(string baseop = "", int year = 0)
        {
            var ri = new ResultInfo();
            try
            {
                IMongoQuery query = null;
                List<IMongoQuery> queries = new List<IMongoQuery>();
                queries.Add(Query.EQ("Year", year));
                queries.Add(Query.EQ("BaseOP", baseop));
                query = Query.And(queries.ToArray());
                var CheckId = MacroEconomic.Populate<MaturityRiskMatrix>(query).FirstOrDefault();

                if (CheckId != null)
                {
                    DataHelper.Delete("WEISMaturityRisk", Query.And(Query.EQ("BaseOP", baseop), Query.EQ("Year", year)));
                    ri.Result = "OK";
                }
                else
                {
                    ri.Result = "NOK";
                }
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetDataMarketEscalation(string country)
        {
            var ri = new ResultInfo();

            //try
            //{
            //    IMongoQuery query = null;
            //    List<IMongoQuery> queries = new List<IMongoQuery>();
            //    queries.Add(Query.Matches("Country",
            //            new BsonRegularExpression(new Regex(country.ToLower(),
            //                RegexOptions.IgnoreCase))));
            //    query = Query.Or(queries.ToArray());
            //    var data = MacroEconomic.Populate<MacroEconomic>(query, sort: SortBy.Ascending("Country"));

            //    if(data !=null && data.Count () > 0)
            //    {
            //        var ct = data.FirstOrDefault().MarketEscalation;
            //        if(ct != null )
            //        {
            //            List<MacroEconomic> mcr = new List<MacroEconomic>();

            //            List<FYExpenseProfile> fye = new List<FYExpenseProfile>();
            //            List<int> fyear = new List<int>();
            //            fyear.Add(2015);
            //            fyear.Add(2016);
            //            fyear.Add(2017);

            //            foreach (var item in fyear)
            //            {
            //                fye.Add(new FYExpenseProfile
            //                {
            //                    Title = "Yearly Escalation",
            //                    Year = item,
            //                    Value = 0
            //                });
            //            }

            //            var exp = new MarketEscalation()
            //            {
            //                FiscalYears = fye
            //            };

            //            var mcrr = new MacroEconomic()
            //            {
            //                Country = country,
            //                MarketEscalation = exp
            //            };
            //            mcrr.Save();

            //        }

            //        data = MacroEconomic.Populate<MacroEconomic>(query, sort: SortBy.Ascending("Country"));

            //    }

            //    ri.Data = data;
            //}
            //catch (Exception e)
            //{
            //    ri.PushException(e);
            //}

            return MvcTools.ToJsonResult(ri);
        }

        //public JsonResult SaveMarketEscalation()
        //{
        //    return MvcResultInfo.Execute(null, (BsonDocument doc) =>
        //    {
        //        List<MacroEconomic> mcr = new List<MacroEconomic>();

        //        List<FYExpenseProfile> fye = new List<FYExpenseProfile>();
        //        List<int> fyear = new List<int>();
        //        fyear.Add(2015);
        //        fyear.Add(2016);
        //        fyear.Add(2017);

        //        foreach (var item in fyear)
        //        {
        //            fye.Add(new FYExpenseProfile
        //            {
        //                Title = "Yearly Escalation",
        //                Year = item,
        //                Value = 0
        //            });
        //        }

        //        var exp = new MarketEscalation()
        //        {
        //            FiscalYears = fye
        //        };

        //        var mcrr = new MacroEconomic()
        //        {
        //            Country = "Indonesia",
        //            MarketEscalation = exp
        //        };
        //        mcrr.Save();


        //        //var exp = BsonSerializer.Deserialize<ActivityExpense>(wauDoc);
        //        //var raw = ActivityExpense.Populate<ActivityExpense>(Query.In("_id", new BsonArray(WellActivityIds)));
        //        ////var rawActivity = WellActivity.Populate<WellActivity>(Query.In("_id", new BsonArray(WellActivityIds)));
        //        ////wau.Save();
        //        //exp.Save();
        //        return mcrr;
        //    });
        //}
    }
    internal class BindingMatrix
    {
        public string Title { get; set; }
        public List<MaturityRiskMatrix> Detail { get; set; }

    }
}