using ECIS.Client.WEIS;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class BusPlanRollUpController : Controller
    {
        //
        // GET: /Shell/BusPlanRollUp/
        public ActionResult Index()
        {
            return View();
        }
        protected class DetailData
        {
            public DetailData()
            {
                Year = string.Empty;
                DataValue = new List<DetailDataPackage>();
            }
            public string Year { get; set; }
            public List<DetailDataPackage> DataValue { get; set; }
        }

        protected class DetailDataPackage
        {
            public DetailDataPackage(){
                WellName = string.Empty;
                RigName = string.Empty;
                ActivityType = string.Empty;
                EscalationCostEDMRig = new DetailValue();
                EscalationCostEDMServices = new DetailValue();
                EscalationCostEDMMaterial = new DetailValue();
                EscalationCostEDMTotal = new DetailValue();
                CSOCostEDM = new DetailValue();
                InflationCostEDM = new DetailValue();
                MeanCostEDM = new DetailValue();
                MeanCostRealTerm = new DetailValue();
                MeanCostMOD = new DetailValue();
                ShellShareMOD = new DetailValue();
            }
            public string WellName { get; set; }
            public string RigName { get; set; }
            public string ActivityType { get; set; }
            public DetailValue EscalationCostEDMRig { get; set; }
            public DetailValue EscalationCostEDMServices { get; set; }
            public DetailValue EscalationCostEDMMaterial { get; set; }
            public DetailValue EscalationCostEDMTotal { get; set; }
            public DetailValue CSOCostEDM { get; set; }
            public DetailValue InflationCostEDM { get; set; }
            public DetailValue MeanCostEDM { get; set; }
            public DetailValue MeanCostRealTerm { get; set; }
            public DetailValue MeanCostMOD { get; set; }
            public DetailValue ShellShareMOD { get; set; }
        }

        protected class DetailValue
        {
            public DetailValue()
            {
                Value = 0;
                Comparison = 0;
                Delta = 0;
            }

            public DetailValue(double v, double c)
            {
                Value = v;
                Comparison = c;
                Delta = (v - c);
            }

            public double Value { get; set; }
            public double Comparison { get; set; }
            public double Delta { get; set; }
        }

        public JsonResult GetData(WaterfallBase wb, BizPlanTLASetting TLASetting, bool isTLA = false, string CompareWith = "CurrentOP")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var email = WebTools.LoginUser.Email;
                var rawData = wb.GetActivitiesForBizPlan(email);

                var bizplanActivityData = rawData.SelectMany(x => x.Phases, (x, p) => new
                {
                    WellName = x.WellName,
                    RigName = x.RigName,
                    Phases = p
                }).Where(x => x.Phases != null && x.Phases.Allocation != null).ToList();

                var anuls = bizplanActivityData.SelectMany(x => x.Phases.Allocation.AnnualyBuckets).GroupBy(x => x.Key).ToList();

                var FiscalYearStart = 2015;
                var FiscalYearFinish = 2015;

                if (anuls.Any())
                {
                    FiscalYearFinish = anuls.Max(x => Convert.ToInt32(x.Key));
                    FiscalYearStart = anuls.Min(x => Convert.ToInt32(x.Key));
                }

                var Particular = new List<string>() { "Esc Cost - Rig", "Esc Cost - Services", "Esc Cost - Materials", "Esc Cost - Total", "CSO Cost", "Inflation Cost", "Mean Cost EDM", "Mean Cost Real Term", "Mean Cost MOD", "Shell Share MOD" };
                var summaryData = new List<BsonDocument>();
                List<DetailData> detailData = new List<DetailData>();

                var currentOPLELS = new Dictionary<int, double>();

                #region Get Detail Data
                var detailList = bizplanActivityData.Where(x => x.Phases != null && x.Phases.Allocation != null).ToList();
                for (var y = FiscalYearStart; y <= FiscalYearFinish; y++)
                {
                    var d = new DetailData();
                    d.Year = "FY" + y;
                    foreach (var dl in detailList)
                    {
                        foreach (var ph in dl.Phases.Allocation.AnnualyBuckets)
                        {
                            if (ph.Key == y.ToString())
                            {
                                DetailDataPackage pkg = new DetailDataPackage();
                                pkg.WellName = dl.WellName;
                                pkg.RigName = dl.RigName;
                                pkg.ActivityType = dl.Phases.ActivityType;

                                var eachYear = Convert.ToInt32(ph.Key);
                                var comparison = BizPlanActivityPhase.GetCurrentOPLELSValueOfYear(dl.WellName, dl.RigName, dl.Phases.ActivityType, eachYear, CompareWith);

                                if (currentOPLELS.ContainsKey(eachYear))
                                    currentOPLELS[eachYear] += comparison;
                                else
                                    currentOPLELS[eachYear] = comparison;

                                pkg.EscalationCostEDMRig = new DetailValue(ph.EscCostRig, comparison);
                                pkg.EscalationCostEDMServices = new DetailValue(ph.EscCostServices, comparison);
                                pkg.EscalationCostEDMMaterial = new DetailValue(ph.EscCostMaterial, comparison);
                                pkg.EscalationCostEDMTotal = new DetailValue(ph.EscCostTotal, comparison);
                                pkg.CSOCostEDM = new DetailValue(ph.CSOCost, comparison);
                                pkg.InflationCostEDM = new DetailValue(ph.InflationCost, comparison);
                                pkg.MeanCostEDM = new DetailValue(ph.MeanCostEDM, comparison);
                                pkg.MeanCostRealTerm = new DetailValue(ph.MeanCostRealTerm, comparison);
                                pkg.MeanCostMOD = new DetailValue(ph.MeanCostMOD, comparison);
                                pkg.ShellShareMOD = new DetailValue(ph.ShellShare, comparison);

                                d.DataValue.Add(pkg);
                            }
                        }
                    }
                    detailData.Add(d);
                    //detailData.Set("FY" + y, DataHelper.ToDictionaryArray(d));
                }
                //foreach (var d in bizplanActivityData)
                //{

                //}
                //i = 1;
                //foreach (var p in Particular)
                //{
                //    BsonDocument data= new BsonDocument();
                //    data.Set("Particular", p);
                //    for (var y = FiscalYearStart; y <= FiscalYearFinish; y++)
                //    {
                //        double val = r.Next(5,100);
                //        double comparison = r.Next(5, 100);
                //        data.Set("FY" + y + "val", val);
                //        data.Set("FY" + y + "comparison", comparison);
                //        data.Set("FY" + y + "delta", val-comparison);
                //    }
                //    summaryData.Add(data);
                //    i++;
                //}   
                #endregion

                #region Get Summary Data
                var i = 1;
                var sumList = bizplanActivityData.Where(x => x.Phases != null && x.Phases.Allocation != null).SelectMany(a => a.Phases.Allocation.Annualy).ToList();
                foreach (var p in Particular)
                {
                    BsonDocument data = new BsonDocument();
                    data.Set("Particular", p);
                    for (var y = FiscalYearStart; y <= FiscalYearFinish; y++)
                    {
                        double val = 0;

                        switch (i)
                        {
                            case 1:
                                val = sumList.Where(x => x.Key == y.ToString()).Sum(x => x.EscalationCostEDMRig);
                                break;
                            case 2:
                                val = sumList.Where(x => x.Key == y.ToString()).Sum(x => x.EscalationCostEDMServices);
                                break;
                            case 3:
                                val = sumList.Where(x => x.Key == y.ToString()).Sum(x => x.EscalationCostEDMMaterial);
                                break;
                            case 4:
                                val = sumList.Where(x => x.Key == y.ToString()).Sum(x => x.EscalationCostEDMTotal);
                                break;
                            case 5:
                                val = sumList.Where(x => x.Key == y.ToString()).Sum(x => x.CSOCostEDM);
                                break;
                            case 6:
                                val = sumList.Where(x => x.Key == y.ToString()).Sum(x => x.InflationCostEDM);
                                break;
                            case 7:
                                val = sumList.Where(x => x.Key == y.ToString()).Sum(x => x.MeanCostEDM);
                                break;
                            case 8:
                                val = sumList.Where(x => x.Key == y.ToString()).Sum(x => x.MeanCostRealTerm);
                                break;
                            case 9:
                                val = sumList.Where(x => x.Key == y.ToString()).Sum(x => x.MeanCostMOD);
                                break;
                            case 10:
                                val = sumList.Where(x => x.Key == y.ToString()).Sum(x => x.ShellShareMOD);
                                break;
                            default: break;
                        }

                        var comparison = 0.0;
                        if (currentOPLELS.ContainsKey(y))
                            comparison = currentOPLELS[y];

                        data.Set("FY" + y + "val", val);
                        data.Set("FY" + y + "comparison", comparison);
                        data.Set("FY" + y + "delta", val - comparison);
                    }
                    summaryData.Add(data);
                    i++;
                }
                #endregion

                return new { Summary = DataHelper.ToDictionaryArray(summaryData), DetailData = detailData, YearStart = FiscalYearStart, YearFinish = FiscalYearFinish };
            });
        }
	}
}