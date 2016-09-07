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
using System.IO;
using Aspose.Cells;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class ReferenceFactor2Controller : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("Index", "ReferenceFactor2/Subjects");
        }

        public FileResult DownloadRFMDetailFile(string stringName)
        {
            //string xlst = Path.Combine(HttpRuntime.AppDomainAppPath + "App_Data\\Temp", "RFMExportTemplate.xlsx");
            var res = Path.Combine(HttpRuntime.AppDomainAppPath + "App_Data\\Temp", stringName);
            //rename file
            var retstringName = stringName;
            return File(res, Tools.GetContentType(".xlsx"), retstringName);
        }


        public void GetMaxMinYear(WEISReferenceFactorModel data, out int start, out int end)
        {
            start = 0;
            end = 0;
            var sm = data.SubjectMatters.SelectMany(x => x.Value);
            var sx = sm.Select(x => x.Key).Distinct().ToList();
            List<int> res = new List<int>();
            foreach (var s in sx)
            {
                res.Add(Convert.ToInt32(s.ToString().Replace("Year_", "")));
            }

            start = res.Min();
            end = res.Max();

        }

        public JsonResult Export(SubjectMatterParam param)
        {
            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "RFMExportTemplate.xlsx");
            //rename file
            string filename = string.Format("{0}-{1}-{2}", param.BaseOPs.FirstOrDefault(), param.Country.Replace(" ", ""), param.GroupCase.Replace(" ", ""));
            var retstringName = "RFM-" + filename + ".xlsx";

            var newFileNameSingle = Path.Combine(HttpRuntime.AppDomainAppPath + "App_Data\\Temp",
                   retstringName);

            #region generate Excel

            var q = Query.And(
                Query.EQ("Country", param.Country),
                Query.EQ("GroupCase", param.GroupCase),
                Query.EQ("BaseOP", param.BaseOPs.FirstOrDefault())
                );
            var rfmData = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(q);



            Workbook wbook = new Workbook(xlst);

            var Messages = "";
            var RFMChecking = new List<object>();

            Worksheet ws = wbook.Worksheets[0];
            Cells cells = ws.Cells;

            int start = 0;
            int finish = 0;
            GetMaxMinYear(rfmData, out start, out finish);

            cells[0, 0].PutValue("Subject");
            cells[1, 0].PutValue("CSO Factors");
            cells[2, 0].PutValue("Inflation Factors");
            cells[3, 0].PutValue("Learning Curve Factors");
            cells[4, 0].PutValue("Material Escalation Factors");
            cells[5, 0].PutValue("Service Escalation Factors");
            int index = 1;
            for (int i = start; i <= finish; i++)
            {
                cells[0, index].PutValue("Year " + i);


                var cso = rfmData.SubjectMatters.Where(x => x.Key.Equals("CSO Factors")).FirstOrDefault();
                var valthisyear = cso.Value.Where(x => x.Key.Equals("Year_" + i)).Count() > 0 ? cso.Value.Where(x => x.Key.Equals("Year_" + i)).FirstOrDefault().Value : 0;
                cells[1, index].PutValue(valthisyear);

                var inf = rfmData.SubjectMatters.Where(x => x.Key.Equals("Inflation Factors")).FirstOrDefault();
                valthisyear = inf.Value.Where(x => x.Key.Equals("Year_" + i)).Count() > 0 ? inf.Value.Where(x => x.Key.Equals("Year_" + i)).FirstOrDefault().Value : 0;
                cells[2, index].PutValue(valthisyear);

                var lrc = rfmData.SubjectMatters.Where(x => x.Key.Equals("Learning Curve Factors")).FirstOrDefault();
                valthisyear = lrc.Value.Where(x => x.Key.Equals("Year_" + i)).Count() > 0 ? lrc.Value.Where(x => x.Key.Equals("Year_" + i)).FirstOrDefault().Value : 0;
                cells[3, index].PutValue(valthisyear);

                var mat = rfmData.SubjectMatters.Where(x => x.Key.Equals("Material Escalation Factors")).FirstOrDefault();
                valthisyear = mat.Value.Where(x => x.Key.Equals("Year_" + i)).Count() > 0 ? mat.Value.Where(x => x.Key.Equals("Year_" + i)).FirstOrDefault().Value : 0;
                cells[4, index].PutValue(valthisyear);

                var serv = rfmData.SubjectMatters.Where(x => x.Key.Equals("Service Escalation Factors")).FirstOrDefault();
                valthisyear = serv.Value.Where(x => x.Key.Equals("Year_" + i)).Count() > 0 ? serv.Value.Where(x => x.Key.Equals("Year_" + i)).FirstOrDefault().Value : 0;
                cells[5, index].PutValue(valthisyear);


                index++;
            }

            


            //cells.InsertRows(startRow, edetail.Count);

            #endregion


            wbook.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return Json(new { Success = true, Path = newFileNameSingle, FileName = retstringName }, JsonRequestBehavior.AllowGet);

        }


        public JsonResult PopulateSampleData()
        {
            new WEISReferenceFactorModel()
            {
                GroupCase = "global",
                Country = "*",
                SubjectMatters = new Dictionary<string, Dictionary<string, double>>() {
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
                var queries = new List<IMongoQuery>();
                queries.Add(Query.EQ("ReferenceFactorModel", param.GroupCase));
                queries.Add(Query.EQ("Country", param.Country));
                if (param.BaseOP != null && param.BaseOP != "")
                {
                    queries.Add(Query.EQ("Phases.Estimate.SaveToOP", param.BaseOP));
                }
                else
                {
                    var getOPs = MasterOP.Populate<MasterOP>().Select(x => x.Name).ToList();
                    queries.Add(Query.In("Phases.Estimate.SaveToOP", new BsonArray(getOPs.ToArray())));
                }
                var query = Query.And(queries);
                var bizPlan = BizPlanActivity.GetAll(query, take: 1).FirstOrDefault();

                if (bizPlan != null)
                {
                    var who = "Well: " + bizPlan.WellName;

                    var phase = bizPlan.Phases.FirstOrDefault();
                    if (phase != null)
                    {
                        who += ", Activity: " + phase.ActivityType;
                    }

                    ri.PushException(new Exception("Sorry an error has occurred. This model is used by " + who));
                    return MvcTools.ToJsonResult(ri);
                }

                if (param.Country.Equals("*"))
                {
                    DataHelper.Delete(new WEISReferenceFactorModel().TableName, Query.And(
                        Query.EQ("GroupCase", param.GroupCase),
                        Query.NE("Country", param.Country),
                        Query.EQ("BaseOP", param.BaseOP)
                    ));
                }
                else
                {
                    var qs = new List<IMongoQuery>();
                    qs.Add(Query.EQ("Country", param.Country));
                    qs.Add(Query.EQ("GroupCase", param.GroupCase));
                    if (param.BaseOP != null && param.BaseOP != "")
                    {
                        qs.Add(Query.EQ("BaseOP", param.BaseOP));
                        var rfm = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.And(qs));
                        if (rfm != null)
                        {
                            rfm.Delete();
                        }
                    }
                    else
                    {
                        var rfms = WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>(Query.And(qs));
                        if (rfms.Any())
                        {
                            foreach (var r in rfms)
                            {
                                r.Delete();
                            }
                        }
                    }

                    qs = new List<IMongoQuery>();
                    qs.Add(Query.EQ("Country", param.Country));
                    qs.Add(Query.EQ("GroupCase", param.GroupCase));
                    var checkRFM = WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>(Query.And(qs));
                    if (!checkRFM.Any())
                    {
                        var crfm = CountryReferenceFactor.Get<CountryReferenceFactor>(Query.EQ("Country", param.Country));
                        if (crfm != null)
                        {
                            crfm.ReferenceFactorModels.Remove(param.GroupCase);
                            crfm.Save();
                        }
                    }

                }

                AdministrationInput.RecalculateBusPlan();

                //var groupcase = DataHelper.Populate("WEISReferenceFactorModel")
                //    .Select(d => Convert.ToString(d.GetString("GroupCase"))).Distinct().OrderBy(d => d);
                var groupcase = new List<string>();
                var getCRFM = CountryReferenceFactor.Get<CountryReferenceFactor>(Query.EQ("Country", param.Country));
                if (getCRFM != null) groupcase = getCRFM.ReferenceFactorModels;
                ri.Data = groupcase;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        private Dictionary<string, object>[] GetData(SubjectMatterParam param, WEISReferenceFactorModel rfm, string baseop, bool isFromReload = false)
        {
            var isNullRFM = false;
            if (rfm == null)
            {
                rfm = new WEISReferenceFactorModel();
                rfm.GroupCase = param.GroupCase;
                rfm.Country = param.Country;
                rfm.WellName = param.WellName;
                rfm.ActivityType = param.ActivityType;
                rfm.BaseOP = baseop;
                isNullRFM = true;
            }

            var CompFactor = getCompundFactor();
            var CompFactorYear = getCompundFactorYear();
            if (rfm.SubjectMatters == null)
                rfm.SubjectMatters = new Dictionary<string, Dictionary<string, double>>();

            //WEISReferenceFactorModel.GetSubjectMatterOptions().ToList().ForEach(d =>
            foreach (var d in WEISReferenceFactorModel.GetSubjectMatterOptions().ToList())
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
                        if (isFromReload)
                        {
                            if (d.Contains("Inflation"))
                            {
                                var tempYear = max + i;
                                inflationCustomValue = GetInflationMacroData(rfm.Country, rfm.BaseOP, tempYear);
                            }
                        }
                        else
                        {
                            //if (rfm.BaseOP.Equals("OP16"))
                            //{ }
                            var tempYear = max + i;
                            if (d.Contains("Inflation") && tempYear < CompFactorYear)
                            {
                                inflationCustomValue = GetInflationMacroData(rfm.Country, rfm.BaseOP, tempYear);
                            }
                            else if (d.Contains("Inflation"))
                            {


                                inflationCustomValue = GetInflationMacroData(rfm.Country, rfm.BaseOP, tempYear);

                                //inflationCustomValue += InflationValue;
                                //inflationCustomValue = (((1.0 + (CompFactor / 100.0)) * (1.0 + (inflationCustomValue / 100.0))) - 1.0) * 100.0;
                            }
                            else
                            {
                                inflationCustomValue = 0.0;
                            }
                        }
                        rfm.SubjectMatters[d]["Year_" + (max + i)] = inflationCustomValue;
                    }
                }
            };

            //if (isNullRFM) rfm.Save();

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

        public double GetInflationMacroData(string country, string baseop, int year)
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
                var getMasterOPs = MasterOP.Populate<MasterOP>().Select(x => x._id.ToString()).ToList();
                foreach (var o in getMasterOPs)
                {
                    param.BaseOPs.Add(o);
                }
            }

            var ListCountries = new List<string>();
            if (param.Country == "All Countries")
            {
                var getMasterCountries = MasterCountry.Populate<MasterCountry>().Select(x => x._id.ToString()).ToList();
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
                        var rfm = newrfm.Get(param.GroupCase, d, param.ActivityType, param.WellName, op);
                        var data = GetData(param, rfm, op, false);
                        var res = new { BaseOP = op, Country = d, Data = data }.ToBsonDocument();
                        dataPerCountries.Add(res);
                    }
                });

                //get Inflation factor
                var initialInflation = 0.0;
                var Country = param.Country;
                var dataME = MacroEconomic.Get<MacroEconomic>(Query.EQ("Country", Country));
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

        public JsonResult SaveToOtherModel(SubjectMatterParam param, string newModel)
        {
            var ri = new ResultInfo();
            if (param.BaseOPs == null || param.BaseOPs.Count == 0)
            {
                param.BaseOPs = new List<string>();
                var getMasterOPs = MasterOP.Populate<MasterOP>().Select(x => x._id.ToString()).ToList();
                foreach (var o in getMasterOPs)
                {
                    param.BaseOPs.Add(o);
                }
            }

            var ListCountries = new List<string>();
            if (param.Country == "All Countries")
            {
                var getMasterCountries = MasterCountry.Populate<MasterCountry>().Select(x => x._id.ToString()).ToList();
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
                        var rfm = newrfm.Get(param.GroupCase, d, param.ActivityType, param.WellName, op);
                        if (rfm != null)
                        {
                            rfm.GroupCase = newModel;
                            rfm.Save();
                        }
                    }
                });

                var crfm = CountryReferenceFactor.Get<CountryReferenceFactor>(Query.EQ("Country", param.Country));
                crfm.ReferenceFactorModels.Add(newModel);
                crfm.Save();

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
                                    else if (d.Contains("Inflation"))
                                    {
                                        //InflationCustomValue += InflationValue;
                                        InflationCustomValue = (((1.0 + (CompFactor / 100.0)) * (1.0 + (InflationCustomValue / 100.0))) - 1.0) * 100.0;
                                    }
                                    rfm.SubjectMatters[d]["Year_" + (now.Year + i)] = InflationCustomValue;
                                }
                            });
                        }

                        //changes.ForEach(d =>
                        foreach (var d in changes)
                        {
                            var subject = Convert.ToString(d["Subject"]);

                            //if (!rfm.SubjectMatters.ContainsKey(subject))
                            //    rfm.SubjectMatters[subject] = new Dictionary<string, double>();

                            rfm.SubjectMatters[subject] = new Dictionary<string, double>();
                            foreach (KeyValuePair<string, object> each in d)
                            {
                                if (!each.Key.Contains("Year_"))
                                    continue;

                                rfm.SubjectMatters[subject][each.Key] = Convert.ToDouble(each.Value);
                            }
                        };
                        var crfm = CountryReferenceFactor.Get<CountryReferenceFactor>(Query.EQ("Country", header.Country));
                        if (crfm == null)
                        {
                            crfm = new CountryReferenceFactor();
                            crfm.Country = header.Country;
                        }
                        crfm.ReferenceFactorModels.Add(header.GroupCase);
                        crfm.Save();
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

        public JsonResult AddYearForSubjectMatter(SubjectMatterParam param, List<Dictionary<string, object>> changes)
        {
            var ri = new ResultInfo();

            try
            {


                //{[Year_2020, 0]}

                var counr = changes[0].ToList().Count;
                var key = changes[0].ToList()[counr - 3].Key;
                var lastyear = Convert.ToInt32(key.Replace("Year_", "").Trim()) + 1;

                var yz = new YearWizard();

                yz.Country = param.Country;
                yz.BaseOP = param.BaseOP;
                yz.ModelName = param.GroupCase;
                var valInf = yz.GetInflation(param.BaseOP, param.Country, lastyear);

                var newObj = new
                {
                    Contry = param.Country,
                    baseOP = param.BaseOPs.FirstOrDefault(),
                    Inf = valInf,
                    CSO = 0,
                    LC = 0,
                    Material = 0,
                    Service = 0,
                    Year = lastyear
                };
                //var rfm = yz.AddYearWizard(changes);
                ri.Data = newObj;


                #region not used
                //var listYEar = changes.Where(x => x.Keys.Contains("Inflation") || x.Keys.Contains("Year_")).Where(x => x.Keys.Contains("Year_")).ToList();
                //var lastyear = listYEar.Select(x => x.Keys).OrderByDescending(x => x).FirstOrDefault();
                //  param.Parse();
                //  var newrfm = new WEISReferenceFactorModel();
                //  var rfm = newrfm.Get(param.GroupCase, param.Country, param.ActivityType, param.WellName, param.BaseOP);

                //  if (rfm == null)
                //  {
                //      if (param.BaseOPs == null) param.BaseOPs = new List<string>();
                //      param.BaseOPs.Add(param.BaseOP);
                //      SaveDataForSubjectMatter(param, null, false);
                //      rfm = newrfm.Get(param.GroupCase, param.Country, param.ActivityType, param.WellName, param.BaseOP);
                //  }

                //  var years = new List<Int32>();
                //  var now = DateTime.Now;

                //  foreach (KeyValuePair<string, Dictionary<string, double>> each in rfm.SubjectMatters)
                //  {
                //      var eachYears = rfm.SubjectMatters[each.Key].Keys.Select(e => Convert.ToInt32(e.Replace("Year_", "")));
                //      var currentYear = eachYears.DefaultIfEmpty(now.Year).Max();
                //      var nextYear = currentYear + 1;

                //      var value = 0.0;
                //      if (rfm.SubjectMatters[each.Key].ContainsKey("Year_" + currentYear))
                //          value = rfm.SubjectMatters[each.Key]["Year_" + currentYear];

                //      if (!each.Key.ToLower().Contains("inflation"))
                //      {
                //          rfm.SubjectMatters[each.Key]["Year_" + nextYear] = 0;
                //      }
                //      else
                //      {
                //          //should take inflation factor from macro economics data
                //          var yz = new YearWizard();
                //          rfm.SubjectMatters[each.Key]["Year_" + nextYear] = yz.GetInflation(rfm.BaseOP, rfm.Country, nextYear);

                //      }
                //  }

                ////  rfm.Save();
                #endregion
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            //AdministrationInput.RecalculateBusPlan();

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RemoveYearForSubjectMatter(SubjectMatterParam param)
        {
            var ri = new ResultInfo();

            try
            {
                param.Parse();
                var newrfm = new WEISReferenceFactorModel();
                var rfm = newrfm.Get(param.GroupCase, param.Country, param.ActivityType, param.WellName, param.BaseOP);

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

                // rfm.Save();
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            //AdministrationInput.RecalculateBusPlan();

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult AutoCompleteDataSource(string country = "", string baseop = "")
        {
            var ri = new ResultInfo();
            try
            {
                ri.Data = DataHelper.Populate("WEISReferenceFactorModel", Query.And(
                    Query.EQ("Country", country),
                    Query.EQ("BaseOP", baseop)
                    ))
                   .Select(d => d.GetString("GroupCase"))
                   .Distinct();
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

        public JsonResult SaveAddYearWizard(string GroupCase, List<AddYearWizardOptions> Options, List<Dictionary<string, object>> changes, List<string> Countries = null, List<string> BaseOPs = null,
            int FromYear = 2015, int ToYear = 2015)
        {
            var ri = new ResultInfo();

            try
            {
                var Country = "";
                if (Countries != null) Country = Countries.FirstOrDefault();
                if (BaseOPs == null) BaseOPs = MasterOP.Populate<MasterOP>().Select(x => x.Name).ToList();


                List<YearWizardRFM> yearWizardDetails = new List<YearWizardRFM>();
                foreach (var opt in Options)
                {
                    var ywz = new YearWizardRFM();
                    ywz.CompoundValue = opt.CompoundFactor;
                    ywz.isCompound = opt.IsCompound;
                    ywz.isOvverideCurrent = opt.OverrideCurrentValues;
                    ywz.Name = opt.Title;
                    ywz.isUpdate = opt.IsUsingLastYearValue;
                    ywz.YearToCompound = opt.YearStartCompound;
                    ywz.Value = opt.Value;
                    yearWizardDetails.Add(ywz);
                }

                foreach (var op in BaseOPs)
                {
                    var yz = new YearWizard();
                    yz.Details = yearWizardDetails;
                    yz.Country = Country;
                    yz.StartYear = FromYear;
                    yz.FinishYear = ToYear;
                    yz.BaseOP = op;
                    yz.ModelName = GroupCase;

                    var rfm = yz.AddYearWizard(changes);
                    if (rfm != null)
                    {
                        rfm.Save();
                    }
                }


                //ri.Data = rfm.ReferenceFactorModels;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            AdministrationInput.RecalculateBusPlan();

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult BindingYearWizard(string GroupCase, List<AddYearWizardOptions> Options, List<Dictionary<string, object>> changes, List<string> Countries = null, List<string> BaseOPs = null,
            int FromYear = 2015, int ToYear = 2015)
        {
            var ri = new ResultInfo();

            try
            {
                var Country = "";
                if (Countries != null) Country = Countries.FirstOrDefault();
                if (BaseOPs == null) BaseOPs = MasterOP.Populate<MasterOP>().Select(x => x.Name).ToList();


                List<YearWizardRFM> yearWizardDetails = new List<YearWizardRFM>();
                var temSubject = new List<object>();
                foreach (var opt in Options)
                {
                    var ywz = new YearWizardRFM();
                    ywz.CompoundValue = opt.CompoundFactor;
                    ywz.isCompound = opt.IsCompound;
                    ywz.isOvverideCurrent = opt.OverrideCurrentValues;
                    ywz.Name = opt.Title;
                    ywz.isUpdate = opt.IsUsingLastYearValue;
                    ywz.YearToCompound = opt.YearStartCompound;
                    ywz.Value = opt.Value;
                    yearWizardDetails.Add(ywz);
                }

                foreach (var op in BaseOPs)
                {
                    var yz = new YearWizard();
                    yz.Details = yearWizardDetails;
                    yz.Country = Country;
                    yz.StartYear = FromYear;
                    yz.FinishYear = ToYear;
                    yz.BaseOP = op;
                    yz.ModelName = GroupCase;

                    var rfm = yz.AddYearWizard(changes);

                    temSubject.Add(rfm);
                    //if (rfm != null)
                    //{
                    //    rfm.Save();
                    //}
                }


                ri.Data = temSubject;
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }



            return MvcTools.ToJsonResult(ri);
        }
        public JsonResult SaveAddYearCustom(SubjectMatterParam param, bool IsAllCountry, int FromYear = 2015, int ToYear = 2015, List<string> Countries = null, List<Dictionary<string, object>> SubjectMattersOptions = null, double InflationInitialValue = 0, double InflationPercentage = 0)
        {
            Countries = (Countries ?? new List<string>());
            SubjectMattersOptions = (SubjectMattersOptions ?? new List<Dictionary<string, object>>());
            var ri = new ResultInfo();
            var startYear = FromYear;

            try
            {
                param.Parse();

                IMongoQuery query = null;
                if (!IsAllCountry)
                {
                    query = Query.In("Country", new BsonArray(Countries));
                }
                var masterOPs = MasterOP.Populate<MasterOP>();

                MacroEconomic.Populate<MacroEconomic>(query).GroupBy(d => d.Country).ToList().ForEach(d =>
                {
                    foreach (var op in masterOPs)
                    {
                        var country = d.Key;

                        var queryEachRFM = Query.And(Query.EQ("Country", country), Query.EQ("GroupCase", param.GroupCase), Query.EQ("BaseOP", op.Name));
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
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }
    }

    public class SubjectMatterParam
    {
        public string ActivityType { set; get; }
        public string Country { set; get; }
        public string WellName { set; get; }
        public string GroupCase { set; get; }
        public List<string> SubjectMatters { set; get; }
        public string BaseOP { set; get; }
        public List<string> BaseOPs { set; get; }

        public void Parse()
        {
            GroupCase = (GroupCase ?? "").Split(',').Select(d => d.Trim()).FirstOrDefault().Trim();
            Country = (Country == null || Country == "" || "All Countries".Equals(Country) ? "*" : Country).Trim();
            WellName = (WellName == null || WellName == "" ? "*" : WellName).Trim();
            ActivityType = (ActivityType == null || ActivityType == "" ? "*" : ActivityType).Trim();
            SubjectMatters = SubjectMatters ?? new List<string>();
        }
    }

    public class AddYearWizardOptions
    {
        public double CompoundFactor { get; set; }
        public bool IsCompound { get; set; }
        public bool IsUsingLastYearValue { get; set; }
        public bool OverrideCurrentValues { get; set; }
        public string Title { get; set; }
        public int YearStartCompound { get; set; }
        public double Value { get; set; }
    }
}