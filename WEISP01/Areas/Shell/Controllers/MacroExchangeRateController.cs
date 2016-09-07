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
    public class MacroExchangeRateController : Controller
    {
        //
        // GET: /Shell/MacroInformation/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetData(string country = "", string baseop = "", string alphabet = "A")
        {
            var ri = new ResultInfo();

            try
            {

                List<IMongoQuery> queries = new List<IMongoQuery>();
                queries.Add(Query.Matches("_id", BsonRegularExpression.Create(new Regex("WRE"))));
                queries.Add(Query.EQ("BaseOP", baseop));
                if (!country.Equals(""))
                {
                    queries.Add(Query.Matches("Country", new BsonRegularExpression(new Regex(country.ToString().ToLower().Trim(), RegexOptions.IgnoreCase))));
                }
                queries.Add(Query.Matches("Country", BsonRegularExpression.Create("^" + alphabet, "i")));
                var q = Query.Null;
                if (queries.Count > 0)
                {
                    q = Query.And(queries.ToArray());
                }
                var data = MacroEconomic.Populate<MacroEconomic>(q, sort: SortBy.Ascending("Country"));
                if (data.Any())
                {
                    var totalExchangeRates = data.Max(d => d.ExchangeRate.AnnualValues.Count);
                    var items = data.Where(d => d.ExchangeRate.AnnualValues.Count < totalExchangeRates);

                    foreach (var item in items)
                    {
                        for (var i = 0; i < totalExchangeRates; i++)
                        {
                            if (item.ExchangeRate.AnnualValues == null)
                            {
                                item.ExchangeRate.AnnualValues = new List<AnnualHelper>();
                            }

                            item.ExchangeRate.AnnualValues.Add(new AnnualHelper());
                        }
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

        public JsonResult getAlphabet()
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                var countries = MacroEconomic.Populate<MacroEconomic>().GroupBy(x => x.Country.Substring(0, 1)).Select(x => x.Key).OrderBy(x => x);
                return countries;
            });
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

        public JsonResult UpdateInline(List<MacroEconomic> updates, string baseop)
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                if (updates.Any())
                {
                    updates.ForEach(e =>
                    {
                        if (e.BaseOP.Equals(baseop))
                        {
                            e.Save();
                        }
                    });
                }

                return "OK";
            });
        }

        public JsonResult GetBaseOP()
        {
            return MvcResultInfo.Execute(() =>
            {
                //initiate batch
                BatchMacroDataWithBaseOP();
                BatchMacroDataCurrencyDefault();
                var data = MacroEconomic.Populate<MacroEconomic>(Query.Matches("_id", BsonRegularExpression.Create(new Regex("WRE")))).GroupBy(x => x.BaseOP).Select(x => x.Key).ToList<string>();
                return data.ToList();
            });
        }
        public JsonResult BatchMacroDataWithBaseOP()
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                List<IMongoQuery> queries = new List<IMongoQuery>();
                queries.Add(Query.Matches("_id", BsonRegularExpression.Create(new Regex("WRE"))));
                queries.Add(Query.Matches("Country", new BsonRegularExpression(new Regex("United States".ToLower().Trim(), RegexOptions.IgnoreCase))));

                var q = Query.Null;
                if (queries.Count > 0)
                {
                    q = Query.And(queries.ToArray());
                }
                var check = MacroEconomic.Get<MacroEconomic>(q);
                if (check.BaseOP == null)
                {
                    var fetchData = MacroEconomic.Populate<MacroEconomic>();
                    DataHelper.Delete("WEISMacroEconomics", Query.Matches("_id", BsonRegularExpression.Create(new Regex("WRE"))));
                    if (fetchData.Any())
                    {
                        foreach (var r in fetchData)
                        {
                            MacroEconomic mc = new MacroEconomic();
                            mc.Country = r.Country;
                            mc.Currency = r.Currency;
                            mc.SwiftCode = r.SwiftCode;
                            mc.Category = r.Category;
                            mc.Continent = r.Continent;
                            mc.MajorCountry = r.MajorCountry;
                            mc.BaseOP = "OP15";
                            mc.InterestRateShort = r.InterestRateShort;
                            mc.InterestRateLong = r.InterestRateLong;
                            mc.ExchangeRate = r.ExchangeRate;
                            mc.Inflation = r.Inflation;
                            mc.GDP = r.GDP;
                            mc.Save();
                        }
                    }
                }
                return "OK";
            });
        }
        public JsonResult BatchMacroDataCurrencyDefault()
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                //Check to Set
                List<IMongoQuery> queriesz = new List<IMongoQuery>();
                queriesz.Add(Query.Matches("_id", BsonRegularExpression.Create(new Regex("WRE"))));
                queriesz.Add(Query.Matches("Country", new BsonRegularExpression(new Regex("United States".ToLower().Trim(), RegexOptions.IgnoreCase))));
                queriesz.Add(Query.NE("BaseOP", "OP15"));
                var qz = Query.Null;
                if (queriesz.Count > 0)
                {
                    qz = Query.And(queriesz.ToArray());
                }
                var checkz = MacroEconomic.Get<MacroEconomic>(qz);
                if (checkz.Currency == null || checkz.Currency == "")
                {
                    List<IMongoQuery> queries = new List<IMongoQuery>();
                    queries.Add(Query.Matches("_id", BsonRegularExpression.Create(new Regex("WRE"))));
                    queries.Add(Query.Matches("Country", new BsonRegularExpression(new Regex("United States".ToLower().Trim(), RegexOptions.IgnoreCase))));
                    queries.Add(Query.NE("BaseOP", "OP15"));

                    var q = Query.Null;
                    if (queries.Count > 0)
                    {
                        q = Query.And(queries.ToArray());
                    }
                    var check = MacroEconomic.Populate<MacroEconomic>(q);
                    //check Currency

                    if (check.Any())
                    {
                        List<IMongoQuery> queriesx = new List<IMongoQuery>();
                        queriesx.Add(Query.Matches("_id", BsonRegularExpression.Create(new Regex("WRE"))));

                        var qx = Query.Null;
                        if (queriesx.Count > 0)
                        {
                            qx = Query.And(queriesx.ToArray());
                        }
                        var fetchData = MacroEconomic.Populate<MacroEconomic>(qx);

                        if (fetchData.Any())
                        {
                            foreach (var r in fetchData)
                            {
                                //set Currency
                                List<IMongoQuery> queries2 = new List<IMongoQuery>();
                                queries2.Add(Query.Matches("_id", BsonRegularExpression.Create(new Regex("WRE"))));
                                queries2.Add(Query.Matches("Country", r.Country));
                                queries2.Add(Query.Matches("BaseOP", "OP15"));

                                var q2 = Query.Null;
                                if (queries2.Count > 0)
                                {
                                    q2 = Query.And(queries2.ToArray());
                                }
                                var _check = MacroEconomic.Get<MacroEconomic>(q2);
                                var currencyId = "";
                                if (_check == null)
                                {
                                    currencyId = "";
                                }
                                else
                                {
                                    currencyId = _check.Currency.ToString();
                                }
                                MacroEconomic mc = new MacroEconomic();
                                mc.Country = r.Country;
                                mc.Currency = currencyId;
                                mc.SwiftCode = r.SwiftCode;
                                mc.Category = r.Category;
                                mc.Continent = r.Continent;
                                mc.MajorCountry = r.MajorCountry;
                                mc.BaseOP = r.BaseOP;
                                mc.InterestRateShort = r.InterestRateShort;
                                mc.InterestRateLong = r.InterestRateLong;
                                mc.ExchangeRate = r.ExchangeRate;
                                mc.Inflation = r.Inflation;
                                mc.GDP = r.GDP;
                                mc.Save();
                            }
                        }
                    }
                }

                return "OK";
            });
        }
    }
}