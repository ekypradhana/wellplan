using ECIS.Core;
using ECIS.Core.DataSerializer;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ECIS.Client.WEIS;
using Aspose.Cells;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using System.IO;
using MongoDB.Bson.Serialization.Attributes;
using ECIS.AppServer.Areas.Shell.Controllers;

namespace ECIS.AppServer.Areas.Palantir.Controllers
{

    [ECAuth()]
    public class PalantirController : Controller
    {
        // GET: Palantir/Palantir
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ReportCapex()
        {
            return View();
        }

        public ActionResult GenerateCapex()
        {
            return View();
        }


        public ActionResult CAPEXMapping(string[] mapname)
        {
            var lsInfo = new PIPAnalysisController().GetLatestLsDate();
            ViewBag.LatestLS = lsInfo;
            ViewBag.ListMapName = mapname;

            return View();
        }

        public JsonResult GetDataCapexMapping(List<string> lMapNames, List<string> CaseNames, List<string> UpdateBy, string DateStart, string DateFinish)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                ParsePalentirQuery pq = new ParsePalentirQuery();
                pq.MapName = lMapNames;
                pq.CaseNames = CaseNames;
                pq.UpdateBy = UpdateBy;
                pq.DateStart = DateStart;
                pq.DateFinish = DateFinish;
                var elementQuery = pq.ParseQuery();

                if (elementQuery != null)
                {
                    var pop = PCapex.Populate<PCapex>(Query.And(elementQuery));
                    return pop;
                }
                else
                {
                    var pop = PCapex.Populate<PCapex>();
                    return pop;
                }
            });
        }


        public static string getBaseOP(out string previousOP, out string nextOP)
        {
            string DefaultOP = "OP15";
            if (Config.GetConfigValue("BaseOPConfig") != null)
            {
                DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
            }
            else
            {
                var config1 = Config.Get<Config>("BaseOPConfig") ?? new Config() { _id = "BaseOPConfig", ConfigModule = "BaseOPDefault" };
                config1.ConfigValue = DefaultOP;
                config1.Save();
            }

            var DefaultOPYear = Convert.ToInt32(DefaultOP.Substring(DefaultOP.Length - 2, 2));
            nextOP = "OP" + ((DefaultOPYear + 1).ToString());
            previousOP = "OP" + ((DefaultOPYear - 1).ToString());
            return DefaultOP;
        }

        internal List<string> removeOldOPs(List<string> ops, string activeop)
        {
            var aco = Convert.ToInt32(activeop.Replace("OP", ""));
            List<string> ret = new List<string>();
            foreach (var t in ops)
            {
                var y = Convert.ToInt32(t.Replace("OP", ""));
                if (y >= aco)
                {
                    ret.Add(t);
                }
            }
            return ret;
        }
        private List<string> GetMasterOP()
        {
            var res = MasterOP.Populate<MasterOP>().Select(x => x.Name).ToList<string>();
            var activeOP = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "BaseOPConfig"));
            string curBaseOP = "OP15";
            if (activeOP.Any())
            {
                curBaseOP = activeOP.FirstOrDefault().GetString("ConfigValue");
            }

            return removeOldOPs(res.ToList(), curBaseOP);
        }

        //public List<BizPlanActivity> GetBizPlanActivity(WaterfallBase wb, FilterGeneratePlanningReportMap pq)
        public List<BizPlanActivity> GetBizPlanActivity(WaterfallBase wb)
        {
            //List<string> Status = new List<string> { "Modified", "Complete" };
            //string PreviousOP = "";
            //string NextOP = "";
            //string DefaultOP = getBaseOP(out PreviousOP, out NextOP);
            //if (Status == null)
            //{
            //    Status = new List<string>();
            //}
            //var division = 1000000.0;
            //var now = Tools.ToUTC(DateTime.Now);
            //var earlyThisMonth = Tools.ToUTC(new DateTime(now.Year, now.Month, 1));
            var email = WebTools.LoginUser.Email;
            wb.PeriodBase = "By Last Sequence";
            //var raw = wb.GetActivitiesForPalantir(pq, email);
            var raw = wb.GetActivitiesForPalantir(null, email);

            return raw;
            //var accessibleWells = WebTools.GetWellActivities();

        }

        //public JsonResult GenerateMapCapex(WaterfallBase wb, FilterGeneratePlanningReportMap pq, string CaseName)
        public JsonResult GenerateMapCapex(WaterfallBase wb)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                try
                {
                    var ret = new List<PCapex>();
                    //var bizplanActivities = GetBizPlanActivity(wb, pq);

                    var bizplanActivities = GetBizPlanActivity(wb);
                    var actgh = DataHelper.Populate("WEISActivities", q: null, fields: new List<string> { "_id", "ActivityCategory" });
                    //PCapex x = new PCapex();
                    //var a = x.AggBizPlan(bizplanActivities, actgh, WebTools.LoginUser.UserName);

                    foreach (var bz in bizplanActivities)
                    {
                        foreach (var bph in bz.Phases)
                        {
                            //have to check only if the well doesnt have a weekly report, because busplan check that -- busplancontroller : 1577
                            //this is because case on sharepoint : 959
                            var isHaveWeeklyReport = WellActivity.isHaveWeeklyReport(bz.WellName, bz.UARigSequenceId, bph.ActivityType);
                            if (!isHaveWeeklyReport)
                            {
                                //var dr = new DateRange(new DateTime(2015, 1, 1), new DateTime(2040, 12, 31));
                                // perf tune up
                                PCapex px = new PCapex();

                                //var gogo = px.TransformsLSFromBusPlan(bz, "", bph.ActivityType, WebTools.LoginUser.UserName, CaseName, dr, actgh);
                                var gogo = px.TransformsLSFromBusPlan(bz, bph.ActivityType, actgh);
                                if (gogo != null)
                                {
                                    ret.AddRange(gogo);
                                }
                            }
                        }

                    }

                    return ret; //new { Status = "OK", Message = "Success" };
                }
                catch (Exception e)
                {
                    return new { Status = "NOK", Message = e.Message };
                }
            });
        }

        public JsonResult ExportAgg(ParsePalentirQuery pq, string mapname)
        {
            var listAggr = PCapexAgg.Populate<PCapexAgg>(Query.EQ("MapName", mapname));
            var grpListAgg = listAggr.GroupBy(x => x.CaseName).ToList();
            CalcCostCurrency currency = new CalcCostCurrency();
            var curr = currency.GetCurrentCurrency(pq.Currency);

            string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "DataCapexExportTemplate.xlsx");
            Workbook wb = new Workbook(xlst);

            #region capex report sheet
            var ws = wb.Worksheets[0];
            int startRow = 2;
            foreach (var t in listAggr)
            {
                foreach (var d in t.CapexSummary)
                {
                    ws.Cells["A" + startRow.ToString()].Value = t.CaseName;

                    //string dmnonthly;
                    var dyear = d.DateId.ToString().Substring(0, 4);

                    if (d.DateId < 202201)
                    {
                        string datemonthlys  = d.DateId.ToString().Substring(4, 2) + "/1/" + dyear;
                        DateTime dt = DateTime.ParseExact(datemonthlys, "M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                        ws.Cells["B" + startRow.ToString()].PutValue(dt);
                        Style styless = ws.Cells["B" + startRow.ToString()].GetStyle();
                        styless.Custom = "M/d/yyyy";
                        ws.Cells["B" + startRow.ToString()].SetStyle(styless);
                    }
                    else
                    {
                        DateTime dt = DateTime.ParseExact(dyear, "yyyy", System.Globalization.CultureInfo.InvariantCulture);
                        ws.Cells["B" + startRow.ToString()].PutValue(dt);
                        Style styless = ws.Cells["B" + startRow.ToString()].GetStyle();
                        styless.Custom = "yyyy";
                        ws.Cells["B" + startRow.ToString()].SetStyle(styless);
                    }

                    //ws.Cells["B" + startRow.ToString()].Value = dmnonthly;

                    if (pq.MoneyType == "EDM")
                    {
                        if (pq.SSTG == "Shell Share")
                        {
                            ws.Cells["C" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalDrillingPDDevTang.EDMSS, 1000000));
                            ws.Cells["D" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalDrillingPDDevInTang.EDMSS, 1000000));
                            ws.Cells["E" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalCompletionPDDevTang.EDMSS, 1000000));
                            ws.Cells["F" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalCompletionPDDevInTang.EDMSS, 1000000));

                            ws.Cells["G" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXDrillingB2ExplTang.EDMSS, 1000000));
                            ws.Cells["H" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXDrillingB2ExplInTang.EDMSS, 1000000));
                            ws.Cells["I" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXCompletionB2ExplTang.EDMSS, 1000000));
                            ws.Cells["J" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXCompletionB2ExplInTang.EDMSS, 1000000));

                            ws.Cells["K" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalExpenDRDVAWells.EDMSS, 1000000));
                            ws.Cells["L" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalExpenDRSubSeaWells.EDMSS, 1000000));
                            ws.Cells["M" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.OPCostIdleRig.EDMSS, 1000000));
                            ws.Cells["N" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.ContigencyTangWells.EDMSS, 1000000));
                            ws.Cells["O" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.ContigencyInTangWells.EDMSS, 1000000));
                        }
                        else
                        {
                            ws.Cells["C" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalDrillingPDDevTang.EDM, 1000000));
                            ws.Cells["D" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalDrillingPDDevInTang.EDM, 1000000));
                            ws.Cells["E" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalCompletionPDDevTang.EDM, 1000000));
                            ws.Cells["F" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalCompletionPDDevInTang.EDM, 1000000));

                            ws.Cells["G" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXDrillingB2ExplTang.EDM, 1000000));
                            ws.Cells["H" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXDrillingB2ExplInTang.EDM, 1000000));
                            ws.Cells["I" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXCompletionB2ExplTang.EDM, 1000000));
                            ws.Cells["J" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXCompletionB2ExplInTang.EDM, 1000000));

                            ws.Cells["K" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalExpenDRDVAWells.EDM, 1000000));
                            ws.Cells["L" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalExpenDRSubSeaWells.EDM, 1000000));
                            ws.Cells["M" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.OPCostIdleRig.EDM, 1000000));
                            ws.Cells["N" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.ContigencyTangWells.EDM, 1000000));
                            ws.Cells["O" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.ContigencyInTangWells.EDM, 1000000));
                        }
                    }
                    else if (pq.MoneyType == "MOD")
                    {
                        if (pq.SSTG == "Shell Share")
                        {
                            ws.Cells["C" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalDrillingPDDevTang.MODSS, 1000000));
                            ws.Cells["D" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalDrillingPDDevInTang.MODSS, 1000000));
                            ws.Cells["E" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalCompletionPDDevTang.MODSS, 1000000));
                            ws.Cells["F" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalCompletionPDDevInTang.MODSS, 1000000));

                            ws.Cells["G" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXDrillingB2ExplTang.MODSS, 1000000));
                            ws.Cells["H" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXDrillingB2ExplInTang.MODSS, 1000000));
                            ws.Cells["I" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXCompletionB2ExplTang.MODSS, 1000000));
                            ws.Cells["J" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXCompletionB2ExplInTang.MODSS, 1000000));

                            ws.Cells["K" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalExpenDRDVAWells.MODSS, 1000000));
                            ws.Cells["L" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalExpenDRSubSeaWells.MODSS, 1000000));
                            ws.Cells["M" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.OPCostIdleRig.MODSS, 1000000));
                            ws.Cells["N" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.ContigencyTangWells.MODSS, 1000000));
                            ws.Cells["O" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.ContigencyInTangWells.MODSS, 1000000));
                        }
                        else
                        {
                            ws.Cells["C" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalDrillingPDDevTang.MOD, 1000000));
                            ws.Cells["D" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalDrillingPDDevInTang.MOD, 1000000));
                            ws.Cells["E" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalCompletionPDDevTang.MOD, 1000000));
                            ws.Cells["F" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalCompletionPDDevInTang.MOD, 1000000));

                            ws.Cells["G" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXDrillingB2ExplTang.MOD, 1000000));
                            ws.Cells["H" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXDrillingB2ExplInTang.MOD, 1000000));
                            ws.Cells["I" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXCompletionB2ExplTang.MOD, 1000000));
                            ws.Cells["J" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXCompletionB2ExplInTang.MOD, 1000000));

                            ws.Cells["K" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalExpenDRDVAWells.MOD, 1000000));
                            ws.Cells["L" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalExpenDRSubSeaWells.MOD, 1000000));
                            ws.Cells["M" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.OPCostIdleRig.MOD, 1000000));
                            ws.Cells["N" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.ContigencyTangWells.MOD, 1000000));
                            ws.Cells["O" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.ContigencyInTangWells.MOD, 1000000));
                        }
                    }
                    else if (pq.MoneyType == "RT")
                    {
                        if (pq.SSTG == "Shell Share")
                        {
                            ws.Cells["C" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalDrillingPDDevTang.RealTermSS, 1000000));
                            ws.Cells["D" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalDrillingPDDevInTang.RealTermSS, 1000000));
                            ws.Cells["E" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalCompletionPDDevTang.RealTermSS, 1000000));
                            ws.Cells["F" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalCompletionPDDevInTang.RealTermSS, 1000000));

                            ws.Cells["G" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXDrillingB2ExplTang.RealTermSS, 1000000));
                            ws.Cells["H" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXDrillingB2ExplInTang.RealTermSS, 1000000));
                            ws.Cells["I" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXCompletionB2ExplTang.RealTermSS, 1000000));
                            ws.Cells["J" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXCompletionB2ExplInTang.RealTermSS, 1000000));

                            ws.Cells["K" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalExpenDRDVAWells.RealTermSS, 1000000));
                            ws.Cells["L" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalExpenDRSubSeaWells.RealTermSS, 1000000));
                            ws.Cells["M" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.OPCostIdleRig.RealTermSS, 1000000));
                            ws.Cells["N" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.ContigencyTangWells.RealTermSS, 1000000));
                            ws.Cells["O" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.ContigencyInTangWells.RealTermSS, 1000000));
                        }
                        else
                        {
                            ws.Cells["C" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalDrillingPDDevTang.RealTerm, 1000000));
                            ws.Cells["D" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalDrillingPDDevInTang.RealTerm, 1000000));
                            ws.Cells["E" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalCompletionPDDevTang.RealTerm, 1000000));
                            ws.Cells["F" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalCompletionPDDevInTang.RealTerm, 1000000));

                            ws.Cells["G" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXDrillingB2ExplTang.RealTerm, 1000000));
                            ws.Cells["H" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXDrillingB2ExplInTang.RealTerm, 1000000));
                            ws.Cells["I" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXCompletionB2ExplTang.RealTerm, 1000000));
                            ws.Cells["J" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.EPEXCompletionB2ExplInTang.RealTerm, 1000000));

                            ws.Cells["K" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalExpenDRDVAWells.RealTerm, 1000000));
                            ws.Cells["L" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.CapitalExpenDRSubSeaWells.RealTerm, 1000000));
                            ws.Cells["M" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.OPCostIdleRig.RealTerm, 1000000));
                            ws.Cells["N" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.ContigencyTangWells.RealTerm, 1000000));
                            ws.Cells["O" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(d.ContigencyInTangWells.RealTerm, 1000000));
                        }
                    }
                    startRow++;
                }
            }
            ws.AutoFitColumn(0);
            #endregion

            #region mapping data sheet
            ws = wb.Worksheets[1];
            //var elementQuery = pq.ParseQuery();
            var doc = new List<PCapex>();
            //if (mapname == "Select Map File")
            //{
            //    doc = PCapex.Populate<PCapex>();
            //}
            //else
            //{
            //var q = new List<IMongoQuery>();
            //if (elementQuery != null)
            //{
            //    q.Add(elementQuery);
            //}

            //q.Add(Query.EQ("MapName", mapname));
            //doc = PCapex.Populate<PCapex>(Query.And(q));

            pq.isGenerated = true;
            if (pq.MapName == null)
            {
                pq.MapName = new List<string> { mapname };
            }
            var elementQuery = pq.ParseQuery();
            doc = PCapex.Populate<PCapex>(elementQuery);
            //}

            startRow = 2;
            foreach (var xx in doc)
            {
                ws.Cells["A" + startRow.ToString()].Value = xx.CaseName;
                ws.Cells["B" + startRow.ToString()].Value = xx.ActivityCategory;
                ws.Cells["C" + startRow.ToString()].Value = xx.WellName;
                ws.Cells["D" + startRow.ToString()].Value = xx.ActivityType;
                ws.Cells["E" + startRow.ToString()].Value = xx.FundingType;
                ws.Cells["F" + startRow.ToString()].Value = xx.FirmOption;
                ws.Cells["G" + startRow.ToString()].Value = xx.RigName;
                ws.Cells["H" + startRow.ToString()].Value = xx.UARigSequenceId;
                startRow++;
            }
            ws.AutoFitColumn(0);
            #endregion

            #region filter sheet
            ws = wb.Worksheets[2];
            startRow = 2;
            ws.Cells["A" + startRow.ToString()].Value = pq.InPlan;
            ws.Cells["B" + startRow.ToString()].Value = pq.MoneyType;
            ws.Cells["C" + startRow.ToString()].Value = pq.Currency;
            ws.Cells["D" + startRow.ToString()].Value = pq.SSTG;
            ws.Cells["E" + startRow.ToString()].Value = WebTools.LoginUser.UserName;
            ws.Cells["F" + startRow.ToString()].Value = Tools.ToUTC(DateTime.Now);
            ws.Cells["G" + startRow.ToString()].Value = mapname;
            #endregion

            #region horizontal sheet
            ws = wb.Worksheets[3];
            startRow = 2;
            //var listAggrHor = PCapexAggHorz.Populate<PCapexAggHorz>(Query.EQ("MapName", mapname), sort: SortBy.Ascending("_id"));
            var listAggrHor = PCapexAggHorizon.Populate<PCapexAggHorizon>(Query.EQ("MapName", mapname), sort: SortBy.Ascending("_id"));

            foreach (var t in listAggrHor)
            {
                foreach (var hd in t.HorizonDetails)
                {
                    ws.Cells["A" + startRow.ToString()].Value = t.CaseName;
                    ws.Cells["B" + startRow.ToString()].Value = hd.Field;
                    if (hd.Details.Any())
                    {
                        int col = 2;
                        foreach (var d in hd.Details)
                        {
                            if (pq.MoneyType == "EDM")
                            {
                                if (pq.SSTG == "Shell Share")
                                {
                                    ws.Cells[startRow - 1, col].Value = CheckDoubleIsNull(Tools.Div(d.EDMSS * curr.Value, 1000000));
                                }
                                else
                                {
                                    ws.Cells[startRow - 1, col].Value = CheckDoubleIsNull(Tools.Div(d.EDM * curr.Value, 1000000));
                                }
                            }
                            else if (pq.MoneyType == "MOD")
                            {
                                if (pq.SSTG == "Shell Share")
                                {
                                    ws.Cells[startRow - 1, col].Value = CheckDoubleIsNull(Tools.Div(d.MODSS * curr.Value, 1000000));
                                }
                                else
                                {
                                    ws.Cells[startRow - 1, col].Value = CheckDoubleIsNull(Tools.Div(d.MOD * curr.Value, 1000000));
                                }
                            }
                            else if (pq.MoneyType == "RT")
                            {
                                if (pq.SSTG == "Shell Share")
                                {
                                    ws.Cells[startRow - 1, col].Value = CheckDoubleIsNull(Tools.Div(d.RTSS * curr.Value, 1000000));
                                }
                                else
                                {
                                    ws.Cells[startRow - 1, col].Value = CheckDoubleIsNull(Tools.Div(d.RT * curr.Value, 1000000));
                                }
                            }
                            col++;
                        }
                    }
                    else
                    {
                        for (int i = 2; i < 117; i++)
                        {
                            ws.Cells[startRow - 1, i].Value = 0.00;
                        }
                    }
                    startRow++;
                }
            }
            ws.AutoFitColumn(0);
            #endregion

            #region summerized sheet
            ws = wb.Worksheets[4];
            startRow = 2;
            PCapex px = new PCapex();
            foreach (var t in listAggrHor.ToList().SelectMany(x => x.HorizonDetails).GroupBy(x => x.Field))
            {
                ws.Cells["A" + startRow.ToString()].Value = mapname;
                ws.Cells["B" + startRow.ToString()].Value = t.Key;

                int monthIdStart = 201601;
                int monthIdFinish = 206612;
                int onlyYearValueStart = 202201;
                var data = t.ToList().SelectMany(x => x.Details);
                //int col = 2;
                do
                {
                    var dataDetail = data.Where(x => x.DateId == monthIdStart).ToList();
                    if (pq.MoneyType == "EDM")
                    {
                        if (pq.SSTG == "Shell Share")
                        {
                            ws.Cells[startRow - 1, dataDetail.FirstOrDefault().Column].Value = CheckDoubleIsNull(Tools.Div(dataDetail.Sum(x => x.EDMSS) * curr.Value, 1000000));
                        }
                        else
                        {
                            ws.Cells[startRow - 1, dataDetail.FirstOrDefault().Column].Value = CheckDoubleIsNull(Tools.Div(dataDetail.Sum(x => x.EDM) * curr.Value, 1000000));
                        }
                    }
                    else if (pq.MoneyType == "MOD")
                    {
                        if (pq.SSTG == "Shell Share")
                        {
                            ws.Cells[startRow - 1, dataDetail.FirstOrDefault().Column].Value = CheckDoubleIsNull(Tools.Div(dataDetail.Sum(x => x.MODSS) * curr.Value, 1000000));
                        }
                        else
                        {
                            ws.Cells[startRow - 1, dataDetail.FirstOrDefault().Column].Value = CheckDoubleIsNull(Tools.Div(dataDetail.Sum(x => x.MOD) * curr.Value, 1000000));
                        }
                    }
                    else if (pq.MoneyType == "RT")
                    {
                        if (pq.SSTG == "Shell Share")
                        {
                            ws.Cells[startRow - 1, dataDetail.FirstOrDefault().Column].Value = CheckDoubleIsNull(Tools.Div(dataDetail.Sum(x => x.RTSS) * curr.Value, 1000000));
                        }
                        else
                        {
                            ws.Cells[startRow - 1, dataDetail.FirstOrDefault().Column].Value = CheckDoubleIsNull(Tools.Div(dataDetail.Sum(x => x.RT) * curr.Value, 1000000));
                        }
                    }
                    monthIdStart = px.AddMonthId(monthIdStart, 1);
                } while (monthIdStart <= monthIdFinish && (monthIdStart != onlyYearValueStart));

                var yearonlystart = Convert.ToInt32(onlyYearValueStart);
                var yearonlyfinish = Convert.ToInt32(monthIdFinish);
                do
                {
                    var dataDetail = data.Where(x => Convert.ToInt32(x.DateId.ToString().Substring(0, 4)) == Convert.ToInt32(yearonlystart.ToString().Substring(0, 4))).ToList();
                    if (pq.MoneyType == "EDM")
                    {
                        if (pq.SSTG == "Shell Share")
                        {
                            ws.Cells[startRow - 1, dataDetail.FirstOrDefault().Column].Value = CheckDoubleIsNull(Tools.Div(dataDetail.Sum(x => x.EDMSS) * curr.Value, 1000000));
                        }
                        else
                        {
                            ws.Cells[startRow - 1, dataDetail.FirstOrDefault().Column].Value = CheckDoubleIsNull(Tools.Div(dataDetail.Sum(x => x.EDM) * curr.Value, 1000000));
                        }
                    }
                    else if (pq.MoneyType == "MOD")
                    {
                        if (pq.SSTG == "Shell Share")
                        {
                            ws.Cells[startRow - 1, dataDetail.FirstOrDefault().Column].Value = CheckDoubleIsNull(Tools.Div(dataDetail.Sum(x => x.MODSS) * curr.Value, 1000000));
                        }
                        else
                        {
                            ws.Cells[startRow - 1, dataDetail.FirstOrDefault().Column].Value = CheckDoubleIsNull(Tools.Div(dataDetail.Sum(x => x.MOD) * curr.Value, 1000000));
                        }
                    }
                    else if (pq.MoneyType == "RT")
                    {
                        if (pq.SSTG == "Shell Share")
                        {
                            ws.Cells[startRow - 1, dataDetail.FirstOrDefault().Column].Value = CheckDoubleIsNull(Tools.Div(dataDetail.Sum(x => x.RTSS) * curr.Value, 1000000));
                        }
                        else
                        {
                            ws.Cells[startRow - 1, dataDetail.FirstOrDefault().Column].Value = CheckDoubleIsNull(Tools.Div(dataDetail.Sum(x => x.RT) * curr.Value, 1000000));
                        }
                    }

                    yearonlystart = yearonlystart + 100;
                } while (yearonlystart <= yearonlyfinish);

                startRow++;
            }
            ws.AutoFitColumn(0);
            #endregion

            #region aggregate sheet
            ws = wb.Worksheets[5];
            startRow = 2;
            foreach (var t in listAggr)
            {
                ws.Cells["A" + startRow.ToString()].Value = t.CaseName;

                if (pq.MoneyType == "EDM")
                {
                    if (pq.SSTG == "Shell Share")
                    {
                        ws.Cells["B" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalDrillingPDDevTang.EDMSS) * curr.Value, 1000000));
                        ws.Cells["C" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalDrillingPDDevInTang.EDMSS) * curr.Value, 1000000));
                        ws.Cells["D" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalCompletionPDDevTang.EDMSS) * curr.Value, 1000000));
                        ws.Cells["E" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalCompletionPDDevInTang.EDMSS) * curr.Value, 1000000));

                        ws.Cells["F" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXDrillingB2ExplTang.EDMSS) * curr.Value, 1000000));
                        ws.Cells["G" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXDrillingB2ExplInTang.EDMSS) * curr.Value, 1000000));
                        ws.Cells["H" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXCompletionB2ExplTang.EDMSS) * curr.Value, 1000000));
                        ws.Cells["I" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXCompletionB2ExplInTang.EDMSS) * curr.Value, 1000000));

                        ws.Cells["J" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalExpenDRDVAWells.EDMSS) * curr.Value, 1000000));
                        ws.Cells["K" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalExpenDRSubSeaWells.EDMSS) * curr.Value, 1000000));
                        ws.Cells["L" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.OPCostIdleRig.EDMSS) * curr.Value, 1000000));
                        ws.Cells["M" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.ContigencyTangWells.EDMSS) * curr.Value, 1000000));
                        ws.Cells["N" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.ContigencyInTangWells.EDMSS) * curr.Value, 1000000));
                    }
                    else
                    {
                        ws.Cells["B" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalDrillingPDDevTang.EDM) * curr.Value, 1000000));
                        ws.Cells["C" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalDrillingPDDevInTang.EDM) * curr.Value, 1000000));
                        ws.Cells["D" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalCompletionPDDevTang.EDM) * curr.Value, 1000000));
                        ws.Cells["E" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalCompletionPDDevInTang.EDM) * curr.Value, 1000000));

                        ws.Cells["F" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXDrillingB2ExplTang.EDM) * curr.Value, 1000000));
                        ws.Cells["G" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXDrillingB2ExplInTang.EDM) * curr.Value, 1000000));
                        ws.Cells["H" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXCompletionB2ExplTang.EDM) * curr.Value, 1000000));
                        ws.Cells["I" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXCompletionB2ExplInTang.EDM) * curr.Value, 1000000));

                        ws.Cells["J" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalExpenDRDVAWells.EDM) * curr.Value, 1000000));
                        ws.Cells["K" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalExpenDRSubSeaWells.EDM) * curr.Value, 1000000));
                        ws.Cells["L" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.OPCostIdleRig.EDM) * curr.Value, 1000000));
                        ws.Cells["M" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.ContigencyTangWells.EDM) * curr.Value, 1000000));
                        ws.Cells["N" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.ContigencyInTangWells.EDM) * curr.Value, 1000000));
                    }
                }
                else if (pq.MoneyType == "MOD")
                {
                    if (pq.SSTG == "Shell Share")
                    {
                        ws.Cells["B" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalDrillingPDDevTang.MODSS) * curr.Value, 1000000));
                        ws.Cells["C" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalDrillingPDDevInTang.MODSS) * curr.Value, 1000000));
                        ws.Cells["D" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalCompletionPDDevTang.MODSS) * curr.Value, 1000000));
                        ws.Cells["E" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalCompletionPDDevInTang.MODSS) * curr.Value, 1000000));

                        ws.Cells["F" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXDrillingB2ExplTang.MODSS) * curr.Value, 1000000));
                        ws.Cells["G" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXDrillingB2ExplInTang.MODSS) * curr.Value, 1000000));
                        ws.Cells["H" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXCompletionB2ExplTang.MODSS) * curr.Value, 1000000));
                        ws.Cells["I" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXCompletionB2ExplInTang.MODSS) * curr.Value, 1000000));

                        ws.Cells["J" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalExpenDRDVAWells.MODSS) * curr.Value, 1000000));
                        ws.Cells["K" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalExpenDRSubSeaWells.MODSS) * curr.Value, 1000000));
                        ws.Cells["L" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.OPCostIdleRig.MODSS) * curr.Value, 1000000));
                        ws.Cells["M" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.ContigencyTangWells.MODSS) * curr.Value, 1000000));
                        ws.Cells["N" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.ContigencyInTangWells.MODSS) * curr.Value, 1000000));
                    }
                    else
                    {
                        ws.Cells["B" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalDrillingPDDevTang.MOD) * curr.Value, 1000000));
                        ws.Cells["C" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalDrillingPDDevInTang.MOD) * curr.Value, 1000000));
                        ws.Cells["D" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalCompletionPDDevTang.MOD) * curr.Value, 1000000));
                        ws.Cells["E" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalCompletionPDDevInTang.MOD) * curr.Value, 1000000));

                        ws.Cells["F" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXDrillingB2ExplTang.MOD) * curr.Value, 1000000));
                        ws.Cells["G" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXDrillingB2ExplInTang.MOD) * curr.Value, 1000000));
                        ws.Cells["H" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXCompletionB2ExplTang.MOD) * curr.Value, 1000000));
                        ws.Cells["I" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXCompletionB2ExplInTang.MOD) * curr.Value, 1000000));

                        ws.Cells["J" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalExpenDRDVAWells.MOD) * curr.Value, 1000000));
                        ws.Cells["K" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalExpenDRSubSeaWells.MOD) * curr.Value, 1000000));
                        ws.Cells["L" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.OPCostIdleRig.MOD) * curr.Value, 1000000));
                        ws.Cells["M" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.ContigencyTangWells.MOD) * curr.Value, 1000000));
                        ws.Cells["N" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.ContigencyInTangWells.MOD) * curr.Value, 1000000));
                    }
                }
                else if (pq.MoneyType == "RT")
                {
                    if (pq.SSTG == "Shell Share")
                    {
                        ws.Cells["B" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalDrillingPDDevTang.RealTermSS) * curr.Value, 1000000));
                        ws.Cells["C" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalDrillingPDDevInTang.RealTermSS) * curr.Value, 1000000));
                        ws.Cells["D" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalCompletionPDDevTang.RealTermSS) * curr.Value, 1000000));
                        ws.Cells["E" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalCompletionPDDevInTang.RealTermSS) * curr.Value, 1000000));

                        ws.Cells["F" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXDrillingB2ExplTang.RealTermSS) * curr.Value, 1000000));
                        ws.Cells["G" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXDrillingB2ExplInTang.RealTermSS) * curr.Value, 1000000));
                        ws.Cells["H" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXCompletionB2ExplTang.RealTermSS) * curr.Value, 1000000));
                        ws.Cells["I" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXCompletionB2ExplInTang.RealTermSS) * curr.Value, 1000000));

                        ws.Cells["J" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalExpenDRDVAWells.RealTermSS) * curr.Value, 1000000));
                        ws.Cells["K" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalExpenDRSubSeaWells.RealTermSS) * curr.Value, 1000000));
                        ws.Cells["L" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.OPCostIdleRig.RealTermSS) * curr.Value, 1000000));
                        ws.Cells["M" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.ContigencyTangWells.RealTermSS) * curr.Value, 1000000));
                        ws.Cells["N" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.ContigencyInTangWells.RealTermSS) * curr.Value, 1000000));
                    }
                    else
                    {
                        ws.Cells["B" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalDrillingPDDevTang.RealTerm) * curr.Value, 1000000));
                        ws.Cells["C" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalDrillingPDDevInTang.RealTerm) * curr.Value, 1000000));
                        ws.Cells["D" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalCompletionPDDevTang.RealTerm) * curr.Value, 1000000));
                        ws.Cells["E" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalCompletionPDDevInTang.RealTerm) * curr.Value, 1000000));

                        ws.Cells["F" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXDrillingB2ExplTang.RealTerm) * curr.Value, 1000000));
                        ws.Cells["G" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXDrillingB2ExplInTang.RealTerm) * curr.Value, 1000000));
                        ws.Cells["H" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXCompletionB2ExplTang.RealTerm) * curr.Value, 1000000));
                        ws.Cells["I" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.EPEXCompletionB2ExplInTang.RealTerm) * curr.Value, 1000000));

                        ws.Cells["J" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalExpenDRDVAWells.RealTerm) * curr.Value, 1000000));
                        ws.Cells["K" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.CapitalExpenDRSubSeaWells.RealTerm) * curr.Value, 1000000));
                        ws.Cells["L" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.OPCostIdleRig.RealTerm) * curr.Value, 1000000));
                        ws.Cells["M" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.ContigencyTangWells.RealTerm) * curr.Value, 1000000));
                        ws.Cells["N" + startRow.ToString()].Value = CheckDoubleIsNull(Tools.Div(t.CapexSummary.Sum(x => x.ContigencyInTangWells.RealTerm) * curr.Value, 1000000));
                    }
                }
                startRow++;
            }
            ws.AutoFitColumn(0);
            #endregion

            var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"),
                     String.Format("DataCapex-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss")));

            string returnName = String.Format("DataCapex-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"));

            wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

            return Json(new { Success = true, Path = returnName }, JsonRequestBehavior.AllowGet);
        }

        private object CheckDoubleIsNull(double d)
        {
            double? db = new double?();
            db = d == 0 ? db = null : db = d;
            return db;
        }
        
        public FileResult DownloadCapexAggFile(string stringName, DateTime date)
        {
            var res = Path.Combine(Server.MapPath("~/App_Data/Temp"), stringName);
            var retstringName = "DataCapex-" + date.ToString("MMM-yyyy") + ".xlsx";

            return File(res, Tools.GetContentType(".xlsx"), retstringName);
        }

        public JsonResult SavePlanCapex(ParsePalentirQuery pq, string mapname)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                try
                {
                    List<PCapex> list = PCapex.Populate<PCapex>(q: Query.EQ("MapName", mapname));

                    SSTG sstg = new SSTG();
                    //pq.InPlan = string.IsNullOrEmpty(pq.InPlan) ? "Both" : pq.InPlan;
                    switch (pq.InPlan)
                    {
                        case "Yes":
                            list = list.Where(x => x.IsInPlan == true).ToList();
                            break;
                        case "No":
                            list = list.Where(x => x.IsInPlan == false).ToList();
                            break;
                        case "Both":
                            list = list.Where(x => x.IsInPlan == true || x.IsInPlan == false).ToList();
                            break;
                    }

                    var bsonResult = new List<string>();
                    //List<PCapexAgg> listAgg = new List<PCapexAgg>();

                    bool isAggrCreated = createCapexAggregate(list, mapname, pq);
                    if (!isAggrCreated)
                    {
                        return new { Status = "NOK", Message = "Error Run Aggregation" };
                    }

                    var reqmapnames = PCapex.Populate<PCapex>().Select(x => x.MapName).Distinct().ToList();
                    for (int i = 0; i < reqmapnames.Count; i++)
                    {
                        bsonResult.Add(reqmapnames[i]);
                    }

                    return new { LMapName = bsonResult };
                }
                catch (Exception e)
                {
                    return new { Status = "NOK", Message = e.Message };
                }
            });
        }

        public JsonResult SaveMapName(List<PCapex> list, string mapnames)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                try
                {
                    //var bizplans = BizPlanActivity.GetAll2(Query.Null);
                    //List<PCapex> liPcapex = new List<PCapex>();
                    foreach (var l in list)
                    {
                        #region marked temp
                        //var getEst = bizplans.Where(x => x.WellName != null && x.WellName == l.WellName
                        //            && x.UARigSequenceId != null && x.UARigSequenceId == l.UARigSequenceId
                        //            && x.Phases != null && x.Phases.Where(d=>d.ActivityType != null && d.ActivityType == l.ActivityType).Count() > 0
                        //    ).FirstOrDefault();
                        //if (getEst != null)
                        //{
                        //    var dr = new DateRange(new DateTime(2015, 1, 1), new DateTime(2040, 12, 31));
                        //    var phase = getEst.Phases.Where(x => x.ActivityType != null && x.ActivityType == l.ActivityType).FirstOrDefault();
                        //    l.Estimate = phase.Estimate;
                        //    l.Allocation = phase.Allocation;
                        //    l.UpdateBy = WebTools.LoginUser.UserName;
                        //    l.MapName = mapnames;

                        //    var qsrfm = new List<IMongoQuery>();
                        //    qsrfm.Add(Query.EQ("GroupCase", l.RFM));
                        //    qsrfm.Add(Query.EQ("BaseOP", phase.Estimate.SaveToOP));
                        //    qsrfm.Add(Query.EQ("Country", phase.Estimate.Country));

                        //    var RefFactorModels = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.And(qsrfm));
                        ////PDetail detail = new PDetail();
                        ////l.CapexSummary = detail;
                        //    var aResult = l.DetailSummaryLS(l, dr, RefFactorModels);
                        //    if (aResult != null)
                        //    {
                        //        aResult.Save();
                        //        liPcapex.Add(aResult);
                        //    }
                        //}
                        #endregion
                        l.MapName = mapnames;
                        l.UpdateBy = WebTools.LoginUser.UserName;
                        l.UpdateVersion = Tools.ToUTC(DateTime.Now, true);
                        l.Save();
                    }
                    //return new { Status = "OK", listMap = liPcapex };
                    return new { Status = "OK", listMap = list };
                }
                catch (Exception ex)
                {
                    return new { Status = "NOK", Message = ex.Message };
                }
            });
        }

        public JsonResult GetListCapexMapping(ParsePalentirQuery pq, string mapname)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var doc = new List<PCapex>();
                if (mapname != "Select Map File")
                {
                    pq.isGenerated = true;
                    if (pq.MapName == null)
                    {
                        pq.MapName = new List<string> { mapname };
                    }
                    var elementQuery = pq.ParseQuery();
                    doc = PCapex.Populate<PCapex>(elementQuery);
                }
                else
                {
                    doc = PCapex.Populate<PCapex>();
                }

                //var elementQuery = pq.ParseQuery();
                //var doc = new List<PCapex>();
                //if (mapname == "Select Map File")
                //{
                //    if (elementQuery != null)
                //    {
                //        doc = PCapex.Populate<PCapex>(elementQuery);
                //    }
                //    else
                //    {
                //        doc = PCapex.Populate<PCapex>();
                //    }
                //}
                //else
                //{
                //    var q = new List<IMongoQuery>();
                //    q.Add(Query.EQ("MapName", mapname));
                //    if (elementQuery != null)
                //    {
                //        q.Add(elementQuery);

                //        doc = PCapex.Populate<PCapex>(Query.And(q));
                //    }
                //    else
                //    {
                //        doc = PCapex.Populate<PCapex>(Query.And(q));
                //    }
                //}

                return DataHelper.ToDictionaryArray(doc.Select(x => x.ToBsonDocument()).ToList());
            });
        }

        #region marked temporary
        //public JsonResult GetCAPEXReport(ParsePalentirQuery wb)
        //{
        //    return MvcResultInfo.Execute(null, (BsonDocument doc) =>
        //    {
        //        /// prepare for server paging
        //        //var agg = new List<BsonDocument>();
        //        //agg.Add(new BsonDocument().Set("$skip", 0));
        //        //agg.Add(new BsonDocument().Set("$limit", 10));
        //        //var aggData = DataHelper.Aggregate(new PCapexAgg().TableName, agg);
        //        //var raw = aggData.Select(BsonSerializer.Deserialize<PCapexAgg>).ToList();

        //        var queryElement = wb.ParseQuery();
        //        SSTG sstg = new SSTG();
        //        PDetail cs = new PDetail();
        //        var raw = DataHelper.Populate<PCapexAgg>("WEISPalantirCapexAgg", queryElement);

        //        var dataGrid = new List<object>();
        //        foreach (var d in raw)
        //        {
        //            var digitLength = d.MonthId.ToString().Length;
        //            string dmnonthly;
        //            var dyear = d.MonthId.ToString().Substring(0, 4);
        //            if (digitLength > 4)
        //                dmnonthly = dyear + "/" + d.MonthId.ToString().Substring(4, 2) + "/01";
        //            else
        //                dmnonthly = dyear;

        //            sstg.GetSSTG(wb, d.CapexSummary, out cs);

        //            dataGrid.Add(new
        //            {
        //                _id = d._id,
        //                MonthId = dmnonthly,
        //                CaseName = d.CaseName,
        //                CapexSummary = cs,
        //                SSorTG = wb.SSTG
        //            });
        //        }

        //        return DataHelper.ToDictionaryArray(dataGrid.Select(x => x.ToBsonDocument()).ToList());
        //    });
        //}

        //public JsonResult DeleteAggr(string[] ids, string report)
        //{
        //    try
        //    {
        //        string collectionName = "";
        //        switch (report)
        //        {
        //            case "capex":
        //                collectionName = "WEISPalantirCapexAgg";
        //                break;
        //            case "pmasterStandart":
        //                collectionName = "WEISPalantirMasterStandardResult";
        //                break;
        //            case "pmasterMonthly":
        //                collectionName = "WEISPalantirMasterResult";
        //                break;
        //        }

        //        foreach (string _id in ids)
        //        {
        //            Int32 thisIsId = 0;
        //            bool isInteger = Int32.TryParse(_id, out thisIsId);
        //            if (isInteger)
        //            {
        //                DataHelper.Delete(collectionName, Query.EQ("_id", thisIsId));
        //            }
        //            else
        //            {
        //                DataHelper.Delete(collectionName, Query.EQ("_id", _id.ToString()));
        //            }

        //        }

        //        return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
        //    }
        //}

        //public JsonResult UpdateCapexAggr(int _id)
        //{
        //    return MvcResultInfo.Execute(null, (BsonDocument doc) =>
        //    {
        //        try
        //        {
        //            var getUpdateData = DataHelper.Populate<PCapexAgg>("WEISPalantirCapexAgg", Query.EQ("_id", _id));
        //            foreach (var data in getUpdateData)
        //            {
        //                //data.
        //            }

        //            var dataGrid = new List<object>();
        //            dataGrid.Add(new
        //            {
        //                Success = true,
        //                data = getUpdateData
        //            });
        //            var result = dataGrid.Select(x => x.ToBsonDocument()).ToList();
        //            return DataHelper.ToDictionaryArray(result);
        //        }
        //        catch (Exception e)
        //        {
        //            var data = new List<object>();
        //            data.Add(new { Success = false, Message = e.Message });
        //            var result = data.Select(x => x.ToBsonDocument()).ToList();
        //            return DataHelper.ToDictionaryArray(result);
        //        }
        //    });
        //}
        #endregion

        public JsonResult DeleteMap(string mapname)
        {
            try
            {
                //DataHelper.Delete("WEISPalantirCapex", Query.And(Query.EQ("UpdateBy", WebTools.LoginUser.UserName), Query.EQ("MapName", mapname)));
                DataHelper.Delete("WEISPalantirCapex", Query.EQ("MapName", mapname));
                DataHelper.Delete("WEISPalantirCapexAgg", Query.EQ("MapName", mapname));
                DataHelper.Delete("WEISPalantirCapexAggHorizontal", Query.EQ("MapName", mapname));
                return Json(new { Success = true });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message });
            }
        }

        #region marked temporary
        //public JsonResult ReGenerate(string mapName, List<PCapex> list)
        //{
        //    try
        //    {
        //        string[] mapNameToArr = mapName.Split(' ');
        //        string[] field = { "MapName" };
        //        bool isDeleted = deleteAgg(field, mapNameToArr, "WEISPalantirCapex");
        //        if (!isDeleted)
        //        {
        //            return Json(new { Success = false, Message = "Something error when deleted aggregate!." }, JsonRequestBehavior.AllowGet);
        //        }

        //        list.ForEach(x => x.IsReGenerate = true);
        //        string messageError = "";
        //        bool isAggrtrue = saveMap(list, true, mapName, true, out messageError);
        //        if (!isAggrtrue)
        //        {
        //            return Json(new { Success = false, Message = messageError }, JsonRequestBehavior.AllowGet);
        //        }

        //        List<PCapexAgg> listAgg = new List<PCapexAgg>();
        //        bool isCreatedAggr = createCapexAggregate(list, mapName, out listAgg);
        //        if (!isCreatedAggr)
        //        {
        //            return Json(new { Success = false, Message = "Cannot create aggregate!." }, JsonRequestBehavior.AllowGet);
        //        }

        //        return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { Success = false, Message = e.Message }, JsonRequestBehavior.AllowGet);
        //    }
        //}
        #endregion

        [HttpPost]
        public JsonResult UploadMapCapex()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                try
                {
                    HttpPostedFileBase file = Request.Files["UploadedFile"]; //Uploaded file
                    string fileName = file.FileName;

                    //To save file, use SaveAs method
                    file.SaveAs(Server.MapPath("~/App_Data/Temp/") + fileName); //File will be saved in application root

                    //List<PCapex> list = new List<PCapex>();
                    string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp/"), fileName);
                    ExtractionHelper e = new ExtractionHelper();
                    IList<BsonDocument> datas = e.Extract(xlst);
                    var list = new PCapex();
                    var toDataModel = datas.Select(x => BsonSerializer.Deserialize<CapexUploadModel>(x)).ToList();
                    //foreach (var data in datas)
                    //{
                    //    PCapex result = new PCapex();
                    //    result.CaseName = data.GetString("Case_Name");
                    //    result.ActivityCategory = data.GetString("Activity_Category");
                    //    result.WellName = data.GetString("Well_Name");
                    //    result.ActivityType = data.GetString("Activity_Type");
                    //    result.FundingType = data.GetString("Funding_Type");
                    //    result.FirmOption = data.GetString("Planning_Classification");
                    //    result.RigName = data.GetString("Rig_Name");
                    //    result.UARigSequenceId = data.GetString("UARigSequenceId");
                    //    list.Add(result);
                    //}

                    #region marked temp
                    //foreach (var data in datas)
                    //{
                    //    PCapex result = new PCapex();
                    //    result.CaseName = data.GetString("Case_Name");
                    //    result.ActivityCategory = data.GetString("Activity_Category");
                    //    result.WellName = data.GetString("Well_Name");
                    //    result.ActivityType = data.GetString("Activity_Type");
                    //    result.FundingType = data.GetString("Funding_Type");
                    //    result.FirmOption = data.GetString("Planning_Classification");
                    //    result.RigName = data.GetString("Rig_Name");
                    //    result.UARigSequenceId = data.GetString("UARigSequenceId");
                    //    list.Add(result);
                    //}

                    var bizplans = BizPlanActivity.GetAll2(Query.Null);
                    var dr = new DateRange(new DateTime(2015, 1, 1), new DateTime(2040, 12, 31));
                    //var actgh = DataHelper.Populate("WEISActivities", q: null, fields: new List<string> { "_id", "ActivityCategory" });

                    //insert kan allocationnya sekalian disini,sekalian exclude yang tidak ketemu di bizplan
                    var res = new List<PCapex>();
                    foreach (var data in toDataModel)
                    {
                        var getMapValue = bizplans.Where(x => (x.WellName != null && x.WellName.Trim() == data.Well_Name.Trim()) && (x.UARigSequenceId != null && x.UARigSequenceId.Trim() == data.UARigSequenceId.Trim())).FirstOrDefault();
                        if (getMapValue != null)
                        {
                            var phase = getMapValue.Phases.Where(x => x.ActivityType != null && x.ActivityType.Trim() == data.Activity_Type.Trim()).FirstOrDefault();
                            if (phase != null)
                            {
                                var result = list.TransformsLSFromBusPlan(getMapValue, "", data.Activity_Type, WebTools.LoginUser.UserName, data.Case_Name, data.Activity_Category, dr);
                                if (result != null)
                                {
                                    result.Estimate = null;
                                    result.Allocation = null;
                                    res.Add(result);
                                }
                                else
                                {

                                }
                            }
                            else
                            {

                            }
                        }
                        else
                        {

                        }
                    }
                    #endregion

                    return new
                    {
                        Success = true,
                        Message = "OK",
                        listMap = res//new List<string>()
                    };
                }
                //catch (InvalidCastException e)
                catch (Exception e)
                {
                    //var a = e;
                    //return null;
                    return new { Success = false, Message = e.Message };
                }
            });
        }

        #region marked temporary upload map
        //[HttpPost]
        //public JsonResult UploadMapCapex()
        //{
        //    return MvcResultInfo.Execute(null, document =>
        //    {
        //        bool success = true;
        //        string message = "";

        //        HttpPostedFileBase file = Request.Files["UploadedFile"]; //Uploaded file
        //        string fileName = file.FileName;
        //        //Use the following properties to get file's name, size and MIMEType
        //        //int fileSize = file.ContentLength;                
        //        //string mimeType = file.ContentType;
        //        //System.IO.Stream fileContent = file.InputStream;

        //        //To save file, use SaveAs method
        //        file.SaveAs(Server.MapPath("~/App_Data/Temp/") + fileName); //File will be saved in application root

        //        List<PCapex> list = new List<PCapex>();
        //        PCapex map = new PCapex();
        //        string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp/"), fileName);
        //        ExtractionHelper e = new ExtractionHelper();
        //        IList<BsonDocument> datas = e.Extract(xlst);
        //        //list = datas.Select(x => BsonSerializer.Deserialize<PCapex>(x)).ToList();
        //        int i = 0;
        //        foreach (var data in datas)
        //        {
        //            PCapex result = new PCapex();
        //            result.CaseName = data.GetString("Case_Name");
        //            result.ActivityCategory = data.GetString("Activity_Category");
        //            result.WellName = data.GetString("Well_Name");
        //            result.ActivityType = data.GetString("Activity_Type");
        //            result.FundingType = data.GetString("Funding_Type");
        //            result.FirmOption = data.GetString("Planning_Classification");
        //            result.RigName = data.GetString("Rig_Name");
        //            result.UARigSequenceId = data.GetString("UARigSequenceId");
        //            list.Add(result);
        //            //if (string.IsNullOrEmpty(data.GetString("Case_Name")))
        //            //{
        //            //    success = false;
        //            //    message = "CaseName should not be emty!";
        //            //    return Json(new { Success = success, Message = message });
        //            //}
        //            //var capexData = map.GetListCapex(data, WebTools.LoginUser.UserName);
        //            //if (capexData != null)
        //            //{
        //            //    list.Add(capexData);
        //            //}

        //            i++;
        //        }

        //        return new
        //        {
        //            Success = success,
        //            Message = message,
        //            listMap = list//new List<string>()
        //        };

        //        //return Json(new { Success = success, Message = message, CollectionName = coltemp_name, TotalData = list.Count(), listMap = new List<string>() });
        //    });
        //}
        #endregion

        #region marked temporary
        //public JsonResult GetTempGenerateCapex(string colName, int take, int skip)
        //{
        //    return MvcResultInfo.Execute(null, document =>
        //    {
        //        var ret = DataHelper.Populate(colName, take: take, skip: skip);
        //        return ret;
        //    });
        //}

        //private bool deleteMap(string[] values, string[] fields, string report, string[] lmapnames, out string message, out List<string> bsonResult)
        //{
        //    message = "";
        //    bsonResult = new List<string>();
        //    try
        //    {
        //        string collection = "";
        //        switch (report)
        //        {
        //            case "capex":
        //                collection = "WEISPalantirCapex";
        //                break;
        //            case "pmasterStandart":
        //                collection = "WEISPalantirMasterStandardResult";
        //                break;
        //            case "pmasterMonthly":
        //                collection = "WEISPalantirMasterResult";
        //                break;
        //        }

        //        bool isDeleted = deleteAgg(fields, values, collection);
        //        if (!isDeleted)
        //        {
        //            message = "Something error when deleted aggregate!.";
        //            return false;
        //            //return Json(new { Success = false, Message = "Something error when deleted aggregate!." }, JsonRequestBehavior.AllowGet);
        //        }

        //        var query = this.query(fields, values);
        //        DataHelper.Delete(collection, query);

        //        //bool isEmpty = !lmapnames.Any();
        //        if ((lmapnames.Length > 0) && (lmapnames[0] != ""))
        //        {
        //            var reqmapnames = PCapex.Populate<PCapex>(Query.In("MapName", new BsonArray(lmapnames))).Select(x => x.MapName).Distinct().ToList();
        //            for (int i = 0; i < reqmapnames.Count; i++)
        //            {
        //                bsonResult.Add(reqmapnames[i]);
        //            }
        //        }
        //        else
        //        {
        //            var mapnamesview = PCapex.Populate<PCapex>().Select(x => x.MapName).Distinct().ToList();
        //            for (int i = 0; i < mapnamesview.Count; i++)
        //            {
        //                bsonResult.Add(mapnamesview[i]);
        //            }
        //        }

        //        return true;
        //    }
        //    catch (Exception e)
        //    {
        //        message = e.Message;
        //        return false;
        //    }
        //}

        //private IMongoQuery query(string[] fields, string[] values)
        //{
        //    var q = new List<IMongoQuery>();
        //    foreach (string field in fields)
        //    {
        //        foreach (string value in values)
        //        {
        //            if (fields.Contains(field) && values.Contains(value))
        //            {
        //                q.Add(Query.EQ(field, value));
        //            }
        //        }
        //    }
        //    return Query.Or(q);
        //}

        //private bool deleteAgg(string[] fields, string[] values, string collection)
        //{
        //    var q = query(fields, values);
        //    var a = DataHelper.Populate(collection, q);

        //    if (a.Count > 0)
        //    {
        //        var ab = a.GroupBy(x => x.ElementAt(3).Value).ToList();
        //        var takeOneData = a.First();

        //        string aggColl = "";
        //        foreach (var fName in ab)
        //        {
        //            switch (collection)
        //            {
        //                case "WEISPalantirCapex":
        //                    aggColl = "WEISPalantirCapexAgg";
        //                    //DataHelper.Delete("WEISPalantirCapexAgg", Query.EQ(takeOneData.ElementAt(3).Name.ToString(), takeOneData.ElementAt(3).Value.ToString()));
        //                    DataHelper.Delete("WEISPalantirCaseNames", Query.EQ("_id", fName.Key.ToString()));
        //                    break;
        //            }

        //            DataHelper.Delete(aggColl, Query.EQ(takeOneData.ElementAt(3).Name.ToString(), fName.Key.ToString()));
        //        }
        //    }
        //    else
        //    {
        //        return false;
        //    }

        //    return true;
        //}
        #endregion

        private bool createCapexAggregate(List<PCapex> list, string mapName, ParsePalentirQuery pq)
        {
            List<PCapexAgg> aggpx = new List<PCapexAgg>();
            try
            {
                PCapex px = new PCapex();
                // resync from bizplan
                var capex = px.ReSyncMapFromBizplan(list, list.FirstOrDefault().MapName);
                DateTime createDate = Tools.ToUTC(DateTime.Now, true);
                bool ok = px.AggregatingPCapex(capex, mapName, WebTools.LoginUser.UserName, createDate);
                if (!ok)
                {
                    return false;
                }

                //DataHelper.Delete("WEISPalantirCapexAgg", Query.EQ("MapName", mapName));
                //DataHelper.Delete("WEISPalantirCapexAggHorizontal", Query.EQ("MapName", mapName));
                //foreach (var t in aggpx)
                //{
                //    t.UpdateVersion = createDate;
                //    t.UpdateBy = WebTools.LoginUser.UserName;
                //    t.MapName = mapName;
                //    t.Save();
                //}

                PCapexAggHorizon ph = new PCapexAggHorizon();
                //foreach (var l in list)
                //{
                ok = ph.HorizontalAggCapex(capex, WebTools.LoginUser.UserName, createDate, pq, mapName);
                if (!ok)
                {
                    return false;
                }
                //}

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        #region marked temporary
        //private bool saveMap(List<PCapex> list, bool isEdited, string mapname, bool createAggr, out string messageError)
        //{
        //    messageError = "";
        //    if (isEdited)
        //    {
        //        list.ForEach(x => { x.MapName = mapname; x.IsEdited = isEdited; });
        //        var getmapname = list.GroupBy(x => x.MapName).Select(x => x.Key).ToList();
        //        string[] values = { getmapname.ElementAt(0) };
        //        string[] fields = { "MapName" };
        //        string report = "capex";
        //        string[] lmapnames = { };
        //        string message = "";
        //        List<string> bsonResult = new List<string>();
        //        bool success = deleteMap(values, fields, report, lmapnames, out message, out bsonResult);
        //        if (!success)
        //        {
        //            messageError = "Cannot delete map";
        //            return false;
        //        }
        //    }

        //    foreach (var g in list)
        //    {

        //        //Remarked by eky because of Akash's comment on June 9,2016:
        //        //For palantir reports : If user leaves some mapping blank, then also he should be able to save. Currently while updating a map, it doesnt save because it says case name blank.

        //        //if (string.IsNullOrEmpty(g.CaseName))
        //        //{
        //        //    messageError = "CaseName cannot empty!.";
        //        //    return false;
        //        //}

        //        bool aggCreated = false;
        //        if (createAggr)
        //        {
        //            aggCreated = true;
        //        }

        //        if (!g.IsReGenerate)
        //        {
        //            g.MapName = mapname;
        //        }

        //        g.IsAggregate = aggCreated;
        //        g.Save();
        //    }
        //    messageError = "Good!";
        //    return true;
        //}
        #endregion

        public JsonResult UpdateLS(List<PCapex> listMap, bool inlastuploadls)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                string success = "";
                string message = "";
                if (inlastuploadls)
                {
                    var latestSeq = DataHelper.Populate("LogLatestLS");
                    foreach (var dataMap in listMap)
                    {
                        var index = latestSeq.FindIndex(x => x.GetString("Rig_Name") == dataMap.RigName && x.GetString("Well_Name") == dataMap.WellName &&
                                x.GetString("Activity_Type") == dataMap.ActivityType);
                        latestSeq.RemoveAt(index);
                    }
                    var actgh = DataHelper.Populate("WEISActivities", q: null, fields: new List<string> { "_id", "ActivityCategory" });

                    foreach (var ls in latestSeq)
                    {

                        PCapex p = new PCapex();
                        p.ActivityType = ls.GetString("Activity_Type");
                        p.WellName = ls.GetString("Well_Name");
                        p.RigName = ls.GetString("Rig_Name");

                        var dr = new DateRange(new DateTime(2015, 1, 1), new DateTime(2040, 12, 31));

                        WellActivity act = WellActivity.Get<WellActivity>(Query.And(
                            Query.EQ("WellName", p.WellName),
                            Query.EQ("RigName", p.RigName),
                            Query.EQ("Phases.ActivityType", p.ActivityType)
                            ));
                        PCapex px = new PCapex();

                        if (act != null)
                        {
                            var gogo = px.TransformsLS(act, "", p.ActivityType, WebTools.LoginUser.UserName, "", dr, false, actgh);
                            //if (pq.LineOfBusiness != null && pq.LineOfBusiness.Count > 0)
                            //    gogo = gogo.Where(x => pq.LineOfBusiness.Contains(x.LoB)).ToList();
                            listMap.AddRange(gogo);
                        }


                    }
                    success = "OK";
                }
                else
                {
                    success = "NOK";
                    message = "Sorry, update LS is unchecked";
                }
                return new { Status = success, Message = message, listSeq = listMap };
            });
        }

        public JsonResult BatchAggrHorizontal() {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                List<PCapex> list = PCapex.Populate<PCapex>();
                //var groupListMap = list.GroupBy(x => x.MapName);
                foreach (var itemList in list.GroupBy(x => x.MapName))
                {
                    PCapex px = new PCapex();
                    var capex = px.ReSyncMapFromBizplan(itemList.ToList(), itemList.ToList().FirstOrDefault().MapName);

                    DateTime createDate = Tools.ToUTC(DateTime.Now, true);
                    //DataHelper.Delete("WEISPalantirCapexAggHorizontalEx", Query.EQ("MapName", itemList.ToList().FirstOrDefault().MapName));
                    PCapexAggHorizon phex = new PCapexAggHorizon();
                    bool okex = phex.HorizontalAggCapex(capex, WebTools.LoginUser.UserName, createDate, null, itemList.ToList().FirstOrDefault().MapName);
                }
                
                return "";
            });
        }

        public JsonResult AggregateBatch()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                try
                {
                    var getAllMapping = PCapex.Populate<PCapex>().GroupBy(x => x.MapName);
                    foreach (var mapname in getAllMapping)
                    {
                        #region create aggregation
                        //var getAggregateByMapname = PCapexAggOld.Populate<PCapexAggOld>(Query.EQ("MapName", mapname.Key));
                        //foreach (var g in getAggregateByMapname.GroupBy(x => x.CaseName))
                        //{
                        //    List<int> oldID = new List<int>(); //ambil ID aggregate yang lama

                        //    PCapexAgg ag = new PCapexAgg();
                        //    ag.CaseName = g.Key;
                        //    ag.UpdateVersion = g.FirstOrDefault().UpdateVersion;
                        //    ag.UpdateBy = g.FirstOrDefault().UpdateBy;
                        //    ag.MapName = g.FirstOrDefault().MapName;
                        //    ag.LastUpdate = g.FirstOrDefault().LastUpdate;
                        //    List<PDetails> ld = new List<PDetails>();
                        //    foreach (var d in g)
                        //    {
                        //        oldID.Add(Convert.ToInt32(d._id));

                        //        PDetails dt = new PDetails();
                        //        dt.Calc = d.CapexSummary.Calc;
                        //        dt.CapitalCompletionPDDevInTang = d.CapexSummary.CapitalCompletionPDDevInTang;
                        //        dt.CapitalCompletionPDDevTang = d.CapexSummary.CapitalCompletionPDDevTang;
                        //        dt.CapitalDrillingPDDevInTang = d.CapexSummary.CapitalDrillingPDDevInTang;
                        //        dt.CapitalDrillingPDDevTang = d.CapexSummary.CapitalDrillingPDDevTang;
                        //        dt.CapitalExpenDRDVAWells = d.CapexSummary.CapitalExpenDRDVAWells;
                        //        dt.CapitalExpenDRSubSeaWells = d.CapexSummary.CapitalExpenDRSubSeaWells;
                        //        dt.ContigencyInTangWells = d.CapexSummary.ContigencyInTangWells;
                        //        dt.ContigencyTangWells = d.CapexSummary.ContigencyTangWells;
                        //        dt.EPEXCompletionB2ExplInTang = d.CapexSummary.EPEXCompletionB2ExplInTang;
                        //        dt.EPEXCompletionB2ExplTang = d.CapexSummary.EPEXCompletionB2ExplTang;
                        //        dt.EPEXDrillingB2ExplInTang = d.CapexSummary.EPEXDrillingB2ExplInTang;
                        //        dt.EPEXDrillingB2ExplTang = d.CapexSummary.EPEXDrillingB2ExplTang;
                        //        dt.OPCostIdleRig = d.CapexSummary.OPCostIdleRig;
                        //        dt.Currency = d.CapexSummary.Currency;
                        //        dt.DateId = d.MonthId;
                        //        dt.Type = d.CapexSummary.Type;
                        //        ld.Add(dt);
                        //    }

                        //    ag.CapexSummary = ld;
                        //    ag._id = "C" + g.Key.Replace(" ", "").Replace("-", "") + "U" + g.FirstOrDefault().UpdateBy.Replace(" ", "").Replace("-", "") + "M" + g.FirstOrDefault().MapName.Replace(" ", "").Replace("-", "");
                        //    ag.Save();
                        //    //DataHelper.Save("WEISPalantirCapexAggTemp", ag.ToBsonDocument());

                        //    int[] listOldID = oldID.ToArray();
                        //    foreach (int id in listOldID)
                        //    {
                        //        DataHelper.Delete("WEISPalantirCapexAgg", id);
                        //    }
                        //}
                        #endregion

                        #region delete by mapname jika diperlukan
                        DataHelper.Delete("WEISPalantirCapexAgg", Query.EQ("MapName", mapname.Key));
                        DataHelper.Delete("WEISPalantirCapexAggHorizontal", Query.EQ("MapName", mapname.Key));
                        #endregion
                    }

                    return new { Status = true };
                }
                catch (Exception e)
                {
                    return new { Status = false };
                }
            });
        }
    }
}