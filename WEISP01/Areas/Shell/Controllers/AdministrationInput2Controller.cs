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
    public class AdministrationInput2Controller : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public IMongoQuery GetQuery(AdminBaseParam param)
        {
            var queries = new List<IMongoQuery>();
            IMongoQuery query = null;

            if (param.Country != null)
                queries.Add(Query.EQ("Country", param.Country));
            if (param.RigName != null)
                queries.Add(Query.EQ("RigName", param.RigName));
            if (param.ActivityType != null)
                queries.Add(Query.EQ("ActivityType", param.ActivityType));

            if (queries.Any())
                query = Query.And(queries);

            return query;
        }

        private void PrepareViewVar(string what)
        {
            var config = Config.Get<Config>(Query.EQ("_id", "FilterOf" + what));
            var value = Convert.ToString(config.ConfigValue).Split(',');

            ViewBag.IsCountry = (config == null) ? false : value.Any(d => d.Equals("Country"));
            ViewBag.IsRigName = (config == null) ? false : value.Any(d => d.Equals("RigName"));
            ViewBag.IsActivityType = (config == null) ? false : value.Any(d => d.Equals("ActivityType"));
        }

        public BizMaster GetObject(AdminBaseParam param)
        {
            var biz = new BizMaster();

            if (param.ActivityType != null)
                biz.ActivityType = param.ActivityType;
            if (param.RigName != null)
                biz.RigName = param.RigName;
            if (param.Country != null)
                biz.Country = param.Country;

            return biz;
        }

        #region Economic Inflation

        public ActionResult EconomicInflation()
        {
            PrepareViewVar("EconomicInflation");
            return View();
        }

        public JsonResult GetEconomicInflationData(AdminBaseParam param)
        {
            var ri = new ResultInfo();
            var query = GetQuery(param);

            try
            {
                var biz = BizMaster.Get<BizMaster>(query) ?? new BizMaster();
                ri.Data = biz.Inflation.Forecast;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult SaveEconomicInflationData(AdminBaseParam param, double Value)
        {
            var ri = new ResultInfo();
            var query = GetQuery(param);

            try
            {
                var already = false;
                var bizz = BizMaster.Populate<BizMaster>(query);
                
                bizz.ForEach(d =>
                {
                    if (already)
                    {
                        d.Inflation.Forecast = 0;
                    }
                    else
                    {
                        d.Inflation.Forecast = Value;
                    }

                    d.Delete();
                    d.Save();
                    already = true;
                });

                if (!bizz.Any())
                {
                    var biz = GetObject(param);
                    biz.Inflation = new Inflation() { Forecast = Value };
                    biz.Save();
                }
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        #endregion

        #region Market Escalation Factor

        public ActionResult MarketEscalationFactor()
        {
            PrepareViewVar("MarketEscalationFactor");
            return View();
        }

        public JsonResult GetMarketEscalationFactorData(AdminBaseParam param)
        {
            var ri = new ResultInfo();
            var query = GetQuery(param);

            try
            {
                string valueType = "";
                var bizz = BizMaster.Populate<BizMaster>(query);
                var biz = bizz.FirstOrDefault(d => d.MarketEscalation.FiscalYears.Any());

                if (biz == null)
                {
                    biz = GetObject(param);
                }

                if (biz.MarketEscalation == null) {
                    biz.MarketEscalation = new MarketEscalation();
                } 

                if (biz.MarketEscalation.FiscalYears == null) {
                    biz.MarketEscalation.FiscalYears = new List<FYExpenseProfile>();
                }

                var market = new { Title = "Yearly Escalation" }.ToBsonDocument();
                var minYear = biz.MarketEscalation.FiscalYears.DefaultIfEmpty(new FYExpenseProfile() { Year = DateTime.Now.Year }).Min(d => d.Year);
                var maxYear = biz.MarketEscalation.FiscalYears.DefaultIfEmpty(new FYExpenseProfile() { Year = DateTime.Now.Year }).Max(d => d.Year);
                var fields = new { Title = new { editable = false } }.ToBsonDocument();
                var columns = new BsonDocument[] {
                    new { field = "Title", width = 150, locked = true, editable = false }.ToBsonDocument()
                }.ToList();

                for (var i = minYear; i <= maxYear; i++)
                {
                    var found = biz.MarketEscalation.FiscalYears.FirstOrDefault(d => d.Year == i);
                    var value = 0.0;

                    if (found != null)
                    {
                        value = found.Value;
                        valueType = found.ValueType;
                    }

                    market.Set("Year_" + i, value);
                    fields.Set("Year_" + i, new { type = "number" }.ToBsonDocument());
                    columns.Add(new
                    {
                        title = "FY" + Convert.ToString(i).Substring(2, 2),
                        field = "Year_" + i,
                        attributes = new { style = "text-align: right" },
                        format = "{0:N2}",
                        width = 100
                    }.ToBsonDocument());
                }

                ri.Data = new
                {
                    MarketEscalation = DataHelper.ToDictionaryArray(new BsonDocument[] { market }),
                    ValueType = valueType,
                    GlobalMarketEscalation = biz.GlobalMarketEscalation ?? new GlobalMarketEscalation(),
                    Fields = fields.ToDictionary(),
                    Columns = DataHelper.ToDictionaryArray(columns)
                };
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult SaveMarketEscalationFactorData(AdminBaseParam param, GlobalMarketEscalation GME, Dictionary<string, object> Changes)
        {
            var ri = new ResultInfo();
            var query = GetQuery(param);

            try
            {
                var bizz = BizMaster.Populate<BizMaster>(query);
                var biz = bizz.FirstOrDefault(d => d.MarketEscalation.FiscalYears.Any());

                if (biz == null)
                {
                    biz = GetObject(param);
                }

                bizz.Where(d => d != biz).ToList().ForEach(d =>
                {
                    d.MarketEscalation.FiscalYears = new List<FYExpenseProfile>();
                    d.GlobalMarketEscalation = new GlobalMarketEscalation();
                    d.Delete();
                    d.Save();
                });

                if (biz.MarketEscalation == null)
                {
                    biz.MarketEscalation = new MarketEscalation();
                }
                
                if (!biz.MarketEscalation.FiscalYears.Any()) 
                {
                    biz.MarketEscalation.FiscalYears = new List<FYExpenseProfile>();
                }

                if (biz.GlobalMarketEscalation == null)
                {
                    biz.GlobalMarketEscalation = new GlobalMarketEscalation();
                }

                biz.GlobalMarketEscalation = GME;

                if (Changes.ContainsKey("Title"))
                {
                    foreach (var each in Changes.ToBsonDocument())
                    {
                        if (each.Name == "Title")
                        {
                            continue;
                        }

                        var year = Convert.ToInt32(each.Name.Replace("Year_", ""));
                        var value = Convert.ToDouble(each.Value);
                        var target = biz.MarketEscalation.FiscalYears.FirstOrDefault(d => d.Year == year);

                        if (target != null)
                        {
                            target.Value = value;
                        }
                        else
                        {
                            biz.MarketEscalation.FiscalYears.Add(new FYExpenseProfile()
                            {
                                Year = year,
                                Value = value,
                                ValueType = "%"
                            });
                        }
                    }
                }

                biz.Delete();
                biz.Save();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult AddYearOnMarketEscalationFactor(AdminBaseParam param)
        {
            var ri = new ResultInfo();
            var query = GetQuery(param);

            try
            {
                var bizz = BizMaster.Populate<BizMaster>(query);
                var biz = bizz.FirstOrDefault(d => d.MarketEscalation.FiscalYears.Any());

                bizz.Where(d => d != biz).ToList().ForEach(d =>
                {
                    d.MarketEscalation.FiscalYears = new List<FYExpenseProfile>();
                    d.GlobalMarketEscalation = new GlobalMarketEscalation();
                    d.Delete();
                    d.Save();
                });

                if (biz.MarketEscalation == null)
                {
                    biz.MarketEscalation = new MarketEscalation();
                }

                if (!biz.MarketEscalation.FiscalYears.Any())
                {
                    biz.MarketEscalation.FiscalYears = new List<FYExpenseProfile>();
                }

                if (biz.GlobalMarketEscalation == null)
                {
                    biz.GlobalMarketEscalation = new GlobalMarketEscalation();
                }

                var latestYear = biz.MarketEscalation.FiscalYears.Max(d => d.Year);
                biz.MarketEscalation.FiscalYears.Add(new FYExpenseProfile()
                {
                    Year = latestYear + 1,
                    Value = 0,
                    ValueType = "%"
                });

                biz.Delete();
                biz.Save();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RemoveYearOnMarketEscalationFactor(AdminBaseParam param)
        {
            var ri = new ResultInfo();
            var query = GetQuery(param);

            try
            {
                var bizz = BizMaster.Populate<BizMaster>(query);
                var biz = bizz.FirstOrDefault(d => d.MarketEscalation.FiscalYears.Any());

                bizz.Where(d => d != biz).ToList().ForEach(d =>
                {
                    d.MarketEscalation.FiscalYears = new List<FYExpenseProfile>();
                    d.GlobalMarketEscalation = new GlobalMarketEscalation();
                    d.Delete();
                    d.Save();
                });

                if (biz.MarketEscalation == null)
                {
                    biz.MarketEscalation = new MarketEscalation();
                }

                if (!biz.MarketEscalation.FiscalYears.Any())
                {
                    biz.MarketEscalation.FiscalYears = new List<FYExpenseProfile>();
                }

                if (biz.GlobalMarketEscalation == null)
                {
                    biz.GlobalMarketEscalation = new GlobalMarketEscalation();
                }

                var latestYear = biz.MarketEscalation.FiscalYears.Max(d => d.Year);
                biz.MarketEscalation.FiscalYears = biz.MarketEscalation.FiscalYears.Where(d => d.Year != latestYear).ToList();

                biz.Delete();
                biz.Save();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        #endregion

        #region Maturity Risk Matrix

        public ActionResult MaturityRiskMatrix()
        {
            PrepareViewVar("MaturityRiskMatrix");
            return View();
        }

        public JsonResult GetMaturityRiskMatrixData(AdminBaseParam param)
        {
            var ri = new ResultInfo();
            var query = GetQuery(param);

            try
            {
                var bizz = BizMaster.Populate<BizMaster>(query);
                var biz = bizz.FirstOrDefault(d => d.MaturityRisks.Any());

                if (biz == null)
                {
                    biz = GetObject(param);
                }

                if (biz == null)
                {
                    biz = bizz.FirstOrDefault();
                }

                if (!biz.MaturityRisks.Any())
                {
                    biz.MaturityRisks = new MaturityRiskMatrix[] {
                        new MaturityRiskMatrix() { Title = "TYPE 0" },
                        new MaturityRiskMatrix() { Title = "TYPE 1" },
                        new MaturityRiskMatrix() { Title = "TYPE 2" },
                        new MaturityRiskMatrix() { Title = "TYPE 3" }
                    }.ToList();
                }

                ri.Data = biz.MaturityRisks.OrderBy(d => d.Title);
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult SaveMaturityRiskMatrixData(AdminBaseParam param, List<MaturityRiskMatrix> Changes)
        {
            var ri = new ResultInfo();
            var query = GetQuery(param);

            try
            {
                var bizz = BizMaster.Populate<BizMaster>(query);
                var biz = bizz.FirstOrDefault(d => d.MaturityRisks.Any());

                bizz.Where(d => d != biz).ToList().ForEach(d =>
                {
                    d.MaturityRisks = new List<Client.WEIS.MaturityRiskMatrix>();
                    d.Delete();
                    d.Save();
                });

                if (biz == null)
                {
                    biz = bizz.FirstOrDefault();
                }

                if (biz == null)
                {
                    biz = GetObject(param);
                }

                if (!biz.MaturityRisks.Any())
                {
                    biz.MaturityRisks = new List<MaturityRiskMatrix>();
                }

                biz.MaturityRisks = Changes;
                biz.Delete();
                biz.Save();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        #endregion

        #region Capitalized Staff Overhead

        public ActionResult CapitalizedStaffOverhead()
        {
            PrepareViewVar("CapitalizedStaffOverhead");
            return View();
        }

        public JsonResult GetCapitalizedStaffOverheadData(AdminBaseParam param)
        {
            var ri = new ResultInfo();
            var query = GetQuery(param);

            try
            {
                string valueType = "";
                var bizz = BizMaster.Populate<BizMaster>(query);
                BizMaster biz = null;

                try
                {
                    biz = bizz.FirstOrDefault(d => d.CSO.CSOValues.Any());
                }
                catch (Exception e)
                {
                    biz = GetObject(param);
                }

                if (biz == null)
                {
                    biz = GetObject(param);
                }

                if (biz.CSO == null)
                {
                    biz.CSO = new CSO();
                }

                if (biz.CSO.CSOValues == null)
                {
                    biz.CSO.CSOValues = new List<FYExpenseProfile>();
                }

                var market = new { Title = "CSO Factor" }.ToBsonDocument();
                var minYear = biz.CSO.CSOValues.DefaultIfEmpty(new FYExpenseProfile() { Year = DateTime.Now.Year }).Min(d => d.Year);
                var maxYear = biz.CSO.CSOValues.DefaultIfEmpty(new FYExpenseProfile() { Year = DateTime.Now.Year }).Max(d => d.Year);
                var fields = new { Title = new { editable = false } }.ToBsonDocument();
                var columns = new BsonDocument[] {
                    new { field = "Title", width = 150, locked = true, editable = false }.ToBsonDocument()
                }.ToList();

                for (var i = minYear; i <= maxYear; i++)
                {
                    var found = biz.CSO.CSOValues.FirstOrDefault(d => d.Year == i);
                    var value = 0.0;

                    if (found != null)
                    {
                        value = found.Value;
                        valueType = found.ValueType;
                    }

                    market.Set("Year_" + i, value);
                    fields.Set("Year_" + i, new { type = "number" }.ToBsonDocument());
                    columns.Add(new
                    {
                        title = "FY" + Convert.ToString(i).Substring(2, 2),
                        field = "Year_" + i,
                        attributes = new { style = "text-align: right" },
                        format = "{0:N2}",
                        width = 100
                    }.ToBsonDocument());
                }

                ri.Data = new
                {
                    CSO = DataHelper.ToDictionaryArray(new BsonDocument[] { market }),
                    ValueType = valueType,
                    Fields = fields.ToDictionary(),
                    Columns = DataHelper.ToDictionaryArray(columns)
                };
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult SaveCapitalizedStaffOverheadData(AdminBaseParam param, Dictionary<string, object> Changes)
        {
            var ri = new ResultInfo();
            var query = GetQuery(param);

            try
            {
                var bizz = BizMaster.Populate<BizMaster>(query);
                BizMaster biz = null;

                try
                {
                    biz = bizz.FirstOrDefault(d => d.CSO.CSOValues.Any());
                }
                catch (Exception e)
                {
                    biz = GetObject(param);
                }

                if (biz == null)
                {
                    biz = GetObject(param);
                }

                bizz.Where(d => d != biz).ToList().ForEach(d =>
                {
                    if (d.CSO == null)
                    {
                        d.CSO = new CSO();
                    }

                    d.CSO.CSOValues = new List<FYExpenseProfile>();
                    d.Delete();
                    d.Save();
                });

                if (biz == null)
                {
                    biz = GetObject(param);
                }

                if (biz.CSO == null)
                {
                    biz.CSO = new CSO();
                }

                if (!biz.CSO.CSOValues.Any())
                {
                    biz.CSO.CSOValues = new List<FYExpenseProfile>();
                }

                if (Changes.ContainsKey("Title"))
                {
                    foreach (var each in Changes.ToBsonDocument())
                    {
                        if (each.Name == "Title")
                        {
                            continue;
                        }

                        var year = Convert.ToInt32(each.Name.Replace("Year_", ""));
                        var value = Convert.ToDouble(each.Value);
                        var target = biz.CSO.CSOValues.FirstOrDefault(d => d.Year == year);

                        if (target != null)
                        {
                            target.Value = value;
                        }
                        else
                        {
                            biz.CSO.CSOValues.Add(new FYExpenseProfile()
                            {
                                Year = year,
                                Value = value,
                                ValueType = "%"
                            });
                        }
                    }
                }

                biz.Delete();
                biz.Save();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult AddYearOnCapitalizedStaffOverhead(AdminBaseParam param)
        {
            var ri = new ResultInfo();
            var query = GetQuery(param);

            try
            {
                var bizz = BizMaster.Populate<BizMaster>(query);
                BizMaster biz = null;

                try
                {
                    biz = bizz.FirstOrDefault(d => d.CSO.CSOValues.Any());
                }
                catch (Exception e)
                {
                    biz = GetObject(param);
                }

                if (biz == null)
                {
                    biz = GetObject(param);
                }

                bizz.Where(d => d != biz).ToList().ForEach(d =>
                {
                    d.CSO.CSOValues = new List<FYExpenseProfile>();
                    d.Delete();
                    d.Save();
                });

                if (biz == null)
                {
                    biz = GetObject(param);
                }

                if (biz.CSO == null)
                {
                    biz.CSO = new CSO();
                }

                if (!biz.CSO.CSOValues.Any())
                {
                    biz.CSO.CSOValues = new List<FYExpenseProfile>();
                }

                var latestYear = biz.CSO.CSOValues.Max(d => d.Year);
                biz.CSO.CSOValues.Add(new FYExpenseProfile()
                {
                    Year = latestYear + 1,
                    Value = 0,
                    ValueType = "%"
                });

                biz.Delete();
                biz.Save();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RemoveYearOnCapitalizedStaffOverhead(AdminBaseParam param)
        {
            var ri = new ResultInfo();
            var query = GetQuery(param);

            try
            {
                var bizz = BizMaster.Populate<BizMaster>(query);
                BizMaster biz = null;

                try
                {
                    biz = bizz.FirstOrDefault(d => d.CSO.CSOValues.Any());
                }
                catch (Exception e)
                {
                    biz = GetObject(param);
                }

                if (biz == null)
                {
                    biz = GetObject(param);
                }

                bizz.Where(d => d != biz).ToList().ForEach(d =>
                {
                    d.CSO.CSOValues = new List<FYExpenseProfile>();
                    d.Delete();
                    d.Save();
                });

                if (biz == null)
                {
                    biz = GetObject(param);
                }

                if (biz.CSO == null)
                {
                    biz.CSO = new CSO();
                }

                if (!biz.CSO.CSOValues.Any())
                {
                    biz.CSO.CSOValues = new List<FYExpenseProfile>();
                }

                var latestYear = biz.CSO.CSOValues.Max(d => d.Year);
                biz.CSO.CSOValues = biz.CSO.CSOValues.Where(d => d.Year != latestYear).ToList();

                biz.Delete();
                biz.Save();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        #endregion

        #region Long Lead Items

        //public ActionResult LongLeadItems()
        //{
        //    PrepareViewVar("LongLeadItems");
        //    return View();
        //}

        //public JsonResult GetLongLeadItemsData(AdminBaseParam param)
        //{
        //    var ri = new ResultInfo();
        //    var query = GetQuery(param);

        //    try
        //    {
        //        var bizz = BizMaster.Populate<BizMaster>(query);

        //        //var result = bizz.GroupBy(d => d.ActivityType).Select(d => d.FirstOrDefault()).Select(d =>
        //        //{
        //        //    if (d.LongLead == null)
        //        //    {
        //        //        d.LongLead = new LongLead();
        //        //    }

        //        //    if (d.LongLead.Tangible == null)
        //        //    {
        //        //        d.LongLead.Tangible = new List<FYExpenseProfile>();
        //        //    }

        //        //    if (d.LongLead.MonthRequired == null)
        //        //    {
        //        //        d.LongLead.MonthRequired = new List<FYExpenseProfile>();
        //        //    }

        //        //    var tempFYE = new FYExpenseProfile() { Year = DateTime.Now.Year };
        //        //    string valueType = "";
        //        //    var market1 = new { Title = "Tangible %" }.ToBsonDocument();
        //        //    var market2 = new { Title = "# of Months required" }.ToBsonDocument();

        //        //    var minYear = new int[] { 
        //        //        d.LongLead.Tangible.DefaultIfEmpty(tempFYE).Min(e => e.Year), 
        //        //        d.LongLead.MonthRequired.DefaultIfEmpty(tempFYE).Min(e => e.Year) 
        //        //    }.Min();
        //        //    var maxYear = new int[] { 
        //        //        d.LongLead.Tangible.DefaultIfEmpty(tempFYE).Max(e => e.Year), 
        //        //        d.LongLead.MonthRequired.DefaultIfEmpty(tempFYE).Max(e => e.Year) 
        //        //    }.Max();

        //        //    var fields = new { Title = new { editable = false } }.ToBsonDocument();
        //        //    var columns = new BsonDocument[] {
        //        //        new { field = "Title", width = 150, locked = true, editable = false }.ToBsonDocument()
        //        //    }.ToList();

        //        //    for (var i = minYear; i <= maxYear; i++)
        //        //    {
        //        //        var found1 = d.LongLead.Tangible.FirstOrDefault(e => e.Year == i);
        //        //        var value1 = 0.0;

        //        //        if (found1 != null)
        //        //        {
        //        //            value1 = found1.Value;
        //        //            valueType = found1.ValueType;
        //        //        }

        //        //        var found2 = d.LongLead.MonthRequired.FirstOrDefault(e => e.Year == i);
        //        //        var value2 = 0.0;

        //        //        if (found2 != null)
        //        //        {
        //        //            value2 = found2.Value;
        //        //        }

        //        //        market1.Set("Year_" + i, value1);
        //        //        market2.Set("Year_" + i, value2);

        //        //        fields.Set("Year_" + i, new { type = "number" }.ToBsonDocument());
        //        //        columns.Add(new
        //        //        {
        //        //            title = "FY" + Convert.ToString(i).Substring(2, 2),
        //        //            field = "Year_" + i,
        //        //            attributes = new { style = "text-align: right" },
        //        //            format = "{0:N2}",
        //        //            width = 100
        //        //        }.ToBsonDocument());
        //        //    }

        //        //    return new LongLeadItemsResult()
        //        //    {
        //        //        TabTitle = d.ActivityType,
        //        //        Items = DataHelper.ToDictionaryArray(new BsonDocument[] { market1, market2 }).ToList(),
        //        //        ValueType = valueType,
        //        //        Fields = fields.ToDictionary(),
        //        //        Columns = DataHelper.ToDictionaryArray(columns).ToList()
        //        //    };
        //        //});

        //        //ri.Data = result;
        //    }
        //    catch (Exception e)
        //    {
        //        ri.PushException(e);
        //    }

        //    return MvcTools.ToJsonResult(ri);
        //}

        //public JsonResult SaveLongLeadItemsData(AdminBaseParam param, List<string> Activities, List<List<Dictionary<string, object>>> AllChanges = null)
        //{
        //    if (AllChanges == null) AllChanges = new List<List<Dictionary<string, object>>>();
        //    if (Activities == null) Activities = new List<string>();

        //    var ri = new ResultInfo();
        //    var query = GetQuery(param);

        //    try
        //    {
        //        var bizzAll = BizMaster.Populate<BizMaster>(query);

        //        var i = 0;
        //        AllChanges.ForEach(d =>
        //        {
        //            var activityType = Activities[i];
        //            var changes = d;

        //            var bizz = bizzAll.Where(e => activityType.Equals(e.ActivityType));
        //            var biz = bizz.FirstOrDefault();

        //            bizz.Where(e => e != biz).ToList().ForEach(e => {
        //                e.LongLead = new LongLead();
        //                e.Delete();
        //                e.Save();
        //            });

        //            for (var j = 0; j < AllChanges[i].Count; j++)
        //            {
        //                var Changes = AllChanges[i][j];

        //                if (Changes.ContainsKey("Title"))
        //                {
        //                    foreach (var each in Changes.ToBsonDocument())
        //                    {
        //                        if (each.Name == "Title")
        //                        {
        //                            continue;
        //                        }

        //                        var year = Convert.ToInt32(each.Name.Replace("Year_", ""));
        //                        var value = Convert.ToDouble(each.Value);
        //                        List<FYExpenseProfile> targets;

        //                        if (j == 0)
        //                        {
        //                            targets = biz.LongLead.Tangible;
        //                        }
        //                        else
        //                        {
        //                            targets = biz.LongLead.MonthRequired;
        //                        }

        //                        var target = targets.FirstOrDefault(e => e.Year == year);

        //                        if (target != null)
        //                        {
        //                            target.Value = value;
        //                        }
        //                        else
        //                        {
        //                            targets.Add(new FYExpenseProfile()
        //                            {
        //                                Year = year,
        //                                Value = value,
        //                                ValueType = "%"
        //                            });
        //                        }
        //                    }
        //                }
        //            }

        //            biz.Delete();
        //            biz.Save();

        //            i++;
        //        });

        //    }
        //    catch (Exception e)
        //    {
        //        ri.PushException(e);
        //    }

        //    return MvcTools.ToJsonResult(ri);
        //}

        //public JsonResult AddYearOnLongLeadItems(AdminBaseParam param)
        //{
        //    var ri = new ResultInfo();
        //    var query = GetQuery(param);

        //    try
        //    {
        //        var bizz = BizMaster.Populate<BizMaster>(query);
        //        bizz.GroupBy(d => d.ActivityType)
        //            .Select(d => d.FirstOrDefault(e => e.LongLead.MonthRequired.Any() || e.LongLead.Tangible.Any()))
        //            .ToList()
        //            .ForEach(d =>
        //            {
        //                bizz.Where(e => d.ActivityType.Equals(e.ActivityType) && e != d).ToList().ForEach(e =>
        //                {
        //                    d.LongLead = new LongLead();
        //                    d.Delete();
        //                    d.Save();
        //                });

        //                if (d.LongLead == null)
        //                {
        //                    d.LongLead = new LongLead();
        //                }

        //                if (d.LongLead.Tangible == null)
        //                {
        //                    d.LongLead.Tangible = new List<FYExpenseProfile>();
        //                }

        //                if (d.LongLead.MonthRequired == null)
        //                {
        //                    d.LongLead.MonthRequired = new List<FYExpenseProfile>();
        //                }

        //                var tempFYE = new FYExpenseProfile() { Year = DateTime.Now.Year };
                        
        //                var latestYear = new int[] { 
        //                    d.LongLead.Tangible.DefaultIfEmpty(tempFYE).Max(e => e.Year), 
        //                    d.LongLead.MonthRequired.DefaultIfEmpty(tempFYE).Max(e => e.Year) 
        //                }.Max();

        //                d.LongLead.Tangible.Add(new FYExpenseProfile()
        //                {
        //                    Year = latestYear + 1,
        //                    Value = 0,
        //                    ValueType = "%"
        //                });

        //                d.LongLead.MonthRequired.Add(new FYExpenseProfile()
        //                {
        //                    Year = latestYear + 1,
        //                    Value = 0,
        //                    ValueType = "%"
        //                });

        //                d.Delete();
        //                d.Save();
        //            });
        //    }
        //    catch (Exception e)
        //    {
        //        ri.PushException(e);
        //    }

        //    return MvcTools.ToJsonResult(ri);
        //}

        //public JsonResult RemoveYearOnLongLeadItems(AdminBaseParam param)
        //{
        //    var ri = new ResultInfo();
        //    var query = GetQuery(param);

        //    try
        //    {
        //        var bizz = BizMaster.Populate<BizMaster>(query);
        //        bizz.GroupBy(d => d.ActivityType)
        //            .Select(d => d.FirstOrDefault(e => e.LongLead.MonthRequired.Any() || e.LongLead.Tangible.Any()))
        //            .ToList()
        //            .ForEach(d =>
        //            {
        //                bizz.Where(e => d.ActivityType.Equals(e.ActivityType) && e != d).ToList().ForEach(e =>
        //                {
        //                    d.LongLead = new LongLead();
        //                    d.Delete();
        //                    d.Save();
        //                });


        //                var latestYear = new int[] { d.LongLead.Tangible.Max(e => e.Year), d.LongLead.MonthRequired.Max(e => e.Year) }.Max();

        //                d.LongLead.Tangible = d.LongLead.Tangible.Where(e => e.Year != latestYear).ToList();
        //                d.LongLead.MonthRequired = d.LongLead.MonthRequired.Where(e => e.Year != latestYear).ToList();

        //                d.Delete();
        //                d.Save();
        //            });
        //    }
        //    catch (Exception e)
        //    {
        //        ri.PushException(e);
        //    }

        //    return MvcTools.ToJsonResult(ri);
        //}

        #endregion

        #region Rig Rates

        public ActionResult RigRates()
        {
            PrepareViewVar("RigRates");
            return View();
        }

        public JsonResult GetRigRatesData()
        {
            var ri = new ResultInfo();

            try
            {
                var fields = new { Title = new { editable = false } }.ToBsonDocument();
                var columns = new BsonDocument[] {
                    new { field = "Title", width = 150, locked = true, editable = false }.ToBsonDocument()
                }.ToList();

                List<BizMaster> bizz = new List<BizMaster>();
                try
                {
                    bizz = BizMaster.Populate<BizMaster>().Where(d => d.RigRate.Value.Any()).ToList();
                }
                catch (Exception e)
                {

                }

                var minYear = DateTime.Now.Year;
                try { minYear = bizz.Min(d => d.RigRate.Value.Min(e => e.Year)); }
                catch (Exception e) {  }

                var maxYear = DateTime.Now.Year;
                try {maxYear =  bizz.Max(d => d.RigRate.Value.Max(e => e.Year)); }
                catch (Exception e) { }

                var counter = 0;

                var items = bizz.Select(d =>
                {
                    if (d.RigRate == null)
                    {
                        d.RigRate = new RigRates();
                    }
                    if (!d.RigRate.Value.Any())
                    {
                        d.RigRate.Value = new List<FYExpenseProfile>();
                    }

                    var doc = new { Title = d.RigName }.ToBsonDocument();

                    for (var i = minYear; i <= maxYear; i++)
                    {
                        var each = d.RigRate.Value.FirstOrDefault(e => e.Year == i);
                        var value = (each != null) ? each.Value : 0;

                        doc.Set("Year_" + i, value);

                        if (counter == 0)
                        {
                            fields.Set("Year_" + i, new { type = "number" }.ToBsonDocument());
                            columns.Add(new
                            {
                                title = "FY" + Convert.ToString(i).Substring(2, 2),
                                field = "Year_" + i,
                                attributes = new { style = "text-align: right" },
                                format = "{0:N2}",
                                width = 100
                            }.ToBsonDocument());
                        }
                    }

                    counter++;
                    return doc;
                }).OrderBy(d => d.GetString("Title"));

                ri.Data = new
                {
                    Items = DataHelper.ToDictionaryArray(items).ToList(),
                    Fields = fields.ToDictionary(),
                    Columns = DataHelper.ToDictionaryArray(columns).ToList(),
                    MinYear = minYear,
                    MaxYear = maxYear
                };
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult SaveRigRatesData(List<Dictionary<string, object>> Changes = null)
        {
            if (Changes == null)
            {
                Changes = new List<Dictionary<string, object>>();
            }

            var ri = new ResultInfo();

            try
            {
                Changes.ForEach(d =>
                {
                    var bizz = BizMaster.Populate<BizMaster>(Query.EQ("RigName", Convert.ToString(d["Title"])));
                    var biz = bizz.FirstOrDefault();

                    bizz.Where(e => e != biz).ToList().ForEach(e =>
                    {
                        e.Delete();
                        e.Save();
                    });

                    if (biz.RigRate == null)
                    {
                        biz.RigRate = new RigRates();
                    }

                    if (!biz.RigRate.Value.Any())
                    {
                        biz.RigRate.Value = new List<FYExpenseProfile>();
                    }

                    var doc = d.ToBsonDocument();
                    foreach (var el in doc)
                    {
                        if (el.Name.Contains("Year_"))
                        {
                            var year = Convert.ToInt32(el.Name.Replace("Year_", ""));
                            var eachYear = biz.RigRate.Value.FirstOrDefault(e => e.Year == year);
                            if (eachYear != null)
                            {
                                eachYear.Value = Convert.ToDouble(el.Value);
                            }
                            else
                            {
                                biz.RigRate.Value.Add(new FYExpenseProfile()
                                {
                                    Year = year,
                                    Value = 0
                                });
                            }
                        }
                    }

                    biz.Delete();
                    biz.Save();
                });
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult AddYearOnRigRates()
        {
            var ri = new ResultInfo();

            try
            {
                var bizz = BizMaster.Populate<BizMaster>()
                    .Where(d => d.RigRate.Value.Any());

                var nextYear = bizz.Max(d => d.RigRate.Value.Max(e => e.Year)) + 1;

                bizz.ToList().ForEach(d =>
                {
                    var next = d.RigRate.Value.FirstOrDefault(e => e.Year == nextYear);
                    if (next == null)
                    {
                        d.RigRate.Value.Add(new FYExpenseProfile()
                        {
                            Year = nextYear,
                            Value = 0
                        });
                    }

                    d.Delete();
                    d.Save();
                });
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RemoveYearOnRigRates()
        {
            var ri = new ResultInfo();

            try
            {
                var bizz = BizMaster.Populate<BizMaster>()
                    .Where(d => d.RigRate.Value.Any());

                var latestYear = bizz.Max(d => d.RigRate.Value.Max(e => e.Year));

                bizz.ToList().ForEach(d =>
                {
                    var next = d.RigRate.Value = d.RigRate.Value.Where(e => e.Year != latestYear).ToList();

                    d.Delete();
                    d.Save();
                });
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
                var rigName = Convert.ToString(insert["Title"]);
                var bizz = BizMaster.Populate<BizMaster>(Query.EQ("RigName", rigName));

                if (!bizz.Any())
                {
                    var newBiz = new BizMaster() { 
                        RigName = rigName, 
                        RigRate = new RigRates()
                    };
                    newBiz.Save();
                    bizz.Add(newBiz);
                } else if (bizz.FirstOrDefault(d => d.RigRate.Value.Any()) != null) 
                {
                    ri.PushException(new Exception("Data with same rig already exists"));
                    return MvcTools.ToJsonResult(ri);
                }

                var biz = bizz.FirstOrDefault();

                if (!biz.RigRate.Value.Any()) {
                    biz.RigRate.Value = new List<FYExpenseProfile>();
                }

                var doc = insert.ToBsonDocument();

                foreach (var each in doc)
                {
                    if (each.Name.Contains("Year_"))
                    {
                        var year = Convert.ToInt32(each.Name.Replace("Year_", ""));
                        var value = Convert.ToDouble(each.Value);

                        biz.RigRate.Value.Add(new FYExpenseProfile()
                        {
                            Year = year,
                            Value = value
                        });
                    }
                }

                biz.Delete();
                biz.Save();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        #endregion

        public JsonResult PopulateSampleData()
        {
            if (BizMaster.Populate<BizMaster>().Any())
            {
                return MvcTools.ToJsonResult(new ResultInfo());
            }

            new BizMaster()
            {
                ActivityType = "WHOLE DRILLING EVENT",
                Country = "Nebraska",
                RigName = "AUGER",
                CSO = new CSO()
                {
                    CSOValues = new FYExpenseProfile[] { 
                        new FYExpenseProfile() { Title = "A", Year = 2015, Value = 20, ValueType = "%" },
                        new FYExpenseProfile() { Title = "B", Year = 2016, Value = 30, ValueType = "%" },
                        new FYExpenseProfile() { Title = "C", Year = 2017, Value = 40, ValueType = "%" }
                    }.ToList()
                },
                GlobalMarketEscalation = new GlobalMarketEscalation()
                {
                    EscalationFactor = "",
                    MaterialCostWeight = 10,
                    RigCostWeight = 20,
                    RigName = "AUGER",
                    ServicesCostWeight = 30
                },
                Inflation = new Inflation()
                {
                    Forecast = 34
                },
                LongLead = new LongLead()
                {
                    //MonthRequired = new FYExpenseProfile[] {
                    //    new FYExpenseProfile() { Title = "A", Year = 2015, Value = 20, ValueType = "%" },
                    //    new FYExpenseProfile() { Title = "B", Year = 2016, Value = 30, ValueType = "%" },
                    //    new FYExpenseProfile() { Title = "C", Year = 2017, Value = 40, ValueType = "%" }
                    //}.ToList(),
                    //Tangible = new FYExpenseProfile[] {
                    //    new FYExpenseProfile() { Title = "A", Year = 2015, Value = 20, ValueType = "" },
                    //    new FYExpenseProfile() { Title = "B", Year = 2016, Value = 30, ValueType = "" },
                    //    new FYExpenseProfile() { Title = "C", Year = 2017, Value = 40, ValueType = "" }
                    //}.ToList(),
                },
                MarketEscalation = new MarketEscalation()
                {
                    FiscalYears = new FYExpenseProfile[] { 
                        new FYExpenseProfile() { Title = "A", Year = 2015, Value = 20, ValueType = "%" },
                        new FYExpenseProfile() { Title = "B", Year = 2016, Value = 30, ValueType = "%" },
                        new FYExpenseProfile() { Title = "C", Year = 2017, Value = 40, ValueType = "%" }
                    }.ToList()
                },
                MaturityRisks = new MaturityRiskMatrix[] {
                    new MaturityRiskMatrix()
                    {
                        Title = "Type 0",
                        NPTCost = 1,
                        NPTTime = 2,
                        TECOPCost = 3,
                        TECOPTime = 4
                    },
                    new MaturityRiskMatrix()
                    {
                        Title = "Type 1",
                        NPTCost = 11,
                        NPTTime = 22,
                        TECOPCost = 33,
                        TECOPTime = 44
                    },
                    new MaturityRiskMatrix()
                    {
                        Title = "Type 2",
                        NPTCost = 4,
                        NPTTime = 5,
                        TECOPCost = 3,
                        TECOPTime = 2
                    },
                    new MaturityRiskMatrix()
                    {
                        Title = "Type 3",
                        NPTCost = 6,
                        NPTTime = 4,
                        TECOPCost = 7,
                        TECOPTime = 3
                    },
                }.ToList(),
                RigRate = new RigRates()
                {
                    Value = new FYExpenseProfile[] { 
                        new FYExpenseProfile() { Title = "A", Year = 2015, Value = 20, ValueType = "%" },
                        new FYExpenseProfile() { Title = "B", Year = 2016, Value = 30, ValueType = "%" },
                        new FYExpenseProfile() { Title = "C", Year = 2017, Value = 40, ValueType = "%" }
                    }.ToList()
                }
            }.Save();

            new BizMaster()
            {
                ActivityType = "WHOLE COMPLETION EVENT",
                Country = "Nebraska",
                RigName = "BRUTUS",
                CSO = new CSO()
                {
                    CSOValues = new FYExpenseProfile[] { 
                        new FYExpenseProfile() { Title = "A", Year = 2015, Value = 20, ValueType = "%" },
                        new FYExpenseProfile() { Title = "B", Year = 2016, Value = 30, ValueType = "%" },
                        new FYExpenseProfile() { Title = "C", Year = 2017, Value = 40, ValueType = "%" }
                    }.ToList()
                },
                GlobalMarketEscalation = new GlobalMarketEscalation()
                {
                    EscalationFactor = "",
                    MaterialCostWeight = 10,
                    RigCostWeight = 20,
                    RigName = "AUGER",
                    ServicesCostWeight = 30
                },
                Inflation = new Inflation()
                {
                    Forecast = 34
                },
                LongLead = new LongLead()
                {
                    //MonthRequired = new FYExpenseProfile[] {
                    //    new FYExpenseProfile() { Title = "A", Year = 2015, Value = 20, ValueType = "%" },
                    //    new FYExpenseProfile() { Title = "B", Year = 2016, Value = 30, ValueType = "%" },
                    //    new FYExpenseProfile() { Title = "C", Year = 2017, Value = 40, ValueType = "%" }
                    //}.ToList(),
                    //Tangible = new FYExpenseProfile[] {
                    //    new FYExpenseProfile() { Title = "A", Year = 2015, Value = 20, ValueType = "" },
                    //    new FYExpenseProfile() { Title = "B", Year = 2016, Value = 30, ValueType = "" },
                    //    new FYExpenseProfile() { Title = "C", Year = 2017, Value = 40, ValueType = "" }
                    //}.ToList(),
                },
                MarketEscalation = new MarketEscalation()
                {
                    FiscalYears = new FYExpenseProfile[] { 
                        new FYExpenseProfile() { Title = "A", Year = 2015, Value = 20, ValueType = "%" },
                        new FYExpenseProfile() { Title = "B", Year = 2016, Value = 30, ValueType = "%" },
                        new FYExpenseProfile() { Title = "C", Year = 2017, Value = 40, ValueType = "%" }
                    }.ToList()
                },
                MaturityRisks = new MaturityRiskMatrix[] {
                    new MaturityRiskMatrix()
                    {
                        Title = "Type 0",
                        NPTCost = 1,
                        NPTTime = 2,
                        TECOPCost = 3,
                        TECOPTime = 4
                    },
                    new MaturityRiskMatrix()
                    {
                        Title = "Type 1",
                        NPTCost = 11,
                        NPTTime = 22,
                        TECOPCost = 33,
                        TECOPTime = 44
                    },
                    new MaturityRiskMatrix()
                    {
                        Title = "Type 2",
                        NPTCost = 4,
                        NPTTime = 5,
                        TECOPCost = 3,
                        TECOPTime = 2
                    },
                    new MaturityRiskMatrix()
                    {
                        Title = "Type 3",
                        NPTCost = 6,
                        NPTTime = 4,
                        TECOPCost = 7,
                        TECOPTime = 3
                    },
                }.ToList(),
                RigRate = new RigRates()
                {
                    Value = new FYExpenseProfile[] { 
                        new FYExpenseProfile() { Title = "A", Year = 2015, Value = 20, ValueType = "%" },
                        new FYExpenseProfile() { Title = "B", Year = 2016, Value = 30, ValueType = "%" },
                        new FYExpenseProfile() { Title = "C", Year = 2017, Value = 40, ValueType = "%" }
                    }.ToList()
                }
            }.Save();

            return MvcTools.ToJsonResult(new ResultInfo());
        }
    }

    public class LongLeadItemsResult
    {
        public String TabTitle { set; get; }
        public List<Dictionary<string, object>> Items { set; get; }
        public string ValueType { set; get; }
        public Dictionary<string, object> Fields { set; get; }
        public List<Dictionary<string, object>> Columns { set; get; }
    }

    public class AdminBaseParam
    {
        public string ActivityType { set; get; }
        public string Country { set; get; }
        public string RigName { set; get; }
    }
}