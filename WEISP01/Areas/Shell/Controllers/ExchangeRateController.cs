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
using System.Threading.Tasks;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class ExchangeRateController : Controller
    {
        //
        // GET: /Shell/ExchangeRate/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetDataExchangeRate(List<string> country)
        {
            var ri = new ResultInfo();

            try
            {
                country = (country == null ? new List<string>() : country);
                List<IMongoQuery> queries = new List<IMongoQuery>();
                if (country.Count > 0)
                        queries.Add(Query.In("Country", new BsonArray(country)));
                queries.Add(Query.Matches("_id", BsonRegularExpression.Create(new Regex("WRE"))));

                var q = Query.Null;
                if (queries.Count > 0)
                {
                    q = Query.And(queries.ToArray());
                }

                 
                var data = MacroEconomic.Populate<MacroEconomic>(q, sort: SortBy.Ascending("Country"));
                
                var checkCurrency = MacroEconomic.Populate<MacroEconomic>(Query.NE("Currency", ""));
                if (checkCurrency.Count() < 10)
                {
                    if (data.Any())
                    {
                        foreach (var r in data)
                        {
                            var fetchCurrency = DataHelper.Get("WEISCountries", Query.EQ("_id", r.Country));
                            if (fetchCurrency != null)
                            {
                                MacroEconomic ma = new MacroEconomic();
                                ma._id = r._id;
                                ma.Country = fetchCurrency[0].ToString();
                                ma.Currency = fetchCurrency[4].ToString();
                                ma.Continent = r.Continent;
                                ma.MajorCountry = r.MajorCountry;
                                ma.InterestRateShort = r.InterestRateShort;
                                ma.InterestRateLong = r.InterestRateLong;
                                ma.ExchangeRate = r.ExchangeRate;
                                ma.Inflation = r.Inflation;
                                ma.GDP = r.GDP;
                                ma.Save();
                            }
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

        public JsonResult GetCountry()
        {
            ResultInfo ri = new ResultInfo();
            try
            {
                var c = MasterCountry.Populate<MasterCountry>();
                var countries = new List<string>();
                foreach (var data in c)
                {
                    var m = MacroEconomic.Get<MacroEconomic>(Query.EQ("Country", data._id.ToString()));
                    if (m == null)
                    {
                        countries.Add(data._id.ToString());
                    }
                }
                ri.Data = countries;
            }
            catch (Exception e)
            {

                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }
        private List<AnnualHelper> GetAnnualValuesOfType(MacroEconomic me, string Type = "GDP", string InterestRatesType = "short")
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

        public JsonResult AddAnnualValuesType(string id, Int32 Year, double Value, string Source = "", string Desc = "", string Type = "GDP", string InterestRatesType = "short")
        {
            var ri = new ResultInfo();
            try
            {
                MacroEconomic.Populate<MacroEconomic>().ForEach(d =>
                {
                    var annualValues = GetAnnualValuesOfType(d, Type, InterestRatesType);
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

        public JsonResult UpdateInlineExchange(List<MacroEconomic> updates)
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

                AdministrationInput.RecalculateBusPlan();

                return "OK";
            });
        }

        public JsonResult SaveCountry(string countryname, string currency="")
        {
            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                try
                {
                    var pcurrency = ""; var pcontinent = "";
                    var getCurrency = MasterCountry.Get<MasterCountry>(Query.EQ("_id", countryname));
                    if (getCurrency == null)
                    {
                        pcurrency = ""; pcontinent = "";
                    }
                    else
                    {
                        pcurrency = getCurrency.Currency;
                        pcontinent = getCurrency.Continent;
                    }
                    List<IMongoQuery> queries = new List<IMongoQuery>();
                    queries.Add(Query.EQ("Country", countryname));

                    if (MacroEconomic.Populate<MacroEconomic>(Query.Or(queries.ToArray())).Count() > 0)
                    {
                        return new
                        {
                            Success = false,
                            Message = "Same Country Name Already Exists"
                        };
                    }

                    var data = MacroEconomic.Populate<MacroEconomic>(Query.EQ("Country", "Australia"));
                    
                    var bizER = data.FirstOrDefault(f => f.ExchangeRate.AnnualValues.Any());
                    var bizGDP = data.FirstOrDefault(g => g.GDP.AnnualValues.Any());
                    var bizInflation = data.FirstOrDefault(h => h.Inflation.AnnualValues.Any());
                    var bizIRShort = data.FirstOrDefault(i => i.InterestRateShort.AnnualValues.Any());
                    var bizIRLong = data.FirstOrDefault(j => j.InterestRateLong.AnnualValues.Any());

                    var maxYearER = bizER.ExchangeRate.AnnualValues.DefaultIfEmpty(new AnnualHelper()).Max(f => f.Year);
                    var maxYearGDP = bizGDP.GDP.AnnualValues.DefaultIfEmpty(new AnnualHelper()).Max(g => g.Year);
                    var maxYearInflation = bizInflation.Inflation.AnnualValues.DefaultIfEmpty(new AnnualHelper()).Max(h => h.Year);
                    var maxYearIRShort = bizIRShort.InterestRateShort.AnnualValues.DefaultIfEmpty(new AnnualHelper()).Max(i => i.Year);
                    var maxYearIRLong = bizIRLong.InterestRateLong.AnnualValues.DefaultIfEmpty(new AnnualHelper()).Max(j => j.Year);

                    List<AnnualHelper> annualER = new List<AnnualHelper>();
                    List<AnnualHelper> annualGDP = new List<AnnualHelper>();
                    List<AnnualHelper> annualInflation = new List<AnnualHelper>();
                    List<AnnualHelper> annualIRShort = new List<AnnualHelper>();
                    List<AnnualHelper> annualIRLong = new List<AnnualHelper>();

                    for (int i = 1995; i <= maxYearER; i++)
                    {
                       annualER.Add(new AnnualHelper
                       {
                           Year = i, Value = 0, Desc = "", Source = ""
                        });
                    }

                    for (int i = 1995; i <= maxYearGDP; i++)
                    {
                        annualGDP.Add(new AnnualHelper
                        {
                            Year = i, Value = 0, Desc = "", Source = ""
                        });
                    }

                    for (int i = 1995; i <= maxYearInflation; i++)
                    {
                        annualInflation.Add(new AnnualHelper
                        {
                            Year = i, Value = 0, Desc = "", Source = ""
                        });
                    }

                    for (int i = 2010; i <= maxYearIRShort; i++)
                    {
                        annualIRShort.Add(new AnnualHelper
                        {
                            Year = i, Value = 0, Desc = "", Source = ""
                        });
                    }

                    for (int i = 2010; i <= maxYearIRLong; i++)
                    {
                        annualIRLong.Add(new AnnualHelper
                        {
                            Year = i, Value = 0, Desc = "", Source = ""
                        });
                    }

                    var exp = new ExchangeRate()
                    {
                        AnnualValues = annualER
                    };

                    var gdp = new GDP()
                    {
                        GDPLevel = 0,
                        AnnualValues = annualGDP
                    };

                    var inflation = new Inflation()
                    {
                        Forecast = 0,
                        AnnualValues = annualInflation
                    };

                    var irshort = new InterestRate()
                    {
                        AnnualValues = annualIRShort
                    };

                    var irlong = new InterestRate()
                    {
                        AnnualValues = annualIRLong
                    };

                    var mcr = new MacroEconomic()
                    {
                        Country = countryname,
                        Currency = pcurrency,
                        Continent = pcontinent,
                        ExchangeRate = exp,
                        GDP = gdp,
                        Inflation = inflation,
                        InterestRateShort = irshort,
                        InterestRateLong = irlong
                    };
                    mcr.Save();

                    AdministrationInput.RecalculateBusPlan();

                    return new
                    {
                        Success = true
                    };
                }
                catch (Exception e)
                {
                    AdministrationInput.RecalculateBusPlan();

                    return new
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
            }
            );
        }

        public JsonResult SaveAll()
        {
            var ri = new ResultInfo();
            try
            {
                MacroEconomic.Populate<MacroEconomic>().ForEach(d =>
                {
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

        public JsonResult SaveZimbabwe()
        {
            var ri = new ResultInfo();
            try
            {
                
                MacroEconomic.Populate<MacroEconomic>(Query.EQ("Country", "Zimbabwe")).ForEach(d =>
                {
                    var annualvalues = new List<AnnualHelper>();
                    for (int i = 1995; i <= 2060; i++)
                    {
                        annualvalues.Add(new AnnualHelper
                        {
                            Year = i,
                            Value = 0.0,
                            Desc = "",
                            Source = ""
                        });
                    }
                    var exp = new ExchangeRate()
                    {
                        AnnualValues = annualvalues
                    };
                    d.ExchangeRate = exp;
                    d.Save();
                });
            }
            catch (Exception e)
            {
                ri.PushException(e);                
            }

            return MvcTools.ToJsonResult(ri);
        }
	    }

}