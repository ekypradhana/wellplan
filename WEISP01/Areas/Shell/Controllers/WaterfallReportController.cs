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
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using ECIS.Identity;
using System.Text.RegularExpressions;
using System.Text;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class WaterfallReportController : Controller
    {
        public ActionResult Index()
        {
            var lts = new PIPAnalysisController().GetLatestLsDate();//new pipana().GetLatestLsDate();
            ViewBag.LatestLS = lts;
            return View();
        }

        public List<string> GetWellListBasedOnEmail()
        {
            var ret = new List<string>();
            var wp = WEISPerson.Populate<WEISPerson>(q: Query.EQ("PersonInfos.Email", WebTools.LoginUser.Email));
            if (wp.Count > 0)
                ret = wp.Select(x => x.WellName).ToList();
            return ret;
        }

        public DateTime GetLatestLsDate()
        {
            //return MvcResultInfo.Execute(null, document =>
            //{
            var ret = new DateTime();
            var t = new UploadLSController().GetCollectionName();
            List<DateTime> docs = new List<DateTime>();
            foreach (string y in t)
            {
                string yName = y.Substring(2, 6);

                DateTime dt = new DateTime();
                dt = DateTime.ParseExact(yName.ToString(), "yyyyMM", System.Globalization.CultureInfo.InvariantCulture);
                docs.Add(dt);
            }
            if (docs.Any())
            {
                var _ret = docs.OrderByDescending(x => x);
                ret = _ret.FirstOrDefault();
            }
            return ret;
            //});
        }
        public double getBankedSaving(List<WellActivity> Was, string Classification, string DayOrCost, bool ShellShare = false)
        {
            double res = 0.0;
            var div = 1;
            switch (Classification.ToLower())
            {
                case "technology and innovation":
                    if (DayOrCost.ToLower().Equals("cost"))
                    {
                        if(ShellShare){
                            var total = 0.0;
                            foreach (var wa in Was) {
                                total += wa.Phases.Sum(x => x.BankedSavingsTechnologyAndInnovation.Cost) * (wa.WorkingInterest > 1 ? Tools.Div(wa.WorkingInterest, 100) : wa.WorkingInterest);
                            }
                            res = Tools.Div(total, div);
                        }
                        else
                            res = Tools.Div(Was.Sum(x => x.Phases.Sum(d => d.BankedSavingsTechnologyAndInnovation.Cost)), div);

                    }
                    else
                    {
                        res = Was.Sum(x => x.Phases.Sum(d => d.BankedSavingsTechnologyAndInnovation.Days));
                    }
                    break;
                case "supply chain transformation":
                    if (DayOrCost.ToLower().Equals("cost"))
                    {
                        if (ShellShare)
                        {
                            var total = 0.0;
                            foreach (var wa in Was)
                            {
                                total += wa.Phases.Sum(x => x.BankedSavingsSupplyChainTransformation.Cost) * (wa.WorkingInterest > 1 ? Tools.Div(wa.WorkingInterest, 100) : wa.WorkingInterest);
                            }
                            res = Tools.Div(total, div);
                        }
                        else
                        res = Tools.Div(Was.Sum(x => x.Phases.Sum(d => d.BankedSavingsSupplyChainTransformation.Cost)), div);
                    }
                    else
                    {
                        res = Was.Sum(x => x.Phases.Sum(d => d.BankedSavingsSupplyChainTransformation.Days));
                    }
                    break;
                case "competitive scope":
                    if (DayOrCost.ToLower().Equals("cost"))
                    {
                        if (ShellShare)
                        {
                            var total = 0.0;
                            foreach (var wa in Was)
                            {
                                total += wa.Phases.Sum(x => x.BankedSavingsCompetitiveScope.Cost) * (wa.WorkingInterest > 1 ? Tools.Div(wa.WorkingInterest, 100) : wa.WorkingInterest);
                            }
                            res = Tools.Div(total, div);
                        }
                        else
                        res = Tools.Div(Was.Sum(x => x.Phases.Sum(d => d.BankedSavingsCompetitiveScope.Cost)), div);
                    }
                    else
                    {
                        res = Was.Sum(x => x.Phases.Sum(d => d.BankedSavingsCompetitiveScope.Days));
                    }
                    break;
                case "efficient execution":
                    if (DayOrCost.ToLower().Equals("cost"))
                    {
                        if (ShellShare)
                        {
                            var total = 0.0;
                            foreach (var wa in Was)
                            {
                                total += wa.Phases.Sum(x => x.BankedSavingsEfficientExecution.Cost) * ( wa.WorkingInterest > 1 ? Tools.Div(wa.WorkingInterest,100 ) : wa.WorkingInterest) ;
                            }
                            res = Tools.Div(total, div);
                        }
                        else
                        res = Tools.Div(Was.Sum(x => x.Phases.Sum(d => d.BankedSavingsEfficientExecution.Cost)), div);
                    }
                    else
                    {
                        res = Was.Sum(x => x.Phases.Sum(d => d.BankedSavingsEfficientExecution.Days));
                    }
                    break;
            }
            return res;
        }

        public JsonResult GetData(WaterfallBase wb, string which, string ExtraItem = "TQ")//extraElem => TQ or BIC
        {
            return MvcResultInfo.Execute(() =>
            {
                if (wb.WellNames == null || !wb.WellNames.Any())
                {
                    if (GetWellListBasedOnEmail().Any(x => x != "*"))
                    {
                        wb.WellNames = GetWellListBasedOnEmail();
                    }
                }
                
                //WaterfallBase wbForOP = new WaterfallBase();
                //wbForOP = BsonHelper.Deserialize<WaterfallBase>(wb.ToBsonDocument());

                //wbForOP.GetWhatData = "OP";
                ////wbForOP.DataFor = "Waterfall";
                //var waForOP = wbForOP.GetActivitiesAndPIPs();

                //wb.GetWhatData = "OP";
                //wb.DataFor = "Waterfall";
                var wa = wb.GetActivitiesAndPIPs();

                if (which.Equals("Waterfall"))
                {
                    var DataLE = ParseWaterfallData(wb, wa, "LE", "LE");
                    var DataExtraItem = ParseWaterfallData(wb, wa, ExtraItem, "Plan");

                    var GetDataRealizedLE = ParseWaterfallRealizedData(wb, wa, "LE", "LE");
                    var DataRealizedLE = new List<BsonDocument>();
                    if (GetDataRealizedLE != null && GetDataRealizedLE.Count > 0 && wb.GroupBy.ToLower().Equals("classification"))
                    {
                        foreach (var a in GetDataRealizedLE)
                        {
                            if (a.GetBool("IsRealized") == true)
                            {
                                var ValueBefore = a.GetDouble("Value");
                                var BankedSaving = getBankedSaving(wa.Activities, a.GetString("Category").ToLower(), wb.DayOrCost, wb.ShellShare);
                                DataRealizedLE.Add(new { Category = a.GetString("Category"), Value = ValueBefore + BankedSaving, Summary = "", IsRealized = true }.ToBsonDocument());
                            }
                            else
                            {
                                if (a.GetString("Category").ToLower() == "gap to le".ToLower())
                                {
                                    var PIPClass = WellPIPClassifications.Populate<WellPIPClassifications>();
                                    foreach (var pc in PIPClass)
                                    {
                                        var check = GetDataRealizedLE.Where(x => x.GetString("Category").Equals(pc.Name)).FirstOrDefault();
                                        if (check == null)
                                        {
                                            var BankedSaving = getBankedSaving(wa.Activities, pc.Name.ToLower(), wb.DayOrCost, wb.ShellShare);
                                            if (!wb.IncludeZero && BankedSaving == 0.0)
                                            { }
                                            else
                                            {
                                                DataRealizedLE.Add(new { Category = pc.Name, Value = BankedSaving, Summary = "", IsRealized = true }.ToBsonDocument());
                                            }
                                        }
                                    }

                                    var DataClass = DataRealizedLE.Where(x => x.GetBool("IsRealized")).Sum(x => x.GetDouble("Value"));
                                    if (DataClass + wa.TotalOP != wa.TotalLE)
                                    {
                                        DataRealizedLE.Add(new { Category = "Gap to LE", Value = (wa.TotalLE - (DataClass + wa.TotalOP)), Summary = "" }.ToBsonDocument());
                                    }

                                }
                                else
                                {
                                    DataRealizedLE.Add(a);
                                }
                            }
                        }
                    }
                    else
                    {
                        DataRealizedLE = GetDataRealizedLE;
                    }

                    var DataRealizedTQ = ParseWaterfallRealizedData(wb, wa, "TQ", "Plan");


                    var DataGrid = ParseWaterfallGridData(wb, wa);
                    //DataHelper.Delete("_DatagridWF");
                    //DataHelper.Save("_DatagridWF", DataGrid);

                   

                    #region ordering data

                    var DataLEOrdered = new List<BsonDocument>();
                    if (DataLE.Any())
                    {
                        if (DataLE.Where(x => x.GetString("Category").Equals(BaseOPTitle(wb.BaseOP))).Any())
                            DataLEOrdered.Add(DataLE.Where(x => x.GetString("Category").Equals(BaseOPTitle(wb.BaseOP))).FirstOrDefault());
                        foreach (var le in DataLE.OrderBy(x => x.GetString("Category")))
                        {
                            if (le.GetString("Category") != BaseOPTitle(wb.BaseOP) && le.GetString("Category") != "LE" && le.GetString("Category") != "Gap")
                            {
                                DataLEOrdered.Add(le);
                            }
                        }
                        if(DataLE.Where(x => x.GetString("Category").Equals("Gap")).Any())
                            DataLEOrdered.Add(DataLE.Where(x => x.GetString("Category").Equals("Gap")).FirstOrDefault());
                        if(DataLE.Where(x => x.GetString("Category").Equals("LE")).Any())
                            DataLEOrdered.Add(DataLE.Where(x => x.GetString("Category").Equals("LE")).FirstOrDefault());
                    }

                    var DataExtraItemOrdered = new List<BsonDocument>();
                    if (DataExtraItem.Any())
                    {
                        if (DataExtraItem.Where(x => x.GetString("Category").Equals(BaseOPTitle(wb.BaseOP))).Any())
                            DataExtraItemOrdered.Add(DataExtraItem.Where(x => x.GetString("Category").Equals(BaseOPTitle(wb.BaseOP))).FirstOrDefault());
                        foreach (var le in DataExtraItem.OrderBy(x => x.GetString("Category")))
                        {
                            if (le.GetString("Category") != BaseOPTitle(wb.BaseOP) && le.GetString("Category") != "TQ" && le.GetString("Category") != "BIC" && le.GetString("Category") != "Gap" && le.GetString("Category") != "Target")
                            {
                                DataExtraItemOrdered.Add(le);
                            }
                        }
                        if(DataExtraItem.Where(x => x.GetString("Category").Equals("Gap")).Any())
                            DataExtraItemOrdered.Add(DataExtraItem.Where(x => x.GetString("Category").Equals("Gap")).FirstOrDefault());
                        if (ExtraItem == "TQ")
                        {
                            if (DataExtraItem.Where(x => x.GetString("Category").Equals("TQ")).Any())
                                DataExtraItemOrdered.Add(DataExtraItem.Where(x => x.GetString("Category").Equals("TQ")).FirstOrDefault());
                        }
                        else if (ExtraItem == "BIC")
                        {
                            if (DataExtraItem.Where(x => x.GetString("Category").Equals("BIC")).Any())
                                DataExtraItemOrdered.Add(DataExtraItem.Where(x => x.GetString("Category").Equals("BIC")).FirstOrDefault());
                        }
                        else
                        {
                            if (DataExtraItem.Where(x => x.GetString("Category").Equals("Target")).Any())
                                DataExtraItemOrdered.Add(DataExtraItem.Where(x => x.GetString("Category").Equals("Target")).FirstOrDefault());
                        }
                    }

                    var DataRealizedLEOrdered = new List<BsonDocument>();
                    if (DataRealizedLE.Any())
                    {
                        if (DataRealizedLE.Where(x => x.GetString("Category").Equals(BaseOPTitle(wb.BaseOP))).Any())
                            DataRealizedLEOrdered.Add(DataRealizedLE.Where(x => x.GetString("Category").Equals(BaseOPTitle(wb.BaseOP))).FirstOrDefault());
                        foreach (var le in DataRealizedLE.Where(x => x.GetBool("IsRealized") == true).OrderBy(x => x.GetString("Category")))
                        {
                            DataRealizedLEOrdered.Add(le);
                        }
                        if (DataRealizedLE.Where(x => x.GetString("Category").Equals("Gap to LE")).Any())
                            DataRealizedLEOrdered.Add(DataRealizedLE.Where(x => x.GetString("Category").Equals("Gap to LE")).FirstOrDefault());

                        if (DataRealizedLE.Where(x => x.GetString("Category").Equals("LE")).Any())
                            DataRealizedLEOrdered.Add(DataRealizedLE.Where(x => x.GetString("Category").Equals("LE")).FirstOrDefault());

                        foreach (var le in DataRealizedLE.Where(x => x.GetBool("IsRealized") == false).OrderBy(x => x.GetString("Category")))
                        {
                            if (le.GetString("Category") != BaseOPTitle(wb.BaseOP) && le.GetString("Category") != "Gap to LE" && le.GetString("Category") != "LE" && le.GetString("Category") != "Unrisked Upside")
                            {
                                DataRealizedLEOrdered.Add(le);
                            }

                        }
                        if (DataRealizedLE.Where(x => x.GetString("Category").Equals("Unrisked Upside")).Any())
                            DataRealizedLEOrdered.Add(DataRealizedLE.Where(x => x.GetString("Category").Equals("Unrisked Upside")).FirstOrDefault());
                    }
                    #endregion

                    //DataHelper.Delete("_DatagridRealized");
                    //DataHelper.Save("_DatagridRealized", DataRealizedLEOrdered);

                    //DataHelper.Delete("_DatagridLE");
                    //DataHelper.Save("_DatagridLE", DataLEOrdered);

                    return new
                    {
                        Waterfall = new
                        {
                            OP = wa.TotalOP,
                            LE = wa.TotalLE,
                            TQ = wa.TotalTQ,
                            BIC = wa.TotalBIC,
                            DataLE = DataHelper.ToDictionaryArray(DataLEOrdered),
                            DataTQ = DataHelper.ToDictionaryArray(DataExtraItemOrdered),
                            MaxHeight = (new Double[] { wa.TotalOP, wa.TotalLE, wa.TotalTQ }.Max() * 1.2),
                        },
                        WaterfallRealized = new
                        {
                            OP = wa.TotalOP,
                            LE = wa.TotalLE,
                            TQ = wa.TotalTQ,
                            BIC = wa.TotalBIC,
                            DataLE = DataHelper.ToDictionaryArray(DataRealizedLEOrdered),
                            DataTQ = DataHelper.ToDictionaryArray(DataRealizedTQ),
                            MaxHeight = (new Double[] { wa.TotalOP, wa.TotalLE, wa.TotalTQ }.Max() * 1.2),
                            LastLS = GetLatestLsDate()
                        },
                        WaterfallGrid = DataHelper.ToDictionaryArray(DataGrid)
                    };
                }
                else
                {
                    var waUnwinded = wa.Activities.SelectMany(d => d.Phases, (d, e) => new ActivityAndPhase() { Activity = d, Phase = e }).ToList();

                    var CumulativeGrid = new List<BsonDocument>();
                    var CumulativeGridColumns = new Dictionary<string, List<Dictionary<string, object>>>();
                    var CumulativeChart = new List<BsonDocument>();
                    var AllocationYears = new List<Int32>();

                    ParseCumulativeData(wb, wa, out CumulativeGrid, out CumulativeGridColumns, out CumulativeChart, out AllocationYears);

                    return new
                    {
                        CumulativeGrid = new
                        {
                            Data = DataHelper.ToDictionaryArray(CumulativeGrid),
                            Columns = CumulativeGridColumns
                        },
                        CumulativeChart = new
                        {
                            Data = DataHelper.ToDictionaryArray(CumulativeChart),
                            AllocationYears = AllocationYears
                        }
                    };
                }
            });
        }

        public JsonResult GetDataWaterfallByRealized(WaterfallBase wb)
        {
            return MvcResultInfo.Execute(() =>
            {
                if (wb.WellNames == null || !wb.WellNames.Any())
                {
                    if (GetWellListBasedOnEmail().Any(x => x != "*"))
                    {
                        wb.WellNames = GetWellListBasedOnEmail();
                    }
                }

                //WaterfallBase wbForOP = new WaterfallBase();
                //wbForOP = BsonHelper.Deserialize<WaterfallBase>(wb.ToBsonDocument());

                //wbForOP.GetWhatData = "OP";
                ////wbForOP.DataFor = "Waterfall";
                //var waForOP = wbForOP.GetActivitiesAndPIPs();

                wb.GetWhatData = "OP";
                //wb.DataFor = "Waterfall";
                var wa = wb.GetActivitiesAndPIPs();
                //var Categories = new List<string>(){wb.BaseOP,"LS Reduction","LS Increase","Adjusted "+wb.BaseOP+" for LS","Competitive Scope","Supply Chain Transformation","Efficient Execution","Technology and Innovation"};
                var GetDataRealizedLE = ParseWaterfallRealizedData(wb, wa, "LE", "LE");
                var DataRealizedLE = new List<BsonDocument>();
                if (GetDataRealizedLE != null && GetDataRealizedLE.Count > 0 && wb.GroupBy.ToLower().Equals("classification"))
                {
                    foreach (var a in GetDataRealizedLE)
                    {
                        if (a.GetBool("IsRealized") == true)
                        {
                            var ValueBefore = a.GetDouble("Value");
                            var BankedSaving = getBankedSaving(wa.Activities, a.GetString("Category").ToLower(), wb.DayOrCost, wb.ShellShare);
                            DataRealizedLE.Add(new { Category = a.GetString("Category"), Value = ValueBefore + BankedSaving, Summary = "", IsRealized = true }.ToBsonDocument());
                        }
                        else
                        {
                            if (a.GetString("Category").ToLower() == "gap to le".ToLower())
                            {
                                var PIPClass = WellPIPClassifications.Populate<WellPIPClassifications>();
                                foreach (var pc in PIPClass)
                                {
                                    var check = GetDataRealizedLE.Where(x => x.GetString("Category").Equals(pc.Name)).FirstOrDefault();
                                    if (check == null)
                                    {
                                        var BankedSaving = getBankedSaving(wa.Activities, pc.Name.ToLower(), wb.DayOrCost, wb.ShellShare);
                                        if (!wb.IncludeZero && BankedSaving == 0.0)
                                        { }
                                        else
                                        {
                                            DataRealizedLE.Add(new { Category = pc.Name, Value = BankedSaving, Summary = "", IsRealized = true }.ToBsonDocument());
                                        }
                                    }
                                }

                                var DataClass = DataRealizedLE.Where(x => x.GetBool("IsRealized")).Sum(x => x.GetDouble("Value"));
                                if (DataClass + wa.TotalOP != wa.TotalLE)
                                {
                                    DataRealizedLE.Add(new { Category = "Gap to LE", Value = (wa.TotalLE - (DataClass + wa.TotalOP)), Summary = "" }.ToBsonDocument());
                                }

                            }
                            else
                            {
                                DataRealizedLE.Add(a);
                            }
                        }
                    }
                }
                else
                {
                    DataRealizedLE = GetDataRealizedLE;
                }

                
                var res = new List<WaterfallByRealizedHelper>();
                var OP = wa.TotalOP;
                var isNegativeOP = OP < 0;
                var PO = 0.0;
                if (wb.DayOrCost == "Days")
                {
                    PO = wa.Activities.SelectMany(x => x.Phases).Where(x => x.IsPostOP).Sum(x => x.OP.Days);
                }
                else
                {
                    PO = wa.Activities.SelectMany(x => x.Phases).Where(x => x.IsPostOP).Sum(x => x.OP.Cost);
                }

                //OP = isNegativeOP ? OP*-1 : OP;

                var _queryForLatestLS = new List<IMongoQuery>();
                var queryForLatestLS = Query.Null;
                if (wb.WellNames != null && wb.WellNames.Any())
                    _queryForLatestLS.Add(Query.In("WellName", new BsonArray(wb.WellNames)));
                if (wb.RigNames != null && wb.RigNames.Any())
                    _queryForLatestLS.Add(Query.In("RigName", new BsonArray(wb.RigNames)));
                if (wb.RigTypes != null && wb.RigTypes.Any())
                    _queryForLatestLS.Add(Query.In("RigType", new BsonArray(wb.RigTypes)));
                if (wb.Activities != null && wb.Activities.Any())
                    _queryForLatestLS.Add(Query.In("ActivityType", new BsonArray(wb.Activities)));
                if (_queryForLatestLS.Any())
                {
                    queryForLatestLS = Query.And(_queryForLatestLS);
                }
                var lsAll = LatestLSDifferences.Populate<LatestLSDifferences>(queryForLatestLS);// DataHelper.Populate("LatestLSDifferences", queryForLatestLS);

                var Increase = lsAll.Where(x => x.DifferentType.Equals("increase")).Sum(x => x.DifferentValue);
                var Decrease = lsAll.Where(x => x.DifferentType.Equals("decrease")).Sum(x => x.DifferentValue);

                var LSReduction = Decrease;
                var isNegativeLSReduction = LSReduction < 0;
                //LSReduction = isNegativeLSReduction ? LSReduction * -1 : LSReduction;

                var LSIncrease = Increase;
                var isNegativeLSIncrease = LSIncrease < 0;
                //LSIncrease = isNegativeLSIncrease ? LSIncrease * -1 : LSIncrease;

                var AdjustedOP = OP + LSReduction + LSIncrease;
                var isNegativeAdjustedOP = AdjustedOP < 0;
                //AdjustedOP = isNegativeAdjustedOP ? AdjustedOP * -1 : AdjustedOP;

                var realizedCompetitiveScope = DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Equals("Competitive Scope") && x.GetBool("IsRealized") == true) != null ? DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Equals("Competitive Scope") && x.GetBool("IsRealized") == true).GetDouble("Value") : 0;
                var isNegativerealizedCompetitiveScope = realizedCompetitiveScope < 0;
                //realizedCompetitiveScope = isNegativerealizedCompetitiveScope ? realizedCompetitiveScope * -1 : realizedCompetitiveScope;

                var realizedSupplyChainTransformation = DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Equals("Supply Chain Transformation") && x.GetBool("IsRealized") == true) != null ? DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Equals("Supply Chain Transformation") && x.GetBool("IsRealized") == true).GetDouble("Value") : 0;
                var isNegativerealizedSupplyChainTransformation = realizedSupplyChainTransformation < 0;
                //realizedSupplyChainTransformation = isNegativerealizedSupplyChainTransformation ? realizedSupplyChainTransformation * -1 : realizedSupplyChainTransformation;

                var realizedEfficientExecution = DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Equals("Efficient Execution") && x.GetBool("IsRealized") == true) != null ? DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Equals("Efficient Execution") && x.GetBool("IsRealized") == true).GetDouble("Value") : 0;
                var isNegativerealizedEfficientExecution = realizedEfficientExecution < 0;
                //realizedEfficientExecution = isNegativerealizedEfficientExecution ? realizedEfficientExecution * -1 : realizedEfficientExecution;

                var realizedTechnologyandInnovation = DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Equals("Technology and Innovation") && x.GetBool("IsRealized") == true) != null ? DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Equals("Technology and Innovation") && x.GetBool("IsRealized") == true).GetDouble("Value") : 0;
                var isNegativerealizedTechnologyandInnovation = realizedTechnologyandInnovation < 0;
                //realizedTechnologyandInnovation = isNegativerealizedTechnologyandInnovation ? realizedTechnologyandInnovation * -1 : realizedTechnologyandInnovation;

                var PostOP = PO; //3000.0;
                var isNegativePostOP = PostOP < 0;
                //PostOP = isNegativePostOP ? PostOP * -1 : PostOP;

                var LE = wa.TotalLE;
                var isNegativeLE = LE < 0;
                //LE = isNegativeLE ? LE * -1 : LE;

                var Gap = LE - PostOP;
                var isNegativeGap = Gap < 0;
                //Gap = isNegativeGap ? Gap * -1 : Gap;
                var jjj = DataRealizedLE.Where(x => x.GetString("Category").Trim().Equals("Competitive Scope"));
                var iiii = DataRealizedLE.Where(x => BsonHelper.GetBool(x, "IsRealized") == false);
                var aaaaa = DataRealizedLE.Where(x => x.GetString("Category").Equals("Competitive Scope") && BsonHelper.GetBool(x,"IsRealized") == false).FirstOrDefault();

                var unrealizedCompetitiveScope = DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Trim().Equals("Competitive Scope") && x.GetBool("IsRealized") != true) != null ? DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Trim().Equals("Competitive Scope") && x.GetBool("IsRealized") != true).GetDouble("Value") : 0;
                var isNegativeunrealizedCompetitiveScope = unrealizedCompetitiveScope < 0;
                //unrealizedCompetitiveScope = isNegativeunrealizedCompetitiveScope ? unrealizedCompetitiveScope * -1 : unrealizedCompetitiveScope;

                var unrealizedSupplyChainTransformation = DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Trim().Equals("Supply Chain Transformation") && x.GetBool("IsRealized") == false) != null ? DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Trim().Equals("Supply Chain Transformation") && x.GetBool("IsRealized") == false).GetDouble("Value") : 0;
                var isNegativeunrealizedSupplyChainTransformation = unrealizedSupplyChainTransformation < 0;
                //unrealizedSupplyChainTransformation = isNegativeunrealizedSupplyChainTransformation ? unrealizedSupplyChainTransformation * -1 : unrealizedSupplyChainTransformation;

                var unrealizedEfficientExecution = DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Trim().Equals("Efficient Execution") && x.GetBool("IsRealized") == false) != null ? DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Trim().Equals("Efficient Execution") && x.GetBool("IsRealized") == false).GetDouble("Value") : 0;
                var isNegativeunrealizedEfficientExecution = unrealizedEfficientExecution < 0;
                //unrealizedEfficientExecution = isNegativeunrealizedEfficientExecution ? unrealizedEfficientExecution * -1 : unrealizedEfficientExecution;

                var unrealizedTechnologyandInnovation = DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Trim().Equals("Technology and Innovation") && x.GetBool("IsRealized") == false) != null ? DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Trim().Equals("Technology and Innovation") && x.GetBool("IsRealized") == false).GetDouble("Value") : 0;
                var isNegativeunrealizedTechnologyandInnovation = unrealizedTechnologyandInnovation < 0;
                //unrealizedTechnologyandInnovation = isNegativeunrealizedTechnologyandInnovation ? unrealizedTechnologyandInnovation * -1 : unrealizedTechnologyandInnovation;

                var UnriskUpside = DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Trim().Equals("Unrisked Upside")) != null ? DataRealizedLE.FirstOrDefault(x => x.GetString("Category").Trim().Equals("Unrisked Upside")).GetDouble("Value") : 0;
                var isNegativeUnriskUpside = UnriskUpside < 0;
                //UnriskUpside = isNegativeUnriskUpside ? UnriskUpside * -1 : UnriskUpside;

                var CostAvoidance = 0.0;
                var PIPs = wa.PIPs;
                if (PIPs.Any())
                {
                    foreach (var pip in PIPs)
                    {
                        if (pip.Elements.Any())
                        {
                            foreach (var e in pip.Elements)
                            {
                                CostAvoidance += e.CostAvoidanceValue;
                            }
                        }
                    }
                }

                #region Remark
                //// series stack structures : OP, LS Reduction, LS Increase, Adjusted OP for LS, Competitive Scope (realized),"Supply Chain Transformation (realized)","Efficient Execution (realized)","Technology and Innovation (realized)",gap & Post OP, LE
                //// Competitive Scope (unrealized),"Supply Chain Transformation (unrealized)","Efficient Execution (unrealized)","Technology and Innovation (unrealized)",unrisked upside, Cost Avoidance
                //var tmpDataBotSingle = 0.0;
                //var DataBottom = new List<double>();
                //var DataTop = new List<double>();
                //DataBottom.Add(0);

                //var DataOP = new List<double>() { Math.Abs(Math.Round(OP, 2)), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                //tmpDataBotSingle = tmpDataBotSingle + OP + LSReduction;
                //DataBottom.Add(tmpDataBotSingle);
                //DataTop.Add(Math.Abs(Math.Round(OP, 2)));

                //var DataLSReduction = new List<double>() { 0, Math.Abs(Math.Round(LSReduction, 2)), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                //tmpDataBotSingle = tmpDataBotSingle;// - LSIncrease;
                //DataBottom.Add(tmpDataBotSingle);
                //DataTop.Add(Math.Abs(Math.Round(LSReduction, 2)));

                //var DataLSIncrease = new List<double>() { 0, 0, Math.Abs(Math.Round(LSIncrease, 2)), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                //tmpDataBotSingle = 0;
                //DataBottom.Add(tmpDataBotSingle);
                //DataTop.Add(Math.Abs(Math.Round(LSIncrease, 2)));

                //var DataAdjustedOP = new List<double>() { 0, 0, 0, Math.Abs(Math.Round(AdjustedOP, 2)), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                //tmpDataBotSingle = tmpDataBotSingle + AdjustedOP;
                //DataBottom.Add(tmpDataBotSingle);
                //DataTop.Add(Math.Abs(Math.Round(AdjustedOP, 2)));

                //var DataRealizedCompetitiveScope = new List<double>() { 0, 0, 0, 0, Math.Abs(Math.Round(realizedCompetitiveScope, 2)), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                //tmpDataBotSingle = tmpDataBotSingle + realizedCompetitiveScope;
                //DataBottom.Add(tmpDataBotSingle);
                //DataTop.Add(Math.Abs(Math.Round(realizedCompetitiveScope, 2)));

                //var DataRealizedSupplyChainTransformation = new List<double>() { 0, 0, 0, 0, 0, Math.Abs(Math.Round(realizedSupplyChainTransformation, 2)), 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                //tmpDataBotSingle = tmpDataBotSingle + realizedSupplyChainTransformation;
                //DataBottom.Add(tmpDataBotSingle);
                //DataTop.Add(Math.Abs(Math.Round(realizedSupplyChainTransformation, 2)));

                //var DataRealizedEfficientExecution = new List<double>() { 0, 0, 0, 0, 0, 0, Math.Abs(Math.Round(realizedEfficientExecution, 2)), 0, 0, 0, 0, 0, 0, 0, 0 };
                //tmpDataBotSingle = tmpDataBotSingle + realizedEfficientExecution;
                //DataBottom.Add(tmpDataBotSingle);
                //DataTop.Add(Math.Abs(Math.Round(realizedEfficientExecution, 2)));

                //var DataRealizedTechnologyandInnovation = new List<double>() { 0, 0, 0, 0, 0, 0, 0, Math.Abs(Math.Round(realizedTechnologyandInnovation, 2)), 0, 0, 0, 0, 0, 0, 0 };
                //tmpDataBotSingle = tmpDataBotSingle + realizedTechnologyandInnovation;
                //DataBottom.Add(LE);
                //DataTop.Add(Math.Abs(Math.Round(realizedTechnologyandInnovation, 2)));

                ////PostOP = 3000.0;
                //var DataPostOP = new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0, Math.Abs(Math.Round(PostOP, 2)), 0, 0, 0, 0, 0, 0 };
                //var bottomPlusPostOP = tmpDataBotSingle + PostOP;
                //tmpDataBotSingle = tmpDataBotSingle + PostOP + Gap;
                //DataBottom.Add(0);
                //DataTop.Add(Math.Abs(Math.Round(PostOP, 2)));

                //var DataLE = new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, Math.Abs(Math.Round(LE, 2)), 0, 0, 0, 0, 0, 0 };
                //tmpDataBotSingle = LE;
                //DataBottom.Add(LE);
                //DataTop.Add(Math.Abs(Math.Round(LE, 2)));

                ////gap
                //Gap = bottomPlusPostOP - LE;
                //var DataGap = new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0, Math.Abs(Math.Round(Gap, 2)), 0, 0, 0, 0, 0, 0 };


                //var DataUnRealizedCompetitiveScope = new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Math.Abs(Math.Round(unrealizedCompetitiveScope, 2)), 0, 0, 0, 0, 0 };
                //tmpDataBotSingle = tmpDataBotSingle + unrealizedCompetitiveScope;
                //DataBottom.Add(tmpDataBotSingle);
                //DataTop.Add(Math.Abs(Math.Round(unrealizedCompetitiveScope, 2)));


                //var DataUnRealizedSupplyChainTransformation = new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Math.Abs(Math.Round(unrealizedSupplyChainTransformation, 2)), 0, 0, 0, 0 };
                //tmpDataBotSingle = tmpDataBotSingle + unrealizedSupplyChainTransformation;
                //DataBottom.Add(tmpDataBotSingle);
                //DataTop.Add(Math.Abs(Math.Round(unrealizedSupplyChainTransformation, 2)));


                //var DataUnRealizedEfficientExecution = new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Math.Abs(Math.Round(unrealizedEfficientExecution, 2)), 0, 0, 0 };
                //tmpDataBotSingle = tmpDataBotSingle + unrealizedEfficientExecution;
                //DataBottom.Add(tmpDataBotSingle);
                //DataTop.Add(Math.Abs(Math.Round(unrealizedEfficientExecution, 2)));

                //var DataUnRealizedTechnologyandInnovation = new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Math.Abs(Math.Round(unrealizedTechnologyandInnovation, 2)), 0,0 };
                //var valueUnRiskUpside = tmpDataBotSingle + unrealizedTechnologyandInnovation;
                //tmpDataBotSingle = 0;//tmpDataBotSingle + unrealizedTechnologyandInnovation;
                //DataBottom.Add(tmpDataBotSingle);
                //DataTop.Add(Math.Abs(Math.Round(unrealizedTechnologyandInnovation, 2)));

                //var DataUnRiskUpside = new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Math.Abs(Math.Round(valueUnRiskUpside, 2)), 0 };
                //tmpDataBotSingle = tmpDataBotSingle + valueUnRiskUpside;
                //DataBottom.Add(tmpDataBotSingle);
                //DataTop.Add(Math.Abs(Math.Round(valueUnRiskUpside, 2)));
                //var DataCostAvoidance = new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, CostAvoidance };
                //DataTop.Add(Math.Abs(Math.Round(CostAvoidance, 2)));


                //var ttUp = new Tooltip() { template = "#= series.name #: #= kendo.format('{0:N2}', value) # ", visible = true };
                //var ttDown = new Tooltip() { template = "#= series.name #: -#= kendo.format('{0:N2}', value) # ", visible = true };
                //res.Add(new WaterfallByRealizedHelper() { color = "transparent", data = DataBottom, name = "Bottom", stack = "yes", tooltip = new Tooltip{visible = false}});
                //res.Add(new WaterfallByRealizedHelper() { color = "#8AAAE0", data = DataOP, name = wb.BaseOP, stack = "yes", tooltip = isNegativeOP ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "red", data = DataLSReduction, name = "LS Reduction", stack = "yes", tooltip = isNegativeLSReduction ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "red", data = DataLSIncrease, name = "LS Increase", stack = "yes", tooltip = isNegativeLSIncrease ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "cyan", data = DataAdjustedOP, name = "Adjusted " + wb.BaseOP, stack = "yes", tooltip = isNegativeAdjustedOP ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "green", data = DataRealizedCompetitiveScope, name = "Competitive Scope", stack = "yes", tooltip = isNegativerealizedCompetitiveScope ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "green", data = DataRealizedSupplyChainTransformation, name = "Supply Chain Transformation", stack = "yes", tooltip = isNegativerealizedSupplyChainTransformation ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "#A0C065", data = DataRealizedEfficientExecution, name = "Efficient Execution", stack = "yes", tooltip = isNegativerealizedEfficientExecution ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "green", data = DataRealizedTechnologyandInnovation, name = "Technology and Innovation", stack = "yes", tooltip = isNegativerealizedTechnologyandInnovation ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "tomato", data = DataPostOP, name = "Post " + wb.BaseOP, stack = "yes", tooltip = isNegativePostOP ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "grey", data = DataGap, name = "Gap", stack = "yes", tooltip = isNegativeGap ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "#A48EC1", data = DataLE, name = "LE", stack = "yes", tooltip = isNegativeLE ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "#C25654", data = DataUnRealizedCompetitiveScope, name = "Unrealized Competitive Scope", stack = "yes", tooltip = isNegativeunrealizedCompetitiveScope ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "#C25654", data = DataUnRealizedSupplyChainTransformation, name = "Unrealized Supply Chain Transformation", stack = "yes", tooltip = isNegativeunrealizedSupplyChainTransformation ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "#C25654", data = DataUnRealizedEfficientExecution, name = "Unrealized Efficient Execution", stack = "yes", tooltip = isNegativeunrealizedEfficientExecution ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "#C25654", data = DataUnRealizedTechnologyandInnovation, name = "Unrealized Technology and Innovation", stack = "yes", tooltip = isNegativeunrealizedTechnologyandInnovation ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "black", data = DataUnRiskUpside, name = "Unrisked Upside", stack = "yes", tooltip = isNegativeUnriskUpside ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "orange", data = DataCostAvoidance, name = "Cost Avoidance", stack = "yes", tooltip = isNegativeCostAvoidance ? ttDown : ttUp });
                //res.Add(new WaterfallByRealizedHelper() { color = "transparent", data = DataTop, name = "Top", stack = "yes", tooltip = new Tooltip { visible = false }, labels = new LabelsWaterfall { visible=true,padding="0",position="insideBase" } });
                #endregion
                var valueUnRiskUpside = unrealizedTechnologyandInnovation;
                var isNegativeCostAvoidance = CostAvoidance < 0;

                var dataReturnWaterfall = new List<DataNewWaterfall>();
                dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(OP, 2), Color = "#8AAAE0", Label = wb.BaseOP });
                if (wb.DayOrCost == "Days")
                {
                    dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(LSReduction, 2), Color = "red", Label = "LS Reduction" });
                    dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(LSIncrease, 2), Color = "red", Label = "LS Increase" });
                }
                dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(AdjustedOP, 2), Color = "cyan", Label = "Adjusted " + wb.BaseOP });
                dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(realizedCompetitiveScope, 2), Color = "green", Label = "Competitive Scope" });
                dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(realizedSupplyChainTransformation, 2), Color = "green", Label = "Supply Chain Transformation" });
                dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(realizedEfficientExecution, 2), Color = "#A0C065", Label = "Efficient Execution" });
                dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(realizedTechnologyandInnovation, 2), Color = "green", Label = "Technology and Innovation" });
                dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(PostOP, 2), Color = "grey", Label = "Post " + wb.BaseOP });
                dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(Gap, 2), Color = "tomato", Label = "Gap" });
                dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(LE, 2), Color = "#A48EC1", Label = "LE" });
                dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(unrealizedCompetitiveScope, 2), Color = "#C25654", Label = "Unrealized Competitive Scope" });
                dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(unrealizedSupplyChainTransformation, 2), Color = "#C25654", Label = "Unrealized Supply Chain Transformation" });
                dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(unrealizedEfficientExecution, 2), Color = "#C25654", Label = "Unrealized Efficient Execution" });
                dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(unrealizedTechnologyandInnovation, 2), Color = "#C25654", Label = "Unrealized Technology and Innovation" });

                var valunriskUp = LE + (unrealizedCompetitiveScope + unrealizedSupplyChainTransformation + unrealizedEfficientExecution + unrealizedTechnologyandInnovation);

                dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(valunriskUp, 2), Color = "black", Label = "Unrisk Upside" });
                dataReturnWaterfall.Add(new DataNewWaterfall() { Value = Math.Round(CostAvoidance, 2), Color = "orange", Label = "Cost Avoidance" });


                var hhg = res.Where(x => x.name != "Bottom" && x.name != "Post " + wb.BaseOP && x.name != "Top").Select(x => x.name).Distinct().ToList();

                var getMax = res.SelectMany(x => x.data).OrderByDescending(x => x).FirstOrDefault();
                var max = getMax * 1.2;


                return new
                {
                    ret = dataReturnWaterfall,
                    categories = hhg,
                    max
                };
            });
        }

        public List<BsonDocument> ParseWaterfallData(WaterfallBase wb, ActivitiesAndPIPs ANP, string OPorLElabel, string OPorLE)
        {
            var series = new List<BsonDocument>();
            series.Add(new { Category = BaseOPTitle(wb.BaseOP), Value = ANP.TotalOP, Summary = "" }.ToBsonDocument());

            var total = 0.0;//("LE".Equals(OPorLElabel) ? ANP.TotalLE : ANP.TotalTQ);
            if (OPorLElabel == "LE")
                total = ANP.TotalLE;
            else if(OPorLElabel == "TQ")
                total = ANP.TotalTQ;
            else if (OPorLElabel == "Target")
                total = ANP.TotalTarget;
            else
                total = ANP.TotalBIC;

            var gap = 0.0;

            var elementsLE = ANP.PIPs.SelectMany(d => d.Elements, (d, e) => new { PIP = d, Elements = e })
                .GroupBy(d =>
                {
                    if (wb.GroupBy.Equals("WellName"))
                        return d.PIP.WellName;

                    if (wb.GroupBy.Equals("RigName"))
                        return d.PIP.RigName;

                    return d.Elements.ToBsonDocument().GetString(wb.GroupBy);
                })
                .Select(d => new
                {
                    Key = d.Key == "" ? "All Others" : d.Key == "BsonNull" ? "All Others" : d.Key,
                    Value = d.Sum(e => GetDayOrCost(wb, e.Elements, OPorLE))
                })
                .Where(d => (!wb.IncludeZero && d.Value != 0) || wb.IncludeZero)
                .OrderByDescending(d => d.Value);

            foreach (var each in elementsLE)
            {
                series.Add(new { Category = each.Key == "BsonNull" ? "All Others" : each.Key, Value = each.Value, Summary = "" }.ToBsonDocument());
                gap += each.Value;
            }

            if (gap + ANP.TotalOP != total)
            {
                series.Add(new { Category = "Gap", Value = (total - (gap + ANP.TotalOP)), Summary = "" }.ToBsonDocument());
            }

            series.Add(new { Category = OPorLElabel, Value = total, Summary = "total" }.ToBsonDocument());

            return series;
        }

        public List<BsonDocument> ParseWaterfallRealizedData(WaterfallBase wb, ActivitiesAndPIPs ANP, string OPorLElabel, string OPorLE)
        {
            var series = new List<BsonDocument>();
            series.Add(new { Category = BaseOPTitle(wb.BaseOP), Value = ANP.TotalOP, Summary = "" }.ToBsonDocument());

            var total = ("LE".Equals(OPorLElabel) ? ANP.TotalLE : ANP.TotalTQ);
            var gap = 0.0;

            var elementsLE = ANP.PIPs
                .Select(d =>
                {
                    if (d.Elements == null)
                        d.Elements = new List<PIPElement>();

                    if (wb.IncludeCR && false)
                    {
                        if (d.CRElements == null)
                            d.CRElements = new List<PIPElement>();

                        d.Elements.AddRange(d.CRElements);
                    }

                    return d;
                })
                .SelectMany(d => d.Elements, (d, e) => new { PIP = d, Elements = e })
                .GroupBy(d =>
                {
                    if (wb.GroupBy.Equals("WellName"))
                    {
                        return new
                        {
                            GroupBy = d.PIP.WellName,
                            Realized = "Realized".Equals(Convert.ToString(d.Elements.Completion))
                        };
                    }

                    if (wb.GroupBy.Equals("RigName"))
                    {
                        return new
                        {
                            GroupBy = d.PIP.RigName,
                            Realized = "Realized".Equals(Convert.ToString(d.Elements.Completion))
                        };
                    }

                    return new
                    {
                        GroupBy = d.Elements.ToBsonDocument().GetString(wb.GroupBy),
                        Realized = "Realized".Equals(Convert.ToString(d.Elements.Completion))
                    };
                })
                .Select(d => new
                {
                    GroupBy = d.Key.GroupBy == "" ? "All Others" : d.Key.GroupBy,
                    Realized = d.Key.Realized,
                    Value = d.Sum(e => GetDayOrCost(wb, e.Elements, OPorLE))
                })
                .Where(d => (!wb.IncludeZero && d.Value != 0) || wb.IncludeZero)
                .OrderByDescending(d => d.Value);

            foreach (var each in elementsLE.Where(d => d.Realized).OrderBy(d => d.Value))
            {
                series.Add(new { Category = each.GroupBy, Value = each.Value, Summary = "", IsRealized = true }.ToBsonDocument());
                gap += each.Value;
            }

            if (gap + ANP.TotalOP != total)
            {
                series.Add(new { Category = "Gap to " + OPorLElabel, Value = (total - (gap + ANP.TotalOP)), Summary = "" }.ToBsonDocument());
            }

            series.Add(new { Category = OPorLElabel, Value = 0, Summary = "runningTotal" }.ToBsonDocument());

            foreach (var each in elementsLE.Where(d => !d.Realized).OrderBy(d => d.Value))
            {
                series.Add(new { Category = each.GroupBy + " ", Value = each.Value, Summary = "", IsRealized = false }.ToBsonDocument());
                gap += each.Value;
            }

            series.Add(new { Category = "Unrisked Upside", Value = 0, Summary = "total" }.ToBsonDocument());

            return series;
        }

        public List<BsonDocument> ParseWaterfallGridData(WaterfallBase wb, ActivitiesAndPIPs ANP)
        {
            
            string DefaultOP = "OP15";
            if (Config.GetConfigValue("BaseOPConfig") != null)
            {
                DefaultOP = Config.GetConfigValue("BaseOPConfig").ToString();
            }

            var activityCategories = ActivityMaster.Populate<ActivityMaster>();

            return ANP.Activities.SelectMany(d => d.Phases, (d, e) => new { Activity = d, Phase = e })
                .Select(d =>
                {
                    var div = (wb.DayOrCost.Equals("Cost") ? 1000000.0 : 1.0);
                    var OP = 0.0;
                    var TQ = 0.0;
                    var LE = 0.0;

                    var baseOpPh = (d.Phase.BaseOP != null && d.Phase.BaseOP.Count() > 0) ? d.Phase.BaseOP.OrderByDescending(x => x).FirstOrDefault() : DefaultOP;

                    //if (wb.BaseOP.ToLower() == DefaultOP.ToLower())
                    if (wb.BaseOP.ToLower().Trim() == baseOpPh.ToLower().Trim())
                    {   
                        //if (d.Phase.BaseOP.Contains(DefaultOP))
                        //{
                            OP = Tools.Div(d.Phase.Plan.ToBsonDocument().GetDouble(wb.DayOrCost), div);
                            //var OPHist = d.Activity.OPHistories.FirstOrDefault(x => x.Type.Equals(wb.BaseOP) && x.ActivityType.Equals(d.Phase.ActivityType) && x.PhaseNo == d.Phase.PhaseNo);
                            //if (OPHist != null)
                            //{
                            //    OP = Tools.Div(OPHist.Plan.ToBsonDocument().GetDouble(wb.DayOrCost), div);
                            //}
                            TQ = Tools.Div(d.Phase.TQ.ToBsonDocument().GetDouble(wb.DayOrCost), div);
                            LE = Tools.Div(d.Phase.LE.ToBsonDocument().GetDouble(wb.DayOrCost), div);
                        //}
                    }
                    else
                    {
                        var OPHist = d.Activity.OPHistories.Where(x => x.Type != null && x.Type.Equals(wb.BaseOP) && x.ActivityType.Equals(d.Phase.ActivityType) && x.PhaseNo == d.Phase.PhaseNo && x.PlanSchedule.Start != Tools.DefaultDate).FirstOrDefault();
                        if (OPHist != null)
                        {
                            if (wb.DayOrCost.ToLower() == "cost")
                            {
                                OP = Tools.Div(OPHist.Plan.Cost, div);
                                var wa = new WellActivity();
                                //var getLE = OPHist.LE.Cost;//0.0;
                                //if (wa.isHaveWeeklyReport2(d.Activity.WellName, d.Activity.UARigSequenceId, d.Phase.ActivityType)) 
                                //    getLE = OPHist.LE.Cost;
                                //LE = Tools.Div(getLE, div);

                                LE = Tools.Div(d.Phase.LE.Cost, div);
                                TQ = Tools.Div(d.Phase.TQ.Cost, div);
                            }
                            else
                            {
                                OP = OPHist.Plan.Days;
                                //var wa = new WellActivity();
                                //var getLE = OPHist.LE.Days; //0.0;
                                //if (wa.isHaveWeeklyReport2(d.Activity.WellName, d.Activity.UARigSequenceId, d.Phase.ActivityType)) getLE = OPHist.LE.Days;
                                //LE = Tools.Div(getLE, div);

                                LE = Tools.Div(d.Phase.LE.Days, div);
                                TQ = Tools.Div(d.Phase.TQ.Days, div);
                            }
                        }
                    }


                    //if (LE == 0) LE = d.Phase.OP.ToBsonDocument().GetDouble(wb.DayOrCost);
                    //if (LE == 0) LE = d.Phase.AFE.ToBsonDocument().GetDouble(wb.DayOrCost);

                    //LE /= div;

                    var LEPIPOpp = d.Phase.PIPs.Sum(f => f.ToBsonDocument().GetDouble(wb.DayOrCost + "CurrentWeekImprovement"));
                    var LEPIPRisk = d.Phase.PIPs.Sum(f => f.ToBsonDocument().GetDouble(wb.DayOrCost + "CurrentWeekRisk"));
                     string actvcateg = "";
                    if(activityCategories.Where(x => x._id.ToString().ToLower().Equals(d.Phase.ActivityType.ToLower().Trim())).Any())
                    {
                        if (activityCategories.Where(x => x._id.ToString().ToLower().Equals(d.Phase.ActivityType.ToLower().Trim())).FirstOrDefault().ActivityCategory != null)
                        {
                            actvcateg = activityCategories.Where(x => x._id.ToString().ToLower().Equals(d.Phase.ActivityType.ToLower().Trim())).FirstOrDefault().ActivityCategory;
                        }
                    }
                

                    return new
                    {
                        WellName = d.Activity.WellName,
                        WorkingInterest = d.Activity.WorkingInterest > 1 ? d.Activity.WorkingInterest / 100 : d.Activity.WorkingInterest,
                        LSStart = d.Phase.PhSchedule.Start,
                        LSFinish = d.Phase.PhSchedule.Finish,
                        ActivityType = d.Phase.ActivityType,
                        RigName = d.Activity.RigName,
                        Type = "CE",
                        LEPIPOpp = LEPIPOpp,
                        LEPIPRisk = LEPIPRisk,
                        Gaps = (OP - LE + (LEPIPOpp + LEPIPRisk)),
                        OP = OP,
                        TQ = TQ,
                        LE = LE,
                        ActivityCategory = actvcateg
                    }.ToBsonDocument();
                })
                //.Where(d =>
                //    d.GetDouble("OP") != 0 ||
                //    d.GetDouble("TQ") != 0 ||
                //    d.GetDouble("LE") != 0 ||
                //    d.GetDouble("LEPIPOpp") != 0 ||
                //    d.GetDouble("LEPIPRisk") != 0 ||
                //    d.GetDouble("Gaps") != 0)
                .OrderBy(d => d.GetString("WellName"))
                .ThenBy(d => d.GetString("ActivityType"))
                .ToList();
        }

        public void ParseCumulativeData(WaterfallBase wb, ActivitiesAndPIPs ANP, out List<BsonDocument> CumulativeGrid, out Dictionary<string, List<Dictionary<string, object>>> CumulativeGridColumns, out List<BsonDocument> CumulativeChart, out List<Int32> AllocationYears)
        {
            var shouldInDate = wb.GetFilterDateRange();
            var columnsRaw = new Dictionary<string, List<BsonDocument>>()
            {
                { "Cost", CreateGridCumulativeFieldsHeader() },
                { "LECost", CreateGridCumulativeFieldsHeader() },
                { "Days", CreateGridCumulativeFieldsHeader() },
                { "LEDays", CreateGridCumulativeFieldsHeader() }
            };

            var isFirstActivity = true;
            var activityCategories = ActivityMaster.Populate<ActivityMaster>();
            var periodFilter = new List<bool>();
            var allRaw = ANP.Activities
                .SelectMany(d => d.Phases, (d, e) => new { d.WellName, d.RigName, Phase = e })
                .SelectMany(d => d.Phase.PIPs, (d, e) => new { d.WellName, d.RigName, d.Phase.ActivityType, Element = e })
                .Where(d =>
                {
                    var isPerformaceUnitFiltered = (wb.PerformanceUnits != null && wb.PerformanceUnits.Count > 0) ? wb.PerformanceUnits.Contains(d.Element.PerformanceUnit) : true;
                    var isPeriodFiltered = wb.FilterPeriod(d.Element.Period);
                    periodFilter.Add(isPeriodFiltered);
                    return isPerformaceUnitFiltered && isPeriodFiltered;
                });

            var years = allRaw.SelectMany(d => d.Element.Allocations).Where(d => d.Period.Year >= 2000).GroupBy(d => d.Period.Year).Select(d => d.Key).OrderBy(d => d).ToList();

            var all = allRaw.Select(d =>
                {
                    if (wb.AllocationYear != 0)
                        d.Element.Allocations = d.Element.Allocations.Where(e =>
                        {
                            var startYear = e.Period >= new DateTime(wb.AllocationYear, 1, 1);
                            var finishYear = e.Period <= new DateTime(wb.AllocationYear, 12, 31);

                            return startYear && finishYear;
                        }).ToList();

                    return d;
                });

            #region grid

            var grid = all.GroupBy(d => new { d.WellName, d.RigName, d.ActivityType })
                .Select(d =>
                {
                    var div = (wb.DayOrCost.Equals("Cost") ? 1000000.0 : 1.0);
                    string actvcateg = "";
                    if(activityCategories.Where(x => x._id.ToString().ToLower().Equals(d.Key.ActivityType.ToLower().Trim())).Any())
                    {
                        if (activityCategories.Where(x => x._id.ToString().ToLower().Equals(d.Key.ActivityType.ToLower().Trim())).FirstOrDefault().ActivityCategory != null)
                        {
                            actvcateg = activityCategories.Where(x => x._id.ToString().ToLower().Equals(d.Key.ActivityType.ToLower().Trim())).FirstOrDefault().ActivityCategory;
                        }
                    }
                    var res = new
                    {
                        WellName = d.Key.WellName,
                        ActivityType = d.Key.ActivityType,
                        RigName = d.Key.RigName,
                        Type = "CE",
                        ActivityCategory = actvcateg
                    }.ToBsonDocument();

                    DateTime dateIterator;
                    DateTime dateMax;

                    if (wb.AllocationYear == 0)
                    {
                        dateIterator = shouldInDate.Start == new DateTime(2000, 1, 1) ? new DateTime(years.Min(), 1, 1) : shouldInDate.Start;
                        dateMax = shouldInDate.Finish == new DateTime(3000, 12, 31) ? new DateTime(years.Max(), 12, 31) : shouldInDate.Finish;
                    }
                    else
                    {
                        dateIterator = Tools.ToUTC(new DateTime(wb.AllocationYear, 1, 1));
                        dateMax = Tools.ToUTC(new DateTime(wb.AllocationYear, 12, 31));
                    }

                    var allocationsRaw = d.Select(e => e.Element).SelectMany(e => e.Allocations);

                    var sumAllCost = 0.0;
                    var sumAllLECost = 0.0;
                    var sumAllDays = 0.0;
                    var sumAllLEDays = 0.0;

                    while (Convert.ToInt32(dateIterator.ToString("yyyyMM")) <= Convert.ToInt32(dateMax.ToString("yyyyMM")))
                    {
                        var allocations = allocationsRaw.Where(e => e.Period != Tools.DefaultDate && e.Period <= dateIterator).DefaultIfEmpty(new PIPAllocation());

                        var cost = allocations.Sum(e => wb.CalculatePipCumulative("Cost", e));
                        var leCost = allocations.Sum(e => wb.CalculatePipCumulative("LECost", e));
                        var days = allocations.Sum(e => wb.CalculatePipCumulative("Days", e));
                        var leDays = allocations.Sum(e => wb.CalculatePipCumulative("LEDays", e));

                        var yearMonth = dateIterator.ToString("MMM_yyyy");

                        var nameOfCost = yearMonth + "_Cost";
                        var nameOfLECost = yearMonth + "_LE_Cost";
                        var nameOfDays = yearMonth + "_Days";
                        var nameOfLEDays = yearMonth + "_LE_Days";

                        res.Set(nameOfCost, cost);
                        res.Set(nameOfLECost, leCost);
                        res.Set(nameOfDays, days);
                        res.Set(nameOfLEDays, leDays);

                        if (isFirstActivity)
                        {
                            columnsRaw["Cost"].Add(CreateGridCumulativeField(nameOfCost, yearMonth));
                            columnsRaw["LECost"].Add(CreateGridCumulativeField(nameOfLECost, yearMonth));
                            columnsRaw["Days"].Add(CreateGridCumulativeField(nameOfDays, yearMonth));
                            columnsRaw["LEDays"].Add(CreateGridCumulativeField(nameOfLEDays, yearMonth));
                        }

                        sumAllCost += cost;
                        sumAllLECost += leCost;
                        sumAllDays += days;
                        sumAllLEDays += leDays;

                        dateIterator = dateIterator.AddMonths(1);
                    }

                    res.Set("ValueFlag", sumAllCost + sumAllLECost + sumAllDays + sumAllLEDays);

                    isFirstActivity = false;

                    return res;
                })
                .Where(d => d.GetDouble("ValueFlag") != 0)
                .OrderBy(d => d.GetString("WellName"))
                .ThenBy(d => d.GetString("ActivityType"))
                .ToList();

            var columnsAll = new Dictionary<string, List<Dictionary<string, object>>>();
            foreach (var each in columnsRaw)
                columnsAll[each.Key] = each.Value.Select(d => d.ToDictionary()).ToList();

            #endregion grid

            #region chart

            var rawFiltered = all.SelectMany(d => d.Element.Allocations);
            var chart = rawFiltered.GroupBy(d => new
            {
                PeriodId = d.Period.ToString("yyyyMM"),
                PeriodName = d.Period.ToString("MMM yy")
            })
            .Select(d =>
            {
                var s = rawFiltered.Where(e => Convert.ToInt32(e.Period.ToString("yyyyMM")) <= Convert.ToInt32(d.Key.PeriodId));
                var r = new
                {
                    PeriodId = d.Key.PeriodId,
                    PeriodName = d.Key.PeriodName,
                    CostImprovement = 0,
                    CostRisk = 0,
                    Cost = 0,
                    DaysImprovement = 0,
                    DaysRisk = 0,
                    Days = 0,
                    LECost = 0,
                    LEDays = 0,
                }.ToBsonDocument();

                try { r.Set("CostImprovement", -s.Sum(e => e.CostPlanImprovement)); }
                catch (Exception e) { }
                try { r.Set("CostRisk", d.Sum(e => e.CostPlanRisk)); }
                catch (Exception e) { }
                try { r.Set("Cost", -s.Sum(e => e.CostPlanImprovement + e.CostPlanRisk)); }
                catch (Exception e) { }
                try { r.Set("DaysImprovement", -s.Sum(e => e.DaysPlanImprovement)); }
                catch (Exception e) { }
                try { r.Set("DaysRisk", d.Sum(e => e.DaysPlanRisk)); }
                catch (Exception e) { }
                try { r.Set("Days", -s.Sum(e => e.DaysPlanImprovement + e.DaysPlanRisk)); }
                catch (Exception e) { }
                try { r.Set("LECost", -s.Sum(e => e.LECost)); }
                catch (Exception e) { }
                try { r.Set("LEDays", -s.Sum(e => e.LEDays)); }
                catch (Exception e) { }

                return r;
            })
            .OrderBy(d => d.GetInt32("PeriodId"))
            .ToList();

            #endregion

            CumulativeGridColumns = columnsAll;
            CumulativeGrid = grid;
            CumulativeChart = chart;
            AllocationYears = years;
        }

        public string BaseOPTitle(string BaseOP)
        {
            var builder = new StringBuilder();
            int count = 0;
            foreach (var c in BaseOP)
            {
                builder.Append(c);
                if ((++count) == 2)
                {
                    builder.Append('-');
                }
            }
            BaseOP = builder.ToString();
            return BaseOP;
        }

        private List<BsonDocument> CreateGridCumulativeFieldsHeader()
        {
            return new BsonDocument[] {
                new { field = "WellName", locked = true, title = "Well", width = 220 }.ToBsonDocument(),
                new { field = "ActivityType", locked = true, title = "Activity", width = 220 }.ToBsonDocument(),
                new { field = "ActivityCategory", locked = true, title = "ActivityCategory", width = 220 }.ToBsonDocument(),
                new { field = "RigName", locked = true, title = "Rig", width = 220 }.ToBsonDocument(),
                new { field = "Type", locked = true, title = "Type", width = 100 }.ToBsonDocument(),
            }.ToList();
        }

        private BsonDocument CreateGridCumulativeField(string field, string yearMonth)
        {
            return new
            {
                field = field,
                title = yearMonth.Replace("_", " "),
                width = 80,
                format = "{0:N1}",
                attributes = new { style = "text-align: right;" }
            }.ToBsonDocument();
        }

        public Double GetDayOrCost(WaterfallBase wb, PIPElement d, string element = "Plan")
        {
            double division = 1;
            double ret = 0;
            switch (element)
            {
                case "Plan":
                    if (wb.DayOrCost.Equals("Days")) ret = d.DaysPlanImprovement + d.DaysPlanRisk;
                    if (wb.DayOrCost.Equals("Cost")) ret = Tools.Div(d.CostPlanImprovement + d.CostPlanRisk, division);
                    break;

                case "Actual":
                    if (wb.DayOrCost.Equals("Days")) ret = d.DaysActualImprovement + d.DaysActualRisk;
                    if (wb.DayOrCost.Equals("Cost")) ret = Tools.Div(d.CostActualImprovement + d.CostActualRisk, division);
                    break;

                case "LE":
                    if (wb.DayOrCost.Equals("Days")) ret = d.DaysCurrentWeekImprovement + d.DaysCurrentWeekRisk;
                    if (wb.DayOrCost.Equals("Cost")) ret = Tools.Div(d.CostCurrentWeekImprovement + d.CostCurrentWeekRisk, division);
                    break;
            }
            return ret;
        }
    }

    public class ActivityAndPhase
    {
        public WellActivity Activity { set; get; }
        public WellActivityPhase Phase { set; get; }
    }

    public class Tooltip
    {
        public string template { get; set; }
        public bool visible { get; set; }
    }
    public class LabelsWaterfall
    {
        public string padding { get; set; }
        public string position { get; set; }
        public bool visible { get; set; }
    }

    public class WaterfallByRealizedHelper
    {
        public string color { get; set; }
        public List<double> data { get; set; }
        public string name { get; set; }
        public string stack { get; set; }
        public Tooltip tooltip { get; set; }
        public LabelsWaterfall labels { get; set; }
        public WaterfallByRealizedHelper()
        {
            tooltip = new Tooltip();
            labels = new LabelsWaterfall() { visible = false };
        }
    }

    public class DataNewWaterfall
    {
        public string Color { get; set; }
        public string Label { get; set; }
        public double Value { get; set; }
    }
}