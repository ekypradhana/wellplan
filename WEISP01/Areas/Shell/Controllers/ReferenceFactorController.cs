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
    public class ReferenceFactorController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("Index", "ReferenceFactor/Subjects");
        }




        public JsonResult PopulateSampleData()
        {
            new WEISReferenceFactorModel()
            {
                GroupCase = "global",
                Country = "*",
                SubjectMatters = new Dictionary<string,Dictionary<string,double>>() {
                    { WEISReferenceFactorModel.GetSubjectMatterOptions()[0], new Dictionary<string,double>() {
                        { "Year_2015", 20.5 },
                        { "Year_2016", 21.6 },
                        { "Year_2017", 22.7 },
                        { "Year_2018", 23.8 },
                    } },
                    { WEISReferenceFactorModel.GetSubjectMatterOptions()[1], new Dictionary<string,double>() {
                        { "Year_2015", 10.5 },
                        { "Year_2016", 11.6 },
                        { "Year_2017", 12.7 },
                        { "Year_2018", 13.8 },
                    } },
                    { WEISReferenceFactorModel.GetSubjectMatterOptions()[2], new Dictionary<string,double>() {
                        { "Year_2015", 30.5 },
                        { "Year_2016", 31.6 },
                        { "Year_2017", 32.7 },
                        { "Year_2018", 33.8 },
                    } },
                    { WEISReferenceFactorModel.GetSubjectMatterOptions()[3], new Dictionary<string,double>() {
                        { "Year_2015", 30.5 },
                        { "Year_2016", 31.6 },
                        { "Year_2017", 32.7 },
                        { "Year_2018", 33.8 },
                    } }
                }
            };//.Save();

            return MvcTools.ToJsonResult(new ResultInfo());
        }

        public bool IsAllowedToEdit()
        {
            var allowed = false;
            var appIssueC = new ECIS.AppServer.Areas.Issue.Controllers.AppIssueController();
            string config = Convert.ToString(Config.GetConfigValue("RolesToEditBizPlan", ""));

            config.Split(',').ToList().ForEach(d =>
            {
                if (!allowed)
                {
                    allowed = appIssueC.isRole(d);
                }
            });

            return allowed;
        }

        public ActionResult Subjects()
        {
            ViewBag.SubjectMatters = WEISReferenceFactorModel.GetSubjectMatterOptions().ToList();
            ViewBag.CanEdit = IsAllowedToEdit();
            var rfm = new WEISReferenceFactorModel();
            var defaultData = rfm.Get("*", "*", "*", "*");
            if (defaultData == null)
            {
                var date = DateTime.Now;

                defaultData = new WEISReferenceFactorModel()
                {
                    ActivityType = "*",
                    Country = "*",
                    GroupCase = "*",
                    WellName = "*",
                    SubjectMatters = new Dictionary<string, Dictionary<string, double>>()
                };

                WEISReferenceFactorModel.GetSubjectMatterOptions().ToList().ForEach(d =>
                {
                    defaultData.SubjectMatters[d] = new Dictionary<string, double>();
                    defaultData.SubjectMatters[d]["Year_" + date.Year] = 0;
                });

                defaultData.Save();
            }

            return View();
        }

        public ActionResult ProjectRFM()
        {
            return View();
        }
        public JsonResult GetProjectRFMData(bool IsOnlyTagged, bool IsNotTagged, List<string> Projects = null, List<string> RFMs = null)
        {
            Projects = Projects ?? new List<string>();
            RFMs = RFMs ?? new List<string>();

            var ri = new ResultInfo();
            try
            {
                var allProjects = MasterProject.Populate<MasterProject>().Select(d => Convert.ToString(d._id))
                    .Concat(ProjectReferenceFactor.Populate<ProjectReferenceFactor>().Select(d => d.ProjectName).ToList())
                    .ToList();

                if (Projects.Any())
                    allProjects = allProjects.Where(d => Projects.Contains(d)).ToList();

                var data = allProjects.GroupBy(d => d).Select(d =>
                {
                    var projectRFM = ProjectReferenceFactor.Get<ProjectReferenceFactor>(Query.EQ("ProjectName", d.Key)) ?? new ProjectReferenceFactor();
                    var projectRFMvalue = (projectRFM.ReferenceFactorModels ?? new List<string>());
                    var stringRFMs = "";

                    if (projectRFM != null)
                        stringRFMs = string.Join(", ", projectRFMvalue.OrderBy(e => e));

                    return new { Project = d.Key, RFMs = stringRFMs };
                })
                .OrderBy(d => d.Project)
                .ToList();

                if (RFMs.Any())
                {
                    RFMs.ForEach(d =>
                    {
                        data = data.Where(e => e.RFMs.Contains(d)).ToList();
                    });
                }

                if (!IsOnlyTagged)
                {
                    data = data.Where(d => d.RFMs.Trim().Equals("")).ToList();
                }

                if (!IsNotTagged)
                {
                    data = data.Where(d => !d.RFMs.Trim().Equals("")).ToList();
                }

                ri.Data = data;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }
        public ActionResult CountryRFM()
        {
            return View();
        }

        public JsonResult GetCountryRFMData(bool IsOnlyTagged, bool IsNotTagged, List<string> Countries = null, List<string> RFMs = null)
        {
            Countries = Countries ?? new List<string>();
            RFMs = RFMs ?? new List<string>();

            var ri = new ResultInfo();
            try
            {
                var allCountries = MasterCountry.Populate<MasterCountry>().Select(d => Convert.ToString(d._id)).ToList();

                if (Countries.Any())
                    allCountries = allCountries.Where(d => Countries.Contains(d)).ToList();

                var data = allCountries.GroupBy(d => d).Select(d =>
                {
                    var countryRFM = CountryReferenceFactor.Get<CountryReferenceFactor>(Query.EQ("Country", d.Key)) ?? new CountryReferenceFactor();
                    var countryRFMvalue = (countryRFM.ReferenceFactorModels ?? new List<string>());
                    var stringRFMs = "";

                    if (countryRFM != null)
                        stringRFMs = string.Join(", ", countryRFMvalue.OrderBy(e => e));

                    return new { Country = d.Key, RFMs = stringRFMs, ListRFM = countryRFM == null ? new List<string>() : countryRFM.ReferenceFactorModels };
                })
                .OrderBy(d => d.Country)
                .ToList();

                if (RFMs.Any())
                {
                    RFMs.ForEach(d =>
                    {
                        data = data.Where(e => e.RFMs.Contains(d)).ToList();
                    });
                }

                if (!IsOnlyTagged)
                {
                    data = data.Where(d => d.RFMs.Trim().Equals("")).ToList();
                }

                if (!IsNotTagged)
                {
                    data = data.Where(d => !d.RFMs.Trim().Equals("")).ToList();
                }

                ri.Data = data;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult Delete(SubjectMatterParam param)
        {
            var ri = new ResultInfo();
            try
            {
                param.Parse();

                var query = Query.And(
                    Query.EQ("ReferenceFactorModel", param.GroupCase),
                    Query.EQ("Country", param.Country),
                    Query.EQ("Phases.Estimate.SaveToOP", param.BaseOP)
                );
                var bizPlan = BizPlanActivity.GetAll(query, take: 1).FirstOrDefault();

                if (bizPlan != null)
                {
                    var who = "Well: " + bizPlan.WellName;

                    var phase = bizPlan.Phases.FirstOrDefault();
                    if (phase != null) {
                        who += ", Activity: " + phase.ActivityType;
                    }

                    ri.PushException(new Exception("Cannot failed. This model is used by " + who));
                    return MvcTools.ToJsonResult(ri);
                }

                if (param.Country.Equals("*"))
                {
                    DataHelper.Delete(new WEISReferenceFactorModel().TableName, Query.And(
                        Query.EQ("GroupCase", param.GroupCase),
                        Query.NE("Country", param.Country),
                        Query.EQ("BaseOP",param.BaseOP)
                    ));
                }
                else
                {
                    var rfm = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.And(
                        Query.EQ("Country", param.Country),
                        Query.EQ("GroupCase", param.GroupCase),
                        Query.EQ("BaseOP",param.BaseOP)
                    ));

                    if (rfm != null)
                    {
                        rfm.Delete();
                    }
                }

                AdministrationInput.RecalculateBusPlan();

                var groupcase = DataHelper.Populate("WEISReferenceFactorModel")
                    .Select(d => Convert.ToString(d.GetString("GroupCase"))).Distinct().OrderBy(d => d);
                ri.Data = groupcase;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        private Dictionary<string, object>[] GetData(SubjectMatterParam param, WEISReferenceFactorModel rfm, string baseop)
        {
            if (rfm == null)
            {
                rfm = new WEISReferenceFactorModel();
                rfm.GroupCase = param.GroupCase;
                rfm.Country = param.Country;
                rfm.WellName = param.WellName;
                rfm.ActivityType = param.ActivityType;
                rfm.BaseOP = baseop;
            }

            var CompFactor = getCompundFactor();
            var CompFactorYear = getCompundFactorYear();
            if (rfm.SubjectMatters == null)
                rfm.SubjectMatters = new Dictionary<string, Dictionary<string, double>>();

            WEISReferenceFactorModel.GetSubjectMatterOptions().ToList().ForEach(d =>
            {
                double InflationValue = 0;
                if (!rfm.SubjectMatters.ContainsKey(d))
                {
                    if (d.Contains("Inflation"))
                    {
                        InflationValue = GetInflationMacroData(rfm.Country, rfm.BaseOP, DateTime.Now.Year);
                    }
                    rfm.SubjectMatters[d] = new Dictionary<string, double>() { { "Year_" + DateTime.Now.Year, InflationValue } };
                }

                if (rfm.SubjectMatters[d].Count < 5)
                {
                    var max = rfm.SubjectMatters[d].Max(e => Convert.ToInt32(e.Key.Replace("Year_", "")));
                    if (max == 0)
                    {
                        max = 2015;
                    }

                    var to = 5 - rfm.SubjectMatters[d].Count;
                    double inflationCustomValue = 0;
                    for (var i = 1; i <= to; i++)
                    {
                        if(rfm.BaseOP.Equals("OP16"))
                        { }
                        var tempYear = max + i;
                        if (d.Contains("Inflation") && tempYear < CompFactorYear)
                        {
                            inflationCustomValue = GetInflationMacroData(rfm.Country, rfm.BaseOP, tempYear);
                        }
                        else if (d.Contains("Inflation"))
                        {
                            inflationCustomValue += InflationValue;
                            inflationCustomValue = (((1.0 + (CompFactor / 100.0)) * (1.0 + (inflationCustomValue / 100.0))) - 1.0) * 100.0;
                        }
                        else
                        {
                            inflationCustomValue = 0.0;
                        }
                        rfm.SubjectMatters[d]["Year_" + (max + i)] = inflationCustomValue;
                    }
                }
            });

            var data = rfm.SubjectMatters.Select(d =>
            {
                var doc = new BsonDocument();
                doc.Set("Subject", d.Key);

                foreach (KeyValuePair<string, double> each in d.Value)
                {
                    doc.Set(each.Key, each.Value);
                }

                return doc;
            })
            .Where(d => param.SubjectMatters.ToList().Contains(d.GetString("Subject")))
            .OrderBy(d => d.GetString("Subject"))
            .ToList();

            return DataHelper.ToDictionaryArray(data);
        }

        public double GetInflationMacroData(string country,string baseop,int year)
        {
            double InflationValue = 0;
            try
            {
                List<IMongoQuery> queries = new List<IMongoQuery>();
                queries.Add(Query.Matches("_id", BsonRegularExpression.Create(new Regex("WRE"))));
                queries.Add(Query.EQ("Country", country));
                queries.Add(Query.EQ("BaseOP", baseop));
                var q = Query.Null;
                q = Query.And(queries.ToArray());

                var getInflation = MacroEconomic.Get<MacroEconomic>(q).Inflation.AnnualValues.Where(x => x.Year.Equals(year)).FirstOrDefault();

                if (getInflation == null)
                {
                    InflationValue = 0;
                }
                else
                {
                    InflationValue = Convert.ToDouble(getInflation.Value);
                }
                return InflationValue;
            }
            catch (Exception)
            {

                InflationValue = 0;
            }
            return InflationValue;
        }

        #region GetDataBySubjectMatter
        public JsonResult GetDataBySubjectMatter(SubjectMatterParam param)
        {
            var ri = new ResultInfo();

            try
            {
                param.Parse();
                var newrfm = new WEISReferenceFactorModel();
                var rfm = newrfm.Get(param.GroupCase, param.Country, param.ActivityType, param.WellName, param.BaseOP);
                ri.Data = GetData(param, rfm, param.BaseOP);
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }
        #endregion
        

        public JsonResult GetDataForAllCountriesBySubjectMatter(SubjectMatterParam param)
        {
            var ri = new ResultInfo();
            if (param.BaseOPs == null || param.BaseOPs.Count == 0)
            {
                param.BaseOPs = new List<string>();
                var getMasterOPs = MasterOP.Populate<MasterOP>().Select(x=>x._id.ToString()).ToList();
                foreach (var o in getMasterOPs)
                {
                    param.BaseOPs.Add(o);
                }
            }

            var ListCountries = new List<string>();
            if (param.Country == "All Countries")
            {
                var getMasterCountries = MasterCountry.Populate<MasterCountry>().Select(x=>x._id.ToString()).ToList();
                ListCountries.AddRange(getMasterCountries);
            }
            else
            {
                ListCountries.Add(param.Country);
            }

            try
            {
                param.Parse();

                var dataPerCountries = new List<BsonDocument>();
                ListCountries.ForEach(d =>
                {
                    var newrfm = new WEISReferenceFactorModel();
                    foreach (var op in param.BaseOPs)
                    {
                        var rfm = newrfm.Get(param.GroupCase, d, param.ActivityType, param.WellName,op);
                        var data = GetData(param, rfm,op);
                        var res = new { BaseOP = op, Country = d, Data = data }.ToBsonDocument();
                        dataPerCountries.Add(res);
                    }
                });

                //get Inflation factor
                var initialInflation = 0.0;
                var Country = param.Country;
                var dataME = MacroEconomic.Get<MacroEconomic>(Query.EQ("Country",Country));
                if (dataME != null)
                {
                    var yearNow = DateTime.Now.Year;
                    var getInf = dataME.Inflation.AnnualValues.Where(x => x.Year.Equals(yearNow)).FirstOrDefault();
                    if (getInf != null)
                    {
                        initialInflation = getInf.Value;
                    }
                }

                ri.Data = new
                {
                    Data = DataHelper.ToDictionaryArray(dataPerCountries),
                    Alphabet = dataPerCountries.GroupBy(d => d.GetString("Country").Substring(0, 1)).Select(d => d.Key).OrderBy(d => d).ToList(),
                    initialInflation
                };
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult SaveDataForSubjectMatter(SubjectMatterParam header, List<Dictionary<string, object>> changes, bool recalc = true)
        {
            var ri = new ResultInfo();

            try
            {
                header.Parse();
                double CompFactor = getCompundFactor();
                double CompFactorYear = getCompundFactorYear();
                if (header.BaseOPs != null && header.BaseOPs.Count > 0)
                {
                    foreach (var op in header.BaseOPs)
                    {
                        var newrfm = new WEISReferenceFactorModel();
                        var rfm = newrfm.Get(header.GroupCase, header.Country, header.ActivityType, header.WellName, op);

                        if (rfm == null)
                        {
                            rfm = new WEISReferenceFactorModel();
                            rfm.GroupCase = header.GroupCase;
                            rfm.Country = header.Country;
                            rfm.WellName = header.WellName;
                            rfm.ActivityType = header.ActivityType;
                            rfm.BaseOP = op;
                        }

                        if (rfm.SubjectMatters == null)
                            rfm.SubjectMatters = new Dictionary<string, Dictionary<string, double>>();

                        if (changes == null)
                            changes = new List<Dictionary<string, object>>();

                        if (!rfm.SubjectMatters.Any())
                        {
                            var now = DateTime.Now;
                            WEISReferenceFactorModel.GetSubjectMatterOptions().ToList().ForEach(d =>
                            {
                                rfm.SubjectMatters[d] = new Dictionary<string, double>();
                                double InflationCustomValue = 0; double InflationValue = 0;
                                for (var i = 0; i < 5; i++)
                                {

                                    if (rfm.BaseOP.Equals("OP16"))
                                    { }

                                    if (d.Contains("Inflation") && ((now.Year + i) < CompFactorYear))
                                    {
                                        InflationValue = GetInflationMacroData(rfm.Country, rfm.BaseOP, now.Year + i);
                                        InflationCustomValue = InflationValue;
                                    }
                                    else if (d.Contains("Inflation") )
                                    {
                                        //InflationCustomValue += InflationValue;
                                        InflationCustomValue = (((1.0 + (CompFactor / 100.0)) * (1.0 + (InflationCustomValue / 100.0))) - 1.0) * 100.0;
                                    }
                                    rfm.SubjectMatters[d]["Year_" + (now.Year + i)] = InflationCustomValue;
                                }
                            });
                        }

                        changes.ForEach(d =>
                        {
                            var subject = Convert.ToString(d["Subject"]);

                            if (!rfm.SubjectMatters.ContainsKey(subject))
                                rfm.SubjectMatters[subject] = new Dictionary<string, double>();

                            foreach (KeyValuePair<string, object> each in d)
                            {
                                if (!each.Key.Contains("Year_"))
                                    continue;

                                rfm.SubjectMatters[subject][each.Key] = Convert.ToDouble(each.Value);
                            }
                        });

                        rfm.Save();
                    }
                }
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            if (recalc)
                AdministrationInput.RecalculateBusPlan();

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult AddYearForSubjectMatter(SubjectMatterParam param)
        {
            var ri = new ResultInfo();

            try
            {
                param.Parse();
                var newrfm = new WEISReferenceFactorModel();
                var rfm = newrfm.Get(param.GroupCase, param.Country, param.ActivityType, param.WellName,param.BaseOP);

                if (rfm == null)
                {
                    if (param.BaseOPs == null) param.BaseOPs = new List<string>();
                    param.BaseOPs.Add(param.BaseOP);
                    SaveDataForSubjectMatter(param, null, false);
                    rfm = newrfm.Get(param.GroupCase, param.Country, param.ActivityType, param.WellName,param.BaseOP);
                }

                var years = new List<Int32>();
                var now = DateTime.Now;
                
                foreach (KeyValuePair<string, Dictionary<string, double>> each in rfm.SubjectMatters)
                {
                    var eachYears = rfm.SubjectMatters[each.Key].Keys.Select(e => Convert.ToInt32(e.Replace("Year_", "")));
                    var currentYear = eachYears.DefaultIfEmpty(now.Year).Max();
                    var nextYear = currentYear + 1;

                    var value = 0.0;
                    if (rfm.SubjectMatters[each.Key].ContainsKey("Year_" + currentYear))
                        value = rfm.SubjectMatters[each.Key]["Year_" + currentYear];

                    rfm.SubjectMatters[each.Key]["Year_" + nextYear] = value;
                }

                rfm.Save();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            AdministrationInput.RecalculateBusPlan();

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RemoveYearForSubjectMatter(SubjectMatterParam param)
        {
            var ri = new ResultInfo();

            try
            {
                param.Parse();
                var newrfm = new WEISReferenceFactorModel();
                var rfm = newrfm.Get(param.GroupCase, param.Country, param.ActivityType, param.WellName,param.BaseOP);

                if (rfm == null)
                {
                    ri.PushException(new Exception("Cannot delete more year"));
                    return MvcTools.ToJsonResult(ri);
                }

                var years = new List<Int32>();
                var now = DateTime.Now;

                foreach (KeyValuePair<string, Dictionary<string, double>> each in rfm.SubjectMatters)
                {
                    if (rfm.SubjectMatters[each.Key].Keys.Count <= 5)
                    {
                        ri.PushException(new Exception("Cannot delete more year"));
                        return MvcTools.ToJsonResult(ri);
                    }

                    var eachYears = rfm.SubjectMatters[each.Key].Keys.Select(e => Convert.ToInt32(e.Replace("Year_", "")));
                    var latestYear = eachYears.DefaultIfEmpty(now.Year).Max();

                    rfm.SubjectMatters[each.Key].Remove("Year_" + latestYear);
                }

                rfm.Save();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            AdministrationInput.RecalculateBusPlan();

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult AutoCompleteDataSource()
        {
            var ri = new ResultInfo();
            try
            {
                ri.Data = DataHelper.Populate("WEISReferenceFactorModel")
                    .Select(d => Convert.ToString(d.GetString("GroupCase")))
                    .Distinct()
                    .OrderBy(d => d).ToList();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetProjectRFM(string ProjectName)
        {
            var ri = new ResultInfo();

            try
            {
                var rfm = ProjectReferenceFactor.Get<ProjectReferenceFactor>(Query.EQ("ProjectName", ProjectName))
                    ?? new ProjectReferenceFactor();

                ri.Data = rfm.ReferenceFactorModels ?? new List<string>();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult SaveProjectRFM(string ProjectName, List<string> RFMs = null)
        {
            var ri = new ResultInfo();

            try
            {
                var rfm = ProjectReferenceFactor.Get<ProjectReferenceFactor>(Query.EQ("ProjectName", ProjectName));
                if (rfm == null)
                    rfm = new ProjectReferenceFactor() { ProjectName = ProjectName };

                rfm.ReferenceFactorModels = (RFMs ?? new List<string>());
                rfm.Save();

                ri.Data = rfm.ReferenceFactorModels;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            AdministrationInput.RecalculateBusPlan();

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult SaveCountryRFM(string Country, List<string> RFMs = null)
        {
            var ri = new ResultInfo();

            try
            {
                var rfm = CountryReferenceFactor.Get<CountryReferenceFactor>(Query.EQ("Country", Country));
                if (rfm == null)
                    rfm = new CountryReferenceFactor() { Country = Country };

                rfm.ReferenceFactorModels = (RFMs ?? new List<string>());
                rfm.Save();

                ri.Data = rfm.ReferenceFactorModels;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            AdministrationInput.RecalculateBusPlan();

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult SaveAddYearCustom(SubjectMatterParam param, bool IsAllCountry, int ToYear = 2015, List<string> Countries = null, List<Dictionary<string, object>> SubjectMattersOptions = null, double InflationInitialValue = 0, double InflationPercentage = 0)
        {
            Countries = (Countries ?? new List<string>());
            SubjectMattersOptions = (SubjectMattersOptions ?? new List<Dictionary<string, object>>());
            var ri = new ResultInfo();
            var startYear = 2015;

            try
            {
                param.Parse();

                IMongoQuery query = null;
                if (!IsAllCountry) {
                    query = Query.In("Country", new BsonArray(Countries));
                }
                var masterOPs = MasterOP.Populate<MasterOP>();

                MacroEconomic.Populate<MacroEconomic>(query).GroupBy(d => d.Country).ToList().ForEach(d =>
                {
                    foreach (var op in masterOPs)
                    {
                        var country = d.Key;

                        var queryEachRFM = Query.And(Query.EQ("Country", country), Query.EQ("GroupCase", param.GroupCase), Query.EQ("BaseOP",op.Name));
                        var rfm = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(queryEachRFM);
                        if (rfm == null)
                        {
                            rfm = new WEISReferenceFactorModel()
                            {
                                WellName = param.WellName,
                                ActivityType = param.ActivityType,
                                GroupCase = param.GroupCase,
                                Country = country,
                                BaseOP = op.Name,
                                SubjectMatters = new Dictionary<string, Dictionary<string, double>>()
                            };

                            WEISReferenceFactorModel.GetSubjectMatterOptions().ToList().ForEach(e =>
                            {
                                if (e.Equals("Inflation Factors"))
                                {
                                    rfm.SubjectMatters[e] = new Dictionary<string, double>() 
                                { 
                                    { "Year_" + startYear, 0 }, 
                                    { "Year_" + (startYear + 1), InflationInitialValue } 
                                };
                                    return;
                                }

                                rfm.SubjectMatters[e] = new Dictionary<string, double>() { { "Year_" + startYear, 0 } };
                            });
                        }

                        SubjectMattersOptions.ForEach(e =>
                        {
                            var sm = Convert.ToString(e["Title"]);
                            var items = rfm.SubjectMatters[sm];

                            if (sm.Equals("Inflation Factors"))
                            {
                                var inflationPreviousValue = 0.0;
                                for (var i = startYear; i <= ToYear; i++)
                                {
                                    if (i == startYear)
                                    {
                                        items["Year_" + i] = inflationPreviousValue = 0;
                                        continue;
                                    }
                                    else if (i == (startYear + 1))
                                    {
                                        items["Year_" + i] = inflationPreviousValue = InflationInitialValue;
                                        continue;
                                    }

                                    var currentValue = (((1.0 + (InflationPercentage / 100.0)) * (1.0 + (inflationPreviousValue / 100.0))) - 1.0) * 100.0;
                                    items["Year_" + i] = currentValue;
                                    inflationPreviousValue = currentValue;
                                }

                                return;
                            }

                            var latestYear = items.Max(f => Convert.ToInt32(f.Key.Replace("Year_", "")));
                            var latestValue = items["Year_" + latestYear];

                            for (var i = (latestYear + 1); i <= ToYear; i++)
                            {
                                items["Year_" + i] = (Convert.ToBoolean(e["IsUsingLastYearValue"])) ? latestValue : Convert.ToDouble(e["Value"]);
                            }
                        });

                        rfm.Save();
                    }
                });
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            AdministrationInput.RecalculateBusPlan();

            return MvcTools.ToJsonResult(ri);
        }

        public double getCompundFactor()
        {
            var Val = Config.Get<Config>(Query.EQ("_id", "CompoundFactor"));
            if (Val != null)
            {
                return Convert.ToDouble(Val.ConfigValue);
            }
            else
            {
                return 0;
            }
        }

        public double getCompundFactorYear()
        {
            var Val = Config.Get<Config>(Query.EQ("_id", "CompoundFactorYear"));
            if (Val != null)
            {
                return Convert.ToDouble(Val.ConfigValue);
            }
            else
            {
                return 0;
            }
        }

        public JsonResult GetCalculationStatus(int Counter = 0)
        {
            var ri = new ResultInfo();
            try 
            {
                if (Counter >= 20)
                {
                    // reset calculation status when too long
                    AdministrationInput.ResetRecalculaeBusPlanStatus();
                }

                var result = new { IsRecalculate = true, Progress = 0.0 }.ToBsonDocument();
                var status = AdministrationInput.IsRecalculateBusPlan();
                result.Set("IsRecalculate", status);

                if (status)
                {
                    var progress = AdministrationInput.GetRecalculationProgress();
                    result.Set("Progress", progress);
                }

                ri.Data = result.ToDictionary();
            }
            catch (Exception e) {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }
	}

    
}