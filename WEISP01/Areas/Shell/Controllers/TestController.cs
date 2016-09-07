using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ECIS.Core;
using ECIS.Core.DataSerializer;
using ECIS.Client.WEIS;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using ECIS.Identity;
using System.Text.RegularExpressions;
using ECIS.AppServer.Areas.WebMenu.Models;
using MongoDB.Bson.Serialization;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    //public class MonthlyEventsBasedOnMonth
    //{
    //    public string _id { set; get; }
    //    public string WellName { set; get; }
    //    public string UARigSequenceId { set; get; }
    //    public string RigName { set; get; }
    //    public string AssetName { set; get; }
    //    public string ActivityType { set; get; }
    //    public DateRange PhSchedule { set; get; }
    //    public DateRange AFESchedule { set; get; }
    //}

    public class MasterRename : ECISModel
    {
        public override string TableName
        {
            get { return "WEISMasterRenames"; }
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            this._id = string.Format("{0}-{1}-{2}", this.TableID, this.OldValue.Replace(" ", ""), this.NewValue.Replace(" ", ""));
            return this.ToBsonDocument();
        }

        public string TableID { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }

    public class WeeklyReportCompare : ECISModel
    {
        public override string TableName
        {
            get { return "_WeeklyReportCompare"; }
        }
        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            this._id = string.Format("W{0}S{1}R{2}A{3}", this.WellName.Replace(" ", ""),
                this.RigName.Replace(" ", ""),
                this.ActivityType.Replace(" ", ""),
            this.SequenceID.Replace(" ", ""));
            return this.ToBsonDocument();
        }
        public string WellName { get; set; }
        public string RigName { get; set; }
        public string ActivityType { get; set; }
        public string SequenceID { get; set; }
        public DateTime UpdateVersion { get; set; }
        public DateTime LastMonth { get; set; }
        public DateTime LastWeek { get; set; }
        public bool isHaveWR { get; set; }
        public DateRange LSSchedule { get; set; }

        public WellDrillData LEinMLE { get; set; }
        public WellDrillData LEinBrow { get; set; }
        public WellDrillData LEinWLE { get; set; }



        public WeeklyReportCompare()
        {
            LEinMLE = new WellDrillData();
            LEinBrow = new WellDrillData();
            LEinWLE = new WellDrillData();

            LSSchedule = new DateRange();
        }
    }

    public class EmailModel
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool TLS { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string SendEmail()
        {
            String ret = "OK";
            try
            {
                ResultInfo ri = Tools.SendMail(From, Subject, Message, Host, Port, UserName, Password, TLS, new string[] { To });
                if (ri.Result != "OK") ret = ri.Message;
            }
            catch (Exception e)
            {
                ret = Tools.PushException(e);
            }
            Status = ret;
            return ret;
        }
    }

    public class TestController : Controller
    {
        // GET: Shell/Test
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GenerateDateLS()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var latest = DataHelper.Get("LogLatestLS", null, SortBy.Descending("Executed_At"));
                WellActivity.SetLastDateUploadedLS(latest.GetDateTime("Executed_At"), latest.GetDateTime("Executed_At"), "eaciit");
                return null;
            });
        }
        public JsonResult TrimAllActivityTypes()
        {
            return MvcResultInfo.Execute(() =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    if (wa.Phases.Any())
                    {
                        foreach (var ph in wa.Phases)
                        {
                            ph.ActivityType = ph.ActivityType.Trim();
                        }
                    }
                    DataHelper.Save(wa.TableName, wa.ToBsonDocument());
                }

                var bas = BizPlanActivity.Populate<BizPlanActivity>();
                foreach (var wa in bas)
                {
                    if (wa.Phases.Any())
                    {
                        foreach (var ph in wa.Phases)
                        {
                            ph.ActivityType = ph.ActivityType.Trim();
                        }
                    }
                    DataHelper.Save(wa.TableName, wa.ToBsonDocument());
                }
                return "ok";
            });
        }

        public JsonResult RefreshLEValueDataBrowser(int isUpdate = 0)
        {
            return MvcResultInfo.Execute(null, document =>
            {

                List<WeeklyReportCompare> datasout = new List<WeeklyReportCompare>();

                var yy = DataHelper.Get<WellActivityUpdateMonthly>("WEISWellActivityUpdatesMonthly", null, sort: SortBy.Descending("UpdateVersion"));

                var ys = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(
                   Query.EQ("UpdateVersion", Tools.ToUTC(yy.UpdateVersion))
               );


                var COmmnetsFilled = ys;

                foreach (var t in COmmnetsFilled)
                {

                    var com = WEISComment.Populate<WEISComment>(
                        Query.EQ("Reference2", t._id.ToString()
                        ));

                    if (com != null && com.Count() > 0)
                    {
                        var wa = WellActivity.Get<WellActivity>(
                            Query.And(
                            Query.EQ("WellName", t.WellName),
                            Query.EQ("UARigSequenceId", t.SequenceId),
                            Query.EQ("Phases.ActivityType", t.Phase.ActivityType)
                            )
                            );
                        if (wa != null)
                        {

                            if (!WellActivity.isHaveWeeklyReport(wa.WellName, wa.UARigSequenceId, t.Phase.ActivityType))
                            {
                                var ph = wa.Phases.FirstOrDefault(x => x.ActivityType.Equals(t.Phase.ActivityType));
                                if (ph != null)
                                {
                                    if (ph.LastMonth == t.UpdateVersion)
                                    {
                                        if (ph.LE.Cost != t.CurrentWeek.Cost || ph.LE.Days != t.CurrentWeek.Days)
                                        {
                                            WeeklyReportCompare cx = new WeeklyReportCompare();
                                            cx.WellName = wa.WellName;
                                            cx.RigName = wa.RigName;
                                            cx.SequenceID = wa.UARigSequenceId;
                                            cx.ActivityType = ph.ActivityType;
                                            cx.LEinMLE = t.CurrentWeek;
                                            cx.LEinBrow = ph.LE;
                                            cx.UpdateVersion = t.UpdateVersion;
                                            cx.LastMonth = ph.LastMonth;
                                            cx.isHaveWR = false;

                                            cx.LSSchedule = ph.PhSchedule;

                                            if (ph.PhSchedule.Start != Tools.DefaultDate)
                                            {
                                                cx.Save();
                                                datasout.Add(cx);
                                            }
                                            if (isUpdate != 0 && ph.PhSchedule.Start != Tools.DefaultDate)
                                            {
                                                ph.LE = cx.LEinMLE;
                                                DataHelper.Save("WEISWellActivities", wa.ToBsonDocument());
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var ph = wa.Phases.FirstOrDefault(x => x.ActivityType.Equals(t.Phase.ActivityType));

                                var wau = DataHelper.Get<WellActivityUpdate>("WEISWellActivityUpdates",
                                    q: Query.And(
                                Query.EQ("WellName", t.WellName),
                                Query.EQ("SequenceId", t.SequenceId),
                                Query.EQ("Phase.ActivityType", t.Phase.ActivityType)
                                ), sort: SortBy.Descending("UpdateVersion")
                                );

                                if (ph != null)
                                {
                                    if (ph.LastWeek == wau.UpdateVersion)
                                    {
                                        if (ph.LE.Cost != wau.CurrentWeek.Cost || ph.LE.Days != wau.CurrentWeek.Days)
                                        {
                                            WeeklyReportCompare cx = new WeeklyReportCompare();
                                            cx.WellName = wa.WellName;
                                            cx.RigName = wa.RigName;
                                            cx.SequenceID = wa.UARigSequenceId;
                                            cx.ActivityType = ph.ActivityType;
                                            cx.LEinMLE = t.CurrentWeek;
                                            cx.LEinWLE = wau.CurrentWeek;
                                            cx.LEinBrow = ph.LE;
                                            cx.UpdateVersion = t.UpdateVersion;
                                            cx.LastMonth = ph.LastMonth;
                                            cx.LastWeek = wau.UpdateVersion;
                                            cx.isHaveWR = true;
                                            cx.LSSchedule = ph.PhSchedule;

                                            if (ph.PhSchedule.Start != Tools.DefaultDate)
                                            {
                                                cx.Save();
                                                datasout.Add(cx);
                                            }


                                            if (isUpdate != 0 && ph.PhSchedule.Start != Tools.DefaultDate)
                                            {
                                                ph.LE = cx.LEinWLE;
                                                DataHelper.Save("WEISWellActivities", wa.ToBsonDocument());
                                            }
                                        }
                                    }
                                }
                            }


                        }


                    }
                }

                return datasout;


            });
        }

        public JsonResult GenerateSTOSData()
        {
            return MvcResultInfo.Execute(null, document =>
            {

                var tx = DataHelper.Populate("LogLatestLS");
                var yyy = WellActivity.Populate<WellActivity>();//Query.And(Query.EQ("WellName", "APPO AC006"), Query.EQ("UARigSequenceId", "UA176")));

                var grp = tx.GroupBy(x => x.GetString("Rig_Name"), x => x.GetString("Well_Name"));
                foreach (IGrouping<string, string> rigs in grp)
                {

                    var arr = rigs.ToArray();
                    foreach (string wellname in arr)
                    {
                        var dr = new DateRange(new DateTime(2015, 1, 1), new DateTime(2040, 12, 31));

                        STOSResult px = new STOSResult();
                        var data = yyy.Where(x => x.RigName.Equals(rigs.Key) && x.WellName.Equals(wellname.ToString())).ToList();
                        var gogo = px.Transforms(data, "eaciit");
                        foreach (var t in gogo)
                        {
                            t.Save();
                        }
                    }
                }

                return "OK";


            });
        }




        public string buildStringIn(string initial, List<string> str)
        {
            // 'WellName' : { '$in' : ['GREAT WHITE GA022']   }
            string first = "'" + initial + "'";
            string middle = " : { '$in' : [";

            List<string> strs = new List<string>();
            foreach (var t in str)
                strs.Add("'" + t + "'");
            string inside = String.Join(",", strs.ToList());
            string outer = "]   }";
            return first + middle + inside + outer;
        }


        public JsonResult TestServerPagingFullQuery()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                List<string> allStrInFilter = new List<string>();

                List<string> wellNames = new List<string>();
                wellNames.Add("GREAT WHITE GA022");
                wellNames.Add("COUGAR A28");
                string matchQs = "";// buildStringIn("WellName", wellNames);
                if (!string.IsNullOrEmpty(matchQs))
                    allStrInFilter.Add(matchQs);

                List<string> rigNames = new List<string>();
                rigNames.Add("Cougar");
                var rigName = "";// buildStringIn("RigName", rigNames);
                if (!string.IsNullOrEmpty(rigName))
                    allStrInFilter.Add(rigName);

                string quryMatches = "";
                if (allStrInFilter != null && allStrInFilter.Count() > 0)
                {
                    quryMatches = string.Join(",", allStrInFilter);
                }

                string aggUW = @"{$unwind:'$Phases'}";
                string aggMatch = "";

                if (!string.IsNullOrEmpty(quryMatches))
                    aggMatch = @"{ $match:{ " + quryMatches + " }  }";

                string aggProject = @"{$project: {
                                        '_id' : '$_id',
	                                    'WellName' : '$WellName',
	                                    'RigName' : '$RigName',
	                                    'Estimate' : '$Phases.Estimate',
	                                    'OPHistories' : '$OPHistories',
                                        }   
                                    }";
                string aggGroup = @"{ $group: { 
            
                            '_id' : '1',
	                        'total' : {'$sum' : 1} 
                            }}";



                List<BsonDocument> pipelines = new List<BsonDocument>();
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggUW));
                if (!string.IsNullOrEmpty(aggMatch))
                    pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggMatch));
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggProject));
                //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggGroup));
                //pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(limit));
                List<BsonDocument> aggregate = DataHelper.Aggregate(new BizPlanActivity().TableName, pipelines);
                return new
                {
                    //Count = aggregate.Count(),
                    Data = DataHelper.ToDictionaryArray(aggregate)// aggregate.ToDictionary
                };
            });
        }

        public JsonResult CheckRigRatesContainSlashOrBackSlash()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var res = new List<RigRatesNew>();
                var data = RigRatesNew.Populate<RigRatesNew>();
                foreach (var x in data)
                {
                    if (!String.IsNullOrEmpty(x.Title))
                    {
                        if (x.Title.Contains("\n") || x.Title.Contains("\r"))
                        {
                            res.Add(x);
                        }
                    }
                }

                return new
                {
                    Count = res.Count(),
                    Data = res
                };
            });
        }
        public void SavePlanMethod(BizPlanActivity data, string updatedby)
        {
            try
            {

                //set selectedCurrency di estimate sesuai dengan currency yg di select di UI
                data.Phases.FirstOrDefault().Estimate.SelectedCurrency = data.Currency;

                //convert dulu semua cost ke USD
                var Currencyx = data.Currency;
                if (!Currencyx.Trim().ToUpper().Equals("USD"))
                {
                    var datas = MacroEconomic.Populate<MacroEconomic>(Query.EQ("Currency", Currencyx));
                    if (datas.Any())
                    {
                        var cx = datas.Where(x => x.Currency.Equals(Currencyx)).FirstOrDefault();

                        var rate = cx.ExchangeRate.AnnualValues.Where(x => x.Year == data.Phases.FirstOrDefault().Estimate.EstimatePeriod.Start.Year).Any() ?
                                cx.ExchangeRate.AnnualValues.Where(x => x.Year == data.Phases.FirstOrDefault().Estimate.EstimatePeriod.Start.Year).FirstOrDefault().Value
                                : 0;

                        // dibagi rate : untuk jadi USD
                        data.Currency = "USD";
                        data.Phases.FirstOrDefault().Estimate.Currency = "USD";
                        data.Phases.FirstOrDefault().Estimate.Services = Tools.Div(data.Phases.FirstOrDefault().Estimate.Services, rate);
                        data.Phases.FirstOrDefault().Estimate.Materials = Tools.Div(data.Phases.FirstOrDefault().Estimate.Materials, rate);

                        data.Phases.FirstOrDefault().Estimate.SpreadRate = Tools.Div(data.Phases.FirstOrDefault().Estimate.SpreadRate, rate);

                        data.Phases.FirstOrDefault().Estimate.SpreadRateTotal = Tools.Div(data.Phases.FirstOrDefault().Estimate.SpreadRateTotal, rate);

                        data.Phases.FirstOrDefault().Estimate.BurnRate = Tools.Div(data.Phases.FirstOrDefault().Estimate.BurnRate, rate);

                        data.Phases.FirstOrDefault().Estimate.RigRate = Tools.Div(data.Phases.FirstOrDefault().Estimate.RigRate, rate);

                        data.Phases.FirstOrDefault().Estimate.ShellShareCalc = Tools.Div(data.Phases.FirstOrDefault().Estimate.ShellShareCalc, rate);
                        data.Phases.FirstOrDefault().Estimate.MeanCostMOD = Tools.Div(data.Phases.FirstOrDefault().Estimate.MeanCostMOD, rate);

                        data.Phases.FirstOrDefault().Estimate.TroubleFreeBeforeLC.Cost = Tools.Div(data.Phases.FirstOrDefault().Estimate.TroubleFreeBeforeLC.Cost, rate);
                        data.Phases.FirstOrDefault().Estimate.NewTroubleFree.Cost = Tools.Div(data.Phases.FirstOrDefault().Estimate.NewTroubleFree.Cost, rate);
                        data.Phases.FirstOrDefault().Estimate.NewNPTTime.Cost = Tools.Div(data.Phases.FirstOrDefault().Estimate.NewNPTTime.Cost, rate);
                        data.Phases.FirstOrDefault().Estimate.NewMean.Cost = Tools.Div(data.Phases.FirstOrDefault().Estimate.NewMean.Cost, rate);
                        data.Phases.FirstOrDefault().Estimate.NewTECOPTime.Cost = Tools.Div(data.Phases.FirstOrDefault().Estimate.NewTECOPTime.Cost, rate);
                        data.Phases.FirstOrDefault().Estimate.isFirstInitiate = false;

                    }
                }

                data.Phases.FirstOrDefault().Estimate.ServicesUSD = data.Phases.FirstOrDefault().Estimate.Services;
                data.Phases.FirstOrDefault().Estimate.MaterialsUSD = data.Phases.FirstOrDefault().Estimate.Materials;
                data.Phases.FirstOrDefault().Estimate.SpreadRateUSD = data.Phases.FirstOrDefault().Estimate.SpreadRate;
                data.Phases.FirstOrDefault().Estimate.SpreadRateTotalUSD = data.Phases.FirstOrDefault().Estimate.SpreadRateTotal;
                data.Phases.FirstOrDefault().Estimate.RigRatesUSD = data.Phases.FirstOrDefault().Estimate.RigRate;
                data.Phases.FirstOrDefault().Estimate.BurnRateUSD = data.Phases.FirstOrDefault().Estimate.BurnRate;

                //flag this phase as PushToWellPlan
                data.Phases.FirstOrDefault().PushToWellPlan = true;

                //change the plan value for this Phase
                //data.Phases.FirstOrDefault().PlanSchedule = data.Phases.FirstOrDefault().Estimate.EstimatePeriod;
                //data.Phases.FirstOrDefault().Plan.Cost = data.Phases.FirstOrDefault().Estimate.MeanCostMOD;
                //data.Phases.FirstOrDefault().Plan.Days = data.Phases.FirstOrDefault().Estimate.EstimatePeriod.Days;

                var busplan = BizPlanActivity.Get<BizPlanActivity>(data._id);
                var OtherPhases = busplan.Phases;

                data.isFirstInitiate = false;
                var cfg = DataHelper.Get("SharedConfigTable", Query.EQ("_id", "BizPlanConfig")).GetBool("ConfigValue");
                //var isUpdateToWellPlan = cfg.ToBsonDocument().GetBool("ConfigValue");

                data.WorkingInterest = data.ShellShare;
                var newPhases = new List<BizPlanActivityPhase>();
                if (OtherPhases != null && OtherPhases.Count > 0)
                {
                    var x = data.Phases.FirstOrDefault();
                    foreach (var d in OtherPhases)
                    {
                        if (d.PhaseNo == x.PhaseNo)
                        {
                            x.LevelOfEstimate = x.Estimate.MaturityLevel;
                            x.Estimate.EventStartDate = Tools.ToUTC(x.Estimate.EstimatePeriod.Start);
                            x.Estimate.EventEndDate = Tools.ToUTC(x.Estimate.EstimatePeriod.Finish);
                            x.Estimate.EstimatePeriod = new DateRange() { Start = Tools.ToUTC(x.Estimate.EventStartDate), Finish = Tools.ToUTC(x.Estimate.EventEndDate) };
                            x.Estimate.isFirstInitiate = false;
                            newPhases.Add(x);
                        }
                        else
                        {
                            newPhases.Add(d);
                        }
                    }
                }

                data.Phases = newPhases;
                data.DataInputBy = updatedby;

                string strMsg = string.Format("WellName : {0}, SequenceID : {1}, Activity Type : {2}, ", data.WellName, data.UARigSequenceId, data.Phases.FirstOrDefault().ActivityType);
                //data.Phases.FirstOrDefault().Estimate.Status = status;

                var status = data.Phases.FirstOrDefault().Estimate.Status;

                if (status.ToLower().Equals("complete") || status.ToLower().Equals("modified"))
                {
                    if (cfg)
                    {
                        // check before update well Plan
                        if (WellActivity.isHaveWeeklyReport(data.WellName, data.UARigSequenceId, data.Phases.FirstOrDefault().ActivityType))
                        {
                            data.Save();
                            BizPlanAllocation.SaveBizPlanAllocation(data);
                            // return new SavePlanResult() { Success = false, Message = "Business Plan succesfully saved but not pushed to WellPlan because it has active Weekly Report" };
                            //Json(new { Success = false, Messages = "Business Plan succesfully saved but not pushed to WellPlan because it has active Weekly Report" }, JsonRequestBehavior.AllowGet);
                        }
                        if (WellActivity.isHaveMonthlyLEReport(data.WellName, data.UARigSequenceId, data.Phases.FirstOrDefault().ActivityType))
                        {
                            data.Save(references: new string[] { "updatetowellplan" });
                            BizPlanAllocation.SaveBizPlanAllocation(data);
                            //return new SavePlanResult() { Success = false, Message = "Business Plan succesfully saved also Well Activity has been updated" };
                            //return Json(new { Success = false, Messages = "Business Plan succesfully saved also Well Activity has been updated" }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            data.Save(references: new string[] { "updatetowellplan" });
                            BizPlanAllocation.SaveBizPlanAllocation(data);
                            // decopling
                            //return new SavePlanResult() { Success = false, Message = "Well Activity has been updated! \n Status Changed to :" + status + "\n" + strMsg };
                            //return Json(new { Success = true, Messages = " Well Activity has been updated! \n Status Changed to :" + status + "\n" + strMsg }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    else
                    {
                        data.Save();
                        BizPlanAllocation.SaveBizPlanAllocation(data);
                        //return new SavePlanResult() { Success = false, Message = "Business Plan Data has been Updated! \n Status Changed to : " + status + "\n" + strMsg };
                        //return Json(new { Success = true, Messages = " Business Plan Data has been Updated! \n Status Changed to : " + status + "\n" + strMsg }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    data.Save();
                    BizPlanAllocation.SaveBizPlanAllocation(data);
                    //return new SavePlanResult() { Success = false, Message = "Business Plan Data has been Updated! Status Changed to : " + status + "\n" + strMsg };
                    //return Json(new { Success = true, Messages = "Business Plan Data has been Updated! Status Changed to : " + status + "\n" + strMsg }, JsonRequestBehavior.AllowGet);
                }

            }
            catch (Exception e)
            {
                //return new SavePlanResult() { Success = false, Message = e.Message };
                //return Json(new { Success = false, Messages = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public void SavePlan(BizPlanActivity data, string status = "Draft")
        {

            if (data.Phases.Count() == 1)
            {
                SavePlanMethod(data, WebTools.LoginUser.UserName);
            }
            else if (data.Phases.Count() > 1)
            {
                int idx = data.Phases.Count() - 1;

                for (int i = 0; i <= idx; i++)
                {
                    var actv = data.Phases[i].ActivityType;
                    data.Phases.RemoveAll(chunk => chunk.ActivityType != actv);
                    SavePlanMethod(data, WebTools.LoginUser.UserName);

                    data = BizPlanActivity.Get<BizPlanActivity>(Query.And(Query.EQ("WellName", data.WellName),
                                Query.EQ("UARigSequenceId", data.UARigSequenceId),
                                Query.EQ("RigName", data.RigName),
                                Query.EQ("Phases.ActivityType", actv)
                                ));
                }

            }


            //SavePlanMethod(data, WebTools.LoginUser.UserName);
            //return Json(new { Success = saveProcess.Success, Messages = saveProcess.Message }, JsonRequestBehavior.AllowGet);
        }

        public List<LatestLSUploaded> GetLatestLSPhases(DateTime latestlsdate)
        {
            List<LatestLSUploaded> result = new List<LatestLSUploaded>();
            var logLast = DataHelper.Populate("LogLatestLS", Query.And(
              Query.EQ("Executed_At", Tools.ToUTC(latestlsdate))));

            foreach (var t in logLast)
            {

                DateTime start = new DateTime();
                DateTime finish = new DateTime();
                try
                {
                    start = DateTime.ParseExact(t.GetString("Start_Date"), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (Exception ec)
                {
                    start = Tools.DefaultDate;
                }
                try
                {
                    finish = DateTime.ParseExact(t.GetString("End_Date"), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (Exception ec)
                {
                    finish = Tools.DefaultDate;
                }


                result.Add(new LatestLSUploaded
                {
                    Activity_Type = t.GetString("Activity_Type"),
                    Rig_Name = t.GetString("Rig_Name"),
                    Well_Name = t.GetString("Well_Name"),
                    LS = new DateRange(start, finish)

                });
            }
            return result;
        }

        public DateTime GetLastDateUploadedLS()
        {
            var latest1 = DataHelper.Get("LogLatestLS", null, SortBy.Descending("Executed_At"));

            DateTime dd = latest1.GetDateTime("Executed_At");

            return dd;
        }


        public JsonResult ResyncLSinBizplanFollowingLastLSWellPlan()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                ResyncLSinBizplan();
                return "OK";
            });
        }

        public void ResyncLSinBizplan()
        {
            UploadLSController lscon = new UploadLSController();
            lscon.AddLatestUploadedLSinBizPlan();
        }


        public JsonResult RemoveWEATHER()
        {
            return MvcResultInfo.Execute(null, document =>
            {

                var phsweather = WellActivity.Populate<WellActivity>(Query.EQ("Phases.ActivityType", "WEATHER"));

                foreach (var ph in phsweather)
                {
                    var phnom = ph.Phases.Where(x => x.ActivityType.Equals("WEATHER")).Select(x => x.PhaseNo);


                    foreach (var pnu in phnom)
                    {
                        // delete on monthly LE
                        var q = Query.And(
                            Query.EQ("WellName", ph.WellName),
                            Query.EQ("SequenceId", ph.UARigSequenceId),
                            Query.EQ("Phase.ActivityType", "WEATHER"),
                            Query.EQ("Phase.PhaseNo", pnu)
                            );
                        var availbaleweathermon = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(q);
                        if (availbaleweathermon != null)
                        {
                            WellPlanWeather bckp = new WellPlanWeather();
                            bckp.OriginalId = availbaleweathermon._id.ToString();
                            bckp.Phase = availbaleweathermon.Phase;
                            bckp.WellName = availbaleweathermon.WellName;
                            bckp.SourceFrom = "WellActivityUpdateMonthly";
                            bckp.RigName = availbaleweathermon.RigName;
                            bckp.SequenceId = availbaleweathermon.SequenceId;
                            bckp.Desc = "Linked With WellActivity";
                            bckp.Save();
                            availbaleweathermon.Delete();

                        }

                        // delete bizplan
                        var qi = Query.And(
                               Query.EQ("WellName", ph.WellName),
                               Query.EQ("UARigSequenceId", ph.UARigSequenceId),
                               Query.EQ("Phases.ActivityType", "WEATHER"),
                               Query.EQ("Phases.PhaseNo", pnu)
                               );
                        var availbaleweatherbiz = BizPlanActivity.Get<BizPlanActivity>(q);
                        if (availbaleweatherbiz != null)
                        {
                            var biznumph = availbaleweatherbiz.Phases.Where(x => x.ActivityType.Equals("WEATHER")).Select(x => x.PhaseNo);
                            foreach (var bizremo in availbaleweatherbiz.Phases.Where(x => x.ActivityType.Equals("WEATHER")))
                            {
                                WellPlanWeather bckp = new WellPlanWeather();
                                bckp.OriginalId = availbaleweatherbiz._id.ToString();
                                bckp.BizPhase = bizremo;
                                bckp.WellName = availbaleweatherbiz.WellName;
                                bckp.SourceFrom = "BizPlanActivity";
                                bckp.RigName = availbaleweatherbiz.RigName;
                                bckp.SequenceId = availbaleweatherbiz.UARigSequenceId;
                                bckp.Desc = "Linked With WellActivity";
                                bckp.Save();
                            }
                            availbaleweatherbiz.Phases.RemoveAll(x => x.ActivityType.Equals("WEATHER"));
                            availbaleweatherbiz.Save();
                        }
                    }


                    WellPlanWeather weather = new WellPlanWeather();
                    weather.OriginalId = ph._id.ToString();
                    weather.Phase = ph.Phases.FirstOrDefault();
                    weather.WellName = ph.WellName;
                    weather.SourceFrom = "WellActivity";
                    weather.RigName = ph.RigName;
                    weather.SequenceId = ph.UARigSequenceId;
                    weather.Desc = "";
                    weather.Save();


                    if (phnom.Count() <= 0)
                    {
                        // delete WELL activities
                        ph.Delete();
                    }
                    else
                    {
                        // delete respective event
                        ph.Phases.RemoveAll(x => x.ActivityType.Equals("WEATHER"));
                        ph.Save();
                    }

                }

                // monthly remove 

                var monthlyWeathes = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.EQ("Phase.ActivityType", "WEATHER"));
                foreach (var min in monthlyWeathes)
                {

                    WellPlanWeather weather = new WellPlanWeather();
                    weather.OriginalId = min._id.ToString();
                    weather.Phase = min.Phase;
                    weather.WellName = min.WellName;
                    weather.SourceFrom = "WellActivityUpdateMonthly";
                    weather.RigName = min.RigName;
                    weather.SequenceId = min.SequenceId;
                    weather.Desc = "";
                    weather.Save();


                    min.Delete();
                }

                // bizplan remove
                var bizplanWeathers = BizPlanActivity.Populate<BizPlanActivity>(Query.EQ("Phases.ActivityType", "WEATHER"));

                foreach (var ph in bizplanWeathers)
                {
                    var phnom = ph.Phases.Where(x => x.ActivityType.Equals("WEATHER")).Select(x => x.PhaseNo);




                    if (ph.Phases.Count() <= 0)
                    {

                        WellPlanWeather weather = new WellPlanWeather();
                        weather.OriginalId = ph._id.ToString();
                        weather.BizPhase = ph.Phases.FirstOrDefault();
                        weather.WellName = ph.WellName;
                        weather.SourceFrom = "BizPlanActivity";
                        weather.RigName = ph.RigName;
                        weather.SequenceId = ph.UARigSequenceId;
                        weather.Desc = "";
                        weather.Save();

                        // delete biz activities
                        ph.Delete();
                    }
                    else
                    {

                        foreach (var who in ph.Phases.Where(x => x.ActivityType.Equals("WEATHER")))
                        {
                            WellPlanWeather weather = new WellPlanWeather();
                            weather.OriginalId = ph._id.ToString();
                            weather.BizPhase = who;
                            weather.WellName = ph.WellName;
                            weather.SourceFrom = "BizPlanActivity";
                            weather.RigName = ph.RigName;
                            weather.SequenceId = ph.UARigSequenceId;
                            weather.Desc = "";
                            weather.Save();
                        }

                        // delete respective event
                        ph.Phases.RemoveAll(x => x.ActivityType.Equals("WEATHER"));
                        ph.Save();
                    }

                }


                return "Process Done - Weather Activities stored to : _WEISWellActivityWeathers";
            });
        }

        public JsonResult AddLatestUploadedLSinBizPlan()
        {
            return MvcResultInfo.Execute(null, document =>
            {

                var lastDateExecutedLS = GetLastDateUploadedLS();
                var latestPhases = GetLatestLSPhases(lastDateExecutedLS);

                var aa = BizPlanActivity.Populate<BizPlanActivity>();
                foreach (var a in aa)
                {
                    if (a.WellName.Equals("DAWN MARIE APPR"))
                    {

                    }

                    foreach (var ph in a.Phases)
                    {

                        var ttt = latestPhases.Where(x => x.Well_Name.Equals(a.WellName) && x.Activity_Type.Equals(ph.ActivityType) && x.Rig_Name.Equals(ph.Estimate.RigName));

                        if (ttt.Count() > 0)
                        {
                            ph.isLatestLS = true;
                            ph.LatestLSDate = lastDateExecutedLS;
                        }
                        else
                        {
                            ph.isLatestLS = false;
                            ph.LatestLSDate = Tools.DefaultDate;
                        }
                        if (ph.Estimate.Status == null)
                        {
                            ph.Estimate.Status = "Draft";
                        }
                        if (ph.Estimate.Status.Equals(""))
                            ph.Estimate.Status = "Draft";
                    }
                    DataHelper.Save("WEISBizPlanActivities", a.ToBsonDocument());
                }

                return "OK";
            });
        }

        public JsonResult ResyncBizPlan()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var aa = BizPlanActivity.Populate<BizPlanActivity>();
                foreach (var a in aa)
                {
                    SavePlan(a);
                }

                return "OK";
            });
        }

        public JsonResult RebindBaseOPinWeeklyArchieve()
        {
            return MvcResultInfo.Execute(null, document =>
            {

                var aa = WellActivityUpdate.Populate<WellActivityUpdate>();
                foreach (var a in aa.GroupBy(x => new { x.WellName, x.SequenceId, x.Phase.ActivityType }))
                {
                    var wellname = a.Key.WellName;
                    var seqId = a.Key.SequenceId;
                    var actv = a.Key.ActivityType;

                    var data = a.ToList();

                    var wa = WellActivity.Get<WellActivity>(Query.And(Query.EQ("WellName", wellname), Query.EQ("UARigSequenceId", seqId), Query.EQ("Phases.ActivityType", actv)));

                    List<BsonDocument> bdocs = new List<BsonDocument>();

                    if (wa != null)
                    {
                        foreach (var d in data)
                        {
                            var bop = wa.Phases.FirstOrDefault(x => x.ActivityType.Equals(actv)).BaseOP;
                            if (bop != null)
                                d.Phase.BaseOP = bop;

                            bdocs.Add(d.ToBsonDocument());
                        }

                        DataHelper.Save("WEISWellActivityUpdates", bdocs);
                    }
                }

                return "OK";
            });
        }

        public JsonResult SyncRignameInBusplan()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var aa = BizPlanActivity.Populate<BizPlanActivity>();
                var zz = new List<BizPlanActivity>();
                foreach (var a in aa)
                {
                    var check = a.Phases.FirstOrDefault(x => String.IsNullOrEmpty(x.Estimate.RigName));
                    if (check != null)
                    {
                        foreach (var p in a.Phases)
                        {
                            if (String.IsNullOrEmpty(p.Estimate.RigName))
                            {
                                p.Estimate.RigName = a.RigName;
                            }
                        }
                        DataHelper.Save(a.TableName, a.ToBsonDocument());
                        zz.Add(a);
                    }
                }
                return zz;
            });
        }

        public JsonResult RefreshInvalidLSinBusPlan()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var aa = BizPlanActivity.Populate<BizPlanActivity>();
                var zz = new List<BizPlanActivity>();
                foreach (var a in aa)
                {
                    if (a.Phases.Any())
                    {
                        var newPhases = new List<BizPlanActivityPhase>();
                        foreach (var ph in a.Phases)
                        {
                            //check valid LS
                            var isValidLS = ph.PhSchedule != null && ph.PhSchedule.Start != Tools.DefaultDate && ph.PhSchedule.Finish != Tools.DefaultDate;

                            //check future LS
                            var isFutureLS = ph.PhSchedule != null && ph.PhSchedule.Start.Date > DateTime.Now.Date;

                            //check isNotHaveWeeklyReport
                            var isNotHaveWeeklyReport = WellActivity.isHaveWeeklyReport(a.WellName, a.UARigSequenceId, ph.ActivityType) == false;

                            if (isValidLS && isFutureLS && isNotHaveWeeklyReport)
                                newPhases.Add(ph);
                        }
                        if (newPhases.Any())
                        {
                            a.Phases = newPhases;
                            DataHelper.Save(a.TableName, a.ToBsonDocument());
                        }
                        else
                        {
                            //tidak punya phase valid, delete sajaaaaa
                            DataHelper.Delete(a.TableName, Query.EQ("_id", Convert.ToInt32(a._id)));
                        }
                    }
                    else
                    {
                        //tidak punya phase, delete sajaaaaa
                        DataHelper.Delete(a.TableName, Query.EQ("_id", Convert.ToInt32(a._id)));
                    }

                }
                return "OK";
            });
        }

        public JsonResult SyncBizPlanAllocation()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var aa = BizPlanActivity.Populate<BizPlanActivity>();
                foreach (var activity in aa)
                {
                    if (activity.Phases.Any())
                    {
                        foreach (var p in activity.Phases)
                        {
                            if (p.Estimate.Status != null && p.Estimate.Status != "")
                            {

                                BizPlanAllocation a = new BizPlanAllocation();
                                a.ActivityType = p.ActivityType;
                                a.WellName = activity.WellName;
                                a.RigName = p.Estimate.RigName;
                                a.EstimatePeriod = p.Estimate.EstimatePeriod;
                                a.SaveToOP = p.Estimate.SaveToOP;

                                a.WorkingInterest = activity.WorkingInterest;

                                var maturityLevel = p.Estimate.MaturityLevel.Substring(p.Estimate.MaturityLevel.Length - 1, 1);

                                BizPlanSummary calc = new BizPlanSummary(activity.WellName, p.Estimate.RigName, p.ActivityType,
                                    activity.Country, activity.ShellShare, p.Estimate.EstimatePeriod, Convert.ToInt32(maturityLevel),
                                    p.Estimate.Services, p.Estimate.Materials, p.Estimate.TroubleFreeBeforeLC.Days, activity.ReferenceFactorModel,
                                    Tools.Div(p.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentDays, 100),
                                    Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100),
                                    p.Estimate.LongLeadMonthRequired, Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100), baseOP: p.Estimate.SaveToOP, lcf: p.Estimate.NewLearningCurveFactor);
                                calc.GeneratePeriodBucket();
                                calc.GenerateAnnualyBucket(calc.MonthlyBuckets);
                                calc.GenerateQuarterBucket(calc.MonthlyBuckets);

                                a.AnnualyBuckets = calc.AnnualyBuckets;
                                a.MonthlyBuckets = calc.MonthlyBuckets;
                                a.QuarterlyBuckets = calc.QuarterBuckets;

                                p.Allocation = a;
                                p.PushToWellPlan = true;
                                a.Save();
                            }
                        }
                    }
                    if (activity.WellName.Equals("APPO AW006"))
                    {

                    }

                    activity.Save();

                }



                return "OK";
            });
        }



        public JsonResult SyncLastUpdate()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var aa = BizPlanActivity.Populate<BizPlanActivity>();
                foreach (var activity in aa)
                {
                    if (activity.Phases.Any())
                    {
                        foreach (var p in activity.Phases)
                        {
                            p.Estimate.LastUpdate = Tools.ToUTC(activity.LastUpdate);
                            p.Estimate.LastUpdateBy = activity.DataInputBy;
                        }
                    }
                    DataHelper.Save(activity.TableName, activity.ToBsonDocument());
                }

                return "Estimate-Last Update Synced";
            });
        }



        public JsonResult CleansingDRLTOPHOLEONLY()
        {
            return MvcResultInfo.Execute(null, document =>
            {

                DataHelper.Delete("WEISMasterAliases");
                MasterAlias.LoadDRLTOPHOLE();


                MasterAlias.MasterCleansing("WEISWellActivityUpdates", "Phase.ActivityType");
                MasterAlias.MasterCleansing("WEISWellActivityUpdatesMonthly", "Phase.ActivityType");
                MasterAlias.MasterCleansing("WEISBizPlanActivities", "ActivityType", true, "OPHistories");
                MasterAlias.MasterCleansing("WEISBizPlanActivities", "ActivityType", true, "Phases");
                MasterAlias.MasterCleansing("WEISBizPlanActivities", "Allocation.ActivityType", true, "Phases");
                MasterAlias.MasterCleansing("WEISWellActivities", "ActivityType", true, "Phases");
                MasterAlias.MasterCleansing("WEISWellActivities", "ActivityType", true, "OPHistories");
                MasterAlias.MasterCleansing("WEISBizPlanAllocation", "ActivityType");
                MasterAlias.MasterCleansing("LogLatestLS", "Activity_Type");




                //MasterAlias.MasterCleansing("WEISWellPIPs", "WellName");
                //MasterAlias.MasterCleansing("WEISWellActivities", "WellName");

                //MasterAlias.MasterCleansing("WEISWellPIPs", "PerformanceUnit", true, "Elements");
                //MasterAlias.MasterCleansing("WEISWellActivityUpdates", "PerformanceUnit", true, "Elements");
                //MasterAlias.MasterCleansing("WEISWellActivities", "ActivityType", true, "Phases");
                //MasterAlias.MasterCleansing("WEISWellActivityUpdates", "Phase.ActivityType");
                //MasterAlias.MasterCleansing("WEISWellPIPs", "ActivityType");
                //MasterAlias.MasterCleansing("WEISWellActivities", "RigType");
                //MasterAlias.MasterCleansing("WEISWellActivities", "PerformanceUnit");
                //MasterAlias.MasterCleansing("WEISWellActivities", "OperatingUnit");
                //MasterAlias.MasterCleansing("WEISWellActivities", "RigName");
                //MasterAlias.MasterCleansing("WEISWellActivities", "ProjectName");
                //MasterAlias.MasterCleansing("WEISWellActivityUpdates", "Project");


                return "OK-2";
            });
        }

        public JsonResult CleansingRigMaintenance()
        {
            return MvcResultInfo.Execute(null, document =>
            {

                //DataHelper.Delete("WEISMasterAliases");
                MasterAlias.LoadDefaultAlias();


                MasterAlias.MasterCleansing("WEISWellActivityUpdates", "Phase.ActivityType");
                MasterAlias.MasterCleansing("WEISWellActivityUpdatesMonthly", "Phase.ActivityType");
                MasterAlias.MasterCleansing("WEISBizPlanActivities", "ActivityType", true, "OPHistories");
                MasterAlias.MasterCleansing("WEISBizPlanActivities", "ActivityType", true, "Phases");
                MasterAlias.MasterCleansing("WEISBizPlanActivities", "Allocation.ActivityType", true, "Phases");
                MasterAlias.MasterCleansing("WEISWellActivities", "ActivityType", true, "Phases");
                MasterAlias.MasterCleansing("WEISWellActivities", "ActivityType", true, "OPHistories");
                MasterAlias.MasterCleansing("WEISBizPlanAllocation", "ActivityType");
                MasterAlias.MasterCleansing("LogLatestLS", "Activity_Type");




                //MasterAlias.MasterCleansing("WEISWellPIPs", "WellName");
                //MasterAlias.MasterCleansing("WEISWellActivities", "WellName");

                //MasterAlias.MasterCleansing("WEISWellPIPs", "PerformanceUnit", true, "Elements");
                //MasterAlias.MasterCleansing("WEISWellActivityUpdates", "PerformanceUnit", true, "Elements");
                //MasterAlias.MasterCleansing("WEISWellActivities", "ActivityType", true, "Phases");
                //MasterAlias.MasterCleansing("WEISWellActivityUpdates", "Phase.ActivityType");
                //MasterAlias.MasterCleansing("WEISWellPIPs", "ActivityType");
                //MasterAlias.MasterCleansing("WEISWellActivities", "RigType");
                //MasterAlias.MasterCleansing("WEISWellActivities", "PerformanceUnit");
                //MasterAlias.MasterCleansing("WEISWellActivities", "OperatingUnit");
                //MasterAlias.MasterCleansing("WEISWellActivities", "RigName");
                //MasterAlias.MasterCleansing("WEISWellActivities", "ProjectName");
                //MasterAlias.MasterCleansing("WEISWellActivityUpdates", "Project");


                return "OK-2";
            });
        }

        public JsonResult ResaveBizplan()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var aa = BizPlanActivity.Populate<BizPlanActivity>();//(Query.EQ("WellName", "CB24-2"));
                foreach (var activity in aa)
                {
                    if (activity.WellName.Equals("NOZOMI GOLD") && activity.UARigSequenceId.Equals("UA2078"))
                    {

                    }

                    var newbiz = BsonSerializer.Deserialize<BizPlanActivity>(activity.ToBsonDocument());
                    if (activity.Phases.Any())
                    {
                        foreach (var p in activity.Phases)
                        {

                            var nPh = p;
                            try
                            {
                                var maturityLevel = p.Estimate.MaturityLevel.Substring(p.Estimate.MaturityLevel.Length - 1, 1);

                                //var newTroubleFreeDays = learningCurveCalc;
                                //nPh.Estimate.NewTroubleFree.Days = learningCurveCalc;



                                BizPlanSummary res = new BizPlanSummary(activity.WellName, p.Estimate.RigName, p.ActivityType, p.Estimate.Country,
                                    activity.WorkingInterest, new DateRange() { Start = p.Estimate.EstimatePeriod.Start, Finish = p.Estimate.EstimatePeriod.Start }, Convert.ToInt32(maturityLevel),
                                    p.Estimate.Services, p.Estimate.Materials, p.Estimate.TroubleFreeBeforeLC.Days, activity.ReferenceFactorModel,
                                    p.Estimate.NewNPTTime.PercentDays > 1 ? Tools.Div(p.Estimate.NewNPTTime.PercentDays, 100) : p.Estimate.NewNPTTime.PercentDays,
                                    p.Estimate.NewTECOPTime.PercentDays > 1 ? Tools.Div(p.Estimate.NewTECOPTime.PercentDays, 100) : p.Estimate.NewTECOPTime.PercentDays,
                                    p.Estimate.NewNPTTime.PercentCost > 1 ? Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100) : p.Estimate.NewNPTTime.PercentCost,
                                    p.Estimate.NewTECOPTime.PercentCost > 1 ? Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100) : p.Estimate.NewTECOPTime.PercentCost,
                                    p.Estimate.LongLeadMonthRequired,
                                    p.Estimate.PercOfMaterialsLongLead > 1 ? Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100) : p.Estimate.PercOfMaterialsLongLead,
                                    baseOP: p.Estimate.SaveToOP, lcf: p.Estimate.NewLearningCurveFactor);

                                res.GeneratePeriodBucket();
                                res.GenerateAnnualyBucket(res.MonthlyBuckets);


                                var finishDate = res.EventPeriod.Start.Date.AddDays(res.MeanTime);
                                res.EventPeriod.Finish = finishDate; //.AddDays(1);

                                nPh.Estimate.NewBaseValue.Days = res.NewBaseValue.Days;
                                nPh.Estimate.NewBaseValue.Cost = res.NewBaseValue.Cost;
                                nPh.Estimate.NewLCFValue.Days = res.NewLCFValue.Days;
                                nPh.Estimate.NewLCFValue.Cost = res.NewLCFValue.Cost;

                                nPh.Estimate.SpreadRate = res.SpreadRateWRig;
                                nPh.Estimate.SpreadRateTotal = res.SpreadRateTotal;
                                nPh.Estimate.SpreadRateUSD = res.SpreadRateWRig;
                                nPh.Estimate.SpreadRateTotalUSD = res.SpreadRateTotal;

                                nPh.Estimate.BurnRate = res.BurnRate;
                                nPh.Estimate.BurnRateUSD = res.BurnRate;
                                nPh.Estimate.RigRate = res.RigRate;
                                nPh.Estimate.RigRatesUSD = res.RigRate;

                                nPh.Estimate.NewTroubleFree.Cost = res.TroubleFreeCostUSD;
                                nPh.Estimate.NewTroubleFreeUSD = res.TroubleFreeCostUSD;
                                nPh.Estimate.NewNPTTime.Cost = res.NPTCostUSD;
                                nPh.Estimate.NewTECOPTime.Cost = res.TECOPCostUSD;

                                nPh.Estimate.NewNPTTime.Days = res.NPT.Days;
                                nPh.Estimate.NewTECOPTime.Days = res.TECOP.Days;

                                nPh.Estimate.NPTCostUSD = res.NPTCostUSD;
                                nPh.Estimate.TECOPCostUSD = res.TECOPCostUSD;
                                nPh.Estimate.NewMean.Cost = res.MeanCostEDM;
                                nPh.Estimate.NewMean.Days = res.MeanTime;
                                nPh.Estimate.MeanUSD = res.MeanCostEDM;
                                nPh.Estimate.MeanCostMOD = res.MeanCostMOD;
                                nPh.Estimate.ShellShareCalc = res.ShellShareCost;

                                nPh.Estimate.LongLeadCalc = nPh.Estimate.Materials * Tools.Div(nPh.Estimate.PercOfMaterialsLongLead, 100);

                                nPh.PushToWellPlan = true;

                                //if (nPh.Estimate.SaveToOP == DefaultOP)
                                //{
                                //    nPh.PlanSchedule.Start = PlanStart;
                                //    nPh.PlanSchedule.Finish = PlanFinish;
                                //}
                                //else
                                //{
                                //    var PeriodFinish = res.EventPeriod.Start.Date.AddDays(res.MeanTime);
                                //    nPh.Estimate.EstimatePeriod.Start = PlanStart;
                                //    nPh.Estimate.EstimatePeriod.Finish = PeriodFinish.AddDays(1);
                                //    nPh.Estimate.EventStartDate = PlanStart;
                                //    nPh.Estimate.EventEndDate = PeriodFinish.AddDays(1);
                                //}



                            }
                            catch (Exception e) { }


                            newbiz.Phases = new List<BizPlanActivityPhase>();
                            newbiz.Phases.Add(nPh);
                            var a = new BizPlanActivity();
                            newbiz.RigName = p.Estimate.RigName;
                            a.SavePlanMethod(newbiz, p.Estimate.LastUpdateBy, p.Estimate.Status, false);
                        }
                    }
                }
                return "OK";
            });
        }

        public JsonResult SyncBizPlanMeanTime()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var aa = BizPlanActivity.Populate<BizPlanActivity>();
                foreach (var activity in aa)
                {
                    if (activity.Phases.Any())
                    {
                        foreach (var p in activity.Phases)
                        {

                            if (p.Estimate.Status != null && p.Estimate.Status != "")
                            {

                                if (p.Estimate.SaveToOP == null)
                                {
                                    p.Estimate.SaveToOP = "OP16";
                                }
                                BizPlanAllocation a = new BizPlanAllocation();
                                a.ActivityType = p.ActivityType;
                                a.WellName = activity.WellName;
                                a.RigName = p.Estimate.RigName;
                                a.EstimatePeriod = p.Estimate.EstimatePeriod;
                                a.SaveToOP = p.Estimate.SaveToOP;
                                a.UARigSequenceId = activity.UARigSequenceId;
                                a.WorkingInterest = activity.WorkingInterest;

                                string maturityLevel = "0";
                                if (p.Estimate.MaturityLevel != null)
                                {
                                    maturityLevel = p.Estimate.MaturityLevel.Substring(p.Estimate.MaturityLevel.Length - 1, 1);
                                }


                                if (p.Estimate.Country == null)
                                {
                                    p.Estimate.Country = "United States";
                                    activity.Country = "United States";
                                }
                                if (activity.ReferenceFactorModel == null)
                                {
                                    activity.ReferenceFactorModel = "default";
                                }
                                if (p.Estimate.Currency == null)
                                {
                                    p.Estimate.Currency = "USD";
                                }

                                // new logic

                                //save value TroubleFreeCostBeforeLC.Days to NewTroubleFree.Days
                                p.Estimate.NewTroubleFree.Days = p.Estimate.TroubleFreeBeforeLC.Days;

                                BizPlanSummary calc = new BizPlanSummary(activity.WellName, p.Estimate.RigName, p.ActivityType,
                                    activity.Country, activity.ShellShare, p.Estimate.EstimatePeriod, Convert.ToInt32(maturityLevel),
                                    p.Estimate.Services, p.Estimate.Materials, p.Estimate.TroubleFreeBeforeLC.Days, activity.ReferenceFactorModel,
                                    Tools.Div(p.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentDays, 100),
                                    Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100),
                                    p.Estimate.LongLeadMonthRequired, Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100), baseOP: p.Estimate.SaveToOP, lcf: p.Estimate.NewLearningCurveFactor, isGetExcRateByCurrency: true);
                                calc.GeneratePeriodBucket();
                                calc.GenerateAnnualyBucket(calc.MonthlyBuckets);
                                calc.GenerateQuarterBucket(calc.MonthlyBuckets);

                                a.AnnualyBuckets = calc.AnnualyBuckets;
                                a.MonthlyBuckets = calc.MonthlyBuckets;
                                a.QuarterlyBuckets = calc.QuarterBuckets;

                                p.Allocation = a;
                                a.Save();
                                p.Estimate.NewMean.Days = calc.MeanTime;
                                p.Estimate.EstimatePeriod.Finish = p.Estimate.EstimatePeriod.Start.AddDays(calc.MeanTime);
                                p.Estimate.EventEndDate = p.Estimate.EstimatePeriod.Finish;
                                p.Estimate.EventStartDate = p.Estimate.EstimatePeriod.Start;

                                p.Estimate.NewLCFValue.Days = Math.Round(calc.NewLCFValue.Days, 2);
                                p.Estimate.NewLCFValue.Cost = Math.Round(calc.NewLCFValue.Cost, 2);
                                p.Estimate.NewBaseValue.Days = Math.Round(calc.NewBaseValue.Days, 2);
                                p.Estimate.NewBaseValue.Cost = Math.Round(calc.NewBaseValue.Cost, 2);
                                p.Estimate.SpreadRate = calc.SpreadRateWRig;
                                p.Estimate.SpreadRateTotal = calc.SpreadRateTotal;
                                p.Estimate.RigRate = calc.RigRate;
                                p.Estimate.BurnRate = calc.BurnRate;

                                //cost things
                                p.Estimate.MeanCostMOD = calc.MeanCostMOD;
                                p.PushToWellPlan = true;

                                p.Estimate.NewTroubleFree.Cost = calc.TroubleFree.Cost;
                                p.Estimate.Materials = calc.Material;
                                p.Estimate.Services = calc.Services;
                                p.Estimate.NewNPTTime.Cost = calc.NPT.Cost;
                                p.Estimate.NewTECOPTime.Cost = calc.TECOP.Cost;
                                p.Estimate.NewMean.Cost = calc.MeanCostEDM;

                                //USD
                                p.Estimate.BurnRateUSD = p.Estimate.BurnRate;
                                p.Estimate.MaterialsUSD = p.Estimate.Materials;
                                p.Estimate.MeanUSD = p.Estimate.NewMean.Cost;
                                p.Estimate.NewTroubleFreeUSD = p.Estimate.NewTroubleFree.Cost;
                                p.Estimate.NPTCostUSD = p.Estimate.NewNPTTime.Cost;
                                p.Estimate.RigRatesUSD = p.Estimate.RigRate;
                                p.Estimate.ServicesUSD = p.Estimate.Services;
                                p.Estimate.SpreadRateTotalUSD = p.Estimate.SpreadRateTotal;
                                p.Estimate.SpreadRateUSD = p.Estimate.SpreadRate;
                                p.Estimate.TECOPCostUSD = p.Estimate.NewTECOPTime.Cost;

                                if (!p.Estimate.Status.Trim().ToLower().Equals("draft"))
                                {
                                    var wa = WellActivity.Get<WellActivity>(
                                    Query.And(
                                        Query.EQ("WellName", activity.WellName),
                                        Query.EQ("UARigSequenceId", activity.UARigSequenceId),
                                        Query.EQ("Phases.ActivityType", p.ActivityType)
                                    )
                                    );

                                    if (wa != null)
                                    {
                                        var pa = wa.Phases.Where(x => x.ActivityType.Equals(p.ActivityType));

                                        if (pa != null)
                                        {
                                            var singlePa = pa.FirstOrDefault();
                                            singlePa.PlanSchedule = p.Estimate.EstimatePeriod;
                                            singlePa.Plan.Cost = p.Estimate.MeanCostMOD;
                                            singlePa.Plan.Days = p.Estimate.NewMean.Days;

                                            wa.Save();
                                        }
                                    }
                                }

                                //p.Estimate.LastUpdate = activity.LastUpdate;
                            }
                        }
                    }



                    activity.Save();



                }



                return "OK-2";
            });
        }


        public JsonResult SyncBizPlanMeanTimeOnlyComplete()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var aa = BizPlanActivity.Populate<BizPlanActivity>();
                foreach (var activity in aa)
                {
                    if (activity.Phases.Any())
                    {
                        foreach (var p in activity.Phases)
                        {

                            if (p.Estimate.Status != null && p.Estimate.Status != "" && (p.Estimate.Status.Equals("Complete") || p.Estimate.Status.Equals("Modified")))
                            {


                                BizPlanAllocation a = new BizPlanAllocation();
                                a.ActivityType = p.ActivityType;
                                a.WellName = activity.WellName;
                                a.RigName = p.Estimate.RigName;
                                a.EstimatePeriod = p.Estimate.EstimatePeriod;
                                a.SaveToOP = p.Estimate.SaveToOP;
                                a.UARigSequenceId = activity.UARigSequenceId;
                                a.WorkingInterest = activity.WorkingInterest;


                                var maturityLevel = p.Estimate.MaturityLevel.Substring(p.Estimate.MaturityLevel.Length - 1, 1);
                                // new logic

                                //save value TroubleFreeCostBeforeLC.Days to NewTroubleFree.Days
                                p.Estimate.NewTroubleFree.Days = p.Estimate.TroubleFreeBeforeLC.Days;

                                BizPlanSummary calc = new BizPlanSummary(activity.WellName, p.Estimate.RigName, p.ActivityType,
                                    activity.Country, activity.ShellShare, p.Estimate.EstimatePeriod, Convert.ToInt32(maturityLevel),
                                    p.Estimate.Services, p.Estimate.Materials, p.Estimate.TroubleFreeBeforeLC.Days, activity.ReferenceFactorModel,
                                    Tools.Div(p.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentDays, 100),
                                    Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100),
                                    p.Estimate.LongLeadMonthRequired, Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100), baseOP: p.Estimate.SaveToOP, lcf: p.Estimate.NewLearningCurveFactor, isGetExcRateByCurrency: true);
                                calc.GeneratePeriodBucket();
                                calc.GenerateAnnualyBucket(calc.MonthlyBuckets);
                                calc.GenerateQuarterBucket(calc.MonthlyBuckets);

                                a.AnnualyBuckets = calc.AnnualyBuckets;
                                a.MonthlyBuckets = calc.MonthlyBuckets;
                                a.QuarterlyBuckets = calc.QuarterBuckets;

                                p.Allocation = a;
                                a.Save();
                                p.Estimate.NewMean.Days = calc.MeanTime;
                                p.Estimate.EstimatePeriod.Finish = p.Estimate.EstimatePeriod.Start.AddDays(calc.MeanTime);
                                p.Estimate.EventEndDate = p.Estimate.EstimatePeriod.Finish;
                                p.Estimate.EventStartDate = p.Estimate.EstimatePeriod.Start;

                                p.Estimate.NewLCFValue.Days = Math.Round(calc.NewLCFValue.Days, 2);
                                p.Estimate.NewLCFValue.Cost = Math.Round(calc.NewLCFValue.Cost, 2);
                                p.Estimate.NewBaseValue.Days = Math.Round(calc.NewBaseValue.Days, 2);
                                p.Estimate.NewBaseValue.Cost = Math.Round(calc.NewBaseValue.Cost, 2);
                                p.Estimate.SpreadRate = calc.SpreadRateWRig;
                                p.Estimate.SpreadRateTotal = calc.SpreadRateTotal;
                                p.Estimate.RigRate = calc.RigRate;
                                p.Estimate.BurnRate = calc.BurnRate;

                                //cost things
                                p.Estimate.MeanCostMOD = calc.MeanCostMOD;
                                p.PushToWellPlan = true;

                                p.Estimate.NewTroubleFree.Cost = calc.TroubleFree.Cost;
                                p.Estimate.Materials = calc.Material;
                                p.Estimate.Services = calc.Services;
                                p.Estimate.NewNPTTime.Cost = calc.NPT.Cost;
                                p.Estimate.NewTECOPTime.Cost = calc.TECOP.Cost;
                                p.Estimate.NewMean.Cost = calc.MeanCostEDM;

                                //USD
                                p.Estimate.BurnRateUSD = p.Estimate.BurnRate;
                                p.Estimate.MaterialsUSD = p.Estimate.Materials;
                                p.Estimate.MeanUSD = p.Estimate.NewMean.Cost;
                                p.Estimate.NewTroubleFreeUSD = p.Estimate.NewTroubleFree.Cost;
                                p.Estimate.NPTCostUSD = p.Estimate.NewNPTTime.Cost;
                                p.Estimate.RigRatesUSD = p.Estimate.RigRate;
                                p.Estimate.ServicesUSD = p.Estimate.Services;
                                p.Estimate.SpreadRateTotalUSD = p.Estimate.SpreadRateTotal;
                                p.Estimate.SpreadRateUSD = p.Estimate.SpreadRate;
                                p.Estimate.TECOPCostUSD = p.Estimate.NewTECOPTime.Cost;

                                if (!p.Estimate.Status.Trim().ToLower().Equals("draft"))
                                {
                                    var wa = WellActivity.Get<WellActivity>(
                                    Query.And(
                                        Query.EQ("WellName", activity.WellName),
                                        Query.EQ("UARigSequenceId", activity.UARigSequenceId),
                                        Query.EQ("Phases.ActivityType", p.ActivityType)
                                    )
                                    );

                                    if (wa != null)
                                    {
                                        var pa = wa.Phases.Where(x => x.ActivityType.Equals(p.ActivityType));

                                        if (pa != null)
                                        {
                                            var singlePa = pa.FirstOrDefault();
                                            singlePa.PlanSchedule = p.Estimate.EstimatePeriod;
                                            singlePa.Plan.Cost = p.Estimate.MeanCostMOD;
                                            singlePa.Plan.Days = p.Estimate.NewMean.Days;

                                            wa.Save();
                                        }
                                    }
                                }

                                //p.Estimate.LastUpdate = activity.LastUpdate;
                            }
                        }
                    }



                    activity.Save();



                }



                return "OK-2";
            });
        }

        public JsonResult SyncBizPlanMeanTimeOnlyCompleteTest()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var aa = BizPlanActivity.Populate<BizPlanActivity>(Query.EQ("WellName", "VITO VN3"));
                foreach (var activity in aa)
                {
                    if (activity.Phases.Any())
                    {
                        foreach (var p in activity.Phases)
                        {

                            if (p.Estimate.Status != null && p.Estimate.Status != "" && (p.Estimate.Status.Equals("Complete") || p.Estimate.Status.Equals("Modified")))
                            {


                                BizPlanAllocation a = new BizPlanAllocation();
                                a.ActivityType = p.ActivityType;
                                a.WellName = activity.WellName;
                                a.RigName = p.Estimate.RigName;
                                a.EstimatePeriod = p.Estimate.EstimatePeriod;
                                a.SaveToOP = p.Estimate.SaveToOP;
                                a.UARigSequenceId = activity.UARigSequenceId;
                                a.WorkingInterest = activity.WorkingInterest;


                                var maturityLevel = p.Estimate.MaturityLevel.Substring(p.Estimate.MaturityLevel.Length - 1, 1);
                                // new logic

                                //save value TroubleFreeCostBeforeLC.Days to NewTroubleFree.Days
                                p.Estimate.NewTroubleFree.Days = p.Estimate.TroubleFreeBeforeLC.Days;

                                BizPlanSummary calc = new BizPlanSummary(activity.WellName, p.Estimate.RigName, p.ActivityType,
                                    activity.Country, activity.ShellShare, p.Estimate.EstimatePeriod, Convert.ToInt32(maturityLevel),
                                    p.Estimate.Services, p.Estimate.Materials, p.Estimate.TroubleFreeBeforeLC.Days, activity.ReferenceFactorModel,
                                    Tools.Div(p.Estimate.NewNPTTime.PercentDays, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentDays, 100),
                                    Tools.Div(p.Estimate.NewNPTTime.PercentCost, 100), Tools.Div(p.Estimate.NewTECOPTime.PercentCost, 100),
                                    p.Estimate.LongLeadMonthRequired, Tools.Div(p.Estimate.PercOfMaterialsLongLead, 100), baseOP: p.Estimate.SaveToOP, lcf: p.Estimate.NewLearningCurveFactor, isGetExcRateByCurrency: true);
                                calc.GeneratePeriodBucket();
                                calc.GenerateAnnualyBucket(calc.MonthlyBuckets);
                                calc.GenerateQuarterBucket(calc.MonthlyBuckets);

                                a.AnnualyBuckets = calc.AnnualyBuckets;
                                a.MonthlyBuckets = calc.MonthlyBuckets;
                                a.QuarterlyBuckets = calc.QuarterBuckets;

                                p.Allocation = a;
                                a.Save();
                                p.Estimate.NewMean.Days = calc.MeanTime;
                                p.Estimate.EstimatePeriod.Finish = p.Estimate.EstimatePeriod.Start.AddDays(calc.MeanTime);
                                p.Estimate.EventEndDate = p.Estimate.EstimatePeriod.Finish;
                                p.Estimate.EventStartDate = p.Estimate.EstimatePeriod.Start;

                                p.Estimate.NewLCFValue.Days = Math.Round(calc.NewLCFValue.Days, 2);
                                p.Estimate.NewLCFValue.Cost = Math.Round(calc.NewLCFValue.Cost, 2);
                                p.Estimate.NewBaseValue.Days = Math.Round(calc.NewBaseValue.Days, 2);
                                p.Estimate.NewBaseValue.Cost = Math.Round(calc.NewBaseValue.Cost, 2);
                                p.Estimate.SpreadRate = calc.SpreadRateWRig;
                                p.Estimate.SpreadRateTotal = calc.SpreadRateTotal;
                                p.Estimate.RigRate = calc.RigRate;
                                p.Estimate.BurnRate = calc.BurnRate;

                                //cost things
                                p.Estimate.MeanCostMOD = calc.MeanCostMOD;
                                p.PushToWellPlan = true;

                                p.Estimate.NewTroubleFree.Cost = calc.TroubleFree.Cost;
                                p.Estimate.Materials = calc.Material;
                                p.Estimate.Services = calc.Services;
                                p.Estimate.NewNPTTime.Cost = calc.NPT.Cost;
                                p.Estimate.NewTECOPTime.Cost = calc.TECOP.Cost;
                                p.Estimate.NewMean.Cost = calc.MeanCostEDM;

                                //USD
                                p.Estimate.BurnRateUSD = p.Estimate.BurnRate;
                                p.Estimate.MaterialsUSD = p.Estimate.Materials;
                                p.Estimate.MeanUSD = p.Estimate.NewMean.Cost;
                                p.Estimate.NewTroubleFreeUSD = p.Estimate.NewTroubleFree.Cost;
                                p.Estimate.NPTCostUSD = p.Estimate.NewNPTTime.Cost;
                                p.Estimate.RigRatesUSD = p.Estimate.RigRate;
                                p.Estimate.ServicesUSD = p.Estimate.Services;
                                p.Estimate.SpreadRateTotalUSD = p.Estimate.SpreadRateTotal;
                                p.Estimate.SpreadRateUSD = p.Estimate.SpreadRate;
                                p.Estimate.TECOPCostUSD = p.Estimate.NewTECOPTime.Cost;

                                if (!p.Estimate.Status.Trim().ToLower().Equals("draft"))
                                {
                                    var wa = WellActivity.Get<WellActivity>(
                                    Query.And(
                                        Query.EQ("WellName", activity.WellName),
                                        Query.EQ("UARigSequenceId", activity.UARigSequenceId),
                                        Query.EQ("Phases.ActivityType", p.ActivityType)
                                    )
                                    );

                                    if (wa != null)
                                    {
                                        var pa = wa.Phases.Where(x => x.ActivityType.Equals(p.ActivityType));

                                        if (pa != null)
                                        {
                                            var singlePa = pa.FirstOrDefault();
                                            singlePa.PlanSchedule = p.Estimate.EstimatePeriod;
                                            singlePa.Plan.Cost = p.Estimate.MeanCostMOD;
                                            singlePa.Plan.Days = p.Estimate.NewMean.Days;

                                            wa.Save();
                                        }
                                    }
                                }

                                //p.Estimate.LastUpdate = activity.LastUpdate;
                            }
                        }
                    }



                    activity.Save();



                }



                return "OK-2";
            });
        }

        public JsonResult SyncBizPlanMissingData()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var check = BizPlanActivity.Populate<BizPlanActivity>().Where(x => String.IsNullOrEmpty(x.PerformanceUnit) || String.IsNullOrEmpty(x.AssetName) || String.IsNullOrEmpty(x.Region) || String.IsNullOrEmpty(x.ReferenceFactorModel) || String.IsNullOrEmpty(x.ProjectName) || String.IsNullOrEmpty(x.Country) || x.ReferenceFactorModel == "default");
                int tot = 0;
                foreach (var activity in check)
                {
                    if (activity.Phases.Any())
                    {
                        foreach (var p in activity.Phases)
                        {
                            if (p.Estimate.Status != "Meta Data Missing")
                            {
                                p.Estimate.Status = "Meta Data Missing";
                                tot++;
                            }
                        }
                    }
                    DataHelper.Save(activity.TableName, activity.ToBsonDocument());
                }
                return tot;
            });
        }

        //2016 08 24
        public JsonResult CleanUpDuplicateActivity()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                string aggUW = @"{ $unwind: '$Phases'}";
                string aggProject = @"{
                  $project: {
                     'WellName'	    : '$WellName',
                     'RigName'      : '$RigName',
                     'ActivityType' : '$Phases.ActivityType'
                  }
                }";
                string aggGroup = @"{
                  $group: {
                    '_id' : {
         		            'WellName' 		: '$WellName',
         		            'RigName'  		: '$RigName',
         		            'ActivityType'	: '$ActivityType'
                     },
                     'count' : { '$sum' : 1 }
                  }
                }";
                string aggMatch = @"{
              $match: {
                'count' : { '$gt' : 1}
              }
            }";
                List<BsonDocument> pipelines = new List<BsonDocument>();
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggUW));
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggProject));
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggGroup));
                pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggMatch));
                List<BsonDocument> aggregate = DataHelper.Aggregate("WEISBizPlanActivities", pipelines);
                List<BizPlanActivity> ba = new List<BizPlanActivity>();
                foreach (var u in aggregate)
                {
                    var Wellname = u.FirstOrDefault().Value[0];
                    var Rigname = u.FirstOrDefault().Value[1];
                    var Activity = u.FirstOrDefault().Value[2];
                    var qq = Query.And(
                                Query.EQ("WellName", Wellname),
                                Query.EQ("RigName", Rigname),
                                Query.EQ("Phases.ActivityType", Activity)
                                );
                    var get = BizPlanActivity.Populate<BizPlanActivity>(qq);

                    if (get.Any())
                    {
                        foreach (var r in get.ToList())
                        {
                            foreach (var p in r.Phases.ToList())
                            {
                                if (p.Estimate.Status == "Draft" || p.Estimate.Status == "Meta Data Missing")
                                {
                                    r.Phases.RemoveAll(x => x.Estimate.Status == "Draft" || x.Estimate.Status == "Meta Data Missing");
                                }
                            }
                            r.Save();
                        }
                    }
                }
                return "OK";
            });
        }

        public JsonResult TestGenerateCapex()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                //var yyy = WellActivity.Populate<WellActivity>(Query.And(Query.EQ("WellName", "APPO AC006"), Query.EQ("UARigSequenceId", "UA176")));
                //var dr = new DateRange(new DateTime(2015, 1, 1), new DateTime(2040, 12, 31));
                //PCapex px = new PCapex();
                //var gogo = px.Transforms(yyy.FirstOrDefault(), "eaciit", "Test", dr);
                var yyy = WellActivity.Populate<WellActivity>(Query.And(Query.EQ("WellName", "APPO AC006"), Query.EQ("UARigSequenceId", "UA176")));
                var dr = new DateRange(new DateTime(2015, 1, 1), new DateTime(2040, 12, 31));
                PCapex px = new PCapex();
                //var gogo = px.Transforms(yyy.FirstOrDefault(), "eaciit", "Test", dr, false);
                //var aggpx = px.AggregatingPCapex(gogo, 201501);

                //List<BsonDocument> bdocs = new List<BsonDocument>();
                //int i = 1;
                //foreach (var t in aggpx)
                //{
                //    bdocs.Add(t.ToBsonDocument().Set("_id", i++));
                //}

                //// only for checking
                //DataHelper.Save("WEISPalantirCapexAgg", bdocs);

                //foreach (var g in gogo)
                //{
                //    g.Save();
                //}
                //string a = "";



                return "OK";
            });
        }

        public JsonResult CurrencyCodeFixing(string PathOP15 = @"D:\CurrencyCode.xlsx")
        {
            return MvcResultInfo.Execute(null, document =>
            {
                DataHelper.Delete("_CurrencyCode");

                ExtractionHelper e = new ExtractionHelper();
                var t = e.Extract(PathOP15);

                foreach (var yx in t)
                {
                    yx.Set("_id", yx.GetString("Country"));
                    DataHelper.Save("_CurrencyCode", yx);
                }


                var macs = MacroEconomic.Populate<MacroEconomic>();

                foreach (var m in macs)
                {
                    var adacountry = t.Where(x => m.Country.ToLower().Trim().Contains(BsonHelper.GetString(x, "Country").ToLower().Trim()));
                    if (adacountry.Any())
                    {
                        m.Currency = adacountry.FirstOrDefault().GetString("Code");
                        string form = string.Format("{0} - {1}", m.Country, m.Currency);
                        DataHelper.Save(m.TableName, m.ToBsonDocument());
                    }
                    else
                    {

                        var cekke2 = t.Where(x => BsonHelper.GetString(x, "Country").ToLower().Trim().Contains(m.Country.ToLower().Trim()));

                        if (cekke2.Any())
                        {
                            m.Currency = cekke2.FirstOrDefault().GetString("Code");
                            string form = string.Format("{0} - {1}", m.Country, m.Currency);
                            DataHelper.Save(m.TableName, m.ToBsonDocument());
                        }
                        else
                        {
                            string form = string.Format("{0} - {1}", m.Country, "Currency Not Found");
                        }

                        if (m.Country.Contains("Russia"))
                        {
                            var cekkrusia = t.Where(x => BsonHelper.GetString(x, "Country").Trim().Equals("Russia"));
                            if (cekkrusia.Any())
                            {
                                m.Currency = cekkrusia.FirstOrDefault().GetString("Code");
                                string form = string.Format("{0} - {1}", m.Country, m.Currency);
                                DataHelper.Save(m.TableName, m.ToBsonDocument());
                            }
                        }
                        if (m.Country.Contains("Australia"))
                        {
                            var cekkrusia = t.Where(x => BsonHelper.GetString(x, "Country").Trim().Equals("Australia"));
                            if (cekkrusia.Any())
                            {
                                m.Currency = cekkrusia.FirstOrDefault().GetString("Code");
                                string form = string.Format("{0} - {1}", m.Country, m.Currency);
                                DataHelper.Save(m.TableName, m.ToBsonDocument());
                            }
                        }
                        if (m.Country.Contains("Canada"))
                        {
                            var cekkrusia = t.Where(x => BsonHelper.GetString(x, "Country").Trim().Equals("Canada"));
                            if (cekkrusia.Any())
                            {
                                m.Currency = cekkrusia.FirstOrDefault().GetString("Code");
                                string form = string.Format("{0} - {1}", m.Country, m.Currency);
                                DataHelper.Save(m.TableName, m.ToBsonDocument());
                            }
                        }
                        if (m.Country.Contains("Norway"))
                        {
                            var cekkrusia = t.Where(x => BsonHelper.GetString(x, "Country").Trim().Equals("Norway"));
                            if (cekkrusia.Any())
                            {
                                m.Currency = cekkrusia.FirstOrDefault().GetString("Code");
                                string form = string.Format("{0} - {1}", m.Country, m.Currency);
                                DataHelper.Save(m.TableName, m.ToBsonDocument());
                            }
                        }
                    }
                }

                return "OK";
            });
        }


        public JsonResult FixingMLEComments()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var come = WEISComment.Populate<WEISComment>(Query.EQ("ReferenceType", "MonthlyReport"));

                foreach (var t in come)
                {
                    if (t._id.ToString().Equals("123"))
                    {
                        var raw = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(
                            Query.And(
                                Query.EQ("WellName", "STONES 10"),
                                Query.EQ("Phase.ActivityType", "WHOLE DRILLING EVENT"),
                                Query.EQ("SequenceId", "2958")
                            )
                            );
                        if (raw != null)
                        {
                            string frm = string.Format("{0}||{1}||{2}", raw.WellName, raw.Phase.ActivityType, raw.SequenceId);
                            t.Reference1 = frm;
                            t.Reference2 = raw._id.ToString();
                            DataHelper.Save("WEISComments", t.ToBsonDocument());
                        }
                    }
                    else if (t._id.ToString().Equals("129") || t._id.ToString().Equals("127"))
                    {
                        // completeion
                        var raw = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(
                            Query.And(
                                Query.EQ("WellName", "KING MC764-5 DEVELOPMENT"),
                                Query.EQ("Phase.ActivityType", "WHOLE COMPLETION EVENT"),
                                Query.EQ("SequenceId", "UA1079")
                            )
                            );
                        if (raw != null)
                        {
                            string frm = string.Format("{0}||{1}||{2}", raw.WellName, raw.Phase.ActivityType, raw.SequenceId);
                            t.Reference1 = frm;
                            t.Reference2 = raw._id.ToString();
                            DataHelper.Save("WEISComments", t.ToBsonDocument());
                        }
                    }
                    else if (t._id.ToString().Equals("128"))//|| t._id.ToString().Equals("127"))
                    {
                        // drilling
                        var raw = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(
                             Query.And(
                                Query.EQ("WellName", "KING MC764-5 DEVELOPMENT"),
                                Query.EQ("Phase.ActivityType", "WHOLE DRILLING EVENT"),
                                Query.EQ("SequenceId", "UA1079")
                            )
                            );
                        if (raw != null)
                        {
                            string frm = string.Format("{0}||{1}||{2}", raw.WellName, raw.Phase.ActivityType, raw.SequenceId);
                            t.Reference1 = frm;
                            t.Reference2 = raw._id.ToString();
                            DataHelper.Save("WEISComments", t.ToBsonDocument());
                        }
                    }
                }


                return "OK";
            });
        }


        public JsonResult RefreshFundingType()
        {
            return MvcResultInfo.Execute(null, document =>
            {


                var ttt = WellActivity.Populate<WellActivity>();
                foreach (var t in ttt)
                {
                    foreach (var p in t.Phases)
                    {
                        if (p.FundingType != null)
                        {
                            var ft = p.FundingType;

                            if (ft.Trim().Equals("Development Capex"))
                                p.FundingType = "CAPEX";
                            if (ft.Trim().Contains("Opex"))
                                p.FundingType = "OPEX";
                        }
                        else
                        {
                            p.FundingType = "";
                        }


                    }

                    DataHelper.Save(t.TableName, t.ToBsonDocument());
                }
                return "OK";
            });
        }

        public JsonResult RunFiscalYear(DateTime Date)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                RunFiscal(Date);
                return null;
            });
        }


        public JsonResult BindTQandBICFromPhaseInfo()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var ts = WellActivity.Populate<WellActivity>();
                foreach (var t in ts)
                {
                    bool isSave = false;
                    foreach (var y in t.Phases)
                    {

                        if (t.WellName != null && y.ActivityType != null && t.RigName != null && t.UARigSequenceId != null && t.PhaseNo != null)
                        {
                            var r = Query.And(
                          Query.EQ("WellName", t.WellName),
                          Query.EQ("ActivityType", y.ActivityType),
                          Query.EQ("RigName", t.RigName),
                          Query.EQ("OPType", "OP15")
                          );

                            var x = WellActivityPhaseInfo.Get<WellActivityPhaseInfo>(r, sort: SortBy.Descending("LastUpdate"));

                            if (x != null)
                            {
                                isSave = true;
                                y.TQ.Days = x.TQTreshold == null ? 0 : x.TQTreshold;
                                y.BIC.Days = x.BICTreshold == null ? 0 : x.BICTreshold;
                            }
                        }

                    }
                    if (isSave)
                        DataHelper.Save("WEISWellActivities", t.ToBsonDocument());
                }


                return "OK";
            });
        }



        #region Fiscal Year

        public bool isEventNotInMonthly(string wellName, string activityType, string sequenceId, List<WellActivityUpdateMonthly> monthlys)
        {
            var y = monthlys.Where(x => x.WellName.Equals(wellName) && x.SequenceId.Equals(sequenceId) && x.Phase.ActivityType.Equals(activityType));
            if (y.Any())
                return true;
            else
                return false;
        }

        public JsonResult RunFiscalYear2(DateTime Date)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                //RunFiscal2(Date);
                return "OK";
            });
        }

        public void RunFiscal2(DateTime Now)
        {


            // 1. get all latest data of monthly Report
            // 2. re-filter only take and set to now month if doesthave weekly report than set date to now month
            // 3. refresh all data in point 2 (pip) reappend
            // 4. save data point 3
            // 5. find another event from wellplan ls active and doesnt have weekly report 
            // 6. transform to MLE report data point 5
            // 7. save data point 6

            var sampleWAUM = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(null, sort: SortBy.Descending("UpdateVersion"));
            var now = new DateTime(Now.Year, Now.Month, 1);

            if (sampleWAUM != null)
            {
                var lastmonth = sampleWAUM.UpdateVersion;
                var allLastmonthlyData = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.EQ("UpdateVersion", Tools.ToUTC(lastmonth)));
                // skip if already have weekly repot
                var nothavewr = allLastmonthlyData.Where(x => WellActivity.isHaveWeeklyReport(x.WellName, x.SequenceId, x.Phase.ActivityType) == false);
                #region refresh PIP and CRElements
                foreach (var d in nothavewr)
                {
                    var ggg = WellActivity.Get<WellActivity>(Query.And(Query.EQ("WellName", d.WellName),
                    Query.EQ("UARigSequenceId", d.SequenceId),
                    Query.EQ("Phases.ActivityType", d.Phase.ActivityType)));

                    if (ggg != null)
                    {
                        if (d.Phase.PhSchedule.Start <= Tools.DefaultDate)
                            d.Phase.PhSchedule = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().PhSchedule;

                        if (d.Phase.PlanSchedule.Start <= Tools.DefaultDate)
                            d.Phase.PlanSchedule = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().PlanSchedule;

                        if (d.Phase.LESchedule.Start <= Tools.DefaultDate)
                            d.Phase.LESchedule = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().LESchedule;

                        if (d.Phase.AFESchedule.Start <= Tools.DefaultDate)
                            d.Phase.AFESchedule = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().AFESchedule;

                        d.CurrentWeek = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().LE;
                        d.Plan = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().Plan;
                        d.OP = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().OP;
                        d.AFE = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().AFE;
                        d.TQ = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().TQ;
                        d.Phase.TQ = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().TQ;
                        d.Phase.AFE = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().AFE;
                        d.Phase.AggredTarget = ggg.Phases.Where(x => x.ActivityType == d.Phase.ActivityType).FirstOrDefault().AggredTarget;
                    }

                    List<IMongoQuery> qs = new List<IMongoQuery>();
                    IMongoQuery qpip = Query.Null;
                    qs.Add(Query.EQ("WellName", d.WellName));
                    qs.Add(Query.EQ("SequenceId", d.SequenceId));
                    qs.Add(Query.EQ("ActivityType", d.Phase.ActivityType));
                    qpip = Query.And(qs);
                    WellPIP pip = WellPIP.Get<WellPIP>(qpip);

                    if (d.Elements.Any())
                    {
                        if (pip != null)
                        {
                            d.Elements = pip.Elements;
                        }
                    }
                    else
                    {
                        d.Elements = new List<PIPElement>();
                        if (pip != null)
                        {
                            d.Elements = pip.Elements;
                        }
                    }
                    if (d.CRElements.Any())
                    {
                        if (pip != null)
                        {
                            d.CRElements = pip.CRElements;
                        }
                    }
                    else
                    {
                        d.CRElements = new List<PIPElement>();
                        if (pip != null)
                        {
                            d.CRElements = pip.CRElements;
                        }
                    }

                    d._id = null;
                    d.UpdateVersion = now;
                    d.Status = "In-Progress";
                    d.InitiateDate = Tools.ToUTC(DateTime.Now);

                    if (d.Phase.PhSchedule.Start > Tools.DefaultDate &&
                        d.Phase.PhSchedule.Finish > Tools.DefaultDate &&
                        !d.Phase.ActivityType.Contains("FLOWBACK"))
                    {
                        d.Save(references: new string[] { });
                    }
                }
                #endregion

                var allWpActvs = GetAllActiveEventFuture(now);
                List<MonthlyEventsBasedOnMonth> addedWp = new List<MonthlyEventsBasedOnMonth>();
                foreach (var t in allWpActvs)
                {
                    if (isEventNotInMonthly(t.WellName, t.ActivityType, t.UARigSequenceId, nothavewr.ToList()) == false)
                        addedWp.Add(t);
                }

                // Added WP data to Monthly LE
                //saving the new activities
                if (addedWp.Any())
                {
                    var wellActivityIds = addedWp.Select(d => d._id + "|" + d.ActivityType).ToList();
                    MonthlyReportInitiate(now, "", wellActivityIds, false, true);
                }
            }
            else
            {
                // Blank collection
                var monthYear = Tools.ToUTC(new DateTime(now.Year, now.Month, now.Day));
                var rawWellActivityIds = GetActivitiesBasedOnMonth(monthYear);
                var wellActivityIds = rawWellActivityIds.Select(d => d._id + "|" + d.ActivityType).ToList();
                MonthlyReportInitiate(monthYear, "", wellActivityIds, false, true);
            }


        }

        public void RunFiscal(DateTime Now)
        {
            var sampleWAUM = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(null);
            var now = new DateTime(Now.Year, Now.Month, 1);
            var monthYear = Tools.ToUTC(new DateTime(now.Year, now.Month, now.Day));
            var rawWellActivityIds = GetActivitiesBasedOnMonth(monthYear);
            if (sampleWAUM == null)
            {
                var wellActivityIds = rawWellActivityIds.Select(d => d._id + "|" + d.ActivityType).ToList();
                MonthlyReportInitiate(monthYear, "", wellActivityIds, false, true);
            }
            else
            {
                var lastUpdateVersion = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q: null, take: 1, sort: SortBy.Descending("UpdateVersion"));
                var prevMonthlyReports = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.EQ("UpdateVersion", lastUpdateVersion.FirstOrDefault().UpdateVersion));

                //save the old registered activities   ---  TAKE ELEMENT AND CRELEMENT FRESH FROM PIP
                var wausDef = new List<WellActivityUpdate>();
                var pips = new List<WellPIP>();
                var wellActs = new List<WellActivity>();
                if (prevMonthlyReports.Any())
                {
                    var wells = prevMonthlyReports.Select(x => x.WellName).Distinct().ToList();
                    var seqs = prevMonthlyReports.Select(x => x.SequenceId).Distinct().ToList();
                    var activities = prevMonthlyReports.Select(x => x.Phase.ActivityType).Distinct().ToList();
                    wausDef = WellActivityUpdate.Populate<WellActivityUpdate>(Query.And(Query.In("WellName", new BsonArray(wells)), Query.In("SequenceId", new BsonArray(seqs)), Query.In("Phase.ActivityType", new BsonArray(activities))));
                    pips = WellPIP.Populate<WellPIP>(Query.And(Query.In("WellName", new BsonArray(wells)), Query.In("SequenceId", new BsonArray(seqs)), Query.In("ActivityType", new BsonArray(activities))));
                    wellActs = WellActivity.Populate<WellActivity>(Query.And(Query.In("WellName", new BsonArray(wells)), Query.In("UARigSequenceId", new BsonArray(seqs)), Query.In("Phases.ActivityType", new BsonArray(activities))));
                }

                foreach (var d in prevMonthlyReports)
                {
                    var weeklyActivityUpdate =
                        wausDef.FirstOrDefault(
                            x =>
                                x.WellName.Equals(d.WellName) && x.SequenceId.Equals(d.SequenceId) &&
                                x.Phase.ActivityType.Equals(d.Phase.ActivityType));
                    List<PIPElement> PIPElement = new List<PIPElement>();
                    List<PIPElement> CRElements = new List<PIPElement>();

                    #region Take Element and CR Element from PIP
                    WellPIP pip =
                        pips.FirstOrDefault(
                            x =>
                                x.WellName.Equals(d.WellName) && x.SequenceId.Equals(d.SequenceId) &&
                                x.ActivityType.Equals(d.Phase.ActivityType));
                    //WellPIP.Get<WellPIP>(qpip);

                    try
                    {
                        if (pip == null)
                        {
                            pip = new WellPIP();
                        }

                        if (pip.Elements == null)
                            PIPElement = new List<PIPElement>();
                        else if (pip.Elements.Count == 0)
                            PIPElement = new List<PIPElement>();
                        else
                            PIPElement = pip.Elements;
                    }
                    catch (Exception e)
                    {
                        PIPElement = new List<PIPElement>();
                    }

                    //add CRElements
                    try
                    {

                        if (pip.CRElements == null)
                            CRElements = new List<PIPElement>();
                        else if (pip.CRElements.Count == 0)
                            CRElements = new List<PIPElement>();
                        else
                            CRElements = pip.CRElements;
                    }
                    catch (Exception e)
                    {
                        CRElements = new List<PIPElement>();
                    }
                    #endregion

                    d._id = null;
                    d.UpdateVersion = monthYear;
                    d.Status = "In-Progress";
                    d.InitiateDate = Tools.ToUTC(DateTime.Now);
                    if (PIPElement.Any())
                        d.Elements = PIPElement;
                    if (CRElements.Any())
                        d.CRElements = CRElements;

                    if (weeklyActivityUpdate == null && d.Phase.PhSchedule.Start > Tools.DefaultDate &&
                        d.Phase.PhSchedule.Finish > Tools.DefaultDate && !d.Phase.ActivityType.Contains("FLOWBACK"))
                    {
                        d.Save(references: new string[] { });
                    }
                };


                // looking for new activity
                lastUpdateVersion = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q: null, take: 1, sort: SortBy.Descending("UpdateVersion"));
                prevMonthlyReports = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.EQ("UpdateVersion", lastUpdateVersion.FirstOrDefault().UpdateVersion));
                var newActivity = new List<MonthlyEventsBasedOnMonth>();
                foreach (var xx in rawWellActivityIds)
                {
                    var finder = prevMonthlyReports.FirstOrDefault(x => x.WellName == xx.WellName && x.Phase.ActivityType == xx.ActivityType && x.SequenceId == xx.UARigSequenceId);
                    if (finder == null)
                        newActivity.Add(xx);
                }
                //saving the new activities
                if (newActivity.Any())
                {
                    var wellActivityIds = newActivity.Select(d => d._id + "|" + d.ActivityType).ToList();
                    MonthlyReportInitiate(monthYear, "", wellActivityIds, false, true);
                }

            }
        }
        public List<PIPElement> GetCRElementsFromExisting(string wellname, string seqid, string activity)
        {
            var t = WellPIP.Get<WellPIP>(Query.And(
                 Query.EQ("WellName", wellname),
                 Query.EQ("SequenceId", seqid),
                 Query.EQ("ActivityType", activity)
                 ));

            if (t != null)
            {
                return t.CRElements;
            }
            return new List<PIPElement>();
        }


        public List<MonthlyEventsBasedOnMonth> GetAllActiveEventFuture(DateTime y)
        {
            List<WellActivity> was1 = WellActivity.Populate<WellActivity>();

            foreach (var c in was1)
            {
                List<WellActivityPhase> phaseDel = c.Phases.Where(t => t.PhSchedule.Start < Tools.ToUTC(y) || t.ActivityType.Contains("RISK")).ToList();
                if (phaseDel.Count > 0)
                {
                    foreach (WellActivityPhase p in phaseDel)
                    {
                        c.Phases.Remove(p);
                    };
                }
            }

            foreach (var x in was1)
            {
                // Delete phases that exist in activityupdates on the selected date
                if (x.Phases.Count > 0)
                {
                    List<WellActivityPhase> dels = new List<WellActivityPhase>();
                    foreach (WellActivityPhase a in x.Phases)
                    {
                        if (WellActivity.isHaveWeeklyReport(x.WellName, x.UARigSequenceId, a.ActivityType))
                            dels.Add(a);
                    }

                    if (dels.Count > 0)
                    {
                        foreach (var az in dels)
                            x.Phases.Remove(az);
                    }
                }
            }

            var yu = was1.Where(x => x.Phases.Count > 0)
                .SelectMany(x => x.Phases, (x, p) => new MonthlyEventsBasedOnMonth()
                {
                    _id = String.Format("{0}|{1}", x._id, p.ActivityType),
                    WellName = x.WellName,
                    UARigSequenceId = x.UARigSequenceId,
                    RigName = x.RigName,
                    AssetName = x.AssetName,
                    ActivityType = p.ActivityType,
                    PhSchedule = p.PhSchedule,
                    AFESchedule = p.AFESchedule
                })
                .ToList();

            return yu;
        }

        public List<MonthlyEventsBasedOnMonth> GetActivitiesBasedOnMonth(DateTime y, string WellName = null, string ActivityType = null, string SequenceId = null)
        {
            IMongoQuery queries = null;
            //queries.Add(Query.GT("Phases.PhSchedule.Start", Tools.ToUTC(y)));
            if (WellName != null && ActivityType != null && SequenceId != null)
            {
                queries = Query.And(Query.EQ("WellName", WellName), Query.EQ("UARigSequenceId", SequenceId), Query.EQ("Phases.ActivityType", ActivityType));
            }

            //IMongoQuery q1 = Query.GT("Phases.PhSchedule.Start", Tools.ToUTC(y));
            List<WellActivity> was1 = WellActivity.Populate<WellActivity>(queries);

            //if (was1.Where(x => x.WellName.Equals("APPO STAT FAILURE PROTEUS 001")).Any())
            //{

            //}
            // hapus phase jika : 
            // t.PhSchedule.Start < y
            // t.ActivityType.Contains("RISK")
            foreach (var c in was1)
            {
                //List<WellActivityPhase> phaseDel = c.Phases.Where(t => t.PhSchedule.Start > Tools.ToUTC(y) || t.PhSchedule.Finish < Tools.ToUTC(y)).ToList();
                List<WellActivityPhase> phaseDel = c.Phases.Where(t => t.PhSchedule.Start < Tools.ToUTC(y) || t.ActivityType.Contains("RISK")).ToList();
                if (phaseDel.Count > 0)
                {
                    foreach (WellActivityPhase p in phaseDel)
                    {
                        c.Phases.Remove(p);
                    };
                }
            }

            foreach (var x in was1)
            {
                // Delete phases that exist in activityupdates on the selected date
                if (x.Phases.Count > 0)
                {
                    List<WellActivityPhase> dels = new List<WellActivityPhase>();
                    foreach (WellActivityPhase a in x.Phases)
                    {
                        List<IMongoQuery> qs = new List<IMongoQuery>();
                        IMongoQuery qua = Query.Null;
                        qs.Add(Query.EQ("WellName", x.WellName));
                        qs.Add(Query.EQ("SequenceId", x.UARigSequenceId));
                        qs.Add(Query.EQ("Phase.PhaseNo", a.PhaseNo));
                        qs.Add(Query.EQ("Phase.ActivityType", a.ActivityType));
                        qs.Add(Query.EQ("UpdateVersion", Tools.ToUTC(y)));
                        qua = Query.And(qs);
                        var wau = WellActivityUpdate.Get<WellActivityUpdate>(qua);
                        if (wau != null)
                        {
                            dels.Add(a);
                        }
                    }

                    if (dels.Count > 0)
                    {
                        foreach (var az in dels)
                        {
                            x.Phases.Remove(az);
                        }

                    }
                }
            }

            var yu = was1.Where(x => x.Phases.Count > 0)
                .SelectMany(x => x.Phases, (x, p) => new MonthlyEventsBasedOnMonth()
                {
                    _id = String.Format("{0}|{1}", x._id, p.ActivityType),
                    WellName = x.WellName,
                    UARigSequenceId = x.UARigSequenceId,
                    RigName = x.RigName,
                    AssetName = x.AssetName,
                    ActivityType = p.ActivityType,
                    PhSchedule = p.PhSchedule,
                    AFESchedule = p.AFESchedule
                })
                .ToList();
            return yu;
        }

        public List<WellActivityUpdateMonthly> MonthlyReportInitiate(DateTime StartDate, string StartComment, List<String> WellActivityIds, bool withSendMail = false, bool isFromInitiate = false, string pUser = null, string pEmail = null)
        {
            //List<int> WellActvID = new List<int>();
            //string[] _id = WellActivityId.Split(',');
            var dt = StartDate;// Tools.GetNearestDay(Tools.ToDateTime(String.Format("{0:dd-MMM-yyyy}", StartDate), true), DayOfWeek.Monday);
            var qwaus = new List<IMongoQuery>();
            qwaus.Add(Query.EQ("UpdateVersion", dt));
            var q = qwaus.Count == 0 ? Query.Null : Query.And(qwaus);
            List<WellActivityUpdateMonthly> waus = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q);

            //if (WellActivityIds.Any())
            //{
            //    WellActivityIds.Select(x => x.)
            //}
            var was = WellActivity.Populate<WellActivity>();
            var wellPIPs = WellPIP.Populate<WellPIP>();

            foreach (string word in WellActivityIds)
            {
                string[] ids = word.Split('|');
                int waId = Tools.ToInt32(ids[0]);
                string activityType = ids[1];
                WellActivity wa = was.FirstOrDefault(x => x._id.Equals(waId));//WellActivity.Get<WellActivity>(waId);

                var phase = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(activityType));
                if (isFromInitiate)
                {
                    if (phase != null) phase.IsActualLE = false;

                    DataHelper.Save(wa.TableName, wa.ToBsonDocument());
                }

                List<IMongoQuery> qs = new List<IMongoQuery>();
                IMongoQuery qpip = Query.Null;
                qs.Add(Query.EQ("WellName", wa.WellName));
                qs.Add(Query.EQ("SequenceId", wa.UARigSequenceId));
                qs.Add(Query.EQ("ActivityType", activityType));
                qpip = Query.And(qs);
                WellPIP pip = wellPIPs.FirstOrDefault(x => x._id.Equals(qpip));//WellPIP.Get<WellPIP>(qpip);
                List<PIPElement> PIPElement = new List<PIPElement>();
                try
                {
                    if (pip == null)
                    {
                        pip = new WellPIP();
                    }

                    if (pip.Elements == null)
                        PIPElement = new List<PIPElement>();
                    else if (pip.Elements.Count == 0)
                        PIPElement = new List<PIPElement>();
                    else
                        PIPElement = pip.Elements;
                }
                catch (Exception e)
                {
                    PIPElement = new List<PIPElement>();
                }

                //add CRElements
                List<PIPElement> CRElements = new List<PIPElement>();
                try
                {

                    if (pip.CRElements == null)
                        CRElements = new List<PIPElement>();
                    else if (pip.CRElements.Count == 0)
                        CRElements = new List<PIPElement>();
                    else
                        CRElements = pip.CRElements;
                }
                catch (Exception e)
                {
                    CRElements = new List<PIPElement>();
                }

                if (phase != null)
                {
                    //if (a.PhSchedule.InRange(dt)) {
                    Dictionary<string, string> variables = new Dictionary<string, string>();
                    WellActivityUpdateMonthly wau = null;
                    if (waus.Count > 0)
                    {
                        wau = waus.FirstOrDefault(
                            e => e.WellName.Equals(wa.WellName)
                            && (e.Phase == null ? "" : e.Phase.ActivityType).Equals(phase.ActivityType)
                            && e.SequenceId.Equals(wa.UARigSequenceId)
                            && e.UpdateVersion.Equals(dt));
                    }
                    if (wau == null)
                    {
                        wau = new WellActivityUpdateMonthly();
                        //wau._id = String.Format("{0}_{1:yyyyMMdd}", wa.UARigSequenceId, dt);
                        //wau.Country = "USA";
                        wau.AssetName = wa.AssetName;
                        wau.NewWell = false;
                        wau.WellName = wa.WellName;
                        wau.SequenceId = wa.UARigSequenceId;

                        wau.Phase = phase;

                        wau.Phase.ActivityType = phase.ActivityType;
                        wau.Phase.ActivityDesc = phase.ActivityDesc;
                        wau.Phase.PhaseNo = phase.PhaseNo;
                        wau.Phase.BaseOP = phase.BaseOP;
                        wau.Phase.EventCreatedFrom = "FinancialCalendar";
                        wau.Status = "In-Progress";
                        wau.UpdateVersion = dt;
                        wau.Elements = PIPElement;
                        wau.CRElements = CRElements;

                        //get before
                        var qBefore = new List<IMongoQuery>();
                        qBefore.Add(Query.EQ("WellName", wau.WellName));
                        qBefore.Add(Query.EQ("Phase.ActivityType", wau.Phase.ActivityType));
                        qBefore.Add(Query.EQ("SequenceId", wau.SequenceId));
                        qBefore.Add(Query.LT("UpdateVersion", wau.UpdateVersion));
                        var before = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(Query.And(qBefore), SortBy.Descending("UpdateVersion"));

                        if (before != null)
                        {
                            // add from latest wau
                            wau.Site = before.Site;
                            wau.Project = before.Project;
                            wau.WellType = before.WellType;
                            wau.Objective = before.Objective;
                            wau.Contractor = before.Contractor;
                            wau.RigSuperintendent = before.RigSuperintendent;
                            wau.Company = before.Company;
                            wau.EventStartDate = before.EventStartDate;
                            wau.EventType = before.EventType;
                            wau.WorkUnit = before.WorkUnit;
                            wau.OriginalSpudDate = before.OriginalSpudDate;
                        }
                        else
                        {
                            // add from wa
                            wau.Project = wa.ProjectName;
                            wau.EventType = wa.ActivityType;
                            wau.EventStartDate = phase.PhSchedule.Start;
                        }
                        wau.Calc();
                        // reset LE 
                        wau.CurrentWeek = new WellDrillData();

                        //wau.Phase.LE = new WellDrillData();
                        wau.UpdateVersion = StartDate;
                        wau.InitiateBy = pUser;
                        wau.InitiateDate = Tools.ToUTC(DateTime.Now);
                        //DataHelper.Save(wau.TableName, wau.ToBsonDocument());

                        //check apakah punya activity weekly update dan valid LS
                        var weeklyActivityUpdate = WellActivityUpdate.Get<WellActivityUpdate>(
                            Query.And(
                                Query.EQ("WellName", wau.WellName),
                                Query.EQ("SequenceId", wau.SequenceId),
                                Query.EQ("Phase.ActivityType", wau.Phase.ActivityType)
                            )
                            );

                        var isValidLS = wau.Phase.PhSchedule.Start > Tools.DefaultDate && wau.Phase.PhSchedule.Finish > Tools.DefaultDate;

                        if (weeklyActivityUpdate == null && isValidLS && !wau.Phase.ActivityType.Contains("FLOWBACK"))
                        {
                            wau.Save(references: new string[] { });//"initiateProcess"

                            //if (withSendMail)
                            //{
                            //    variables.Add("WellName", wa.WellName);
                            //    variables.Add("ActivityType", phase.ActivityType);
                            //    variables.Add("UpdateVersion", String.Format("{0:dd-MMM-yyyy}", dt));
                            //    variables.Add("DueDate", String.Format("{0:dd-MMM-yyyy}", dt));
                            //    variables.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("index", "weeklyreport"));
                            //    Email.Send("WRInitiate",
                            //        persons.Select(d => d.Email).ToArray(),
                            //        ccs.Count == 0 ? null : ccs.Select(d => d.Email).ToArray(),
                            //        variables: variables,
                            //        developerModeEmail: pEmail);
                            //    waus.Add(wau);
                            //}
                        }

                    }
                    else
                    {
                        //wau.Status = "In-Progress";
                        //emailSent = Email.Send("WRInitiate",
                        //    new string[] { teamLeadEmail, leadEngineerEmail },
                        //    variables: variables,
                        //    developerModeEmail: WebTools.LoginUser.Email);
                        //wau.Save();
                    }
                    //}
                }
            }

            #region comment
            //IMongoQuery q = Query.Null;
            //List<IMongoQuery> qs = new List<IMongoQuery>();
            //qs.Add(Query.EQ("UpdateVersion", dt));
            //if (qs.Count > 0) q = Query.And(qs);
            //List<WellActivityUpdateMonthly> waus = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q);
            ////List<WellActivity> was = WellActivity.Populate<WellActivity>(qWellAc);
            //foreach (var wa in was)
            //{
            //    foreach (var a in wa.Phases)
            //    {
            //        if (a.PhSchedule.InRange(dt))
            //        {
            //            JsonResult emailSent = null;
            //            Dictionary<string, string> variables = new Dictionary<string, string>();
            //            variables.Add("WellName", wa.WellName);
            //            variables.Add("ActivityType", a.ActivityType);
            //            variables.Add("UpdateVersion", String.Format("{0:dd-MMM-yyyy}", dt));
            //            variables.Add("DueDate", String.Format("{0:dd-MMM-yyyy}", dt));
            //            variables.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("index", "weeklyreport"));
            //            string teamLeadEmail = a.TeamLead == null ? "" : a.TeamLead.Email==null ? "" : a.TeamLead.Email;
            //            string leadEngineerEmail = a.LeadEngineer == null ? "" : a.LeadEngineer.Email == null ? "" : a.LeadEngineer.Email;

            //            var wau = waus.FirstOrDefault(
            //                e => e.WellName.Equals(wa.WellName)
            //                && e.Phase.ActivityType.Equals(a.ActivityType)
            //                && e.SequenceId.Equals(wa.UARigSequenceId)
            //                && e.UpdateVersion.Equals(dt));
            //            if (wau == null)
            //            {
            //                wau = new WellActivityUpdateMonthly();
            //                wau._id = String.Format("{0}_{1:yyyyMMdd}", wa.UARigSequenceId, dt);
            //                //wau.Country = "USA";
            //                wau.AssetName = wa.AssetName;
            //                wau.NewWell = false;
            //                wau.WellName = wa.WellName;
            //                wau.SequenceId = wa.UARigSequenceId;
            //                wau.Phase.ActivityType = a.ActivityType;
            //                wau.Phase.ActivityDesc = a.ActivityDesc;
            //                wau.Phase.PhaseNo = a.PhaseNo;
            //                wau.Status = "In-Progress";
            //                wau.UpdateVersion = dt;
            //                wau.Save();
            //                emailSent = Email.Send("WRInitiate", 
            //                    new string[] {teamLeadEmail, leadEngineerEmail},
            //                    variables: variables,
            //                    developerModeEmail:WebTools.LoginUser.Email);
            //                waus.Add(wau);
            //            }
            //            else
            //            {
            //                wau.Status = "In-Progress";
            //                emailSent = Email.Send("WRInitiate",
            //                    new string[] { teamLeadEmail, leadEngineerEmail },
            //                    variables: variables,
            //                    developerModeEmail: WebTools.LoginUser.Email);
            //                wau.Save();
            //            }
            //        }
            //    }
            //};
            #endregion

            return waus;
        }
        #endregion

        public JsonResult SaveWAU(WellActivityUpdate model)
        {

            return MvcResultInfo.Execute(null, (BsonDocument d) =>
            {
                model.Actual.Cost *= 1000000;

                //var existing = 
                //if (existing != null)
                //{
                //    model._id = existing._id;
                //}
                //else
                //{
                //    model._id = SequenceNo.Get(new WellActivityUpdate().TableName).ClaimAsInt();
                //}
                //model.CurrentWeek.Cost = model.CurrentWeek.Cost * 1000000;

                List<PIPElement> ListElement = new List<PIPElement>();
                List<PIPAllocation> ListAllocation = new List<PIPAllocation>();
                PIPAllocation Allocation = new PIPAllocation();
                WellActivityUpdate NewWau = new WellActivityUpdate();
                WellActivityUpdate wau = WellActivityUpdate.Get<WellActivityUpdate>(model._id);
                WellActivityUpdate temp = WellActivityUpdate.Get<WellActivityUpdate>(model._id);

                NewWau = model;

                foreach (var x in model.Elements)
                {
                    PIPElement Element = new PIPElement();
                    Element = x;
                    PIPElement pe = wau.Elements.Where(a => a.ElementId.Equals(x.ElementId)).FirstOrDefault();
                    List<PIPAllocation> NewAllocation = new List<PIPAllocation>();

                    if (pe != null)
                    {
                        if (x.CostCurrentWeekImprovement != pe.CostCurrentWeekImprovement ||
                                x.DaysCurrentWeekImprovement != pe.DaysCurrentWeekImprovement ||
                                x.CostCurrentWeekRisk != pe.CostCurrentWeekRisk ||
                                x.DaysCurrentWeekRisk != pe.DaysCurrentWeekRisk)
                        {
                            //NewAllocation = null;
                            double TotalDays = (pe.Period.Finish - pe.Period.Start).TotalDays;
                            double diff = Tools.Div(TotalDays, 30);
                            var mthNumber = Math.Ceiling(diff);
                            for (var mthIdx = 0; mthIdx < mthNumber; mthIdx++)
                            {
                                var dt = pe.Period.Start.AddMonths(mthIdx);
                                if (dt > pe.Period.Finish) dt = pe.Period.Finish;
                                NewAllocation.Add(new PIPAllocation
                                {
                                    AllocationID = mthIdx + 1,
                                    Period = dt,
                                    CostPlanImprovement = Math.Round(Tools.Div(pe.CostPlanImprovement, mthNumber), 1),
                                    CostPlanRisk = Math.Round(Tools.Div(pe.CostPlanRisk, mthNumber), 1),
                                    DaysPlanImprovement = Math.Round(Tools.Div(pe.DaysPlanImprovement, mthNumber), 1),
                                    DaysPlanRisk = Math.Round(Tools.Div(pe.DaysPlanRisk, mthNumber), 1),
                                    LEDays = Math.Round(Tools.Div(x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk, mthNumber), 1),
                                    LECost = Math.Round(Tools.Div(x.CostCurrentWeekImprovement + x.CostCurrentWeekRisk, mthNumber), 1)
                                });
                            }
                        }
                        else
                        {
                            NewAllocation = pe.Allocations;
                        }

                        //decide is positive or negative?
                        Element.isPositive = true;
                        if ((Element.CostPlanImprovement + Element.CostPlanRisk) - (Element.CostCurrentWeekImprovement + Element.CostCurrentWeekRisk) >= 0)
                        {
                            Element.isPositive = false;
                        }

                        Element.Allocations = NewAllocation;
                        Element.Period = pe.Period;
                        Element.AssignTOOps.AddRange(x.AssignTOOps);
                        ListElement.Add(Element);
                    }
                    else
                    {
                        ListElement.Add(x);
                    }
                }
                NewWau.Elements = ListElement;

                NewWau.Save(references: new string[] { "", "CalcLeSchedule", "updateLeStatus" });

                string url = (HttpContext.Request).Path.ToString();
                WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                    LogType.Update, NewWau.TableName, url, temp.ToBsonDocument(), NewWau.ToBsonDocument());

                //string url = (HttpContext.Request).Path.ToString();
                //WEISUserActivityLog.SaveUserActivityLog(WebTools.LoginUser.UserName, WebTools.LoginUser.Email,
                //    LogType.Insert, NewWau.TableName, url, NewWau.ToBsonDocument(), null);

                return NewWau;
            });

        }

        public ActionResult Email()
        {
            return View();
        }

        public JsonResult RepairCRElementSingleWell(DateTime date, string wellname, string activitytype, string sequenceid)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                date = Tools.ToUTC(date, true);
                if (date.Day != 1)
                {
                    return new Exception("Date haves to be in early month");
                }
                var monthYear = Tools.ToUTC(new DateTime(date.Year, date.Month, 1));

                var prevMonthlyReports = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(
                    Query.And(
                    //Query.EQ("", date.AddMonths(-1)),
                        Query.EQ("WellName", wellname),
                        Query.EQ("Phase.ActivityType", activitytype),
                        Query.EQ("SequenceId", sequenceid)
                        )
                    );

                if (!prevMonthlyReports.Any())
                {
                    var rawWellActivityIds = new MonthlyReportController().GetActivitiesBasedOnMonth(monthYear, wellname, activitytype, sequenceid);
                    var wellActivityIds = rawWellActivityIds.Select(d => d._id + "|" + d.ActivityType).ToList();
                    new MonthlyReportController().MonthlyReportInitiate(date, "", wellActivityIds, false, true);
                }
                else
                {
                    prevMonthlyReports = prevMonthlyReports.OrderByDescending(x => x.UpdateVersion).Take(1).ToList();//
                    foreach (var d in prevMonthlyReports)
                    {
                        var weeklyActivityUpdate = WellActivityUpdate.Get<WellActivityUpdate>(
                            Query.And(
                                Query.EQ("WellName", d.WellName),
                                Query.EQ("SequenceId", d.SequenceId),
                                Query.EQ("Phase.ActivityType", d.Phase.ActivityType)
                                )
                            );

                        d._id = null;
                        d.UpdateVersion = date;
                        d.Status = "In-Progress";
                        d.InitiateDate = Tools.ToUTC(DateTime.Now);
                        if (d.CRElements.Any())
                        {
                            d.CRElements = new MonthlyReportController().GetCRElementsFromExisting(d.WellName, d.SequenceId, d.Phase.ActivityType);
                        }
                        d.Save();
                    };
                }

                return null;
            });
        }


        public JsonResult RepairCRElementsAllLatestWR()
        {

            return MvcResultInfo.Execute(() =>
            {

                try
                {

                    var actv = WellActivityUpdateMonthly.Get<WellActivityUpdateMonthly>(null, sort: SortBy.Descending("UpdateVersion"));

                    if (actv != null)
                    {
                        var updateVers = actv.UpdateVersion;
                        var latestdata = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.EQ("UpdateVersion", updateVers));

                        foreach (var t in latestdata)
                        {
                            if (!t.CRElements.Any())
                            {
                                var pip = WellPIP.Get<WellPIP>(
                                    Query.And(
                                    Query.EQ("WellName", t.WellName),
                                    Query.EQ("ActivityType", t.Phase.ActivityType),
                                    Query.EQ("SequenceId", t.SequenceId)
                                    ));
                                if (pip != null)
                                {
                                    if (pip.CRElements.Any())
                                    {
                                        t.CRElements = new List<PIPElement>();
                                        t.CRElements = pip.CRElements;
                                    }
                                    DataHelper.Save("WEISWellActivityUpdatesMonthly", t.ToBsonDocument());
                                }
                            }
                        }

                    }
                    return "OK";
                }
                catch (Exception ex)
                {
                    return ex.Message + " " + ex.InnerException;
                }

            });


        }



        public JsonResult ChangeWAUElement()
        {
            return MvcResultInfo.Execute(() =>
            {
                var qs = Query.And(
                        Query.EQ("WellName", "RAM POWELL A7"),
                        Query.EQ("Phase.ActivityType", "WHOLE COMPLETION EVENT")
                    );
                var wau = WellActivityUpdate.Get<WellActivityUpdate>(qs);
                if (wau != null)
                {
                    wau.Delete();
                    wau.Phase.ActivityType = "WHOLE DRILLING EVENT";
                    wau.Phase.PhaseNo = 1;
                    wau.Save();
                }
                return "OK";
            });
        }
        public JsonResult ChangeRigEscalation()
        {
            return MvcResultInfo.Execute(() =>
            {
                var rr = RigEscalation.Populate<RigEscalation>();
                foreach (var r in rr)
                {
                    r.Value = 1;
                    r.Save();
                }
                return "OK";
            });
        }
        public JsonResult GetFundingType()
        {
            return MvcResultInfo.Execute(() =>
            {
                var get = DataHelper.Populate<WellActivity>("WEISWellActivities")
                            .SelectMany(d => d.Phases).Where(d => d.FundingType != null).GroupBy(d => d.FundingType).Select(d => d.Key).OrderBy(d => (d.Equals("n/a") ? "" : d));
                return get;
            });
        }

        public JsonResult GenerateRigRatesNew()
        {
            return MvcResultInfo.Execute(() =>
            {
                RigRatesNew rrn = new RigRatesNew();
                rrn.Title = "Auger";
                rrn.Values = new List<RigRateValue>();


                RigRateValue r = new RigRateValue();
                r.Period = new DateRange(new DateTime(2015, 1, 1), new DateTime(2015, 12, 31));
                r.Value = 300000;
                r.Type = "ACTIVE";
                r.ValueType = "Absolute";

                rrn.Values.Add(r);

                r = new RigRateValue();
                r.Period = new DateRange(new DateTime(2015, 1, 1), new DateTime(2015, 12, 31));
                r.Value = 100000;
                r.Type = "IDLE";
                r.ValueType = "Absolute";

                rrn.Values.Add(r);

                r = new RigRateValue();
                r.Period = new DateRange(new DateTime(2016, 1, 1), new DateTime(2017, 8, 1));
                r.Value = 250000;
                r.Type = "ACTIVE";
                r.ValueType = "Absolute";

                rrn.Values.Add(r);

                r = new RigRateValue();
                r.Period = new DateRange(new DateTime(2016, 1, 1), new DateTime(2017, 8, 1));
                r.Value = 80000;
                r.Type = "IDLE";
                r.ValueType = "Absolute";

                rrn.Values.Add(r);

                r = new RigRateValue();
                r.Period = new DateRange(new DateTime(2017, 8, 2), new DateTime(2018, 12, 31));
                r.Value = 350000;
                r.Type = "ACTIVE";
                r.ValueType = "Absolute";

                rrn.Values.Add(r);

                r = new RigRateValue();
                r.Period = new DateRange(new DateTime(2017, 8, 2), new DateTime(2018, 12, 31));

                r.Value = 70000;
                r.Type = "IDLE";
                r.ValueType = "Absolute";

                rrn.Values.Add(r);

                rrn.Save();
                return "OK";
            });
        }

        public JsonResult InitiateAllToOP14()
        {
            return MvcResultInfo.Execute(() =>
            {

                var wellactv = WellActivity.Populate<WellActivity>();

                var pips = WellPIP.Populate<WellPIP>();

                List<string> ignoiring = new List<string>();
                ignoiring.Add("ignorewau");
                ignoiring.Add("ignoreresetcrelement");


                foreach (var pip in pips)
                {
                    foreach (var ele in pip.Elements)
                    {
                        ele.AssignTOOps = new List<string>();
                        ele.AssignTOOps.Add("OP14");
                    }
                    pip.Save(references: ignoiring.ToArray());
                }
                ignoiring = new List<string>();

                ignoiring.Add("ignorewellpip");
                ignoiring.Add("ignoreInitLE");

                // refresh weekly
                var wau = WellActivityUpdate.Populate<WellActivityUpdate>();

                foreach (var w in wau)
                {
                    foreach (var ele in w.Elements)
                    {
                        ele.AssignTOOps = new List<string>();
                        ele.AssignTOOps.Add("OP14");
                    }

                    var t = wellactv.Where(x =>
                        x.WellName.Equals(w.WellName) &&
                        x.Phases.Where(y => y.ActivityType.Equals(w.Phase.ActivityType)).Count() > 0 &&
                        x.Phases.Where(y => y.PhaseNo.Equals(w.Phase.PhaseNo)).Count() > 0);

                    w.Phase.BaseOP = new List<string>();
                    if (t.Any())
                    {
                        if (t.FirstOrDefault().Phases.Where(y => y.ActivityType.Equals(w.Phase.ActivityType) && y.PhaseNo.Equals(w.Phase.PhaseNo)).Any())
                        {
                            var yu = t.FirstOrDefault().Phases.Where(y => y.ActivityType.Equals(w.Phase.ActivityType) && y.PhaseNo.Equals(w.Phase.PhaseNo));
                            w.Phase.BaseOP = yu.FirstOrDefault().BaseOP == null ? new List<string>() : yu.FirstOrDefault().BaseOP;
                        }
                    }
                    w.Save(references: ignoiring.ToArray());
                }

                return "OK";
            });
        }

        public JsonResult SyncOPPlan()
        {
            return MvcResultInfo.Execute(() =>
            {
                BizPlan.SyncFromWellPlan();
                return "OK";
            });
        }

        public JsonResult DistinctBaseOPInWA()
        {
            return MvcResultInfo.Execute(() =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    if (wa.Phases.Any())
                    {
                        var newPhases = new List<WellActivityPhase>();
                        foreach (var ph in wa.Phases)
                        {
                            var newPh = ph;
                            if (ph.BaseOP.Any())
                            {
                                newPh.BaseOP = ph.BaseOP.Distinct().ToList();
                            }
                            newPhases.Add(newPh);
                        }
                        var newWA = wa;
                        newWA.Phases = newPhases;
                        //newWA.Save();
                        DataHelper.Save(newWA.TableName, newWA.ToBsonDocument());
                    }
                }
                return "OK";
            });
        }

        public JsonResult RemoveCrazyMasterData()
        {
            return MvcResultInfo.Execute(() =>
            {
                SaveInitialEmpytMaster();
                return "OK";
            });
        }

        public static void SaveInitialEmpytMaster()
        {
            var emptyDocs = new BsonDocument();
            emptyDocs.Set("_id", "");
            DataHelper.Populate("WEISRegions").ForEach(d =>
            {
                var id = d.Get("_id");
                if (!id.GetType().Name.Equals("BsonString"))
                    DataHelper.Delete("WEISRegions", Query.EQ("_id", id));
            });
            DataHelper.Save("WEISRegions", emptyDocs);

            DataHelper.Populate("WEISRigTypes").ForEach(d =>
            {
                var id = d.Get("_id");
                if (!id.GetType().Name.Equals("BsonString"))
                    DataHelper.Delete("WEISRigTypes", Query.EQ("_id", id));
            });
            DataHelper.Save("WEISRigTypes", emptyDocs);

            DataHelper.Populate("WEISRigNames").ForEach(d =>
            {
                var id = d.Get("_id");
                if (!id.GetType().Name.Equals("BsonString"))
                    DataHelper.Delete("WEISRigNames", Query.EQ("_id", id));
            });
            DataHelper.Save("WEISRigNames", emptyDocs);

            DataHelper.Populate("WEISOperatingUnits").ForEach(d =>
            {
                var id = d.Get("_id");
                if (!id.GetType().Name.Equals("BsonString"))
                    DataHelper.Delete("WEISOperatingUnits", Query.EQ("_id", id));
            });
            DataHelper.Save("WEISOperatingUnits", emptyDocs);

            DataHelper.Populate("WEISPerformanceUnits").ForEach(d =>
            {
                var id = d.Get("_id");
                if (!id.GetType().Name.Equals("BsonString"))
                    DataHelper.Delete("WEISPerformanceUnits", Query.EQ("_id", id));
            });
            DataHelper.Save("WEISPerformanceUnits", emptyDocs);

            DataHelper.Populate("WEISAssetNames").ForEach(d =>
            {
                var id = d.Get("_id");
                if (!id.GetType().Name.Equals("BsonString"))
                    DataHelper.Delete("WEISAssetNames", Query.EQ("_id", id));
            });
            DataHelper.Save("WEISAssetNames", emptyDocs);

            DataHelper.Populate("WEISProjectNames").ForEach(d =>
            {
                var id = d.Get("_id");
                if (!id.GetType().Name.Equals("BsonString"))
                    DataHelper.Delete("WEISProjectNames", Query.EQ("_id", id));
            });
            DataHelper.Save("WEISProjectNames", emptyDocs);
        }

        public JsonResult SyncWAULE()
        {
            return MvcResultInfo.Execute(() =>
            {
                var waus = WellActivityUpdate.Populate<WellActivityUpdate>(Query.EQ("Status", "In-Progress"));
                foreach (var wau in waus)
                {
                    wau.Calc();
                    wau.Save();
                }

                return "OK";
            });
        }


        public void BindTQandBICFromPhaseInfo2()
        {
            var ts = WellActivity.Populate<WellActivity>();
            foreach (var t in ts)
            {
                bool isSave = false;
                foreach (var y in t.Phases)
                {

                    if (t.WellName != null && y.ActivityType != null && t.RigName != null && t.UARigSequenceId != null && t.PhaseNo != null)
                    {
                        var r = Query.And(
                      Query.EQ("WellName", t.WellName),
                      Query.EQ("ActivityType", y.ActivityType),
                      Query.EQ("RigName", t.RigName),
                      Query.EQ("OPType", "OP15")
                      );

                        var x = WellActivityPhaseInfo.Get<WellActivityPhaseInfo>(r, sort: SortBy.Descending("LastUpdate"));

                        if (x != null)
                        {
                            isSave = true;
                            y.TQ.Days = x.TQTreshold == null ? 0 : x.TQTreshold;
                            y.BIC.Days = x.BICTreshold == null ? 0 : x.BICTreshold;
                        }
                    }

                }
                if (isSave)
                    DataHelper.Save("WEISWellActivities", t.ToBsonDocument());
            }
        }


        #region Biz Plan Activity
        // 1. Move WellPlan data to Biz Plan Activity 
        public JsonResult SyncBizPlanActivityFromWellPlan()
        {
            return MvcResultInfo.Execute(() =>
            {
                SetMaturityRiskNewVersionVoid();

                /* Take TQ and BIC from Phase Info and update to WellPlan */
                BindTQandBICFromPhaseInfo2();

                /* 
                 * Delete RFM Object ID
                    Add WEISOPs [14,15,16]
                    Delete and Reset to OP16
                    Add learning curve
                    Set US to All RFM -- countryRefferenceFactor
                    addDefaultVisibleCurrencyInBizPlan 
                 */
                SetDefaultOPForRFMVoid();

                BatchMacroDataWithBaseOP();
                GenerateMasterCountryAndRFM();
                BizPlan.SyncFromWellPlan();
                return "OK";
            });
        }

        // 2. For Existing Data --- Execute this GetLatestUploadedOP and inject to 
        public JsonResult GetLatestUploadedOPandInjectToPhaseInfo()
        {
            return MvcResultInfo.Execute(() =>
            {
                BizPlan.GetLatestUploadedOP();
                return "OK";
            });
        }
        //3. Sync Shell Share taken from OPPlan's WorkingInterest
        public JsonResult SyncShellShare()
        {
            ResultInfo ri = new ResultInfo();
            var WellName = "";
            var SequenceId = "";
            var RigName = "";
            try
            {
                var wb = new WaterfallBase();
                wb.DateFinish = "2030-12-31";
                wb.DateFinish2 = "2015-12-31";
                wb.DateRelation = "OR";
                wb.DateStart = "2015-01-01";
                wb.DateStart2 = "2015-01-01";
                wb.PeriodBase = "By Last Estimate";
                wb.PeriodView = "Fiscal View";
                var email = WebTools.LoginUser.Email;
                var bps = wb.GetActivitiesForBizPlan(email);

                foreach (var bp in bps)
                {
                    WellName = bp.WellName;
                    RigName = bp.RigName;
                    SequenceId = bp.UARigSequenceId;

                    var qs = new List<IMongoQuery>();
                    qs.Add(Query.EQ("WellName", bp.WellName));
                    qs.Add(Query.EQ("UARigSequenceId", bp.UARigSequenceId));
                    qs.Add(Query.EQ("RigName", bp.RigName));
                    var q = Query.And(qs);
                    var wa = WellActivity.Get<WellActivity>(q);
                    if (wa != null)
                    {
                        var workingInterest = wa.WorkingInterest;
                        if (workingInterest <= 1)
                        {
                            bp.ShellShare = workingInterest * 100;
                        }
                        else
                        {
                            bp.ShellShare = workingInterest;
                        }
                    }
                    bp.Save();
                }
            }
            catch (Exception e)
            {
                ri.Data = "WellName=" + WellName + "|RigName=" + RigName + "|SequenceId=" + SequenceId;
                ri.PushException(e);
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult ResetMonthlyLEData()
        {
            return MvcResultInfo.Execute(() =>
            {
                DataHelper.Delete("WEISFinancialCalendar");
                DataHelper.Delete("WEISWellActivityUpdatesMonthly");
                return "OK";

            });
        }

        public JsonResult LoadDefaultAlias()
        {
            return MvcResultInfo.Execute(() =>
            {
                MasterAlias.LoadDefaultAlias();

                return "OK";

            });
        }
        #endregion

        // refresh data wellplan with latest weekly report
        public JsonResult UpdateLatestStatusOnAllWellPlanWhichHaveWeeklyReport()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                WellActivityUpdate.UpdateLatestStatusOnAllWellPlanWhichHaveWeeklyReport();

                return true;
            });
        }

        public JsonResult SyncAssignedToOPForCRElements()
        {
            return MvcResultInfo.Execute(() =>
            {
                var was = WellActivity.Populate<WellActivity>();
                //var WellPIPs = WellPIP.Populate<WellPIP>();
                //var waus = WellActivityUpdate.Populate<WellActivityUpdate>();
                //var waums = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>();
                var rignames = was.GroupBy(x => x.RigName).Select(x => x.Key).ToList();
                foreach (var r in rignames)
                {
                    var WellNames = was.Where(x => x.RigName.Equals(r)).Select(x => x.WellName).ToList();
                    var WellNamePIP = r + "_CR";
                    var pip = WellPIP.Get<WellPIP>(Query.EQ("WellName", WellNamePIP));
                    if (pip != null)
                    {
                        //get data element
                        var originalCRElements = pip.Elements;

                        //sync WellPIPs
                        var dataWellPIP = WellPIP.Populate<WellPIP>(Query.In("WellName", new BsonArray(WellNames.ToArray())));
                        if (dataWellPIP.Any())
                        {
                            foreach (var wp in dataWellPIP)
                            {
                                var cres = wp.CRElements;
                                if (cres.Any())
                                {
                                    foreach (var cre in cres)
                                    {
                                        var matchedCRE = originalCRElements.Where(x => x.ElementId.Equals(cre.ElementId) && x.Title.Trim().Equals(cre.Title.Trim())).FirstOrDefault();
                                        if (matchedCRE != null)
                                        {
                                            cre.AssignTOOps = matchedCRE.AssignTOOps;
                                        }
                                    }
                                }
                                DataHelper.Save(wp.TableName, wp.ToBsonDocument());
                            }
                        }

                        //sync WAUs
                        var dataWAUs = WellActivityUpdate.Populate<WellActivityUpdate>(Query.In("WellName", new BsonArray(WellNames.ToArray())));
                        if (dataWAUs.Any())
                        {
                            foreach (var wp in dataWAUs)
                            {
                                var cres = wp.CRElements;
                                if (cres.Any())
                                {
                                    foreach (var cre in cres)
                                    {
                                        var matchedCRE = originalCRElements.Where(x => x.ElementId.Equals(cre.ElementId) && x.Title.Trim().Equals(cre.Title.Trim())).FirstOrDefault();
                                        if (matchedCRE != null)
                                        {
                                            cre.AssignTOOps = matchedCRE.AssignTOOps;
                                        }
                                    }
                                }
                                DataHelper.Save(wp.TableName, wp.ToBsonDocument());
                            }
                        }

                        //sync WAUMs
                        var dataWAUMs = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.In("WellName", new BsonArray(WellNames.ToArray())));
                        if (dataWAUMs.Any())
                        {
                            foreach (var wp in dataWAUMs)
                            {
                                var cres = wp.CRElements;
                                if (cres.Any())
                                {
                                    foreach (var cre in cres)
                                    {
                                        var matchedCRE = originalCRElements.Where(x => x.ElementId.Equals(cre.ElementId) && x.Title.Trim().Equals(cre.Title.Trim())).FirstOrDefault();
                                        if (matchedCRE != null)
                                        {
                                            cre.AssignTOOps = matchedCRE.AssignTOOps;
                                        }
                                    }
                                }
                                DataHelper.Save(wp.TableName, wp.ToBsonDocument());
                            }
                        }
                    }
                }
                return "OK";
            });
        }

        public JsonResult AddUser()
        {

            /*
             * eaciit
	            navdeep
	            hector.romero
	            shivang.khandelwal
	            local.admin
	            yoga.bangkit
             * 
             *  wengyuen.chia
                sara.chong
                keewooi.tang
             */
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {

                //string[] users = new string[] { "eaciit", "navdeep", "hector.romero", "local.admin", "yoga.bangkit", 
                //    "ian.pirsch" };

                //DataHelper.Delete("_Users");
                //var usx = DataHelper.Populate("Users");
                //DataHelper.Save("_Users", usx.ToList());

                //DataHelper.Delete("Users", Query.Not(Query.In("UserName", new BsonArray(users))));
                //DataHelper.Delete("Users", Query.EQ("UserName", "akash.jain"));
                var yyy = DataHelper.Get("Users", Query.EQ("UserName", "eaciit"));

                if (yyy != null)
                {
                    // create user 
                    string username = "akash.jain";
                    string email = "akash.jain@eaciit.com";
                    string fullname = "Akash Jain";
                    yyy.Set("UserName", username);
                    yyy.Set("Email", email);
                    yyy.Set("FullName", fullname);
                    yyy.Set("HasChangePassword", false);
                    yyy.Set("_id", new ObjectId());
                    DataHelper.Save("Users", yyy);
                    var xxx = WEISPerson.Get<WEISPerson>(Query.EQ("_id", "W*SA*"));
                    if (xxx != null)
                    {
                        xxx.PersonInfos.Add(new WEISPersonInfo { Email = email, FullName = fullname, LineOfBusiness = null, RoleId = "APP-SUPPORTS" });
                        xxx.Save();
                    }
                }



                return true;
            });
        }


        public JsonResult BuildEACIITDevUser()
        {

            /*
             * eaciit
	            navdeep
	            hector.romero
	            shivang.khandelwal
	            local.admin
	            yoga.bangkit
             * 
             *  wengyuen.chia
                sara.chong
                keewooi.tang
             */
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {

                string[] users = new string[] { "eaciit", "navdeep", "hector.romero", "local.admin", "yoga.bangkit", 
                    "ian.pirsch" };

                DataHelper.Delete("_Users");
                var usx = DataHelper.Populate("Users");
                DataHelper.Save("_Users", usx.ToList());

                DataHelper.Delete("Users", Query.Not(Query.In("UserName", new BsonArray(users))));

                var yyy = DataHelper.Populate("Users", Query.In("UserName", new List<BsonValue>() { "sara.chong", "keewooi.tang" }));

                if (yyy == null)
                {
                    // create user 
                    var userMgr = new IdentityUser();
                    //userMgr.Create(new IdentityUser
                    //{
                    //    UserName = "sara.chong",// username.ToLower(),
                    //    Email = email.ToLower(),
                    //    FullName = userinfo["FullName"].ToString(),
                    //    LineOfBusiness = userinfo["LineOfBusiness"].ToString(),
                    //    Enable = userinfo["Enable"].ToString() == "True",
                    //    ADUser = userinfo["ADUser"].ToString() == "True",
                    //    ConfirmedAtUtc = Tools.ToUTC(DateTime.Now),
                    //    HasChangePassword = false
                    //}, password);
                }



                return true;
            });
        }


        public JsonResult BuildEACIITShellDevUser()
        {

            /*
             * eaciit
	            navdeep
	            hector.romero
	            shivang.khandelwal
	            local.admin
	            yoga.bangkit
             * 
             *  wengyuen.chia
                sara.chong
                keewooi.tang
             */
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {


                string[] users = new string[] { "eaciit", "navdeep", "hector.romero", "local.admin", "yoga.bangkit", 
                    "wengyuen.chia", "sara.chong", "keewooi.tang", "ian.pirsch" };

                DataHelper.Delete("_Users");
                var usx = DataHelper.Populate("Users");
                DataHelper.Save("_Users", usx.ToList());

                DataHelper.Delete("Users", Query.Not(Query.In("UserName", new BsonArray(users))));

                var yyy = DataHelper.Populate("Users", Query.In("UserName", new List<BsonValue>() { "sara.chong", "keewooi.tang" }));

                if (yyy == null)
                {
                    // create user 
                    var userMgr = new IdentityUser();
                    //userMgr.Create(new IdentityUser
                    //{
                    //    UserName = "sara.chong",// username.ToLower(),
                    //    Email = email.ToLower(),
                    //    FullName = userinfo["FullName"].ToString(),
                    //    LineOfBusiness = userinfo["LineOfBusiness"].ToString(),
                    //    Enable = userinfo["Enable"].ToString() == "True",
                    //    ADUser = userinfo["ADUser"].ToString() == "True",
                    //    ConfirmedAtUtc = Tools.ToUTC(DateTime.Now),
                    //    HasChangePassword = false
                    //}, password);
                }



                return true;
            });
        }

        public JsonResult BuildProdMenu()
        {

            /*
             * Delete Business Plan, Multi Simulation, Admin - Business Plan
             */
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                DataHelper.Delete("_Main_Menu");
                var usx = DataHelper.Populate("Main_Menu");
                DataHelper.Save("_Main_Menu", usx.ToList());


                DataHelper.Delete("Main_Menu", Query.EQ("Title", "Business Plan"));
                DataHelper.Delete("Main_Menu", Query.EQ("Title", "Multi Simulation"));

                var admin = DataHelper.Get<Menu>("Main_Menu", Query.EQ("Title", "Administration"));

                var bizplanmaster = admin.Submenus.Where(x => x.Title.Equals("Business Plan"));
                if (bizplanmaster.Any())
                {
                    admin.Submenus.Remove(bizplanmaster.FirstOrDefault());
                    DataHelper.Save("Main_Menu", admin.ToBsonDocument());
                }


                return true;
            });
        }
        public static Menu setId(Menu menu, int currentId, out int newId)
        {
            var newI = currentId + 1;
            int outId = 0;
            menu._id = "M" + (newI).ToString("D5");
            if (menu.Submenus.Any())
            {
                int maxfromloop = 0;
                foreach (var t in menu.Submenus)
                {
                    setId(t, newI, out outId);
                    newI = outId;
                    maxfromloop = outId;
                }
                newId = maxfromloop;
            }
            else
            {
                newId = newI;
            }
            return menu;
        }

        public JsonResult RefreshMenuId()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                #region Menu Modification
                var menus = DataHelper.Populate<Menu>("Main_Menu");

                List<Menu> newMenus = new List<Menu>();
                int max = 0;

                foreach (var m in menus)
                {
                    newMenus.Add(setId(m, max, out max));
                }

                // drop and Save Main_Menu
                DataHelper.Delete("Main_Menu");
                foreach (var mi in newMenus)
                {
                    DataHelper.Save("Main_Menu", mi.ToBsonDocument());
                }

                // Save to Sequence Tabel max+1
                var setData = DataHelper.Get("SequenceNos", "Main_Menu");
                setData.Set("NextNo", max + 1);
                DataHelper.Save("SequenceNos", setData);

                var menusNew = DataHelper.Populate<Menu>("Main_Menu");
                List<Menu> menu2 = new List<Menu>();
                foreach (var yyy in menusNew)
                {

                    if (yyy.Submenus.Any())
                    {
                        foreach (var yx in yyy.Submenus)
                        {
                            if (yx.Submenus.Any())
                            {
                                foreach (var three in yx.Submenus)
                                {
                                    menu2.Add(three);
                                }
                            }
                            else
                            {
                                menu2.Add(yx);
                            }
                        }
                    }
                    else
                    {
                        menu2.Add(yyy);
                    }
                }

                int maxId = 0;
                foreach (var y in menus)
                {
                    int h = getMaxId(y);
                    if (h > maxId)
                    {
                        maxId = h;
                    }
                }

                #endregion
                return "OK";
            });
        }
        public static int getMaxId(Menu menu)
        {
            int maxNumber = 0;
            if (menu.Submenus.Any())
            {
                foreach (var t in menu.Submenus)
                {
                    int num = getMaxId(t);
                    if (maxNumber < num)
                        maxNumber = num;
                }
                return maxNumber;
            }
            else
            {
                return System.Convert.ToInt32(menu._id.Substring(1, menu._id.ToString().Length - 1));
            }
        }


        public JsonResult SyncRFMSubjectMatterData()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                #region Deepwater Model
                var DeepwaterModel = new Dictionary<string, Dictionary<string, double>>() {
	                { "Material Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0.0 },
		                { "Year_2016", -2.0 },
		                { "Year_2017", -2.0 },
		                { "Year_2018", -2.0 },
		                { "Year_2019", -2.0 },
		                { "Year_2020", -2.0 },
		                { "Year_2021", -2.0 },
		                { "Year_2022", -2.0 },
		                { "Year_2023", -2.0 },
		                { "Year_2024", -2.0 },
		                { "Year_2025", -2.0 },
		                { "Year_2026", -2.0 },
		                { "Year_2027", -2.0 },
		                { "Year_2028", -2.0 },
		                { "Year_2029", -2.0 },
		                { "Year_2030", -2.0 },
		                { "Year_2031", -2.0 },
		                { "Year_2032", -2.0 },
		                { "Year_2033", -2.0 },
		                { "Year_2034", -2.0 },
		                { "Year_2035", -2.0 },
		                { "Year_2036", -2.0 },
		                { "Year_2037", -2.0 },
		                { "Year_2038", -2.0 },
		                { "Year_2039", -2.0 },
		                { "Year_2040", -2.0 },
		                { "Year_2041", -2.0 },
		                { "Year_2042", -2.0 },
		                { "Year_2043", -2.0 },
		                { "Year_2044", -2.0 },
		                { "Year_2045", -2.0 },
		                { "Year_2046", -2.0 },
		                { "Year_2047", -2.0 },
		                { "Year_2048", -2.0 },
		                { "Year_2049", -2.0 },
		                { "Year_2050", -2.0 }
	                } },
	                { "Service Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0.0 },
		                { "Year_2016", -2.0 },
		                { "Year_2017", -2.0 },
		                { "Year_2018", -2.0 },
		                { "Year_2019", -2.0 },
		                { "Year_2020", -2.0 },
		                { "Year_2021", -2.0 },
		                { "Year_2022", -2.0 },
		                { "Year_2023", -2.0 },
		                { "Year_2024", -2.0 },
		                { "Year_2025", -2.0 },
		                { "Year_2026", -2.0 },
		                { "Year_2027", -2.0 },
		                { "Year_2028", -2.0 },
		                { "Year_2029", -2.0 },
		                { "Year_2030", -2.0 },
		                { "Year_2031", -2.0 },
		                { "Year_2032", -2.0 },
		                { "Year_2033", -2.0 },
		                { "Year_2034", -2.0 },
		                { "Year_2035", -2.0 },
		                { "Year_2036", -2.0 },
		                { "Year_2037", -2.0 },
		                { "Year_2038", -2.0 },
		                { "Year_2039", -2.0 },
		                { "Year_2040", -2.0 },
		                { "Year_2041", -2.0 },
		                { "Year_2042", -2.0 },
		                { "Year_2043", -2.0 },
		                { "Year_2044", -2.0 },
		                { "Year_2045", -2.0 },
		                { "Year_2046", -2.0 },
		                { "Year_2047", -2.0 },
		                { "Year_2048", -2.0 },
		                { "Year_2049", -2.0 },
		                { "Year_2050", -2.0 }
	                } },
	                { "CSO Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0.0 },
		                { "Year_2016", 0.0 },
		                { "Year_2017", 0.0 },
		                { "Year_2018", 0.0 },
		                { "Year_2019", 0.0 },
		                { "Year_2020", 0.0 },
		                { "Year_2021", 0.0 },
		                { "Year_2022", 0.0 },
		                { "Year_2023", 0.0 },
		                { "Year_2024", 0.0 },
		                { "Year_2025", 0.0 },
		                { "Year_2026", 0.0 },
		                { "Year_2027", 0.0 },
		                { "Year_2028", 0.0 },
		                { "Year_2029", 0.0 },
		                { "Year_2030", 0.0 },
		                { "Year_2031", 0.0 },
		                { "Year_2032", 0.0 },
		                { "Year_2033", 0.0 },
		                { "Year_2034", 0.0 },
		                { "Year_2035", 0.0 },
		                { "Year_2036", 0.0 },
		                { "Year_2037", 0.0 },
		                { "Year_2038", 0.0 },
		                { "Year_2039", 0.0 },
		                { "Year_2040", 0.0 },
		                { "Year_2041", 0.0 },
		                { "Year_2042", 0.0 },
		                { "Year_2043", 0.0 },
		                { "Year_2044", 0.0 },
		                { "Year_2045", 0.0 },
		                { "Year_2046", 0.0 },
		                { "Year_2047", 0.0 },
		                { "Year_2048", 0.0 },
		                { "Year_2049", 0.0 },
		                { "Year_2050", 0.0 }
	                } },
	                { "Inflation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 2.2 },
		                { "Year_2017", 4.244 },
		                { "Year_2018", 6.329 },
		                { "Year_2019", 8.455 },
		                { "Year_2020", 10.625 },
		                { "Year_2021", 12.837 },
		                { "Year_2022", 15.094 },
		                { "Year_2023", 17.396 },
		                { "Year_2024", 19.744 },
		                { "Year_2025", 22.138 },
		                { "Year_2026", 24.581 },
		                { "Year_2027", 27.073 },
		                { "Year_2028", 29.614 },
		                { "Year_2029", 32.207 },
		                { "Year_2030", 34.851 },
		                { "Year_2031", 37.548 },
		                { "Year_2032", 40.299 },
		                { "Year_2033", 43.105 },
		                { "Year_2034", 45.967 },
		                { "Year_2035", 48.886 },
		                { "Year_2036", 51.864 },
		                { "Year_2037", 54.901 },
		                { "Year_2038", 57.999 },
		                { "Year_2039", 61.159 },
		                { "Year_2040", 64.382 },
		                { "Year_2041", 67.67 },
		                { "Year_2042", 71.023 },
		                { "Year_2043", 74.444 },
		                { "Year_2044", 77.933 },
		                { "Year_2045", 81.491 },
		                { "Year_2046", 85.121 },
		                { "Year_2047", 88.824 },
		                { "Year_2048", 92.6 },
		                { "Year_2049", 96.452 },
		                { "Year_2050", 100.381 }
	                } },
                };
                #endregion

                #region Example Model 2
                var ExampleModel2 = new Dictionary<string, Dictionary<string, double>>() {
	                { "Material Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 5 },
		                { "Year_2016", 10 },
		                { "Year_2017", 10 },
		                { "Year_2018", 10 },
		                { "Year_2019", 10 },
		                { "Year_2020", 20 },
		                { "Year_2021", 20 },
		                { "Year_2022", 20 },
		                { "Year_2023", 20 },
		                { "Year_2024", 20 },
		                { "Year_2025", 20 },
		                { "Year_2026", 20 },
		                { "Year_2027", 20 },
		                { "Year_2028", 20 },
		                { "Year_2029", 20 },
		                { "Year_2030", 20 },
		                { "Year_2031", 20 },
		                { "Year_2032", 20 },
		                { "Year_2033", 20 },
		                { "Year_2034", 20 },
		                { "Year_2035", 20 },
		                { "Year_2036", 20 },
		                { "Year_2037", 20 },
		                { "Year_2038", 20 },
		                { "Year_2039", 20 },
		                { "Year_2040", 20 },
		                { "Year_2041", 20 },
		                { "Year_2042", 20 },
		                { "Year_2043", 20 },
		                { "Year_2044", 20 },
		                { "Year_2045", 20 },
		                { "Year_2046", 20 },
		                { "Year_2047", 20 },
		                { "Year_2048", 20 },
		                { "Year_2049", 20 },
		                { "Year_2050", 20 }
	                } },
	                { "Service Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 5 },
		                { "Year_2016", 10 },
		                { "Year_2017", 10 },
		                { "Year_2018", 10 },
		                { "Year_2019", 10 },
		                { "Year_2020", 20 },
		                { "Year_2021", 20 },
		                { "Year_2022", 20 },
		                { "Year_2023", 20 },
		                { "Year_2024", 20 },
		                { "Year_2025", 20 },
		                { "Year_2026", 20 },
		                { "Year_2027", 20 },
		                { "Year_2028", 20 },
		                { "Year_2029", 20 },
		                { "Year_2030", 20 },
		                { "Year_2031", 20 },
		                { "Year_2032", 20 },
		                { "Year_2033", 20 },
		                { "Year_2034", 20 },
		                { "Year_2035", 20 },
		                { "Year_2036", 20 },
		                { "Year_2037", 20 },
		                { "Year_2038", 20 },
		                { "Year_2039", 20 },
		                { "Year_2040", 20 },
		                { "Year_2041", 20 },
		                { "Year_2042", 20 },
		                { "Year_2043", 20 },
		                { "Year_2044", 20 },
		                { "Year_2045", 20 },
		                { "Year_2046", 20 },
		                { "Year_2047", 20 },
		                { "Year_2048", 20 },
		                { "Year_2049", 20 },
		                { "Year_2050", 20 }
	                } },
	                { "CSO Factors", new Dictionary<string, double>() {
		                { "Year_2015", 2.0 },
		                { "Year_2016", 2.0 },
		                { "Year_2017", 2.0 },
		                { "Year_2018", 2.0 },
		                { "Year_2019", 2.0 },
		                { "Year_2020", 2.0 },
		                { "Year_2021", 2.0 },
		                { "Year_2022", 2.0 },
		                { "Year_2023", 2.0 },
		                { "Year_2024", 2.0 },
		                { "Year_2025", 2.0 },
		                { "Year_2026", 2.0 },
		                { "Year_2027", 2.0 },
		                { "Year_2028", 2.0 },
		                { "Year_2029", 2.0 },
		                { "Year_2030", 2.0 },
		                { "Year_2031", 2.0 },
		                { "Year_2032", 2.0 },
		                { "Year_2033", 2.0 },
		                { "Year_2034", 2.0 },
		                { "Year_2035", 2.0 },
		                { "Year_2036", 2.0 },
		                { "Year_2037", 2.0 },
		                { "Year_2038", 2.0 },
		                { "Year_2039", 2.0 },
		                { "Year_2040", 2.0 },
		                { "Year_2041", 2.0 },
		                { "Year_2042", 2.0 },
		                { "Year_2043", 2.0 },
		                { "Year_2044", 2.0 },
		                { "Year_2045", 2.0 },
		                { "Year_2046", 2.0 },
		                { "Year_2047", 2.0 },
		                { "Year_2048", 2.0 },
		                { "Year_2049", 2.0 },
		                { "Year_2050", 2.0 }
	                } },
	                { "Inflation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 4.3 },
		                { "Year_2017", 8.055 },
		                { "Year_2018", 11.729 },
		                { "Year_2019", 15.192 },
		                { "Year_2020", 18.648 },
		                { "Year_2021", 21.97 },
		                { "Year_2022", 25.263 },
		                { "Year_2023", 28.52 },
		                { "Year_2024", 31.733 },
		                { "Year_2025", 34.895 },
		                { "Year_2026", 38.132 },
		                { "Year_2027", 41.447 },
		                { "Year_2028", 44.842 },
		                { "Year_2029", 48.318 },
		                { "Year_2030", 51.878 },
		                { "Year_2031", 55.523 },
		                { "Year_2032", 59.1 },
		                { "Year_2033", 62.759 },
		                { "Year_2034", 66.503 },
		                { "Year_2035", 70.332 },
		                { "Year_2036", 74.25 },
		                { "Year_2037", 78.258 },
		                { "Year_2038", 82.358 },
		                { "Year_2039", 86.37 },
		                { "Year_2040", 90.47 },
		                { "Year_2041", 94.66 },
		                { "Year_2042", 98.943 },
		                { "Year_2043", 103.319 },
		                { "Year_2044", 107.792 },
		                { "Year_2045", 112.364 },
		                { "Year_2046", 117.036 },
		                { "Year_2047", 121.594 },
		                { "Year_2048", 126.247 },
		                { "Year_2049", 130.998 },
		                { "Year_2050", 135.849 }
	                } },
                };
                #endregion

                #region DW MODU - Flagship/SNEPCO
                var DWMODU_Flagship_SNEPCO = new Dictionary<string, Dictionary<string, double>>() {
	                { "Material Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", -2.153 },
		                { "Year_2017", -2.153 },
		                { "Year_2018", -2.153 },
		                { "Year_2019", -2.153 },
		                { "Year_2020", -2.153 },
		                { "Year_2021", -2.153 },
		                { "Year_2022", -2.153 },
		                { "Year_2023", -2.153 },
		                { "Year_2024", -2.153 },
		                { "Year_2025", -2.153 },
		                { "Year_2026", -2.153 },
		                { "Year_2027", -2.153 },
		                { "Year_2028", -2.153 },
		                { "Year_2029", -2.153 },
		                { "Year_2030", -2.153 },
		                { "Year_2031", -2.153 },
		                { "Year_2032", -2.153 },
		                { "Year_2033", -2.153 },
		                { "Year_2034", -2.153 },
		                { "Year_2035", -2.153 },
		                { "Year_2036", -2.153 },
		                { "Year_2037", -2.153 },
		                { "Year_2038", -2.153 },
		                { "Year_2039", -2.153 },
		                { "Year_2040", -2.153 },
		                { "Year_2041", -2.153 },
		                { "Year_2042", -2.153 },
		                { "Year_2043", -2.153 },
		                { "Year_2044", -2.153 },
		                { "Year_2045", -2.153 },
		                { "Year_2046", -2.153 },
		                { "Year_2047", -2.153 },
		                { "Year_2048", -2.153 },
		                { "Year_2049", -2.153 },
		                { "Year_2050", -2.153 }
	                } },
	                { "Service Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", -2.153 },
		                { "Year_2017", -2.153 },
		                { "Year_2018", -2.153 },
		                { "Year_2019", -2.153 },
		                { "Year_2020", -2.153 },
		                { "Year_2021", -2.153 },
		                { "Year_2022", -2.153 },
		                { "Year_2023", -2.153 },
		                { "Year_2024", -2.153 },
		                { "Year_2025", -2.153 },
		                { "Year_2026", -2.153 },
		                { "Year_2027", -2.153 },
		                { "Year_2028", -2.153 },
		                { "Year_2029", -2.153 },
		                { "Year_2030", -2.153 },
		                { "Year_2031", -2.153 },
		                { "Year_2032", -2.153 },
		                { "Year_2033", -2.153 },
		                { "Year_2034", -2.153 },
		                { "Year_2035", -2.153 },
		                { "Year_2036", -2.153 },
		                { "Year_2037", -2.153 },
		                { "Year_2038", -2.153 },
		                { "Year_2039", -2.153 },
		                { "Year_2040", -2.153 },
		                { "Year_2041", -2.153 },
		                { "Year_2042", -2.153 },
		                { "Year_2043", -2.153 },
		                { "Year_2044", -2.153 },
		                { "Year_2045", -2.153 },
		                { "Year_2046", -2.153 },
		                { "Year_2047", -2.153 },
		                { "Year_2048", -2.153 },
		                { "Year_2049", -2.153 },
		                { "Year_2050", -2.153 }
	                } },
	                { "CSO Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0.0 },
		                { "Year_2016", 0.0 },
		                { "Year_2017", 0.0 },
		                { "Year_2018", 0.0 },
		                { "Year_2019", 0.0 },
		                { "Year_2020", 0.0 },
		                { "Year_2021", 0.0 },
		                { "Year_2022", 0.0 },
		                { "Year_2023", 0.0 },
		                { "Year_2024", 0.0 },
		                { "Year_2025", 0.0 },
		                { "Year_2026", 0.0 },
		                { "Year_2027", 0.0 },
		                { "Year_2028", 0.0 },
		                { "Year_2029", 0.0 },
		                { "Year_2030", 0.0 },
		                { "Year_2031", 0.0 },
		                { "Year_2032", 0.0 },
		                { "Year_2033", 0.0 },
		                { "Year_2034", 0.0 },
		                { "Year_2035", 0.0 },
		                { "Year_2036", 0.0 },
		                { "Year_2037", 0.0 },
		                { "Year_2038", 0.0 },
		                { "Year_2039", 0.0 },
		                { "Year_2040", 0.0 },
		                { "Year_2041", 0.0 },
		                { "Year_2042", 0.0 },
		                { "Year_2043", 0.0 },
		                { "Year_2044", 0.0 },
		                { "Year_2045", 0.0 },
		                { "Year_2046", 0.0 },
		                { "Year_2047", 0.0 },
		                { "Year_2048", 0.0 },
		                { "Year_2049", 0.0 },
		                { "Year_2050", 0.0 }
	                } },
	                { "Inflation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 2.2 },
		                { "Year_2017", 4.244 },
		                { "Year_2018", 6.329 },
		                { "Year_2019", 8.455 },
		                { "Year_2020", 10.625 },
		                { "Year_2021", 12.837 },
		                { "Year_2022", 15.094 },
		                { "Year_2023", 17.396 },
		                { "Year_2024", 19.744 },
		                { "Year_2025", 22.138 },
		                { "Year_2026", 24.581 },
		                { "Year_2027", 27.073 },
		                { "Year_2028", 29.614 },
		                { "Year_2029", 32.207 },
		                { "Year_2030", 34.851 },
		                { "Year_2031", 37.548 },
		                { "Year_2032", 40.299 },
		                { "Year_2033", 43.105 },
		                { "Year_2034", 45.967 },
		                { "Year_2035", 48.886 },
		                { "Year_2036", 51.864 },
		                { "Year_2037", 54.901 },
		                { "Year_2038", 57.999 },
		                { "Year_2039", 61.159 },
		                { "Year_2040", 64.382 },
		                { "Year_2041", 67.67 },
		                { "Year_2042", 71.023 },
		                { "Year_2043", 74.444 },
		                { "Year_2044", 77.933 },
		                { "Year_2045", 81.491 },
		                { "Year_2046", 85.121 },
		                { "Year_2047", 88.824 },
		                { "Year_2048", 92.6 },
		                { "Year_2049", 96.452 },
		                { "Year_2050", 100.381 }
	                } },
                };
                #endregion

                #region DW MODU - Non Flagship
                var DWMODU_NonFlagship = new Dictionary<string, Dictionary<string, double>>() {
	                { "Material Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", -2.153 },
		                { "Year_2017", -2.153 },
		                { "Year_2018", -2.153 },
		                { "Year_2019", -2.153 },
		                { "Year_2020", -2.153 },
		                { "Year_2021", -2.153 },
		                { "Year_2022", -2.153 },
		                { "Year_2023", -2.153 },
		                { "Year_2024", -2.153 },
		                { "Year_2025", -2.153 },
		                { "Year_2026", -2.153 },
		                { "Year_2027", -2.153 },
		                { "Year_2028", -2.153 },
		                { "Year_2029", -2.153 },
		                { "Year_2030", -2.153 },
		                { "Year_2031", -2.153 },
		                { "Year_2032", -2.153 },
		                { "Year_2033", -2.153 },
		                { "Year_2034", -2.153 },
		                { "Year_2035", -2.153 },
		                { "Year_2036", -2.153 },
		                { "Year_2037", -2.153 },
		                { "Year_2038", -2.153 },
		                { "Year_2039", -2.153 },
		                { "Year_2040", -2.153 },
		                { "Year_2041", -2.153 },
		                { "Year_2042", -2.153 },
		                { "Year_2043", -2.153 },
		                { "Year_2044", -2.153 },
		                { "Year_2045", -2.153 },
		                { "Year_2046", -2.153 },
		                { "Year_2047", -2.153 },
		                { "Year_2048", -2.153 },
		                { "Year_2049", -2.153 },
		                { "Year_2050", -2.153 }
	                } },
	                { "Service Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", -2.153 },
		                { "Year_2017", -2.153 },
		                { "Year_2018", -2.153 },
		                { "Year_2019", -2.153 },
		                { "Year_2020", -2.153 },
		                { "Year_2021", -2.153 },
		                { "Year_2022", -2.153 },
		                { "Year_2023", -2.153 },
		                { "Year_2024", -2.153 },
		                { "Year_2025", -2.153 },
		                { "Year_2026", -2.153 },
		                { "Year_2027", -2.153 },
		                { "Year_2028", -2.153 },
		                { "Year_2029", -2.153 },
		                { "Year_2030", -2.153 },
		                { "Year_2031", -2.153 },
		                { "Year_2032", -2.153 },
		                { "Year_2033", -2.153 },
		                { "Year_2034", -2.153 },
		                { "Year_2035", -2.153 },
		                { "Year_2036", -2.153 },
		                { "Year_2037", -2.153 },
		                { "Year_2038", -2.153 },
		                { "Year_2039", -2.153 },
		                { "Year_2040", -2.153 },
		                { "Year_2041", -2.153 },
		                { "Year_2042", -2.153 },
		                { "Year_2043", -2.153 },
		                { "Year_2044", -2.153 },
		                { "Year_2045", -2.153 },
		                { "Year_2046", -2.153 },
		                { "Year_2047", -2.153 },
		                { "Year_2048", -2.153 },
		                { "Year_2049", -2.153 },
		                { "Year_2050", -2.153 }
	                } },
	                { "CSO Factors", new Dictionary<string, double>() {
		                { "Year_2015", 2.0 },
		                { "Year_2016", 2.0 },
		                { "Year_2017", 2.0 },
		                { "Year_2018", 2.0 },
		                { "Year_2019", 2.0 },
		                { "Year_2020", 2.0 },
		                { "Year_2021", 2.0 },
		                { "Year_2022", 2.0 },
		                { "Year_2023", 2.0 },
		                { "Year_2024", 2.0 },
		                { "Year_2025", 2.0 },
		                { "Year_2026", 2.0 },
		                { "Year_2027", 2.0 },
		                { "Year_2028", 2.0 },
		                { "Year_2029", 2.0 },
		                { "Year_2030", 2.0 },
		                { "Year_2031", 2.0 },
		                { "Year_2032", 2.0 },
		                { "Year_2033", 2.0 },
		                { "Year_2034", 2.0 },
		                { "Year_2035", 2.0 },
		                { "Year_2036", 2.0 },
		                { "Year_2037", 2.0 },
		                { "Year_2038", 2.0 },
		                { "Year_2039", 2.0 },
		                { "Year_2040", 2.0 },
		                { "Year_2041", 2.0 },
		                { "Year_2042", 2.0 },
		                { "Year_2043", 2.0 },
		                { "Year_2044", 2.0 },
		                { "Year_2045", 2.0 },
		                { "Year_2046", 2.0 },
		                { "Year_2047", 2.0 },
		                { "Year_2048", 2.0 },
		                { "Year_2049", 2.0 },
		                { "Year_2050", 2.0 }
	                } },
	                { "Inflation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 2.2 },
		                { "Year_2017", 4.244 },
		                { "Year_2018", 6.329 },
		                { "Year_2019", 8.455 },
		                { "Year_2020", 10.625 },
		                { "Year_2021", 12.837 },
		                { "Year_2022", 15.094 },
		                { "Year_2023", 17.396 },
		                { "Year_2024", 19.744 },
		                { "Year_2025", 22.138 },
		                { "Year_2026", 24.581 },
		                { "Year_2027", 27.073 },
		                { "Year_2028", 29.614 },
		                { "Year_2029", 32.207 },
		                { "Year_2030", 34.851 },
		                { "Year_2031", 37.548 },
		                { "Year_2032", 40.299 },
		                { "Year_2033", 43.105 },
		                { "Year_2034", 45.967 },
		                { "Year_2035", 48.886 },
		                { "Year_2036", 51.864 },
		                { "Year_2037", 54.901 },
		                { "Year_2038", 57.999 },
		                { "Year_2039", 61.159 },
		                { "Year_2040", 64.382 },
		                { "Year_2041", 67.67 },
		                { "Year_2042", 71.023 },
		                { "Year_2043", 74.444 },
		                { "Year_2044", 77.933 },
		                { "Year_2045", 81.491 },
		                { "Year_2046", 85.121 },
		                { "Year_2047", 88.824 },
		                { "Year_2048", 92.6 },
		                { "Year_2049", 96.452 },
		                { "Year_2050", 100.381 }
	                } },
                };
                #endregion

                #region DW MODU - 24%
                var DW_MODU_24 = new Dictionary<string, Dictionary<string, double>>() {
	                { "Material Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 0 },
		                { "Year_2017", -1.136 },
		                { "Year_2018", -4.545 },
		                { "Year_2019", 4.545 },
		                { "Year_2020", 10.227 },
		                { "Year_2021", 10.227 },
		                { "Year_2022", 10.227 },
		                { "Year_2023", 10.227 },
		                { "Year_2024", 10.227 },
		                { "Year_2025", 10.227 },
		                { "Year_2026", 10.227 },
		                { "Year_2027", 10.227 },
		                { "Year_2028", 10.227 },
		                { "Year_2029", 10.227 },
		                { "Year_2030", 10.227 },
		                { "Year_2031", 10.227 },
		                { "Year_2032", 10.227 },
		                { "Year_2033", 10.227 },
		                { "Year_2034", 10.227 },
		                { "Year_2035", 10.227 },
		                { "Year_2036", 10.227 },
		                { "Year_2037", 10.227 },
		                { "Year_2038", 10.227 },
		                { "Year_2039", 10.227 },
		                { "Year_2040", 10.227 },
		                { "Year_2041", 10.227 },
		                { "Year_2042", 10.227 },
		                { "Year_2043", 10.227 },
		                { "Year_2044", 10.227 },
		                { "Year_2045", 10.227 },
		                { "Year_2046", 10.227 },
		                { "Year_2047", 10.227 },
		                { "Year_2048", 10.227 },
		                { "Year_2049", 10.227 },
		                { "Year_2050", 10.227 }
	                } },
	                { "Service Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 0 },
		                { "Year_2017", -1.136 },
		                { "Year_2018", -4.545 },
		                { "Year_2019", 4.545 },
		                { "Year_2020", 10.227 },
		                { "Year_2021", 10.227 },
		                { "Year_2022", 10.227 },
		                { "Year_2023", 10.227 },
		                { "Year_2024", 10.227 },
		                { "Year_2025", 10.227 },
		                { "Year_2026", 10.227 },
		                { "Year_2027", 10.227 },
		                { "Year_2028", 10.227 },
		                { "Year_2029", 10.227 },
		                { "Year_2030", 10.227 },
		                { "Year_2031", 10.227 },
		                { "Year_2032", 10.227 },
		                { "Year_2033", 10.227 },
		                { "Year_2034", 10.227 },
		                { "Year_2035", 10.227 },
		                { "Year_2036", 10.227 },
		                { "Year_2037", 10.227 },
		                { "Year_2038", 10.227 },
		                { "Year_2039", 10.227 },
		                { "Year_2040", 10.227 },
		                { "Year_2041", 10.227 },
		                { "Year_2042", 10.227 },
		                { "Year_2043", 10.227 },
		                { "Year_2044", 10.227 },
		                { "Year_2045", 10.227 },
		                { "Year_2046", 10.227 },
		                { "Year_2047", 10.227 },
		                { "Year_2048", 10.227 },
		                { "Year_2049", 10.227 },
		                { "Year_2050", 10.227 }
	                } },
	                { "CSO Factors", new Dictionary<string, double>() {
		                { "Year_2015", 23.5 },
		                { "Year_2016", 23.5 },
		                { "Year_2017", 23.5 },
		                { "Year_2018", 23.5 },
		                { "Year_2019", 23.5 },
		                { "Year_2020", 23.5 },
		                { "Year_2021", 23.5 },
		                { "Year_2022", 23.5 },
		                { "Year_2023", 23.5 },
		                { "Year_2024", 23.5 },
		                { "Year_2025", 23.5 },
		                { "Year_2026", 23.5 },
		                { "Year_2027", 23.5 },
		                { "Year_2028", 23.5 },
		                { "Year_2029", 23.5 },
		                { "Year_2030", 23.5 },
		                { "Year_2031", 23.5 },
		                { "Year_2032", 23.5 },
		                { "Year_2033", 23.5 },
		                { "Year_2034", 23.5 },
		                { "Year_2035", 23.5 },
		                { "Year_2036", 23.5 },
		                { "Year_2037", 23.5 },
		                { "Year_2038", 23.5 },
		                { "Year_2039", 23.5 },
		                { "Year_2040", 23.5 },
		                { "Year_2041", 23.5 },
		                { "Year_2042", 23.5 },
		                { "Year_2043", 23.5 },
		                { "Year_2044", 23.5 },
		                { "Year_2045", 23.5 },
		                { "Year_2046", 23.5 },
		                { "Year_2047", 23.5 },
		                { "Year_2048", 23.5 },
		                { "Year_2049", 23.5 },
		                { "Year_2050", 23.5 }
	                } },
	                { "Inflation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 2 },
		                { "Year_2017", 4.04 },
		                { "Year_2018", 6.121 },
		                { "Year_2019", 8.243 },
		                { "Year_2020", 10.408 },
		                { "Year_2021", 12.616 },
		                { "Year_2022", 14.869 },
		                { "Year_2023", 17.166 },
		                { "Year_2024", 19.509 },
		                { "Year_2025", 21.899 },
		                { "Year_2026", 24.337 },
		                { "Year_2027", 26.824 },
		                { "Year_2028", 29.361 },
		                { "Year_2029", 31.948 },
		                { "Year_2030", 34.587 },
		                { "Year_2031", 37.279 },
		                { "Year_2032", 40.024 },
		                { "Year_2033", 42.825 },
		                { "Year_2034", 45.681 },
		                { "Year_2035", 48.595 },
		                { "Year_2036", 51.567 },
		                { "Year_2037", 54.598 },
		                { "Year_2038", 57.69 },
		                { "Year_2039", 60.844 },
		                { "Year_2040", 64.061 },
		                { "Year_2041", 67.342 },
		                { "Year_2042", 70.689 },
		                { "Year_2043", 74.102 },
		                { "Year_2044", 77.584 },
		                { "Year_2045", 81.136 },
		                { "Year_2046", 84.759 },
		                { "Year_2047", 88.454 },
		                { "Year_2048", 92.223 },
		                { "Year_2049", 96.068 },
		                { "Year_2050", 99.989 }
	                } },
                };
                #endregion

                #region DW MODU - 17%
                var DW_MODU_17 = new Dictionary<string, Dictionary<string, double>>() {
	                { "Material Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 0 },
		                { "Year_2017", -1.136 },
		                { "Year_2018", -4.545 },
		                { "Year_2019", 4.545 },
		                { "Year_2020", 10.227 },
		                { "Year_2021", 10.227 },
		                { "Year_2022", 10.227 },
		                { "Year_2023", 10.227 },
		                { "Year_2024", 10.227 },
		                { "Year_2025", 10.227 },
		                { "Year_2026", 10.227 },
		                { "Year_2027", 10.227 },
		                { "Year_2028", 10.227 },
		                { "Year_2029", 10.227 },
		                { "Year_2030", 10.227 },
		                { "Year_2031", 10.227 },
		                { "Year_2032", 10.227 },
		                { "Year_2033", 10.227 },
		                { "Year_2034", 10.227 },
		                { "Year_2035", 10.227 },
		                { "Year_2036", 10.227 },
		                { "Year_2037", 10.227 },
		                { "Year_2038", 10.227 },
		                { "Year_2039", 10.227 },
		                { "Year_2040", 10.227 },
		                { "Year_2041", 10.227 },
		                { "Year_2042", 10.227 },
		                { "Year_2043", 10.227 },
		                { "Year_2044", 10.227 },
		                { "Year_2045", 10.227 },
		                { "Year_2046", 10.227 },
		                { "Year_2047", 10.227 },
		                { "Year_2048", 10.227 },
		                { "Year_2049", 10.227 },
		                { "Year_2050", 10.227 }
	                } },
	                { "Service Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 0 },
		                { "Year_2017", -1.136 },
		                { "Year_2018", -4.545 },
		                { "Year_2019", 4.545 },
		                { "Year_2020", 10.227 },
		                { "Year_2021", 10.227 },
		                { "Year_2022", 10.227 },
		                { "Year_2023", 10.227 },
		                { "Year_2024", 10.227 },
		                { "Year_2025", 10.227 },
		                { "Year_2026", 10.227 },
		                { "Year_2027", 10.227 },
		                { "Year_2028", 10.227 },
		                { "Year_2029", 10.227 },
		                { "Year_2030", 10.227 },
		                { "Year_2031", 10.227 },
		                { "Year_2032", 10.227 },
		                { "Year_2033", 10.227 },
		                { "Year_2034", 10.227 },
		                { "Year_2035", 10.227 },
		                { "Year_2036", 10.227 },
		                { "Year_2037", 10.227 },
		                { "Year_2038", 10.227 },
		                { "Year_2039", 10.227 },
		                { "Year_2040", 10.227 },
		                { "Year_2041", 10.227 },
		                { "Year_2042", 10.227 },
		                { "Year_2043", 10.227 },
		                { "Year_2044", 10.227 },
		                { "Year_2045", 10.227 },
		                { "Year_2046", 10.227 },
		                { "Year_2047", 10.227 },
		                { "Year_2048", 10.227 },
		                { "Year_2049", 10.227 },
		                { "Year_2050", 10.227 }
	                } },
	                { "CSO Factors", new Dictionary<string, double>() {
		                { "Year_2015", 17.26 },
		                { "Year_2016", 17.26 },
		                { "Year_2017", 17.26 },
		                { "Year_2018", 17.26 },
		                { "Year_2019", 17.26 },
		                { "Year_2020", 17.26 },
		                { "Year_2021", 17.26 },
		                { "Year_2022", 17.26 },
		                { "Year_2023", 17.26 },
		                { "Year_2024", 17.26 },
		                { "Year_2025", 17.26 },
		                { "Year_2026", 17.26 },
		                { "Year_2027", 17.26 },
		                { "Year_2028", 17.26 },
		                { "Year_2029", 17.26 },
		                { "Year_2030", 17.26 },
		                { "Year_2031", 17.26 },
		                { "Year_2032", 17.26 },
		                { "Year_2033", 17.26 },
		                { "Year_2034", 17.26 },
		                { "Year_2035", 17.26 },
		                { "Year_2036", 17.26 },
		                { "Year_2037", 17.26 },
		                { "Year_2038", 17.26 },
		                { "Year_2039", 17.26 },
		                { "Year_2040", 17.26 },
		                { "Year_2041", 17.26 },
		                { "Year_2042", 17.26 },
		                { "Year_2043", 17.26 },
		                { "Year_2044", 17.26 },
		                { "Year_2045", 17.26 },
		                { "Year_2046", 17.26 },
		                { "Year_2047", 17.26 },
		                { "Year_2048", 17.26 },
		                { "Year_2049", 17.26 },
		                { "Year_2050", 17.26 }
	                } },
	                { "Inflation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 2 },
		                { "Year_2017", 4.04 },
		                { "Year_2018", 6.121 },
		                { "Year_2019", 8.243 },
		                { "Year_2020", 10.408 },
		                { "Year_2021", 12.616 },
		                { "Year_2022", 14.869 },
		                { "Year_2023", 17.166 },
		                { "Year_2024", 19.509 },
		                { "Year_2025", 21.899 },
		                { "Year_2026", 24.337 },
		                { "Year_2027", 26.824 },
		                { "Year_2028", 29.361 },
		                { "Year_2029", 31.948 },
		                { "Year_2030", 34.587 },
		                { "Year_2031", 37.279 },
		                { "Year_2032", 40.024 },
		                { "Year_2033", 42.825 },
		                { "Year_2034", 45.681 },
		                { "Year_2035", 48.595 },
		                { "Year_2036", 51.567 },
		                { "Year_2037", 54.598 },
		                { "Year_2038", 57.69 },
		                { "Year_2039", 60.844 },
		                { "Year_2040", 64.061 },
		                { "Year_2041", 67.342 },
		                { "Year_2042", 70.689 },
		                { "Year_2043", 74.102 },
		                { "Year_2044", 77.584 },
		                { "Year_2045", 81.136 },
		                { "Year_2046", 84.759 },
		                { "Year_2047", 88.454 },
		                { "Year_2048", 92.223 },
		                { "Year_2049", 96.068 },
		                { "Year_2050", 99.989 }
	                } },
                };
                #endregion

                #region RIG SUBSIDY MODEL
                var RIG_SUBSIDY_MODEL = new Dictionary<string, Dictionary<string, double>>() {
	                { "Material Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 0 },
		                { "Year_2017", 0 },
		                { "Year_2018", 0 },
		                { "Year_2019", 0 },
		                { "Year_2020", 0 },
		                { "Year_2021", 0 },
		                { "Year_2022", 0 },
		                { "Year_2023", 0 },
		                { "Year_2024", 0 },
		                { "Year_2025", 0 },
		                { "Year_2026", 0 },
		                { "Year_2027", 0 },
		                { "Year_2028", 0 },
		                { "Year_2029", 0 },
		                { "Year_2030", 0 },
		                { "Year_2031", 0 },
		                { "Year_2032", 0 },
		                { "Year_2033", 0 },
		                { "Year_2034", 0 },
		                { "Year_2035", 0 },
		                { "Year_2036", 0 },
		                { "Year_2037", 0 },
		                { "Year_2038", 0 },
		                { "Year_2039", 0 },
		                { "Year_2040", 0 },
		                { "Year_2041", 0 },
		                { "Year_2042", 0 },
		                { "Year_2043", 0 },
		                { "Year_2044", 0 },
		                { "Year_2045", 0 },
		                { "Year_2046", 0 },
		                { "Year_2047", 0 },
		                { "Year_2048", 0 },
		                { "Year_2049", 0 },
		                { "Year_2050", 0  }
	                } },
	                { "Service Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 0 },
		                { "Year_2017", 0 },
		                { "Year_2018", 0 },
		                { "Year_2019", 0 },
		                { "Year_2020", 0 },
		                { "Year_2021", 0 },
		                { "Year_2022", 0 },
		                { "Year_2023", 0 },
		                { "Year_2024", 0 },
		                { "Year_2025", 0 },
		                { "Year_2026", 0 },
		                { "Year_2027", 0 },
		                { "Year_2028", 0 },
		                { "Year_2029", 0 },
		                { "Year_2030", 0 },
		                { "Year_2031", 0 },
		                { "Year_2032", 0 },
		                { "Year_2033", 0 },
		                { "Year_2034", 0 },
		                { "Year_2035", 0 },
		                { "Year_2036", 0 },
		                { "Year_2037", 0 },
		                { "Year_2038", 0 },
		                { "Year_2039", 0 },
		                { "Year_2040", 0 },
		                { "Year_2041", 0 },
		                { "Year_2042", 0 },
		                { "Year_2043", 0 },
		                { "Year_2044", 0 },
		                { "Year_2045", 0 },
		                { "Year_2046", 0 },
		                { "Year_2047", 0 },
		                { "Year_2048", 0 },
		                { "Year_2049", 0 },
		                { "Year_2050", 0  }
	                } },
	                { "CSO Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 0 },
		                { "Year_2017", 0 },
		                { "Year_2018", 0 },
		                { "Year_2019", 0 },
		                { "Year_2020", 0 },
		                { "Year_2021", 0 },
		                { "Year_2022", 0 },
		                { "Year_2023", 0 },
		                { "Year_2024", 0 },
		                { "Year_2025", 0 },
		                { "Year_2026", 0 },
		                { "Year_2027", 0 },
		                { "Year_2028", 0 },
		                { "Year_2029", 0 },
		                { "Year_2030", 0 },
		                { "Year_2031", 0 },
		                { "Year_2032", 0 },
		                { "Year_2033", 0 },
		                { "Year_2034", 0 },
		                { "Year_2035", 0 },
		                { "Year_2036", 0 },
		                { "Year_2037", 0 },
		                { "Year_2038", 0 },
		                { "Year_2039", 0 },
		                { "Year_2040", 0 },
		                { "Year_2041", 0 },
		                { "Year_2042", 0 },
		                { "Year_2043", 0 },
		                { "Year_2044", 0 },
		                { "Year_2045", 0 },
		                { "Year_2046", 0 },
		                { "Year_2047", 0 },
		                { "Year_2048", 0 },
		                { "Year_2049", 0 },
		                { "Year_2050", 0  }
	                } },
	                { "Inflation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 2 },
		                { "Year_2017", 4.04 },
		                { "Year_2018", 6.121 },
		                { "Year_2019", 8.243 },
		                { "Year_2020", 10.408 },
		                { "Year_2021", 12.616 },
		                { "Year_2022", 14.869 },
		                { "Year_2023", 17.166 },
		                { "Year_2024", 19.509 },
		                { "Year_2025", 21.899 },
		                { "Year_2026", 24.337 },
		                { "Year_2027", 26.824 },
		                { "Year_2028", 29.361 },
		                { "Year_2029", 31.948 },
		                { "Year_2030", 34.587 },
		                { "Year_2031", 37.279 },
		                { "Year_2032", 40.024 },
		                { "Year_2033", 42.825 },
		                { "Year_2034", 45.681 },
		                { "Year_2035", 48.595 },
		                { "Year_2036", 51.567 },
		                { "Year_2037", 54.598 },
		                { "Year_2038", 57.69 },
		                { "Year_2039", 60.844 },
		                { "Year_2040", 64.061 },
		                { "Year_2041", 67.342 },
		                { "Year_2042", 70.689 },
		                { "Year_2043", 74.102 },
		                { "Year_2044", 77.584 },
		                { "Year_2045", 81.136 },
		                { "Year_2046", 84.759 },
		                { "Year_2047", 88.454 },
		                { "Year_2048", 92.223 },
		                { "Year_2049", 96.068 },
		                { "Year_2050", 99.989 }
	                } },
                };
                #endregion

                #region NGT2 Sequence
                var NGT2_Sequence = new Dictionary<string, Dictionary<string, double>>() {
	                { "Material Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 0 },
		                { "Year_2017", -1.136 },
		                { "Year_2018", -4.545 },
		                { "Year_2019", 4.545 },
		                { "Year_2020", 10.227 },
		                { "Year_2021", 10.227 },
		                { "Year_2022", 10.227 },
		                { "Year_2023", 10.227 },
		                { "Year_2024", 10.227 },
		                { "Year_2025", 10.227 },
		                { "Year_2026", 10.227 },
		                { "Year_2027", 10.227 },
		                { "Year_2028", 10.227 },
		                { "Year_2029", 10.227 },
		                { "Year_2030", 10.227 },
		                { "Year_2031", 10.227 },
		                { "Year_2032", 10.227 },
		                { "Year_2033", 10.227 },
		                { "Year_2034", 10.227 },
		                { "Year_2035", 10.227 },
		                { "Year_2036", 10.227 },
		                { "Year_2037", 10.227 },
		                { "Year_2038", 10.227 },
		                { "Year_2039", 10.227 },
		                { "Year_2040", 10.227 },
		                { "Year_2041", 10.227 },
		                { "Year_2042", 10.227 },
		                { "Year_2043", 10.227 },
		                { "Year_2044", 10.227 },
		                { "Year_2045", 10.227 },
		                { "Year_2046", 10.227 },
		                { "Year_2047", 10.227 },
		                { "Year_2048", 10.227 },
		                { "Year_2049", 10.227 },
		                { "Year_2050", 10.227 }
	                } },
	                { "Service Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 0 },
		                { "Year_2017", -1.136 },
		                { "Year_2018", -4.545 },
		                { "Year_2019", 4.545 },
		                { "Year_2020", 10.227 },
		                { "Year_2021", 10.227 },
		                { "Year_2022", 10.227 },
		                { "Year_2023", 10.227 },
		                { "Year_2024", 10.227 },
		                { "Year_2025", 10.227 },
		                { "Year_2026", 10.227 },
		                { "Year_2027", 10.227 },
		                { "Year_2028", 10.227 },
		                { "Year_2029", 10.227 },
		                { "Year_2030", 10.227 },
		                { "Year_2031", 10.227 },
		                { "Year_2032", 10.227 },
		                { "Year_2033", 10.227 },
		                { "Year_2034", 10.227 },
		                { "Year_2035", 10.227 },
		                { "Year_2036", 10.227 },
		                { "Year_2037", 10.227 },
		                { "Year_2038", 10.227 },
		                { "Year_2039", 10.227 },
		                { "Year_2040", 10.227 },
		                { "Year_2041", 10.227 },
		                { "Year_2042", 10.227 },
		                { "Year_2043", 10.227 },
		                { "Year_2044", 10.227 },
		                { "Year_2045", 10.227 },
		                { "Year_2046", 10.227 },
		                { "Year_2047", 10.227 },
		                { "Year_2048", 10.227 },
		                { "Year_2049", 10.227 },
		                { "Year_2050", 10.227 }
	                } },
	                { "CSO Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 0 },
		                { "Year_2017", 0 },
		                { "Year_2018", 0 },
		                { "Year_2019", 0 },
		                { "Year_2020", 0 },
		                { "Year_2021", 0 },
		                { "Year_2022", 0 },
		                { "Year_2023", 0 },
		                { "Year_2024", 0 },
		                { "Year_2025", 0 },
		                { "Year_2026", 0 },
		                { "Year_2027", 0 },
		                { "Year_2028", 0 },
		                { "Year_2029", 0 },
		                { "Year_2030", 0 },
		                { "Year_2031", 0 },
		                { "Year_2032", 0 },
		                { "Year_2033", 0 },
		                { "Year_2034", 0 },
		                { "Year_2035", 0 },
		                { "Year_2036", 0 },
		                { "Year_2037", 0 },
		                { "Year_2038", 0 },
		                { "Year_2039", 0 },
		                { "Year_2040", 0 },
		                { "Year_2041", 0 },
		                { "Year_2042", 0 },
		                { "Year_2043", 0 },
		                { "Year_2044", 0 },
		                { "Year_2045", 0 },
		                { "Year_2046", 0 },
		                { "Year_2047", 0 },
		                { "Year_2048", 0 },
		                { "Year_2049", 0 },
		                { "Year_2050", 0  }
	                } },
	                { "Inflation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 2 },
		                { "Year_2017", 4.04 },
		                { "Year_2018", 6.121 },
		                { "Year_2019", 8.243 },
		                { "Year_2020", 10.408 },
		                { "Year_2021", 12.616 },
		                { "Year_2022", 14.869 },
		                { "Year_2023", 17.166 },
		                { "Year_2024", 19.509 },
		                { "Year_2025", 21.899 },
		                { "Year_2026", 24.337 },
		                { "Year_2027", 26.824 },
		                { "Year_2028", 29.361 },
		                { "Year_2029", 31.948 },
		                { "Year_2030", 34.587 },
		                { "Year_2031", 37.279 },
		                { "Year_2032", 40.024 },
		                { "Year_2033", 42.825 },
		                { "Year_2034", 45.681 },
		                { "Year_2035", 48.595 },
		                { "Year_2036", 51.567 },
		                { "Year_2037", 54.598 },
		                { "Year_2038", 57.69 },
		                { "Year_2039", 60.844 },
		                { "Year_2040", 64.061 },
		                { "Year_2041", 67.342 },
		                { "Year_2042", 70.689 },
		                { "Year_2043", 74.102 },
		                { "Year_2044", 77.584 },
		                { "Year_2045", 81.136 },
		                { "Year_2046", 84.759 },
		                { "Year_2047", 88.454 },
		                { "Year_2048", 92.223 },
		                { "Year_2049", 96.068 },
		                { "Year_2050", 99.989 }
	                } },
                };
                #endregion

                #region DW Colombia
                var DW_Colombia = new Dictionary<string, Dictionary<string, double>>() {
	                { "Material Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 0 },
		                { "Year_2017", 0 },
		                { "Year_2018", 0 },
		                { "Year_2019", 0 },
		                { "Year_2020", 0 },
		                { "Year_2021", 0 },
		                { "Year_2022", 0 },
		                { "Year_2023", 0 },
		                { "Year_2024", 0 },
		                { "Year_2025", 0 },
		                { "Year_2026", 0 },
		                { "Year_2027", 0 },
		                { "Year_2028", 0 },
		                { "Year_2029", 0 },
		                { "Year_2030", 0 },
		                { "Year_2031", 0 },
		                { "Year_2032", 0 },
		                { "Year_2033", 0 },
		                { "Year_2034", 0 },
		                { "Year_2035", 0 },
		                { "Year_2036", 0 },
		                { "Year_2037", 0 },
		                { "Year_2038", 0 },
		                { "Year_2039", 0 },
		                { "Year_2040", 0 },
		                { "Year_2041", 0 },
		                { "Year_2042", 0 },
		                { "Year_2043", 0 },
		                { "Year_2044", 0 },
		                { "Year_2045", 0 },
		                { "Year_2046", 0 },
		                { "Year_2047", 0 },
		                { "Year_2048", 0 },
		                { "Year_2049", 0 },
		                { "Year_2050", 0 }
	                } },
	                { "Service Escalation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 0 },
		                { "Year_2017", 0 },
		                { "Year_2018", 0 },
		                { "Year_2019", 0 },
		                { "Year_2020", 0 },
		                { "Year_2021", 0 },
		                { "Year_2022", 0 },
		                { "Year_2023", 0 },
		                { "Year_2024", 0 },
		                { "Year_2025", 0 },
		                { "Year_2026", 0 },
		                { "Year_2027", 0 },
		                { "Year_2028", 0 },
		                { "Year_2029", 0 },
		                { "Year_2030", 0 },
		                { "Year_2031", 0 },
		                { "Year_2032", 0 },
		                { "Year_2033", 0 },
		                { "Year_2034", 0 },
		                { "Year_2035", 0 },
		                { "Year_2036", 0 },
		                { "Year_2037", 0 },
		                { "Year_2038", 0 },
		                { "Year_2039", 0 },
		                { "Year_2040", 0 },
		                { "Year_2041", 0 },
		                { "Year_2042", 0 },
		                { "Year_2043", 0 },
		                { "Year_2044", 0 },
		                { "Year_2045", 0 },
		                { "Year_2046", 0 },
		                { "Year_2047", 0 },
		                { "Year_2048", 0 },
		                { "Year_2049", 0 },
		                { "Year_2050", 0 }
	                } },
	                { "CSO Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 0 },
		                { "Year_2017", 0 },
		                { "Year_2018", 0 },
		                { "Year_2019", 0 },
		                { "Year_2020", 0 },
		                { "Year_2021", 0 },
		                { "Year_2022", 0 },
		                { "Year_2023", 0 },
		                { "Year_2024", 0 },
		                { "Year_2025", 0 },
		                { "Year_2026", 0 },
		                { "Year_2027", 0 },
		                { "Year_2028", 0 },
		                { "Year_2029", 0 },
		                { "Year_2030", 0 },
		                { "Year_2031", 0 },
		                { "Year_2032", 0 },
		                { "Year_2033", 0 },
		                { "Year_2034", 0 },
		                { "Year_2035", 0 },
		                { "Year_2036", 0 },
		                { "Year_2037", 0 },
		                { "Year_2038", 0 },
		                { "Year_2039", 0 },
		                { "Year_2040", 0 },
		                { "Year_2041", 0 },
		                { "Year_2042", 0 },
		                { "Year_2043", 0 },
		                { "Year_2044", 0 },
		                { "Year_2045", 0 },
		                { "Year_2046", 0 },
		                { "Year_2047", 0 },
		                { "Year_2048", 0 },
		                { "Year_2049", 0 },
		                { "Year_2050", 0  }
	                } },
	                { "Inflation Factors", new Dictionary<string, double>() {
		                { "Year_2015", 0 },
		                { "Year_2016", 2.2 },
		                { "Year_2017", 4.244 },
		                { "Year_2018", 6.329 },
		                { "Year_2019", 8.455 },
		                { "Year_2020", 10.625 },
		                { "Year_2021", 12.837 },
		                { "Year_2022", 15.094 },
		                { "Year_2023", 17.396 },
		                { "Year_2024", 19.744 },
		                { "Year_2025", 22.138 },
		                { "Year_2026", 24.581 },
		                { "Year_2027", 27.073 },
		                { "Year_2028", 29.614 },
		                { "Year_2029", 32.207 },
		                { "Year_2030", 34.851 },
		                { "Year_2031", 37.548 },
		                { "Year_2032", 40.299 },
		                { "Year_2033", 43.105 },
		                { "Year_2034", 45.967 },
		                { "Year_2035", 48.886 },
		                { "Year_2036", 51.864 },
		                { "Year_2037", 54.901 },
		                { "Year_2038", 57.999 },
		                { "Year_2039", 61.159 },
		                { "Year_2040", 64.382 },
		                { "Year_2041", 67.67 },
		                { "Year_2042", 71.023 },
		                { "Year_2043", 74.444 },
		                { "Year_2044", 77.933 },
		                { "Year_2045", 81.491 },
		                { "Year_2046", 85.121 },
		                { "Year_2047", 88.824 },
		                { "Year_2048", 92.6 },
		                { "Year_2049", 96.452 },
		                { "Year_2050", 100.381 }
	                } },
                };
                #endregion

                var models = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>() {
                    { "Deepwater Model", DeepwaterModel },
                    { "Example Model 2", ExampleModel2 },
                    { "DW MODU - Flagship/SNEPCO", DWMODU_Flagship_SNEPCO },
                    { "DW MODU - Non Flagship", DWMODU_NonFlagship },
                    { "DW MODU - 24%", DW_MODU_24 },
                    { "DW MODU - 17%", DW_MODU_17 },
                    { "RIG SUBSIDY MODEL", RIG_SUBSIDY_MODEL },
                    { "NGT2 Sequence", NGT2_Sequence },
                    { "DW Colombia", DW_Colombia },
                };

                MacroEconomic.Populate<MacroEconomic>().GroupBy(d => d.Country).Select(d => d.Key).ToList().ForEach(country =>
                {
                    foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, double>>> model in models)
                    {
                        var rfm = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(Query.And(
                            Query.EQ("GroupCase", model.Key),
                            Query.EQ("Country", country)
                        ));

                        if (rfm != null)
                        {
                            rfm.Delete();
                        }

                        new WEISReferenceFactorModel()
                        {
                            ActivityType = "*",
                            Country = country,
                            GroupCase = model.Key,
                            SubjectMatters = model.Value,
                            WellName = "*"
                        }.Save();
                    }
                });

                return true;
            });
        }

        public JsonResult SyncActivityCategory()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                #region activitiyCategories
                var activitiyCategories = new Dictionary<string, List<string>>()
                {
                    { "ABANDONMENT", new List<string>() { 
                        "ABANDONMENT", 
                        "ABANDONMENT CAT. 1", 
                        "ABANDONMENT CAT. 2", 
                        "ABANDONMENT CAT. 3", 
                        "ABANDONMENT CAT. 4", 
                        "RISK - ABANDONMENT", 
                        "TEMP ABANDONMENT" } },
                    { "COMPLETION", new List<string>() { 
                        "CLEANUP AND FLOW",
                        "DE-COMPLETION",
                        "INITIAL COMPLETION",
                        "LONG LEAD - COMPLETE",
                        "LOWER COMPLETION",
                        "RE-COMPLETION",
                        "RISK - COMPLETION",
                        "RISK - DE-COMPLETION",
                        "RISK - SLOT RECOVERY",
                        "SLOT RECOVERY",
                        "STIMULATION",
                        "TESTING",
                        "UPPER COMPLETION",
                        "WHOLE COMPLETION EVENT",
                        "ZONAL ISOLATION",
                        "FLOWBACK" } },
                    { "DRILLING", new List<string>() { 
                        "CONDUCTOR JETTING",
                        "DEEPEN",
                        "DRILL AND ABANDON",
                        "INTERM. & PROD ONLY",
                        "INTERMEDIATE SECTION ONLY",
                        "LONG LEAD - DRILL",
                        "PILOT HOLE",
                        "PRODUCTION SECTION ONLY",
                        "RE-DRILL",
                        "RISK - APPRAISAL",
                        "RISK - APPRAISAL RISK",
                        "RISK - DRILLING",
                        "SIDE TRACK",
                        "TOP HOLE ONLY",
                        "WHOLE DRILLING EVENT",
                        "BATCH SET 1",
                        "BATCH SET 2",
                        "PRE-DRILL",
                        "RE-ENTRY" } },
                    { "MANAGEMENT ADJUSTMENT", new List<string>() { 
                        "MANAGEMENT ADJUSTMENT" } },
                    { "PROJECTS", new List<string>() { 
                        "LOGISTICS",
                        "PROJECTS",
                        "PROJECTS - OTHER",
                        "SITE PREPARATION",
                        "SUBSEA INSTALLATION",
                        "SURFACE INSTALLATION",
                        "PORTFOLIO RISK",
                        "TECH DEVELOPMENT",
                        "TP EQUIPMENT INTEGRATION",
                        "D&PM -WELLS SYSTEMS",
                        "C&WI EQUIPMENT",
                        "PROJECT LONG LEADS" } },
                    { "RIG", new List<string>() { 
                        "RIG DEMOBILISATION",
                        "RIG IDLE",
                        "RIG MAINTENANCE",
                        "RIG MOBILISATION",
                        "RIG MOVE/PREP",
                        "RIG PREPARATION AND INSPECTION",
                        "RIG RAMP UP/DOWN",
                        "RIG SHUTDOWN",
                        "RISER MAINTENANCE (TLP/SPAR)",
                        "RISK - RIG",
                        "RISK - RIG SHUTDOWN",
                        "RISK - RISER MAINTENANCE",
                        "RISK - SBS (CAISSON)",
                        "SBS (CAISSON)",
                        "WEATHER",
                        "RIG DELAY" } },
                    { "TOTAL WELL", new List<string>() { 
                        "LONG LEAD - TOTAL WELL",
                        "RISK - TOTAL WELL",
                        "TOTAL WELL" } },
                    { "WORKOVER", new List<string>() { 
                        "RISK - WORKOVER",
                        "SUSPEND",
                        "WELL INTERVENTION",
                        "WORKOVER" } },
                    { "WRFM OPTIMIZATION", new List<string>() { 
                        "ACID STIMULATION",
                        "ADDITIONAL PERFORATIONS",
                        "ART. LIFT RE-SIZING",
                        "BEAN UPS/ CHOKE BACK",
                        "CEMENT SHUT-OFFS",
                        "CHEMICAL SHUT-OFFS",
                        "DELIQUIFICATION",
                        "FRACTURING",
                        "GAS / WATER INJECTION",
                        "GAS LIFT VALVE CHANGEOUTS",
                        "MECHANICAL STRADDLE SHUT-OFFS",
                        "PLUG SETTING SHUT-OFFS",
                        "PLUNGER LIFTING",
                        "REPERFORATIONS",
                        "RISK - WRFM OPTIMIZATION",
                        "SHIFTING SLEEVES",
                        "TUB. SIZE OPT.",
                        "VELOCITY STRING/TAIL PIPE EXT.",
                        "WELL CONVERSION E.G. OP>WI",
                        "WRFM OPTIMIZATION - OTHER",
                        "ZONE CHANGE",
                        "ZONE CHANGE" } },
                    { "WRFM RESTORATION", new List<string>() {
                        "ART. LIFT REPAIR",
                        "CT KICK OFFS",
                        "FOAM LIFTING",
                        "GASLIFT VALVE REPAIRS",
                        "INTEGRITY RE-COMPLETION",
                        "RESERVOIR WELLBORE CLEANOUTS",
                        "RISK - WRFM RESTORATION",
                        "SAND CONTROL REPAIR",
                        "SCALE REMOVAL",
                        "SCALE SQUEEZES",
                        "SC-SSSV REPAIRS",
                        "TREE RESTORATIONS",
                        "WATER WASHES",
                        "WRFM RESTORATION - OTHER" } },
                    { "WRFM SURVEILLANCE", new List<string>() {
                        "FLUID PROPERTIES",
                        "LOGGING",
                        "PRESS. MONITORING",
                        "PRESS. TRANSIENT ANALYSIS",
                        "PROD./INJ. PROFILE LOG",
                        "RISK - WRFM SURVEILLANCE",
                        "WELL INTEGR. MONITORING",
                        "WRFM SURVEILLANCE - OTHER" } }
                };
                #endregion

                ActivityMaster.Populate<ActivityMaster>().ForEach(d =>
                {
                    var eachDoc = d.ToBsonDocument();
                    eachDoc.Remove("ActivityCategory");
                    DataHelper.Save(d.TableName, eachDoc);
                });

                foreach (KeyValuePair<string, List<string>> each in activitiyCategories)
                {
                    each.Value.ForEach(d =>
                    {
                        var act = ActivityMaster.Get<ActivityMaster>(Query.EQ("_id", d));
                        if (act != null)
                        {
                            act.ActivityCategory = each.Key;
                            act.Save();
                        }
                    });
                }

                ActivityMaster.Populate<ActivityMaster>(Query.EQ("ActivityCategory", BsonNull.Value)).ForEach(d =>
                {
                    var actType = Convert.ToString(d._id);

                    if (actType.Contains("DRL") || actType.Contains("DRILL"))
                    {
                        d.ActivityCategory = "DRILLING";
                        d.Save();
                    }
                    else if (actType.Contains("COM"))
                    {
                        d.ActivityCategory = "COMPLETION";
                        d.Save();
                    }
                    else if (actType.Contains("RIG"))
                    {
                        d.ActivityCategory = "RIG";
                        d.Save();
                    }
                    else if (actType.Contains("PROJECTS"))
                    {
                        d.ActivityCategory = "PROJECTS";
                        d.Save();
                    }
                    else if (actType.Contains("SUSPEND"))
                    {
                        d.ActivityCategory = "WORKOVER";
                        d.Save();
                    }
                    else if (actType.Contains("WRFM") && actType.Contains("RISK"))
                    {
                        d.ActivityCategory = "WRFM SURVEILLANCE";
                        d.Save();
                    }
                    else if (actType.Contains("WRFM") && actType.Contains("RST"))
                    {
                        d.ActivityCategory = "WRFM RESTORATION";
                        d.Save();
                    }
                });

                return true;
            });
        }

        // calculate CalcOP
        public JsonResult ProcessCalculatedOP()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                WellActivity.ProcessCalculatedOP();

                return true;
            });
        }

        public JsonResult DeleteOtherUsers()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var excludes = new List<string> { "yoga.bangkit@eaciit.com", "hector.romero@shell.com", "arief@eaciit.com" };
                DataHelper.Populate("Users").ForEach(d =>
                {
                    if (excludes.Contains(BsonHelper.GetString(d, "Email")))
                        return;

                    DataHelper.Save("Users_Old", d);
                    DataHelper.Delete("Users", Query.EQ("_id", BsonHelper.Get(d, "_id")));
                });
                return true;
            });
        }


        public JsonResult lastWeekRemoval()
        {
            return MvcResultInfo.Execute(() =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    if (wa.Phases.Any())
                    {
                        foreach (var ph in wa.Phases)
                        {
                            if (ph.BaseOP.Contains("OP14") && !WellActivity.isHaveWeeklyReport(wa.WellName, wa.UARigSequenceId, ph.ActivityType))
                            {
                                ph.LastWeek = Tools.DefaultDate;
                            }
                        }
                    }
                    DataHelper.Save(wa.TableName, wa.ToBsonDocument());

                }
                return "OK";
            });
        }

        public JsonResult DefaultPIPAssign()
        {
            return MvcResultInfo.Execute(() =>
            {
                var was = WellPIP.Populate<WellPIP>();
                foreach (var wa in was)
                {
                    if (wa.Elements.Any())
                    {
                        foreach (var ph in wa.Elements)
                        {
                            ph.AssignTOOps.Add("OP14");
                        }
                    }
                    DataHelper.Save(wa.TableName, wa.ToBsonDocument());
                }

                var waps = WellActivityUpdate.Populate<WellActivityUpdate>();
                foreach (var wa in waps)
                {
                    if (wa.Elements.Any())
                    {
                        foreach (var ph in wa.Elements)
                        {
                            ph.AssignTOOps.Add("OP14");
                        }
                    }
                    DataHelper.Save(wa.TableName, wa.ToBsonDocument());
                }

                return "OK";
            });
        }

        // Master Rig Rates
        public JsonResult InitiateRigRatesData()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                DataHelper.Delete("WEISRigRates");
                List<string> fields = new List<string>();
                fields.Add("RigName");
                var rignames = WellActivity.Populate<WellActivity>().Select(x => x.RigName).Distinct();

                var data = RigRates.Populate<RigRates>();
                int min = 0;
                int max = 0;
                if (data.Count() > 0)
                {
                    max = data.FirstOrDefault().Value.Max(x => x.Year);
                    min = data.FirstOrDefault().Value.Min(x => x.Year);

                }
                else
                {
                    min = 2015;
                    max = 2025;
                }

                foreach (var r in rignames)
                {
                    RigRates t = new RigRates();
                    List<FYExpenseProfile> vals = new List<FYExpenseProfile>();

                    for (int i = min; i <= max; i++)
                    {
                        vals.Add(new FYExpenseProfile
                        {
                            Title = "FY" + i.ToString(),
                            Value = 100000,
                            ValueType = "Absolute",
                            Year = i

                        });
                    }
                    t.Title = r;
                    t.Value = vals;
                    t.Save();
                }

                return true;
            });
        }

        // master long leads
        public JsonResult InitiateLongLeadsData()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                DataHelper.Delete("WEISLongLeadItems");

                int min = 2015;
                int max = 2025;

                List<string> eventType = new List<string>();
                eventType.Add("DRILLING");
                eventType.Add("COMPLETION");
                eventType.Add("ABANDONMENT");

                foreach (var s in eventType)
                {
                    LongLead ll = new LongLead();
                    ll.Title = s;

                    for (int i = min; i <= max; i++)
                    {
                        ll.Details.Add(new FYLongLead
                        {
                            MonthRequiredValue = 18,
                            MonthRequiredValueType = "Absolute",
                            TangibleValue = 30,
                            TangibleValueType = "Percentage",
                            Title = "FY" + i.ToString().Substring(2, 2),
                            Year = i

                        });
                    }
                    ll.Save();
                }

                return true;
            });
        }


        // master maturity risk
        public JsonResult InitiateMaturityRisk()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                DataHelper.Delete("WEISMaturityRisk");
                for (int i = 0; i <= 4; i++)
                {
                    MaturityRiskMatrix mm = new MaturityRiskMatrix();
                    mm.Title = "Type " + i.ToString();
                    mm.TECOPCost = 30;
                    mm.TECOPTime = 30;
                    mm.NPTTime = 30;
                    mm.NPTCost = 30;
                    mm.Save();
                }

                return true;
            });
        }

        // master rig escalation factor
        //public JsonResult InitiateRigEscalationFactor()
        //{
        //    return MvcResultInfo.Execute(null, (BsonDocument doc) =>
        //    {
        //        DataHelper.Delete("WEISRigEscalation");
        //        List<string> fields = new List<string>();
        //        fields.Add("RigName");
        //        var rigname = MasterRigName.Populate<MasterRigName>().Select(y => y._id.ToString()).Distinct();

        //        foreach (var r in rigname)
        //        {
        //            RigEscalation t = new RigEscalation();
        //            t.RigName = r;
        //            t.Value = 30;
        //            t.Save();
        //        }

        //        return true;
        //    });
        //}

        // Master Rig escalation
        public JsonResult InitiateRigEscalation()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                DataHelper.Delete("WEISRigEscalation");
                List<string> fields = new List<string>();
                fields.Add("RigName");
                var rignames = MasterRigName.Populate<MasterRigName>().Select(x => x._id.ToString()).Distinct();

                var data = RigEscalationFactor.Populate<RigEscalationFactor>();
                int min = 0;
                int max = 0;
                if (data.Count() > 0)
                {
                    max = data.FirstOrDefault().Value.Max(x => x.Year);
                    min = data.FirstOrDefault().Value.Min(x => x.Year);


                }
                else
                {
                    min = 2015;
                    max = 2025;
                }

                foreach (var r in rignames)
                {
                    RigEscalationFactor t = new RigEscalationFactor();
                    List<FYExpenseProfile> vals = new List<FYExpenseProfile>();

                    for (int i = min; i <= max; i++)
                    {
                        var valuerig = 0;
                        if (i == 2015)
                        {
                            valuerig = 100;
                        }
                        else
                        {
                            valuerig = 90;
                        }
                        vals.Add(new FYExpenseProfile
                        {
                            Title = "FY" + i.ToString(),
                            Value = valuerig,
                            ValueType = "Percentage",
                            Year = i

                        });
                    }
                    t.Title = r;
                    t.Value = vals;
                    t.Save();
                }

                return true;
            });
        }



        public JsonResult RefreshBizplanWithLatestUploadedLS(string colName = "OP20160516V2")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                DataHelper.Delete("SyncStatus_" + colName);
                var colls = DataHelper.Populate(colName);
                foreach (var t in colls)
                {
                    try
                    {
                        var rigName = t.GetString("Rig_Name");
                        var wellName = t.GetString("Well_Name");
                        var activityType = t.GetString("Activity_Type");

                        var sd = DateTime.ParseExact(BsonHelper.GetString(t, "Start_Date"), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                        var fd = DateTime.ParseExact(BsonHelper.GetString(t, "End_Date"), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

                        var q = Query.And(
                            Query.EQ("WellName", wellName),
                            Query.EQ("Phases.ActivityType", activityType),
                            Query.EQ("Phases.Estimate.RigName", rigName)
                            );

                        var bp = BizPlanActivity.Get<BizPlanActivity>(q);
                        if (bp != null)
                        {
                            #region update LS bizplan

                            var y = bp.Phases.Where(x => x.ActivityType.Equals(activityType));
                            if (y != null)
                            {
                                y.FirstOrDefault().PhSchedule.Start = sd;
                                y.FirstOrDefault().PhSchedule.Finish = fd;
                            }
                            DataHelper.Save(bp.TableName, bp.ToBsonDocument());
                            t.Set("StatusSyncBizplan", "Update LS");
                            DataHelper.Save("SyncStatus_" + colName, t.ToBsonDocument());
                            #endregion
                        }
                        else
                        {

                            q = Query.And(
                            Query.EQ("WellName", wellName),
                            Query.EQ("Phases.ActivityType", activityType),
                            Query.EQ("RigName", rigName)
                            );
                            var y = WellActivity.Get<WellActivity>(q);


                            q = Query.And(
                            Query.EQ("WellName", wellName),
                            Query.EQ("UARigSequenceId", y.UARigSequenceId),
                            Query.EQ("Phases.Estimate.RigName", rigName)
                            );

                            var bpsameRigName = BizPlanActivity.Get<BizPlanActivity>(q);
                            if (bpsameRigName != null)
                            {
                                // add phases
                                var phase = y.Phases.Where(x => x.ActivityType.Equals(activityType)).FirstOrDefault();
                                BizPlan.SyncFromUploadLS(bpsameRigName, y, phase);

                                t.Set("StatusSyncBizplan", "New Biz Plan - add phases");
                                DataHelper.Save("SyncStatus_" + colName, t.ToBsonDocument());

                            }
                            else
                            {
                                var phase = y.Phases.Where(x => x.ActivityType.Equals(activityType)).FirstOrDefault();
                                BizPlan.SyncFromUploadLS(null, y, phase);

                                t.Set("StatusSyncBizplan", "New Biz Plan ");
                                DataHelper.Save("SyncStatus_" + colName, t.ToBsonDocument());
                            }

                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                return "OK";
            });
        }

        public JsonResult InitiateMaturityRiskMatrix()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                int tecopC = 0;
                int nptT = 0;
                for (int i = 0; i <= 4; i++)
                {
                    MaturityRiskMatrix mr = new MaturityRiskMatrix();
                    mr.Title = "Type " + i.ToString();
                    mr.TECOPCost = tecopC + 30;
                    mr.NPTCost = tecopC + 30;
                    mr.NPTTime = nptT + 30;
                    mr.TECOPTime = nptT + 30;

                    nptT = nptT + 10;
                    tecopC = tecopC + 10;

                    mr.Save();
                }

                return true;
            });
        }

        public JsonResult InitiateRigRate()
        {

            return MvcResultInfo.Execute(() =>
            {
                var t = WellActivity.Populate<WellActivity>().Select(x => x.RigName).Distinct();
                foreach (var ty in t)
                {

                    RigRatesNew rrn = new RigRatesNew();
                    rrn.Title = ty;
                    rrn.Values = new List<RigRateValue>();


                    RigRateValue r = new RigRateValue();
                    r.Period = new DateRange(new DateTime(2015, 1, 1), new DateTime(2015, 12, 31));
                    r.Value = 300000;
                    r.Type = "ACTIVE";
                    r.ValueType = "Absolute";

                    rrn.Values.Add(r);

                    r = new RigRateValue();
                    r.Period = new DateRange(new DateTime(2015, 1, 1), new DateTime(2015, 12, 31));
                    r.Value = 100000;
                    r.Type = "IDLE";
                    r.ValueType = "Absolute";

                    rrn.Values.Add(r);

                    r = new RigRateValue();
                    r.Period = new DateRange(new DateTime(2016, 1, 1), new DateTime(2017, 8, 1));
                    r.Value = 250000;
                    r.Type = "ACTIVE";
                    r.ValueType = "Absolute";

                    rrn.Values.Add(r);

                    r = new RigRateValue();
                    r.Period = new DateRange(new DateTime(2016, 1, 1), new DateTime(2017, 8, 1));
                    r.Value = 80000;
                    r.Type = "IDLE";
                    r.ValueType = "Absolute";

                    rrn.Values.Add(r);

                    r = new RigRateValue();
                    r.Period = new DateRange(new DateTime(2017, 8, 2), new DateTime(2018, 12, 31));
                    r.Value = 350000;
                    r.Type = "ACTIVE";
                    r.ValueType = "Absolute";

                    rrn.Values.Add(r);

                    r = new RigRateValue();
                    r.Period = new DateRange(new DateTime(2017, 8, 2), new DateTime(2018, 12, 31));

                    r.Value = 70000;
                    r.Type = "IDLE";
                    r.ValueType = "Absolute";

                    rrn.Values.Add(r);

                    r = new RigRateValue();
                    r.Period = new DateRange(new DateTime(2019, 1, 1), new DateTime(2025, 12, 31));
                    r.Value = 450000;
                    r.Type = "ACTIVE";
                    r.ValueType = "Absolute";

                    rrn.Values.Add(r);

                    r = new RigRateValue();
                    r.Period = new DateRange(new DateTime(2019, 1, 1), new DateTime(2025, 12, 31));
                    r.Value = 90000;
                    r.Type = "IDLE";
                    r.ValueType = "Absolute";

                    rrn.Values.Add(r);

                    rrn.Save();
                }

                return "OK";
            });



        }

        public JsonResult SyncPIP()
        {
            return MvcResultInfo.Execute(() =>
            {
                var wps = WellPIP.Populate<WellPIP>()
                    .Select(d =>
                    {
                        d.Save();
                        var wau = WellActivityUpdate.GetById(d.WellName, d.SequenceId, d.PhaseNo, null, true);
                        if (wau != null) wau.Save();
                        return d;
                    });

                return "OK";
            });
        }

        public JsonResult RefreshCRElements()
        {
            return MvcResultInfo.Execute(() =>
            {
                var q = Query.Matches("WellName", new BsonRegularExpression(new Regex("_CR", RegexOptions.IgnoreCase)));
                var wps = WellPIP.Populate<WellPIP>(q);
                List<string> WellNames = new List<string>();
                foreach (var p in wps)
                {
                    p.Save();
                    WellNames.Add(p.WellName);
                }

                return WellNames.ToArray();
            });
        }

        public JsonResult RefreshPIPs()
        {
            return MvcResultInfo.Execute(() =>
            {
                WellPIP pip = new WellPIP();
                return pip.RefreshPIPs();
            });
        }

        public JsonResult SyncPIPElement()
        {
            return MvcResultInfo.Execute(() =>
            {
                var pips = WellPIP.Populate<WellPIP>();
                foreach (var pip in pips)
                {
                    var wau = WellActivityUpdate.GetById(pip.WellName, pip.SequenceId, pip.PhaseNo, null, true, false);
                    foreach (var e in pip.Elements)
                    {
                        if (wau == null)
                            e.ResetAllocation(false, null, true, true, true);
                        else
                            e.ResetAllocation(true, wau, true, true, false);
                    }
                    pip.Save();
                }

                return "OK";
            });
        }

        public JsonResult Fix(bool init = false)
        {
            return MvcResultInfo.Execute(() =>
            {
                if (init == true)
                {
                    WellActivity.SyncSequenceId();

                    var was = WellActivity.Populate<WellActivity>();
                    foreach (var wa in was)
                    {
                        DateTime dtFrom = wa.PsSchedule.Start;
                        DateTime dtTo;
                        foreach (var ph in wa.Phases)
                        {
                            //if (ph.PlanSchedule == null || ph.PlanSchedule.Start.Year < 2000) ph.PlanSchedule = ph.PhSchedule;
                            dtTo = dtFrom.AddDays(ph.Plan.Days);
                            ph.PlanSchedule = new DateRange(dtFrom, dtTo);
                            //if (ph.PhSchedule == null || ph.PhSchedule.Start.Year < 2000) ph.PhSchedule = ph.PlanSchedule;
                            ph.LESchedule = ph.PhSchedule;
                            dtFrom = dtTo.AddDays(1);
                        }
                        wa.Save();
                    }
                }

                #region load last WR and update
                var wauKeys = WellActivityUpdate.Populate<WellActivityUpdate>()
                    .GroupBy(d => new { d.WellName, d.SequenceId, d.Phase.ActivityType })
                    .Select(d => new
                    {
                        Keys = d.Key,
                        UpdateVersion = d.Max(x => x.UpdateVersion)
                    })
                    .ToList();
                foreach (var wauKey in wauKeys)
                {
                    var wau = WellActivityUpdate.Get<WellActivityUpdate>(Query.And(
                            Query.EQ("WellName", wauKey.Keys.WellName),
                            Query.EQ("SequenceId", wauKey.Keys.SequenceId),
                            Query.EQ("Phase.ActivityType", wauKey.Keys.ActivityType),
                            Query.EQ("UpdateVersion", wauKey.UpdateVersion)
                        ));
                    if (wau != null)
                    {
                        wau.Calc();
                        wau.Save();
                    }
                }
                #endregion

                WellActivity.UpdateLESchedule();
                return "OK";
            });
        }

        public JsonResult LoadLS(string id = "OP201502V2")
        {
            return MvcResultInfo.Execute(() =>
            {
                string nowDate2 = DateTime.Now.ToString("yyyyMMddhhmm");
                var currentDB = DataHelper.Populate<WellActivity>("WEISWellActivities");
                List<BsonDocument> bdoncs = new List<BsonDocument>();
                foreach (var t in currentDB)
                {
                    bdoncs.Add(t.ToBsonDocument());
                }
                DataHelper.Save("_WellActivity_" + nowDate2, bdoncs);

                WellActivity.LoadLastSequence(currentDB, id);
                return "OK";
            });
        }

        public JsonResult RenameUserToLower()
        {
            return MvcResultInfo.Execute(() =>
            {
                var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                var userMgr = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
                var users = DataHelper.Populate<IdentityUser>("Users");
                foreach (var usr in users)
                {
                    var u = userMgr.FindByName(usr.UserName);
                    if (u != null)
                    {
                        u.UserName = u.UserName.ToLower();
                        u.Email = u.Email.ToLower();
                        userMgr.Update(u);
                    }
                }

                var wps = WEISPerson.Populate<WEISPerson>();
                foreach (var wp in wps)
                {
                    foreach (var pi in wp.PersonInfos)
                    {
                        pi.Email = pi.Email.ToLower();
                    }
                    wp.Save();
                }

                return "ok";
            });
        }



        public JsonResult MaskingWellAndRig()
        {
            return MvcResultInfo.Execute(() =>
            {
                ShellPOCDataGenerator();
                return "ok";
            });
        }

        public static double GetRandomNumber(double minimum, double maximum)
        {
            Random random = new Random();
            return random.NextDouble() * (maximum - minimum) + minimum;
        }
        public static void ShellPOCDataGenerator()
        {
            //DataHelper.Delete("WEISMasterRenames");
            //DataHelper.Delete("WEISMasterAliases");



            // rename master rigname
            var rigNames = MasterRigName.Populate<MasterRigName>();
            int i = 1;
            foreach (var r in rigNames)
            {
                string newName = string.Format("{0}{1}", "RIG", i.ToString("0000"));
                MasterRename m = new MasterRename();
                m.NewValue = newName;
                m.OldValue = r._id.ToString();
                m.TableID = "WEISRigNames";
                m.Save();

                MasterAlias ali = new MasterAlias();
                ali.MasterId = "RigName";
                ali.AliasId = m.NewValue;
                ali.Aliases.Add(m.OldValue);
                ali.Aliases.Add(m.NewValue);
                ali.Save();

                r.Delete();
                MasterRigName newInsert = new MasterRigName();
                newInsert = r;
                newInsert.Name = m.NewValue;
                newInsert._id = m.NewValue;
                newInsert.Save();
                i++;
            }

            // rename master projectname
            var projectNames = MasterProject.Populate<MasterProject>();
            i = 1;
            foreach (var r in projectNames)
            {
                string newName = string.Format("{0}{1}", "PROJECT", i.ToString("0000"));
                MasterRename m = new MasterRename();
                m.NewValue = newName;
                m.OldValue = r._id.ToString();
                m.TableID = "WEISProjectNames";
                m.Save();
                MasterAlias ali = new MasterAlias();
                ali.MasterId = "ProjectName";
                ali.AliasId = m.NewValue;
                ali.Aliases.Add(m.OldValue);
                ali.Aliases.Add(m.NewValue);
                ali.Save();

                ali = new MasterAlias();
                ali.MasterId = "Project";
                ali.AliasId = m.OldValue;
                ali.Aliases.Add(m.OldValue);
                ali.Aliases.Add(m.NewValue);
                ali.Save();

                r.Delete();
                MasterProject newInsert = new MasterProject();
                newInsert = r;
                newInsert.Name = m.NewValue;
                newInsert._id = m.NewValue;
                newInsert.Save();

                i++;
            }

            // rename master assetname
            var assets = MasterAssetName.Populate<MasterAssetName>();
            i = 1;
            foreach (var r in assets)
            {
                string newName = string.Format("{0}{1}", "ASSET", i.ToString("0000"));
                MasterRename m = new MasterRename();
                m.NewValue = newName;
                m.OldValue = r._id.ToString();
                m.TableID = "WEISAssetNames";
                m.Save();

                MasterAlias ali = new MasterAlias();
                ali.MasterId = "AssetName";
                ali.AliasId = m.NewValue;
                ali.Aliases.Add(m.OldValue);
                ali.Aliases.Add(m.NewValue);
                ali.Save();

                r.Delete();
                MasterAssetName newInsert = new MasterAssetName();
                newInsert = r;
                newInsert.Name = m.NewValue;
                newInsert._id = m.NewValue;
                newInsert.Save();

                i++;
            }


            // rename master wellMatch
            var wellnames = WellInfo.Populate<WellInfo>();
            i = 1;
            foreach (var r in wellnames)
            {
                string newName = string.Format("{0}{1}", "WELL", i.ToString("0000"));
                MasterRename m = new MasterRename();
                m.NewValue = newName;
                m.OldValue = r._id.ToString();
                m.TableID = "WEISWellNames";
                m.Save();

                MasterAlias ali = new MasterAlias();
                ali.MasterId = "WellName";
                ali.AliasId = m.NewValue;
                ali.Aliases.Add(m.OldValue);
                ali.Aliases.Add(m.NewValue);
                ali.Save();

                r.Delete();
                WellInfo newInsert = new WellInfo();
                newInsert = r;
                newInsert.Title = m.NewValue;
                newInsert._id = m.NewValue;
                newInsert.Save();

                i++;
            }

            // -----------------------------
            // cleansing wellactivity


            MasterAlias.MasterCleansing("WEISWellActivities", "WellName");
            MasterAlias.MasterCleansing("WEISWellActivities", "AssetName");
            MasterAlias.MasterCleansing("WEISWellActivities", "ProjectName");
            MasterAlias.MasterCleansing("WEISWellActivities", "RigName");
            MasterAlias.MasterCleansing("WEISWellActivities", "RigName", true, "OPHistories");
            MasterAlias.MasterCleansing("WEISWellActivities", "WellName", true, "OPHistories");


            // cleansing wellpip
            MasterAlias.MasterCleansing("WEISWellPIPs", "WellName");

            // cleansing wellactivityupdate
            MasterAlias.MasterCleansing("WEISWellActivityUpdates", "WellName");
            MasterAlias.MasterCleansing("WEISWellActivityUpdates", "AssetName");
            MasterAlias.MasterCleansing("WEISWellActivityUpdates", "Project");


            // cleansing wellactivityupdatemonthly
            MasterAlias.MasterCleansing("WEISWellActivityUpdatesMonthly", "WellName");
            MasterAlias.MasterCleansing("WEISWellActivityUpdatesMonthly", "AssetName");
            MasterAlias.MasterCleansing("WEISWellActivityUpdatesMonthly", "Project");

            // cleansing bizplan
            MasterAlias.MasterCleansing("WEISBizPlanActivities", "WellName");
            MasterAlias.MasterCleansing("WEISBizPlanActivities", "AssetName");
            MasterAlias.MasterCleansing("WEISBizPlanActivities", "ProjectName");
            MasterAlias.MasterCleansing("WEISBizPlanActivities", "RigName");
            MasterAlias.MasterCleansing("WEISBizPlanActivities", "RigName", true, "OPHistories");
            MasterAlias.MasterCleansing("WEISBizPlanActivities", "WellName", true, "OPHistories");

            // cleansing bizplan
            MasterAlias.MasterCleansing("WEISBizPlanAllocation", "WellName");
            MasterAlias.MasterCleansing("WEISBizPlanAllocation", "RigName");

        }
        public static void RecalcRandom(double minimum, double maximum)
        {
            double randVal = GetRandomNumber(minimum, maximum);

            var ts = WellActivity.Populate<WellActivity>(
                    );
            foreach (var t in ts)
            {
                Console.WriteLine("Calc Random WellActivity : " + t.WellName);
                foreach (var y in t.Phases)
                {
                    y.Plan.Cost = y.Plan.Cost + (randVal * y.Plan.Cost);
                    y.TQ.Cost = y.TQ.Cost + (randVal * y.TQ.Cost);
                    y.AFE.Cost = y.AFE.Cost + (randVal * y.AFE.Cost);
                    y.Actual.Cost = y.Actual.Cost + (randVal * y.Actual.Cost);
                    y.CalcOP.Cost = y.CalcOP.Cost + (randVal * y.CalcOP.Cost);
                    y.LE.Cost = y.LE.Cost + (randVal * y.LE.Cost);
                    y.LWE.Cost = y.LWE.Cost + (randVal * y.LWE.Cost);
                    y.LME.Cost = y.LME.Cost + (randVal * y.LME.Cost);
                }
                t.Save();
            }

            var waus = WellActivityUpdate.Populate<WellActivityUpdate>(
                    );
            foreach (var y in waus)
            {
                Console.WriteLine("Calc Random WellActivityUpdate : " + y.WellName);
                y.CurrentWeek.Cost = y.CurrentWeek.Cost + (randVal * y.CurrentWeek.Cost);
                y.Plan.Cost = y.Plan.Cost + (randVal * y.Plan.Cost);
                y.OP.Cost = y.OP.Cost + (randVal * y.OP.Cost);
                y.AFE.Cost = y.AFE.Cost + (randVal * y.AFE.Cost);
                y.Actual.Cost = y.Actual.Cost + (randVal * y.Actual.Cost);
                y.LastWeek.Cost = y.LastWeek.Cost + (randVal * y.LastWeek.Cost);
                y.TQ.Cost = y.TQ.Cost + (randVal * y.TQ.Cost);
                y.NPT.Cost = y.NPT.Cost + (randVal * y.NPT.Cost);


                y.Phase.TQ.Cost = y.Phase.TQ.Cost + (randVal * y.Phase.TQ.Cost);
                y.Phase.AFE.Cost = y.Phase.AFE.Cost + (randVal * y.Phase.AFE.Cost);
                y.Phase.Plan.Cost = y.Phase.Plan.Cost + (randVal * y.Phase.Plan.Cost);
                y.Phase.Actual.Cost = y.Phase.Actual.Cost + (randVal * y.Phase.Actual.Cost);
                y.Phase.CalcOP.Cost = y.Phase.CalcOP.Cost + (randVal * y.Phase.CalcOP.Cost);
                y.Phase.OP.Cost = y.Phase.OP.Cost + (randVal * y.Phase.OP.Cost);
                y.Phase.LE.Cost = y.Phase.LE.Cost + (randVal * y.Phase.LE.Cost);
                y.Phase.LWE.Cost = y.Phase.LWE.Cost + (randVal * y.Phase.LWE.Cost);
                y.Phase.LME.Cost = y.Phase.LME.Cost + (randVal * y.Phase.LME.Cost);

                y.Save();
            }


            var monss = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(
                   );
            foreach (var y in monss)
            {
                Console.WriteLine("Calc Random Montlhy LE : " + y.WellName);

                y.CurrentWeek.Cost = y.CurrentWeek.Cost + (randVal * y.CurrentWeek.Cost);
                y.CurrentMonth.Cost = y.CurrentMonth.Cost + (randVal * y.CurrentMonth.Cost);
                y.Plan.Cost = y.Plan.Cost + (randVal * y.Plan.Cost);
                y.OP.Cost = y.OP.Cost + (randVal * y.OP.Cost);
                y.AFE.Cost = y.AFE.Cost + (randVal * y.AFE.Cost);
                y.Actual.Cost = y.Actual.Cost + (randVal * y.Actual.Cost);
                y.LastWeek.Cost = y.LastWeek.Cost + (randVal * y.LastWeek.Cost);
                y.TQ.Cost = y.TQ.Cost + (randVal * y.TQ.Cost);
                y.NPT.Cost = y.NPT.Cost + (randVal * y.NPT.Cost);
                y.Delta.Cost = y.Delta.Cost + (randVal * y.Delta.Cost);
                y.CapitalReductionSCM.Cost = y.CapitalReductionSCM.Cost + (randVal * y.CapitalReductionSCM.Cost);
                y.CapitalReductionOther.Cost = y.CapitalReductionOther.Cost + (randVal * y.CapitalReductionOther.Cost);
                y.CapitalEfficiency.Cost = y.CapitalEfficiency.Cost + (randVal * y.CapitalEfficiency.Cost);
                y.Sequence.Cost = y.Sequence.Cost + (randVal * y.Sequence.Cost);


                y.BankedSavingsSupplyChainTransformation.Cost = y.BankedSavingsSupplyChainTransformation.Cost + (randVal * y.BankedSavingsSupplyChainTransformation.Cost);
                y.BankedSavingsCompetitiveScope.Cost = y.BankedSavingsCompetitiveScope.Cost + (randVal * y.BankedSavingsCompetitiveScope.Cost);
                y.BankedSavingsEfficientExecution.Cost = y.BankedSavingsEfficientExecution.Cost + (randVal * y.BankedSavingsEfficientExecution.Cost);
                y.BankedSavingsTechnologyAndInnovation.Cost = y.BankedSavingsTechnologyAndInnovation.Cost + (randVal * y.BankedSavingsTechnologyAndInnovation.Cost);
                y.RealizedPIPElemSupplyChainTransformation.Cost = y.RealizedPIPElemSupplyChainTransformation.Cost + (randVal * y.RealizedPIPElemSupplyChainTransformation.Cost);
                y.RealizedPIPElemCompetitiveScope.Cost = y.RealizedPIPElemCompetitiveScope.Cost + (randVal * y.RealizedPIPElemCompetitiveScope.Cost);

                y.RealizedPIPElemEfficientExecution.Cost = y.RealizedPIPElemEfficientExecution.Cost + (randVal * y.RealizedPIPElemEfficientExecution.Cost);
                y.RealizedPIPElemTechnologyAndInnovation.Cost = y.RealizedPIPElemTechnologyAndInnovation.Cost + (randVal * y.RealizedPIPElemTechnologyAndInnovation.Cost);
                y.BestInClass.Cost = y.BestInClass.Cost + (randVal * y.BestInClass.Cost);


                y.Phase.TQ.Cost = y.Phase.TQ.Cost + (randVal * y.Phase.TQ.Cost);
                y.Phase.AFE.Cost = y.Phase.AFE.Cost + (randVal * y.Phase.AFE.Cost);
                y.Phase.Plan.Cost = y.Phase.Plan.Cost + (randVal * y.Phase.Plan.Cost);
                y.Phase.Actual.Cost = y.Phase.Actual.Cost + (randVal * y.Phase.Actual.Cost);
                y.Phase.CalcOP.Cost = y.Phase.CalcOP.Cost + (randVal * y.Phase.CalcOP.Cost);
                y.Phase.OP.Cost = y.Phase.OP.Cost + (randVal * y.Phase.OP.Cost);
                y.Phase.LE.Cost = y.Phase.LE.Cost + (randVal * y.Phase.LE.Cost);
                y.Phase.LWE.Cost = y.Phase.LWE.Cost + (randVal * y.Phase.LWE.Cost);
                y.Phase.LME.Cost = y.Phase.LME.Cost + (randVal * y.Phase.LME.Cost);

                y.Save();
            }
        }

        public JsonResult SyncPhase()
        {
            return MvcResultInfo.Execute(() =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    foreach (var ph in wa.Phases)
                    {
                        var wau = WellActivityUpdate.GetById(wa.WellName, wa.UARigSequenceId, ph.PhaseNo, null, true, false);
                        if (wau == null)
                        {
                            ph.LE = new WellDrillData();
                        }
                        else
                        {
                            if (wau.CurrentWeek.Days == 0 && wau.CurrentWeek.Cost == 0)
                            {
                                wau.Calc();
                                var prev = wau.GetBefore();
                                if (prev != null)
                                {
                                    ph.LastWeek = prev.UpdateVersion;
                                    ph.PreviousWeek = prev.UpdateVersion.AddDays(-7);
                                    ph.LE = prev.CurrentWeek;
                                    ph.LWE = prev.LastWeek;
                                }
                            }
                            else
                            {
                                ph.LastWeek = wau.UpdateVersion;
                                ph.PreviousWeek = wau.UpdateVersion.AddDays(-7);
                                ph.LE = wau.CurrentWeek;
                                ph.LWE = wau.LastWeek;
                            }
                        }

                        var act = WellActivityActual.GetById(wa.WellName, wa.UARigSequenceId, null, true, false);
                        if (act == null)
                        {
                            ph.Actual = new WellDrillData();
                            ph.AFE = new WellDrillData();
                        }
                        else
                        {
                            var phAct = act.Actual.FirstOrDefault(d => d.PhaseNo.Equals(ph.PhaseNo));
                            if (phAct != null) ph.Actual = phAct.Data; else ph.Actual = new WellDrillData();
                            var phAFE = act.AFE.FirstOrDefault(d => d.PhaseNo.Equals(ph.PhaseNo));
                            //if (phAFE!=null && phAFE.Data.Days == 0 && phAFE.Data.Cost == 0)
                            //{
                            //    var bfr = act.GetBefore();
                            //    if (bfr != null)
                            //    {
                            //        var phAFEBefore = act.AFE.FirstOrDefault(d => d.PhaseNo.Equals(ph.PhaseNo));
                            //        if (phAFEBefore != null) phAFE = phAFEBefore;
                            //    }
                            //}
                            if (phAFE != null) ph.AFE = phAFE.Data; else ph.AFE = new WellDrillData();
                        }

                        if (ph.LevelOfEstimate == null) ph.LevelOfEstimate = "n/a";
                        if (ph.Plan.Cost == 0) ph.Plan.Cost = ph.OP.Cost;

                        ph.OP.Days = ph.PhSchedule.Days;
                    }
                    wa.Save();
                }
                return "OK";
            });
            //return MvcTools.ToJsonResult(email.SendEmail());
        }

        public JsonResult SendEmail(EmailModel email)
        {
            return MvcResultInfo.Execute(() =>
            {
                var s = email.SendEmail();
                if (s != "OK") throw new Exception(s);
                return "OK";
            });
            //return MvcTools.ToJsonResult(email.SendEmail());
        }

        public JsonResult DummyBC()
        {
            return MvcResultInfo.Execute(() =>
            {
                var email = ECIS.Client.WEIS.Email.Get<ECIS.Client.WEIS.Email>("DummyBC");
                var users = DataHelper.Populate<IdentityUser>("Users");
                var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                var userManager = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
                foreach (var u in users.Where(d => d.HasChangePassword == false))
                {
                    if (new string[] { "hector.romero", "navdeep", "eaciit" }.Contains(u.UserName.ToLower()) == false)
                    {
                        var np = Tools.GenerateRandomString(8);
                        //var np = "Password.1";
                        var user = userManager.FindByName(u.UserName);
                        var isFirstLogin = !user.HasChangePassword;
                        String hashpassword = userManager.PasswordHasher.HashPassword(np);
                        user.PasswordHash = hashpassword;
                        userManager.Update(user);

                        Dictionary<string, string> vars = new Dictionary<string, string>();
                        vars.Add("FullName", u.FullName);
                        vars.Add("Email", u.Email);
                        vars.Add("UserName", u.UserName);
                        vars.Add("Password", np);
                        vars.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("Index", "Home"));

                        var ri = email.Send(new string[] { u.Email }, new string[] { "hector.romero@shell.com" }, vars, null, "arief@eaciit.com");
                        //var ri = email.Send(new string[] { u.Email }, new string[] { "arief@eaciit.com" }, vars, null, "arief@eaciit.com");
                        if (ri.Result != "OK") throw new Exception(ri.Message + ri.Trace);
                    }
                }

                return "OK";
            });
            //return MvcTools.ToJsonResult(email.SendEmail());
        }

        public JsonResult DummyBC2()
        {
            return MvcResultInfo.Execute(() =>
            {
                var email = ECIS.Client.WEIS.Email.Get<ECIS.Client.WEIS.Email>("DummyBC");
                var users = DataHelper.Populate<IdentityUser>("Users");
                var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                var userManager = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
                foreach (var u in users)
                {
                    if (new string[] { "eaciit" }.Contains(u.UserName.ToLower()) == true)
                    {
                        //var np = Tools.GenerateRandomString(8);
                        var np = "W31sAdmin";
                        var user = userManager.FindByName(u.UserName);
                        var isFirstLogin = !user.HasChangePassword;
                        String hashpassword = userManager.PasswordHasher.HashPassword(np);
                        user.PasswordHash = hashpassword;
                        userManager.Update(user);

                        Dictionary<string, string> vars = new Dictionary<string, string>();
                        vars.Add("FullName", u.FullName);
                        vars.Add("Email", u.Email);
                        vars.Add("UserName", u.UserName);
                        vars.Add("Password", np);
                        vars.Add("UrlAction", "http://" + Request.Url.Host + Url.Action("Index", "Home"));

                        //var ri = email.Send(new string[] { u.Email }, new string[] { "hector.romero@eaciit.com" }, vars, null, "arief@eaciit.com");
                        //var ri = email.Send(new string[] { u.Email }, new string[] { "arief@eaciit.com" }, vars, null, "arief@eaciit.com");
                        //if (ri.Result != "OK") throw new Exception(ri.Message + ri.Trace);
                    }
                }

                return "OK";
            });
            //return MvcTools.ToJsonResult(email.SendEmail());
        }


        public ActionResult Validation()
        {
            return View();
        }

        public ResultInfo TestMail()
        {
            return Tools.SendMail("Default", "Send email from API", "Ini adalah kiriman email dari API",
                new string[] { "arief@eaciit.com", "adarmawan.2006@gmail.com", "ariefda@hotmail.com", "yoga.bangkit@eaciit.com", "noval.agung@eaciit.com" });
        }

        public JsonResult LoadPerson()
        {
            List<string> RoleIds = new List<string>();
            return MvcResultInfo.Execute(() =>
            {
                var tmps = DataHelper.Populate("_tmpPersons").OrderBy(d => d.GetString("WellName")).ToList();
                foreach (var tmp in tmps)
                {
                    var wellName = tmp.GetString("WellName");
                    var activity = tmp.GetString("WellActivity");
                    var email = tmp.GetString("Email");
                    WEISPerson person = null;
                    var wa = WellActivity.Get<WellActivity>(Query.And(
                            Query.EQ("WellName", wellName),
                            Query.EQ("Phases.ActivityType", activity)
                        ));
                    if (wa != null)
                    {
                        var act = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(activity));
                        if (act != null)
                        {
                            person = WEISPerson.Get<WEISPerson>(Query.And(
                                    Query.EQ("WellName", wellName),
                                    Query.EQ("ActivityType", activity)
                                ));
                            if (person == null)
                            {
                                person = new WEISPerson();
                                person.WellName = wellName;
                                person.SequenceId = wa.UARigSequenceId;
                                person.ActivityType = act.ActivityType;
                                person.PhaseNo = act.PhaseNo;
                                person.PersonInfos = new List<WEISPersonInfo>();
                            }
                            else
                            {
                                person.PersonInfos.RemoveAll(d => d.Email.Equals(""));
                            }
                            var p = person.PersonInfos.FirstOrDefault(d => d.Email.Equals(email));
                            if (p == null)
                            {
                                p = new WEISPersonInfo
                                {
                                    Email = email
                                };
                                person.PersonInfos.Add(p);
                            }
                            else
                            {
                                p.Email = email;
                            }
                            p.FullName = tmp.GetString("FullName");
                            p.RoleId = tmp.GetString("Role");
                            person.Save();
                            if (RoleIds.Contains(p.RoleId) == false)
                                RoleIds.Add(p.RoleId);

                            var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                            var userMgr = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
                            var user = DataHelper.Get<IdentityUser>("Users", Query.EQ("Email", email));
                            if (user == null)
                            {
                                userMgr.Create(new IdentityUser
                                {
                                    UserName = tmp.GetString("UserName").ToLower(),
                                    Email = email,
                                    FullName = tmp.GetString("FullName"),
                                    Enable = true,
                                    ADUser = false,
                                    ConfirmedAtUtc = Tools.ToUTC(DateTime.Now)
                                }, "Password.1");
                            }
                            else
                            {
                                user.FullName = tmp.GetString("FullName");
                                user.UserName = tmp.GetString("UserName").ToLower();
                                userMgr.Update(user);
                            }
                        }
                    }
                }

                foreach (var roleId in RoleIds)
                {
                    var role = WEISRole.Get<WEISRole>(roleId);
                    if (role == null)
                    {
                        role = new WEISRole
                        {
                            _id = roleId,
                            Title = roleId,
                            RoleName = roleId
                        };
                    }
                    else
                    {
                        role.RoleName = roleId;
                    }
                    role.Save();
                }

                return "OK";
            });
        }

        public JsonResult SetPerson()
        {
            return MvcResultInfo.Execute(() =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    foreach (var ph in wa.Phases)
                    {
                        List<IMongoQuery> qs = new List<IMongoQuery>();
                        qs.Add(Query.And(Query.EQ("ProjectInformation.Title", "Common Well Name"),
                            Query.EQ("ProjectInformation.Detail", wa.WellName)));
                        qs.Add(Query.And(Query.EQ("ProjectInformation.Title", "Activity Type"),
                            Query.EQ("ProjectInformation.Detail", ph.ActivityType)));
                        var qpip = Query.And(qs);
                        BsonDocument docRaw = DataHelper.Get("PIP", qpip);
                        if (docRaw != null)
                        {
                            qs.Clear();
                            qs.Add(Query.EQ("WellName", wa.WellName));
                            qs.Add(Query.EQ("ActivityType", ph.ActivityType));
                            var qpersons = Query.And(qs);
                            WEISPerson WEISPerson = WEISPerson.Get<WEISPerson>(qpersons);
                            if (WEISPerson == null)
                            {
                                WEISPerson = new WEISPerson();
                                WEISPerson.WellName = wa.WellName;
                                WEISPerson.ActivityType = ph.ActivityType;
                            }
                            WEISPerson.SequenceId = wa.UARigSequenceId;
                            WEISPerson.PhaseNo = ph.PhaseNo;
                            WEISPerson.PersonInfos.Clear();
                            List<BsonDocument> docInfos = docRaw.Get("ProjectInformation").AsBsonArray.Select(d => d.ToBsonDocument()).ToList();

                            setPersonFromDoc(WEISPerson, docInfos, "Team Lead", "TEAM-LEAD");
                            setPersonFromDoc(WEISPerson, docInfos, "Lead Engineer", "LEAD-ENG");
                            setPersonFromDoc(WEISPerson, docInfos, "Optimization Engineer", "OPTMZ-ENG");
                            WEISPerson.Save();
                        }
                    }
                }
                return "OK";
            });
        }

        private void setPersonFromDoc(WEISPerson p, List<BsonDocument> docs, string title, string roleid)
        {
            BsonDocument doc = docs.FirstOrDefault(d => d.GetString("Title").Equals(title));
            if (doc != null)
            {
                p.PersonInfos.Add(new WEISPersonInfo
                {
                    FullName = doc.GetString("Detail"),
                    Email = "",
                    RoleId = roleid
                });
            }
        }

        public JsonResult Load()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                WellActivity.LoadOP();
                return "OK";
            });
        }

        public JsonResult UpdatePIPAllocation()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var pips = WellPIP.Populate<WellPIP>();
                foreach (var pip in pips)
                {
                    pip.Save();
                }
                return "OK";
            });
        }

        public JsonResult UpdatePIPOtherInfo()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var pips = WellPIP.Populate<WellPIP>();
                var pipOlds = DataHelper.Populate<WellPIP>("WEISWellPIPs_copy");
                foreach (var pip in pips)
                {
                    var old = pipOlds.FirstOrDefault(d => d.WellName.Equals(pip.WellName) && d.ActivityType.Equals(pip.ActivityType));
                    if (old != null)
                    {
                        pip.PerformanceMetrics = old.PerformanceMetrics;
                        pip.ProjectMilestones = old.ProjectMilestones;
                        pip.Scaled = old.Scaled;
                        pip.Field = old.Field;
                        pip.Save();
                    }
                }
                return "OK";
            });
        }

        public JsonResult AddPerformanceUnitElement()
        {
            var pips = WellPIP.Populate<WellPIP>();
            var pipmst = DataHelper.Populate("_PIP_20150108");

            foreach (var pip in pips)
            {
                foreach (var elem in pip.Elements)
                {

                    var pm = pipmst.Where(x => BsonHelper.GetString(x, "Well_Name").Equals(pip.WellName) &&
                    BsonHelper.GetString(x, "Activity_Type").Equals(pip.ActivityType) &&
                    BsonHelper.GetString(x, "Idea").Equals(elem.Title)
                    );

                    if (pm.Count() > 0)
                        elem.PerformanceUnit = BsonHelper.GetString(pm.FirstOrDefault(), "Performance_Unit");
                    else
                        elem.PerformanceUnit = "";
                }
                pip.Save();
            }

            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                return pips;
            });
        }

        public JsonResult GetLast(int id)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var wa = WellActivity.Get<WellActivity>(id);
                if (wa != null) wa.GetUpdate(DateTime.Now, true);
                return wa;
            });
        }

        public JsonResult UpdateEDM()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var acts = ActivityMaster.Populate<ActivityMaster>();
                foreach (var a in acts)
                {
                    if (a._id.ToString().Contains("RISK")) a.EDMActivityId = "RISK";
                    else if (a._id.ToString().Contains("COM")) a.EDMActivityId = "COM";
                    else if (a._id.ToString().Contains("DRIL")) a.EDMActivityId = "DRL";
                    else if (a._id.ToString().Contains("ABAN")) a.EDMActivityId = "ABA";
                    else a.EDMActivityId = "";

                    a.Save();
                }
                return "OK";
            });
        }

        public JsonResult UpdateWaus()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var waus = WellActivityUpdate.Populate<WellActivityUpdate>().OrderBy(d => d.UpdateVersion);
                foreach (var wau in waus)
                {
                    wau.Calc();
                    wau.Save();
                }
                return "OK";
            });
        }

        public JsonResult UpdateActual()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var dt = Tools.GetNearestDay(new DateTime(2014, 12, 1), DayOfWeek.Monday);

                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    bool waSave = false;
                    var well = WellInfo.Get<WellInfo>(wa.WellName);
                    if (String.IsNullOrEmpty(well.EDMWellId) == false)
                    {
                        foreach (var ph in wa.Phases)
                        {
                            var act = ActivityMaster.Get<ActivityMaster>(ph.ActivityType);
                            if (String.IsNullOrEmpty(act.EDMActivityId) == false)
                            {
                                var qafe = Query.And(
                                        Query.EQ("WELLID", well.EDMWellId),
                                        Query.EQ("EVENTCODE", act.EDMActivityId)
                                    );
                                var afe = DataHelper.Get("_WellEventAFE", qafe, SortBy.Descending("DAYSONLOCATION"));
                                if (afe != null)
                                {
                                    waSave = true;
                                    var actual = WellActivityActual.GetById(wa.WellName, wa.UARigSequenceId, DateTime.Now, false, true);
                                    actual.Update(ph.PhaseNo,
                                        new WellDrillData { Days = afe.GetDouble("DAYSONLOCATION"), Cost = afe.GetDouble("ACTUALCOST") });
                                    actual.Save();

                                    ph.Actual = actual.GetActual(ph.PhaseNo).Data;
                                    ph.AFE = new WellDrillData
                                    {
                                        Days = afe.GetDouble("AFEDAYS"),
                                        Cost = afe.GetDouble("AFECOST")
                                    };
                                }
                            }
                        }
                    }
                    if (waSave) wa.Save();
                }

                var q = Query.EQ("UpdateVersion", dt);
                return WellActivityActual.Populate<WellActivityActual>(q);
            });
        }

        public JsonResult UpdateWell()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var wells = WellInfo.Populate<WellInfo>();
                foreach (var w in wells)
                {
                    var j = DataHelper.Get("WEISWellJunction", Query.EQ("OUR_WELL_ID", w._id.ToString()));
                    if (j != null && String.IsNullOrEmpty(j.GetString("SHELL_WELL_ID")) == false)
                    {
                        w.EDMWellId = j.GetString("SHELL_WELL_ID");
                        w.EDMWellName = j.GetString("SHELL_WELL_NAME");
                    }
                    w.Save();
                }
                var q = Query.GT("EDMWellId", "");
                return WellInfo.Populate<WellInfo>(q);
            });
        }

        public JsonResult UpdateOPDays()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    foreach (var ph in wa.Phases)
                    {
                        if (ph.PhSchedule.Start > Tools.DefaultDate && ph.PhSchedule.Finish > Tools.DefaultDate)
                        {
                            ph.OP.Days = ph.PhSchedule.Days;
                        }
                    }
                    wa.Save();
                }
                return "OK";
            });
        }

        public JsonResult UpdateTheme()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var themes = DataHelper.Populate("_tmpTheme");
                var wps = WellPIP.Populate<WellPIP>();
                foreach (var wp in wps)
                {
                    foreach (var e in wp.Elements)
                    {
                        var t = themes.FirstOrDefault(d => d.GetString("_id").Equals(e.Classification));
                        if (t != null)
                        {
                            e.Theme = t.GetString("Theme");
                        }
                    }
                    wp.Save();
                }
                return "OK";
            });
        }

        public JsonResult Update()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    //var phIndex = 1;
                    //foreach (var ph in wa.Phases)
                    //{
                    //    ph.PhaseNo = phIndex;
                    //    phIndex++;
                    //}
                    wa.Save();
                }

                var waus = WellActivityUpdate.Populate<WellActivityUpdate>();
                foreach (var wau in waus)
                {
                    var wa = WellActivity.GetByUniqueKey(wau.WellName, wau.SequenceId);
                    if (wa != null)
                    {
                        var a = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(wau.Phase.ActivityType));
                        if (a != null)
                        {
                            wau.Phase.PhaseNo = a.PhaseNo;
                            wau.Save();
                        }
                    }
                }

                var pips = WellPIP.Populate<WellPIP>();
                foreach (var pip in pips)
                {
                    var wa = WellActivity.GetByUniqueKey(pip.WellName, pip.SequenceId);
                    if (wa != null)
                    {
                        var a = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(pip.ActivityType));
                        if (a != null)
                        {
                            pip.PhaseNo = a.PhaseNo;
                            pip.Save();
                        }
                    }
                }
                return "OK";
            });
        }

        public JsonResult PIP(string tablename = "")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    foreach (var a in wa.Phases)
                    {
                        var wp = WellPIP.GetByOpsActivity(wa.UARigSequenceId, a.PhaseNo);
                        if (wp == null)
                        {
                            wp = new WellPIP();
                            wp.SequenceId = wa.UARigSequenceId;
                            wp.PhaseNo = a.PhaseNo;
                            wp.ActivityType = a.ActivityType;
                            wp.WellName = wa.WellName;
                        }
                        var q = Query.And(
                                Query.EQ("ProjectInformation.Detail", wa.WellName),
                                Query.EQ("ProjectInformation.Detail", a.ActivityType)
                            );
                        wp.Elements.Clear();
                        var pip = DataHelper.Get(tablename, q);
                        if (pip != null && pip.HasElement("OpportunityRiskData"))
                        {
                            var els = pip.GetValue("OpportunityRiskData").AsBsonArray;
                            foreach (var el in els)
                            {
                                var eld = el.ToBsonDocument();
                                var pe = new PIPElement();
                                pe.ElementId = wp.Elements.Count() + 1;
                                pe.Title = eld.GetString("Idea");
                                pe.Classification = eld.GetString("Classification");
                                pe.DaysPlanImprovement = -eld.GetDouble("Schedule_Opportunity");
                                pe.DaysPlanRisk = eld.GetDouble("Schedule_Risk");
                                pe.CostPlanImprovement = -eld.GetDouble("Cost_Opportunity");
                                pe.CostPlanRisk = eld.GetDouble("Cost_Risk");
                                wp.Elements.Add(pe);
                            }
                        }
                        wp.Status = wp.Elements.Count > 0 ? "Publish" : "Draft";
                        wp.Save();
                    }
                }
                return "OK";
            });
        }
        public JsonResult PIPLoad(string tablename = "")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var pips = DataHelper.Populate(tablename)
                    .OrderBy(d => d.GetString("Well_Name"))
                    .ThenBy(d => d.GetString("Activity_Type"))
                    .ToList();
                string prevWellName = "", prevActType = "";
                bool saveIt = false;
                WellPIP wp = null;
                foreach (var pip in pips)
                {
                    //bool newStart = false;
                    var wellname = pip.GetString("Well_Name");
                    var actType = pip.GetString("Activity_Type");
                    if (prevWellName != wellname || prevActType != actType)
                    {
                        if (wp != null && saveIt) wp.Save();
                        //newStart = true;
                        saveIt = true;
                        prevWellName = wellname;
                        prevActType = actType;
                        var qwas = new List<IMongoQuery>();
                        var SequenceId = "";
                        var PhaseNo = 0;
                        qwas.Add(Query.EQ("WellName", wellname));
                        qwas.Add(Query.EQ("Phases.ActivityType", actType));
                        var was = WellActivity.Populate<WellActivity>(Query.And(qwas))
                            .Select(d =>
                            {
                                d.Phases = d.Phases.Where(e => e.ActivityType.Equals(actType)).ToList();
                                return d;
                            })
                            //.OrderBy(d=>d.Phases[0].PlanSchedule.Finish)
                            .ToList();
                        if (was.Count > 0)
                        {
                            SequenceId = was[0].UARigSequenceId;
                            PhaseNo = was[0].Phases[0].PhaseNo;
                        }

                        saveIt = false;
                        wp = WellPIP.GetByWellActivity(wellname, actType);
                        if (wp == null)
                        {
                            saveIt = true;
                            wp = new WellPIP();
                            wp.WellName = wellname;
                            wp.SequenceId = SequenceId;
                            wp.PhaseNo = PhaseNo;
                            wp.ActivityType = actType;
                            wp.Status = "Publish";
                            //wp.Elements.Clear();
                        }
                    }

                    if (wp != null)
                    {
                        var idea = pip.GetString("Idea");
                        var pe = wp.Elements.FirstOrDefault(d => d.Title.Equals(idea));
                        if (pe == null)
                        {
                            pe = new PIPElement();
                            pe.ElementId = wp.Elements.Count() + 1;
                            pe.Title = idea;
                            pe.PerformanceUnit = pip.GetString("Performance_Unit");
                            pe.Theme = pip.GetString("Theme");
                            pe.Classification = pip.GetString("High_Level_Classification");
                            pe.DaysPlanImprovement = pip.GetDouble("Opportunity__Days_(-)");
                            pe.DaysPlanRisk = pip.GetDouble("Risk__Days_(+)");
                            pe.CostPlanImprovement = pip.GetDouble("Opportunity_Cost_MM__(-)");
                            pe.CostPlanRisk = pip.GetDouble("Risk_Cost_MM_(+)");
                            pe.LECost = pe.CostActualImprovement + pe.CostActualRisk;
                            pe.LEDays = pe.DaysActualImprovement + pe.DaysActualRisk;
                            pe.Period = new DateRange(
                                    pip.GetDateTime("Activity_Start"),
                                    pip.GetDateTime("Activity_End")
                                );
                            wp.Elements.Add(pe);
                            pe.ResetAllocation();
                        }
                    }
                }
                if (wp != null && saveIt) wp.Save();

                return "OK";
            });
        }

        public JsonResult UpdatePIPPhaseNo()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var pips = WellPIP.Populate<WellPIP>();
                foreach (var pip in pips)
                {
                    pip.Delete();
                    var q = Query.And(
                            Query.EQ("WellName", pip.WellName),
                            Query.EQ("Phases.ActivityType", pip.ActivityType)
                        );
                    var wa = WellActivity.Get<WellActivity>(q);
                    if (wa != null)
                    {
                        var ph = wa.Phases.FirstOrDefault(d => d.ActivityType.Equals(pip.ActivityType));
                        if (ph != null)
                        {
                            pip.SequenceId = wa.UARigSequenceId;
                            pip.PhaseNo = ph.PhaseNo;
                            pip.Status = "Publish";
                            pip.Save();
                        }
                        else
                        {
                            pip.Status = "Draft";
                            pip.Save();
                        }
                    }
                    else
                    {
                        pip.Status = "Draft";
                        pip.Save();
                    }
                }
                return "OK";
            });
        }

        public JsonResult UpdateTQ()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    foreach (var ph in wa.Phases)
                    {
                        var q = Query.And(
                                Query.EQ("Well_Name", wa.WellName),
                                Query.EQ("Activity_Type\r\n(PTW_Buckets)", ph.ActivityType)
                            );
                        var op = DataHelper.Get("OP", q);
                        if (op != null)
                        {
                            var TQ_Days = op.GetDouble("DURATION_TQ");
                            var TQ_Cost = op.GetDouble("TQ_Cost");
                            if (TQ_Days > 0)
                                ph.TQ.Days = TQ_Days;
                            if (TQ_Cost > 0)
                                ph.TQ.Cost = TQ_Cost;
                        }
                    }
                    wa.Save();
                }
                return "OK";
            });
        }

        public JsonResult UpdatePlan()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    foreach (var ph in wa.Phases)
                    {
                        var q = Query.And(
                                Query.EQ("Well_Name", wa.WellName),
                                Query.EQ("Activity_Type\r\n(PTW_Buckets)", ph.ActivityType)
                            );
                        var op = DataHelper.Get("OP", q);
                        if (op != null)
                        {
                            var Days = op.GetDouble("DURATION_TOTAL_");
                            var Cost = op.GetDouble("COST_TOTAL_");
                            if (ph.Plan == null) ph.Plan = new WellDrillData();
                            if (ph.Plan.Days == 0) ph.Plan.Days = Days;
                            if (ph.Plan.Cost == 0) ph.Plan.Cost = Cost;
                        }
                    }
                    wa.Save();
                }
                return "OK";
            });
        }

        public JsonResult TestConvertActual()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {

                #region Process Read Last Actual
                DataHelper.Delete("_OracleActual_LastData");
                List<WellActivityActual> activityActuals = new List<WellActivityActual>();
                List<WellActivityActual> actMax = new List<WellActivityActual>();

                DateTime date = new DateTime(2015, 01, 05);

                var y = DataHelper.Populate("_OracleActual").Where(x => BsonHelper.GetDateTime(x, "REPORTDATE") >= date);

                List<string> wells = y.Select(x => BsonHelper.GetString(x, "WELLNAME")).Distinct().ToList();

                List<BsonDocument> LastUpdater = new List<BsonDocument>();  // last update single Wellname and Code

                foreach (string x in wells)
                {
                    var wellSameCode = y.Where(n => BsonHelper.GetString(n, "WELLNAME").Equals(x)).ToList();
                    List<string> codes = wellSameCode.Select(u => BsonHelper.GetString(u, "EVENTCODE")).Distinct().ToList();
                    foreach (string code in codes)
                    {
                        var lastwellCode = y.Where(n => BsonHelper.GetString(n, "WELLNAME").Equals(x) && BsonHelper.GetString(n, "EVENTCODE").Equals(code)).OrderByDescending(h => BsonHelper.GetDateTime(h, "REPORTDATE")).FirstOrDefault();
                        LastUpdater.Add(lastwellCode);
                        DataHelper.Save("_OracleActual_LastData", lastwellCode.ToBsonDocument());
                    }
                }

                #endregion

                DataHelper.Delete("_OracleActual_Failed");
                DataHelper.Delete("_OracleActual_Success");
                foreach (var t in y)
                {
                    var actv = WellActivityActual.Convert(t);

                    if (actv == null)
                    {
                        DataHelper.Save("_OracleActual_Failed", t);
                    }
                    else
                    {
                        actv.Save();
                    }
                }
                return "OK";
            });
        }

        public object ErrorAllocation(object PIPId, int ElementId, string WellName, string ActivityType, string note, string SequenceId)
        {
            var ret = new { PIPId = PIPId, ElementId = ElementId, WellName = WellName, ActivityType = ActivityType, SequenceId = SequenceId, Note = note };
            return ret;
        }

        public JsonResult CheckNotValidAllocations()
        {
            List<object> res = new List<object>();
            //var q = Query.EQ("WellName", "BRUTUS A4 ST6");
            //var PIPs = WellPIP.Populate<WellPIP>(q);
            var PIPs = WellPIP.Populate<WellPIP>();
            foreach (var pip in PIPs)
            {
                foreach (var element in pip.Elements)
                {
                    var PeriodStart = element.Period.Start.ToString("MM-yyyy");
                    var PeriodFinish = element.Period.Finish.ToString("MM-yyyy");
                    var numOfAllocation = element.Allocations.Count;
                    var AllocationStart = element.Allocations.OrderBy(x => x.Period).FirstOrDefault().Period.ToString("MM-yyyy");
                    var AllocationFinish = element.Allocations.OrderByDescending(x => x.Period).FirstOrDefault().Period.ToString("MM-yyyy");
                    var dt = element.Period.Start;
                    int mthNumber = 1;
                    var loop = true;
                    while (loop)
                    {
                        if (dt.ToString("MM-yyyy") == PeriodFinish)
                        {
                            loop = false;
                        }
                        else
                        {
                            mthNumber++;
                            dt = dt.AddMonths(1);
                        }
                    }
                    //Check number of allocation
                    if (mthNumber > 0)
                    {
                        if (numOfAllocation != mthNumber)
                        {
                            res.Add(ErrorAllocation(pip._id, element.ElementId, pip.WellName, pip.ActivityType, "Number of Allocation doesnt match with period!", pip.SequenceId));
                        }
                        else
                        {
                            //Check Allocation start
                            if (PeriodStart != AllocationStart)
                            {
                                res.Add(ErrorAllocation(pip._id, element.ElementId, pip.WellName, pip.ActivityType, "Allocation Start doesnt match with Element Period Start", pip.SequenceId));
                            }
                            else
                            {
                                //Check Allocation finish
                                if (PeriodFinish != AllocationFinish)
                                {
                                    res.Add(ErrorAllocation(pip._id, element.ElementId, pip.WellName, pip.ActivityType, "Allocation Finish doesnt match with Element Period Finish", pip.SequenceId));
                                }
                            }
                        }
                    }
                }
            }

            //return MvcTools.ToJsonResult(res);
            return Json(new { Total = res.Count, Data = res }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult FixNotValidAllocations()
        {
            List<object> res = new List<object>();
            //var q = Query.EQ("WellName", "BRUTUS A4 ST6");
            //var PIPs = WellPIP.Populate<WellPIP>(q);
            var PIPs = WellPIP.Populate<WellPIP>();
            foreach (var pip in PIPs)
            {
                foreach (var element in pip.Elements)
                {
                    var PeriodStart = element.Period.Start.ToString("MM-yyyy");
                    var PeriodFinish = element.Period.Finish.ToString("MM-yyyy");
                    var numOfAllocation = element.Allocations.Count;
                    var AllocationStart = element.Allocations.OrderBy(x => x.Period).FirstOrDefault().Period.ToString("MM-yyyy");
                    var AllocationFinish = element.Allocations.OrderByDescending(x => x.Period).FirstOrDefault().Period.ToString("MM-yyyy");
                    var dt = element.Period.Start;
                    int mthNumber = 1;
                    var loop = true;
                    while (loop)
                    {
                        if (dt.ToString("MM-yyyy") == PeriodFinish)
                        {
                            loop = false;
                        }
                        else
                        {
                            mthNumber++;
                            dt = dt.AddMonths(1);
                        }
                    }
                    //Check number of allocation
                    if (mthNumber > 0)
                    {
                        if (numOfAllocation != mthNumber)
                        {
                            res.Add(ErrorAllocation(pip._id, element.ElementId, pip.WellName, pip.ActivityType, "Number of Allocation doesnt match with period!", pip.SequenceId));
                        }
                        else
                        {
                            //Check Allocation start
                            if (PeriodStart != AllocationStart)
                            {
                                res.Add(ErrorAllocation(pip._id, element.ElementId, pip.WellName, pip.ActivityType, "Allocation Start doesnt match with Element Period Start", pip.SequenceId));
                            }
                            else
                            {
                                //Check Allocation finish
                                if (PeriodFinish != AllocationFinish)
                                {
                                    res.Add(ErrorAllocation(pip._id, element.ElementId, pip.WellName, pip.ActivityType, "Allocation Finish doesnt match with Element Period Finish", pip.SequenceId));
                                }
                            }
                        }
                    }
                }
            }

            foreach (var x in res)
            {
                ResetSingleAllocation(x.ToBsonDocument().GetString("WellName"), x.ToBsonDocument().GetString("ActivityType"), x.ToBsonDocument().GetString("SequenceId"), x.ToBsonDocument().GetInt32("ElementId"));
            }

            //return MvcTools.ToJsonResult(res);
            return Json(new { Total = res.Count, FixedAllocations = res }, JsonRequestBehavior.AllowGet);
        }

        public static void ResetSingleAllocation(string WellName, string ActivityType, string SequenceId, int ElementId = 0)
        {

            List<IMongoQuery> qs = new List<IMongoQuery>();
            IMongoQuery q = Query.Null;


            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("ActivityType", ActivityType));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            q = Query.And(qs);

            List<PIPElement> ListElement = new List<PIPElement>();
            PIPElement Element = new PIPElement();
            List<PIPAllocation> Allocation = new List<PIPAllocation>();
            WellPIP NewPIP = new WellPIP();

            WellPIP pip = WellPIP.Get<WellPIP>(q);
            NewPIP = pip;
            foreach (var x in pip.Elements)
            {
                Element = x;
                if (ElementId != 0)
                {
                    if (x.ElementId == ElementId)
                        Element.Allocations = null;
                }
                ListElement.Add(Element);
            }
            NewPIP.Elements = ListElement;
            NewPIP.Save();
        }

        public JsonResult ResetAllocation()
        {
            return MvcResultInfo.Execute(() =>
            {
                #region reset single
                //string WellName = "RAM POWELL A7";
                //string SequenceId = "2670";
                //string ActivityType = "WHOLE COMPLETION EVENT";

                //List<IMongoQuery> qs = new List<IMongoQuery>();
                //IMongoQuery q = Query.Null;


                //qs.Add(Query.EQ("WellName", WellName));
                //qs.Add(Query.EQ("ActivityType", ActivityType));
                //qs.Add(Query.EQ("SequenceId", SequenceId));
                //q = Query.And(qs);

                //List<PIPElement> ListElement = new List<PIPElement>();
                //PIPElement Element = new PIPElement();
                //List<PIPAllocation> Allocation = new List<PIPAllocation>();
                //WellPIP NewPIP = new WellPIP();

                //WellPIP pip = WellPIP.Get<WellPIP>(q);
                //NewPIP = pip;
                //foreach (var x in pip.Elements)
                //{
                //    Element = x;
                //    Element.Allocations = null;
                //    ListElement.Add(Element);
                //}
                //NewPIP.Elements = ListElement;
                //NewPIP.Save();
                #endregion

                List<WellPIP> ListPIP = WellPIP.Populate<WellPIP>();
                WellPIP NewPIP = new WellPIP();
                PIPElement NewElement = new PIPElement();
                foreach (var x in ListPIP)
                {
                    NewPIP = x;
                    List<PIPElement> ListElement = new List<PIPElement>();
                    foreach (var y in x.Elements)
                    {
                        NewElement = y;
                        NewElement.Allocations = null;
                        ListElement.Add(NewElement);
                    }
                    NewPIP.Elements = ListElement;
                    NewPIP.Save();
                }
                return "OK";
            });
        }

        public JsonResult ResetEXTypeAllActivity()
        {
            return MvcResultInfo.Execute(() =>
            {
                List<WellActivity> WellActivities = WellPIP.Populate<WellActivity>();
                foreach (var x in WellActivities)
                {
                    x.EXType = "";
                    x.Save();
                }
                return "OK";
            });
        }

        public JsonResult ResetWellActivityUpdatePIP()
        {
            return MvcResultInfo.Execute(() =>
            {
                #region Old
                //List<WellActivityUpdate> ListWAU = WellActivityUpdate.Populate<WellActivityUpdate>();
                //WellActivityUpdate NewWAU = new WellActivityUpdate();
                //foreach (var x in ListWAU)
                //{
                //    NewWAU = x;
                //    List<IMongoQuery> qs = new List<IMongoQuery>();
                //    IMongoQuery q = Query.Null;
                //    qs.Add(Query.EQ("WellName", x.WellName));
                //    qs.Add(Query.EQ("ActivityType", x.Phase.ActivityType));
                //    qs.Add(Query.EQ("SequenceId", x.SequenceId));
                //    q = Query.And(qs);
                //    WellPIP PIP = WellPIP.Get<WellPIP>(q);
                //    if (PIP != null)
                //    {
                //        NewWAU.Elements = PIP.Elements;
                //        NewWAU.Save();
                //    }
                //}
                #endregion

                List<WellActivityUpdate> waus = WellActivityUpdate
                    .Populate<WellActivityUpdate>()
                    //.Where(d => d.WellName.Equals("RAM POWELL A7"))
                    .OrderBy(d => d.UpdateVersion)
                    .ToList();
                foreach (var x in waus)
                {
                    List<IMongoQuery> qs = new List<IMongoQuery>();
                    IMongoQuery q = Query.Null;
                    qs.Add(Query.EQ("WellName", x.WellName));
                    qs.Add(Query.EQ("ActivityType", x.Phase.ActivityType));
                    qs.Add(Query.EQ("SequenceId", x.SequenceId));
                    q = Query.And(qs);
                    WellPIP pip = WellPIP.Get<WellPIP>(q);
                    if (pip != null)
                    {
                        foreach (var e in x.Elements)
                        {
                            var pipElement = pip.Elements.FirstOrDefault(d => d.ElementId.Equals(e.ElementId));
                            if (pipElement != null)
                            {
                                e.Period = pipElement.Period;
                                e.Allocations = null;
                                e.ResetAllocation();
                                pipElement.DaysCurrentWeekImprovement = e.DaysCurrentWeekImprovement;
                                pipElement.DaysCurrentWeekRisk = e.DaysCurrentWeekRisk;
                                pipElement.CostCurrentWeekImprovement = e.CostCurrentWeekImprovement;
                                pipElement.CostCurrentWeekRisk = e.CostCurrentWeekRisk;
                                pipElement.Allocations = null;
                                pipElement.ResetAllocation();
                            }
                        }
                        pip.Save();
                    }
                    x.Save();
                }
                return "OK";
            });
        }

        public JsonResult UpdateUserPersonFromExcel(string logPath = @"D:\LogUser.txt", string inputpath = @"D:\Security Matrix (rev2).xlsx")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                #region 27012015 Update User Role, Security Matrix


                ECIS.Core.DataSerializer.ExtractionHelper e = new Core.DataSerializer.ExtractionHelper();
                var datauser = e.Extract(inputpath);

                System.Text.StringBuilder sb = new System.Text.StringBuilder();


                sb.Append("######## Update Security Matrix From Excel Files ######## \n");
                sb.Append("######## File Path : " + inputpath + " # \n");
                sb.Append("######## Log File Save To : " + logPath + " # \n\n\n");

                int i = 0;
                foreach (var t in datauser)
                {
                    i++;
                    string Rig_Name = BsonHelper.GetString(t, "Rig_Name");
                    string Well_Name = BsonHelper.GetString(t, "Well_Name");
                    Well_Name = Well_Name.Trim();
                    string u = "";

                    if (Well_Name.Equals("COUGAR A-01"))
                    {
                        u = "asda";
                        Console.WriteLine(Well_Name);

                    }
                    u = "sdasdas";
                    string Activity = BsonHelper.GetString(t, "Activity");
                    Activity = Activity.Trim();

                    string Role = BsonHelper.GetString(t, "Role");
                    Role = Role.Trim();

                    string User_Name = BsonHelper.GetString(t, "User_Name");
                    string Email = BsonHelper.GetString(t, "Email");
                    string Full_Name = BsonHelper.GetString(t, "Full_Name");

                    sb.Append("# --------- Processing ( " + i.ToString() + " ) ---------  #\n");
                    sb.Append("  Rigname  : " + Rig_Name + "\n");
                    sb.Append("  Well_Name  : " + Well_Name + "\n");
                    sb.Append("  Activity  : " + Activity + "\n");
                    sb.Append("  Role  : " + Role + "\n");
                    sb.Append("  User_Name  : " + User_Name + "\n");
                    sb.Append("  Email  : " + Email + "\n");
                    sb.Append("  Full_Name  : " + Full_Name + "\n");


                    // create WEISRoles
                    var role = WEISRole.Get<WEISRole>(Query.EQ("_id", Role));
                    if (role == null)
                    {
                        sb.Append("$ Saving new Role $" + "\n");
                        // insert new role 
                        WEISRole r = new WEISRole();
                        r._id = Role;
                        r.Title = Role;
                        r.RoleName = Role;
                        r.Save();
                    }
                    else
                    {
                        sb.Append("$ Role " + Role + " Already Exist in WEISRoles $" + "\n");
                    }

                    // create Users
                    var user = DataHelper.Populate("Users", Query.Matches("Email", new BsonRegularExpression(Email.Trim().ToLower(), "i")));
                    if (user == null || user.Count <= 0)
                    {
                        // new user
                        //Tools.
                        sb.Append("-- Saving new User --" + "\n");
                        var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                        var userMgr = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
                        IdentityUser use = new IdentityUser();

                        use.UserName = User_Name;
                        use.Email = Email;
                        use.FullName = Full_Name;
                        use.Enable = true;
                        use.ADUser = false;
                        use.ConfirmedAtUtc = Tools.ToUTC(DateTime.Now);
                        use.HasChangePassword = false;

                        String hashpassword = userMgr.PasswordHasher.HashPassword(Tools.GenerateRandomString(8));
                        use.PasswordHash = hashpassword;

                        DataHelper.Save("Users", use.ToBsonDocument());

                    }
                    else
                    {
                        sb.Append("$ User " + User_Name + "  Already Exist in WEIS Users $" + "\n");
                    }

                    // update WEISPersons
                    List<IMongoQuery> qs = new List<IMongoQuery>();
                    qs.Add(Query.EQ("WellName", Well_Name.Trim()));
                    var wellActivity = DataHelper.Populate<WellActivity>("WEISWellActivities", Query.And(qs));
                    if (wellActivity != null && wellActivity.Count > 0)
                    {
                        string seqid = wellActivity.FirstOrDefault().UARigSequenceId;
                        var phase = wellActivity.FirstOrDefault().Phases.Where(x => x.ActivityType.Equals(Activity));
                        if (phase != null && phase.Count() > 0)
                        {
                            #region contains on phases
                            string actvName = phase.FirstOrDefault().ActivityType;
                            int PhaseNo = phase.FirstOrDefault().PhaseNo;
                            qs.Add(Query.EQ("SequenceId", seqid));
                            qs.Add(Query.EQ("ActivityType", actvName));
                            qs.Add(Query.EQ("PhaseNo", PhaseNo));
                            var persons = DataHelper.Populate<WEISPerson>("WEISPersons", Query.And(qs));

                            if (persons != null && persons.Count() > 0)
                            {
                                var person = persons.FirstOrDefault();
                                int personExist = person.PersonInfos.Where(x => x.Email.Trim().ToLower().Equals(Email.Trim().ToLower())).Count();
                                if (personExist <= 0)
                                {
                                    // add personInfo
                                    WEISPersonInfo pi = new WEISPersonInfo();
                                    pi.Email = Email;
                                    pi.FullName = Full_Name;
                                    pi.RoleId = Role;

                                    person.PersonInfos.Add(pi);
                                    person.Save();
                                    sb.Append("++ Adding PersonInfo : " + pi.Email + " to WEISPerson : " + person._id + "\n");
                                }
                                else
                                {
                                    sb.Append("&& Person Info already in WEISPerson" + person._id + "\n");
                                }
                            }
                            else
                            {
                                // tidak ada di PersonInfo
                                sb.Append("===========> No data Person Info in WEISPerson ||" + "\n");

                                sb.Append("Insert new WEISPerson : " + Email + "\n");
                                WEISPerson person = new WEISPerson();
                                person.SequenceId = seqid;
                                person.WellName = Well_Name;
                                person.ActivityType = Activity;
                                person.PhaseNo = PhaseNo;

                                WEISPersonInfo info = new WEISPersonInfo();
                                info.FullName = Full_Name;
                                info.Email = Email;
                                info.RoleId = Role;
                                person.PersonInfos.Add(info);// = new List<WEISPersonInfo>();

                                person.Save();
                            }
                            #endregion
                        }
                        else
                        {
                            #region No phases
                            // Tidak ada Phases yg name Type nya sama
                            sb.Append("===========> No Phases ++ Activity Type : " + Activity + "\n");
                            sb.Append("Insert  new Activity to WEISActivities : " + Activity + "\n");

                            ActivityMaster actvMaster = new ActivityMaster();
                            actvMaster._id = Activity;
                            actvMaster.EDMActivityId = "";
                            actvMaster.Save();

                            qs.Add(Query.EQ("ActivityType", Activity));
                            var persons = DataHelper.Populate<WEISPerson>("WEISPersons", Query.And(qs));
                            if (persons != null && persons.Count() > 0)
                            {
                                var person = persons.FirstOrDefault();
                                int personExist = person.PersonInfos.Where(x => x.Email.Trim().ToLower().Equals(Email.Trim().ToLower())).Count();
                                if (personExist <= 0)
                                {
                                    // add personInfo
                                    WEISPersonInfo pi = new WEISPersonInfo();
                                    pi.Email = Email;
                                    pi.FullName = Full_Name;
                                    pi.RoleId = Role;

                                    person.PersonInfos.Add(pi);
                                    person.Save();
                                    sb.Append("++ Adding PersonInfo : " + pi.Email + " to WEISPerson : " + person._id + "\n");
                                }
                                else
                                {
                                    sb.Append("&& Person Info already in WEISPerson " + person._id + "\n");
                                }
                            }
                            else
                            {
                                // tidak ada di PersonInfo
                                sb.Append("===========> No data Person Info in WEISPerson ||" + "\n");

                                sb.Append("Insert new WEISPerson : " + Email + "\n");
                                WEISPerson person = new WEISPerson();
                                person.SequenceId = seqid;
                                person.WellName = Well_Name;
                                person.ActivityType = Activity;
                                person.PhaseNo = 0;

                                WEISPersonInfo info = new WEISPersonInfo();
                                info.FullName = Full_Name;
                                info.Email = Email;
                                info.RoleId = Role;
                                person.PersonInfos.Add(info);// = new List<WEISPersonInfo>();

                                person.Save();
                            }
                            #endregion

                        }

                    }
                    else
                    {
                        #region dont have wellActivity
                        sb.Append("===========> No Data Well Name && : " + Well_Name + "\n");
                        sb.Append("Insert new Well Name to WEISWellNames : " + Well_Name + "\n");
                        var well = DataHelper.Populate<WellInfo>("WEISWellNames", Query.EQ("_id", Well_Name));
                        if (well == null || well.Count() <= 0)
                        {
                            WellInfo wellName = new WellInfo();
                            wellName._id = Well_Name;
                            wellName.Save();
                        }
                        sb.Append("Insert  new Activity to WEISActivities : " + Activity + "\n");
                        var actv = DataHelper.Populate<ActivityMaster>("WEISActivities", Query.EQ("_id", Activity));
                        if (actv == null || actv.Count() <= 0)
                        {
                            ActivityMaster actvmstr = new ActivityMaster();
                            actvmstr._id = Activity;
                            actvmstr.Save();
                        }


                        qs.Add(Query.EQ("ActivityType", Activity));
                        var persons = DataHelper.Populate<WEISPerson>("WEISPersons", Query.And(qs));

                        if (persons != null && persons.Count() > 0)
                        {
                            var person = persons.FirstOrDefault();
                            int personExist = person.PersonInfos.Where(x => x.Email.Trim().ToLower().Equals(Email.Trim().ToLower())).Count();
                            if (personExist <= 0)
                            {
                                // add personInfo
                                WEISPersonInfo pi = new WEISPersonInfo();
                                pi.Email = Email;
                                pi.FullName = Full_Name;
                                pi.RoleId = Role;

                                person.PersonInfos.Add(pi);
                                person.Save();
                                sb.Append("++ Adding PersonInfo : " + pi.Email + " to WEISPerson : " + person._id + "\n");
                            }
                            else
                            {
                                sb.Append("&& Person Info already in WEISPerson " + person._id + "\n");
                            }
                        }
                        else
                        {
                            // tidak ada di PersonInfo
                            sb.Append("===========> No data Person Info in WEISPerson ||" + "\n");

                            sb.Append("Insert new WEISPerson : " + Email + "\n");
                            WEISPerson person = new WEISPerson();
                            person.SequenceId = "";
                            person.WellName = Well_Name;
                            person.ActivityType = Activity;
                            person.PhaseNo = 0;

                            WEISPersonInfo info = new WEISPersonInfo();
                            info.FullName = Full_Name;
                            info.Email = Email;
                            info.RoleId = Role;
                            person.PersonInfos.Add(info);// = new List<WEISPersonInfo>();

                            person.Save();
                        }
                        #endregion
                    }
                    sb.Append("# --------- End Processing -----------  #" + "\n\n\n");

                }
                //File.WriteAllText(logPath, sb.ToString());
                #endregion

                return sb.ToString();
            });
        }

        public JsonResult ResetSingleAllocation()
        {
            return MvcResultInfo.Execute(() =>
            {
                #region reset single
                string WellName = "PRINCESS P8";
                string SequenceId = "3062";
                string ActivityType = "WHOLE COMPLETION EVENT";
                int ElementId = 3;

                List<IMongoQuery> qs = new List<IMongoQuery>();
                IMongoQuery q = Query.Null;


                qs.Add(Query.EQ("WellName", WellName));
                qs.Add(Query.EQ("ActivityType", ActivityType));
                qs.Add(Query.EQ("SequenceId", SequenceId));
                q = Query.And(qs);

                List<PIPElement> ListElement = new List<PIPElement>();
                PIPElement Element = new PIPElement();
                List<PIPAllocation> Allocation = new List<PIPAllocation>();
                WellPIP NewPIP = new WellPIP();

                WellPIP pip = WellPIP.Get<WellPIP>(q);
                NewPIP = pip;
                foreach (var x in pip.Elements)
                {
                    Element = x;
                    if (Element.ElementId == ElementId)
                    {
                        Element.Allocations = null;
                    }
                    ListElement.Add(Element);
                }
                NewPIP.Elements = ListElement;
                NewPIP.Save();

                #endregion
                return "OK";
            });
        }


        public JsonResult LoadLastestSequence()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                ECIS.Core.DataSerializer.ExtractionHelper e = new ECIS.Core.DataSerializer.ExtractionHelper();
                string Path = @"D:\Latest_Sequence_Feb_1_2015.xlsx";
                Aspose.Cells.Workbook wb = new Aspose.Cells.Workbook(Path);
                var latestsequence = e.ExtractWithReplace(wb, "A1", "I341", 0);
                DateTime d = new DateTime(2015, 02, 01);

                DataHelper.Delete("OP" + d.ToString("yyyyMM") + "V2");

                foreach (var t in latestsequence)
                {
                    DataHelper.Save("OP" + d.ToString("yyyyMM") + "V2", t);
                }

                var rows = DataHelper.Populate("OP201502V2");
                foreach (var y in rows)
                {
                    DateTime sd = new DateTime();
                    sd = DateTime.ParseExact(BsonHelper.GetString(y, "Start_Date"), "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                    DateTime ed = new DateTime();
                    ed = DateTime.ParseExact(BsonHelper.GetString(y, "End_Date"), "yyyy-MM-dd hh:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                    y.Set("Start_Date", Tools.ToUTC(sd));
                    y.Set("End_Date", Tools.ToUTC(ed));
                    DataHelper.Save("OP" + d.ToString("yyyyMM") + "V2", y);
                }

                return latestsequence.ToString();
            });

        }

        public JsonResult BuildThemeNameTable()
        {
            var wp = WellPIP.Populate<WellPIP>()
                    .SelectMany(d => d.Elements, (d, e) => new
                    {
                        Theme = e.Theme
                    })
                    .Distinct()
                    .Select(x => x.Theme)
                    .ToList();

            foreach (var t in wp)
            {
                var theme = new WellPIPThemes();
                theme.Name = t;
                theme.Save();
            }
            return Json(wp, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ReFixElementAllocation()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var PIP = WellPIP.Populate<WellPIP>();
                List<string> result = new List<string>();
                foreach (var x in PIP)
                {
                    WellPIP wp = new WellPIP();
                    var WellName = x.WellName;
                    var SequenceId = x.SequenceId;
                    var PhaseNo = x.PhaseNo;

                    wp = x;

                    if (x.Elements.Count > 0)
                    {
                        wp.Elements = CheckElementAllocation(x.Elements);
                        wp.Save();
                    }
                    if (wp.Elements != x.Elements)
                    {
                        result.Add(WellName + "|" + x.ActivityType + "|" + SequenceId);
                    }


                    List<IMongoQuery> queries = new List<IMongoQuery>();
                    queries.Add(Query.EQ("WellName", WellName));
                    queries.Add(Query.EQ("SequenceId", SequenceId));
                    queries.Add(Query.EQ("Phase.PhaseNo", PhaseNo));
                    queries.Add(Query.EQ("Phase.ActivityType", x.ActivityType));
                    var queryWAU = Query.And(queries);

                    var wau = WellActivityUpdate.Populate<WellActivityUpdate>(queryWAU)
                                .OrderByDescending(d => d.UpdateVersion)
                                .FirstOrDefault();
                    if (wau != null)
                    {
                        if (wau.Elements.Count > 0)
                        {
                            wau.Elements = CheckElementAllocation(wau.Elements);
                            wau.Save();
                        }
                    }

                }

                return result;
            });
        }

        private static List<PIPElement> CheckElementAllocation(List<PIPElement> element)
        {
            List<PIPElement> NewElement = new List<PIPElement>();
            foreach (var e in element)
            {
                bool isEdited = false;
                PIPElement el = new PIPElement();
                el = e;
                if (e.Allocations.Count > 0)
                {
                    foreach (var alloc in e.Allocations)
                    {
                        if (alloc.LECost != 0 || alloc.LEDays != 0)
                        {
                            isEdited = true;
                        }
                    }
                }

                if (!isEdited)
                {
                    if (e.Allocations.Count > 0)
                    {
                        el.Allocations = ReFixAllocation(e.Allocations);
                    }
                }
                NewElement.Add(el);
            }

            return NewElement;
        }

        private static List<PIPAllocation> ReFixAllocation(List<PIPAllocation> allocations)
        {
            List<PIPAllocation> NewAllocation = new List<PIPAllocation>();
            foreach (var a in allocations)
            {
                PIPAllocation alloc = new PIPAllocation();
                var LEDays = a.DaysPlanImprovement + a.DaysPlanRisk;
                var LECost = a.CostPlanImprovement + a.CostPlanRisk;
                alloc.DaysPlanImprovement = a.DaysPlanImprovement;
                alloc.DaysPlanRisk = a.DaysPlanRisk;
                alloc.CostPlanImprovement = a.CostPlanImprovement;
                alloc.CostPlanRisk = a.CostPlanRisk;
                alloc.LEDays = LEDays;
                alloc.LECost = LECost;
                alloc.AllocationID = a.AllocationID;
                alloc.Period = a.Period;
                NewAllocation.Add(alloc);
            }

            return NewAllocation;
        }

        public List<PIPAllocation> ResetAllocationWithoutLE(PIPElement Element)
        {
            var dt = Element.Period.Start;
            int mthNumber = 1;
            while (dt < Element.Period.Finish)
            {
                mthNumber++;
                dt = dt.AddMonths(1);
            }

            var CostPlanImprovement = Element.CostPlanImprovement;
            var CostPlanRisk = Element.CostPlanRisk;
            var DaysPlanImprovement = Element.DaysPlanImprovement;
            var DaysPlanRisk = Element.DaysPlanRisk;

            //if (DaysCurrentWeekImprovement == 0) DaysCurrentWeekImprovement = Element.DaysPlanImprovement;
            //if (DaysCurrentWeekRisk == 0) DaysCurrentWeekRisk = Element.DaysPlanRisk;
            //if (CostCurrentWeekImprovement == 0) CostCurrentWeekImprovement = Element.CostPlanImprovement;
            //if (CostCurrentWeekRisk == 0) CostCurrentWeekRisk = Element.CostPlanRisk;

            var newAllocations = new List<PIPAllocation>();
            var totalAlloc = new PIPAllocation();
            for (var mthIdx = 0; mthIdx < mthNumber; mthIdx++)
            {
                dt = Element.Period.Start.AddMonths(mthIdx);
                if (dt > Element.Period.Finish) dt = Element.Period.Finish;
                var newAlloc = Element.Allocations.FirstOrDefault(d => d.Period.Year.Equals(dt.Year) && d.Period.Month.Equals(dt.Month));
                if (newAlloc == null)
                {
                    newAlloc = new PIPAllocation
                    {
                        AllocationID = mthIdx + 1,
                        Period = dt,
                        LEDays = 0, //Math.Round(Tools.Div(DaysCurrentWeekImprovement + DaysCurrentWeekRisk, mthNumber), 1),
                        LECost = 0 //Math.Round(Tools.Div(CostCurrentWeekImprovement + CostCurrentWeekRisk, mthNumber), 1)
                    };
                }

                newAlloc.CostPlanImprovement = Tools.Div(CostPlanImprovement, mthNumber);
                newAlloc.CostPlanRisk = Tools.Div(CostPlanRisk, mthNumber);
                newAlloc.DaysPlanImprovement = Tools.Div(DaysPlanImprovement, mthNumber);
                newAlloc.DaysPlanRisk = Tools.Div(DaysPlanRisk, mthNumber);

                totalAlloc.CostPlanImprovement += newAlloc.CostPlanImprovement;
                totalAlloc.CostPlanRisk += newAlloc.CostPlanRisk;
                totalAlloc.DaysPlanImprovement += newAlloc.DaysPlanImprovement;
                totalAlloc.DaysPlanRisk += newAlloc.DaysPlanRisk;

                if (totalAlloc.CostPlanImprovement < CostPlanImprovement)
                {
                    newAlloc.CostPlanImprovement -= totalAlloc.CostPlanImprovement - CostPlanImprovement;
                    totalAlloc.CostPlanImprovement = CostPlanImprovement;
                }

                if (totalAlloc.CostPlanRisk > CostPlanRisk)
                {
                    newAlloc.CostPlanRisk -= totalAlloc.CostPlanRisk - CostPlanRisk;
                    totalAlloc.CostPlanRisk = CostPlanRisk;
                }

                if (totalAlloc.DaysPlanImprovement < DaysPlanImprovement)
                {
                    newAlloc.DaysPlanImprovement -= totalAlloc.DaysPlanImprovement - DaysPlanImprovement;
                    totalAlloc.DaysPlanImprovement = DaysPlanImprovement;
                }

                if (totalAlloc.DaysPlanRisk > DaysPlanRisk)
                {
                    newAlloc.DaysPlanRisk -= totalAlloc.DaysPlanRisk - DaysPlanRisk;
                    totalAlloc.DaysPlanRisk = DaysPlanRisk;
                }
                newAllocations.Add(newAlloc);

            }

            return newAllocations;

        }

        public JsonResult CheckAndApplyisLEActual()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var wellactv = WellActivity.Populate<WellActivity>();

                foreach (var wa in wellactv)
                {
                    foreach (var p in wa.Phases.Where(x => x.IsActualLE == false))
                    {
                        if (WellActivity.isHaveWeeklyReport(wa.WellName, wa.UARigSequenceId, p.ActivityType))
                        {
                            p.IsActualLE = true;
                        }
                        else
                        {
                            if (WellActivity.isHaveMonthlyLEReport(wa.WellName, wa.UARigSequenceId, p.ActivityType))
                            {
                                p.IsActualLE = true;
                            }
                            else
                            {
                                p.IsActualLE = false;
                            }
                        }
                    }
                }

                var weeks = WellActivityUpdate.Populate<WellActivityUpdate>();
                List<string> yyy = new List<string>();
                yyy.Add("updateLeStatus");
                foreach (var w in weeks)
                {
                    w.Save(references: yyy.ToArray());
                }
                return "OK";
            });
        }

        public JsonResult ResetPIPType()
        {
            var pip = WellPIP.Populate<WellPIP>();
            foreach (var x in pip)
            {
                var newpip = x;
                newpip.Type = "Efficient";
                newpip.Save();
            }
            return null;
        }

        public JsonResult ResetNullPIPType()
        {
            var pip = WellPIP.Populate<WellPIP>();
            foreach (var x in pip)
            {
                if (x.Type == null || x.Type == "")
                {
                    var newpip = x;
                    newpip.Type = "Efficient";
                    newpip.Save();
                }
            }
            return null;
        }

        public JsonResult ReFixElementAllocationWithoutLE()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                //List<IMongoQuery> q1 = new List<IMongoQuery>();
                //q1.Add(Query.EQ("WellName", "APPO PNE3"));
                //q1.Add(Query.EQ("Phases.ActivityType", "WHOLE DRILLING EVENT"));
                //var q2 = Query.And(q1);
                //var WA = WellActivity.Populate<WellActivity>(q2);
                var WA = WellActivity.Populate<WellActivity>();
                foreach (var eachWA in WA)
                {
                    var WellName = eachWA.WellName;
                    var SequenceId = eachWA.UARigSequenceId;
                    foreach (var phase in eachWA.Phases)
                    {
                        var PhaseNo = phase.PhaseNo;
                        var ActivityType = phase.ActivityType;

                        List<IMongoQuery> queries = new List<IMongoQuery>();
                        queries.Add(Query.EQ("WellName", WellName));
                        queries.Add(Query.EQ("SequenceId", SequenceId));
                        queries.Add(Query.EQ("PhaseNo", PhaseNo));
                        queries.Add(Query.EQ("ActivityType", ActivityType));
                        var query = Query.And(queries);

                        #region PIP

                        var PIP = WellPIP.Get<WellPIP>(query);
                        var newPIP = PIP;
                        var NewPIPElements = new List<PIPElement>();
                        if (PIP != null)
                        {
                            if (PIP.Elements != null && PIP.Elements.Count > 0)
                            {
                                foreach (var el in PIP.Elements)
                                {
                                    var NewElement = el;
                                    NewElement.Allocations = ResetAllocationWithoutLE(el);
                                    NewPIPElements.Add(NewElement);
                                }
                                newPIP.Elements = NewPIPElements;

                                DataHelper.Save(PIP.TableName, newPIP.ToBsonDocument());
                            }
                        }
                        #endregion

                        List<IMongoQuery> queriesWAU = new List<IMongoQuery>();
                        queriesWAU.Add(Query.EQ("WellName", WellName));
                        queriesWAU.Add(Query.EQ("SequenceId", SequenceId));
                        queriesWAU.Add(Query.EQ("Phase.PhaseNo", PhaseNo));
                        queriesWAU.Add(Query.EQ("Phase.ActivityType", ActivityType));
                        var queryWAU = Query.And(queriesWAU);

                        var WAU = WellActivityUpdate.Populate<WellActivityUpdate>(queryWAU)
                                    .OrderByDescending(x => x.UpdateVersion);
                        if (WAU != null && WAU.Count() > 0)
                        {
                            var LatestWAU = WAU.FirstOrDefault();
                            if (LatestWAU.Elements != null && LatestWAU.Elements.Count > 0)
                            {
                                //LatestWAU.Elements = NewPIPElements;
                                foreach (var t in NewPIPElements)
                                {
                                    LatestWAU.Elements.Add(t);
                                }
                            }
                            else
                            {
                                LatestWAU.Elements = new List<PIPElement>();
                                foreach (var t in NewPIPElements)
                                {
                                    LatestWAU.Elements.Add(t);
                                }
                                //LatestWAU.Elements = NewPIPElements;
                            }
                            DataHelper.Save(LatestWAU.TableName, LatestWAU.ToBsonDocument());
                        }


                    }
                }

                return "ok";


                //var PIP = WellPIP.Populate<WellPIP>();
                //List<string> result = new List<string>();
                //foreach (var x in PIP)
                //{
                //    WellPIP wp = new WellPIP();
                //    var WellName = x.WellName;
                //    var SequenceId = x.SequenceId;
                //    var PhaseNo = x.PhaseNo;

                //    wp = x;

                //    if (x.Elements.Count > 0)
                //    {
                //        wp.Elements = CheckElementAllocation(x.Elements);
                //        wp.Save();
                //    }
                //    if (wp.Elements != x.Elements)
                //    {
                //        result.Add(WellName + "|" + x.ActivityType + "|" + SequenceId);
                //    }


                //    List<IMongoQuery> queries = new List<IMongoQuery>();
                //    queries.Add(Query.EQ("WellName", WellName));
                //    queries.Add(Query.EQ("SequenceId", SequenceId));
                //    queries.Add(Query.EQ("Phase.PhaseNo", PhaseNo));
                //    queries.Add(Query.EQ("Phase.ActivityType", x.ActivityType));
                //    var queryWAU = Query.And(queries);

                //    var wau = WellActivityUpdate.Populate<WellActivityUpdate>(queryWAU)
                //                .OrderByDescending(d => d.UpdateVersion)
                //                .FirstOrDefault();
                //    if (wau != null)
                //    {
                //        if (wau.Elements.Count > 0)
                //        {
                //            wau.Elements = CheckElementAllocation(wau.Elements);
                //            wau.Save();
                //        }
                //    }

                //}

                //return result;
            });
        }

        public JsonResult CheckEmptyLSFromWellPlan()
        {
            return MvcResultInfo.Execute(() =>
            {
                var qs = Query.And(
                        Query.EQ("Phases.PhSchedule.Start", Tools.DefaultDate),
                        Query.EQ("Phases.PhSchedule.Finish", Tools.DefaultDate)
                        );
                var wa = WellActivity.Populate<WellActivity>(qs);
                var countPhases = wa.SelectMany(x => x.Phases, (x, p) => new
                    {
                        x.WellName,
                        x.UARigSequenceId,
                        p.ActivityType,
                        p.PhaseNo
                    })
                    .ToList();
                return countPhases.Count;
            });
        }

        public JsonResult CountEmptyLSThatHaveWAU()
        {
            return MvcResultInfo.Execute(() =>
            {
                var q = Query.And(
                        Query.EQ("Phases.PhSchedule.Start", Tools.DefaultDate),
                        Query.EQ("Phases.PhSchedule.Finish", Tools.DefaultDate),
                        Query.GT("Phases.LastWeek", Tools.DefaultDate)
                        );
                var wa = WellActivity.Populate<WellActivity>();
                var countPhases = wa.SelectMany(x => x.Phases, (x, p) => new
                {
                    x.WellName,
                    p.PhSchedule,
                    p.LastWeek,
                    p.PhaseNo,
                    p.ActivityType,
                    x.UARigSequenceId
                }).Where(x => x.PhSchedule.Start.Equals(Tools.DefaultDate) && x.PhSchedule.Finish.Equals(Tools.DefaultDate) && x.LastWeek > Tools.DefaultDate)
                    .ToList();
                return new
                {
                    Count = countPhases.Count,
                    Data = countPhases
                };
            });
        }

        public JsonResult SyncEmptyLSWithWAU()
        {
            return MvcResultInfo.Execute(() =>
            {
                var qs = Query.And(
                        Query.EQ("Phases.PhSchedule.Start", Tools.DefaultDate),
                        Query.GT("Phases.LastWeek", Tools.DefaultDate)
                        );
                var wa = WellActivity.Populate<WellActivity>(qs);
                Int32 fixedPhases = 0;
                if (wa != null && wa.Count > 0)
                {
                    foreach (var a in wa)
                    {
                        //var Phases = new List<WellActivityPhase>();
                        if (a.Phases != null && a.Phases.Count > 0)
                        {
                            foreach (var p in a.Phases)
                            {
                                if (p.PhSchedule.Start == Tools.DefaultDate && p.LastWeek > Tools.DefaultDate)
                                {

                                    if (p.LE.Days != 0 || p.LE.Cost != 0)
                                    {
                                        p.PhSchedule = p.LESchedule;
                                        p.OP = p.LE;
                                    }
                                    else
                                    {
                                        //get from WAU
                                        var getWAU = WellActivityUpdate.GetById(a.WellName, a.UARigSequenceId, p.PhaseNo, null, true);

                                        if (getWAU != null)
                                        {

                                            if (getWAU.Phase.PhSchedule.Start != Tools.DefaultDate && getWAU.Phase.PhSchedule.Finish != Tools.DefaultDate)
                                            {
                                                p.PhSchedule.Start = getWAU.Phase.PhSchedule.Start;
                                                p.PhSchedule.Finish = getWAU.Phase.PhSchedule.Finish;
                                                p.OP = getWAU.OP;

                                                fixedPhases++;
                                            }
                                            else
                                            {
                                                var finish = getWAU.UpdateVersion;
                                                var start = getWAU.UpdateVersion.AddDays(-1 * getWAU.CurrentWeek.Days);
                                                p.PhSchedule.Start = start;
                                                p.PhSchedule.Finish = finish;
                                                p.OP = getWAU.OP;
                                                fixedPhases++;
                                            }
                                        }
                                    }


                                }
                                //Phases.Add(p);
                            }
                        }
                        //a.Phases = Phases;
                        a.Save();
                        //DataHelper.Delete(a.TableName, Query.EQ("_id", a._id.ToString()));
                        //DataHelper.Save(a.TableName, a.ToBsonDocument());
                    }
                }
                return fixedPhases;
            });
        }

        public JsonResult SetDefaultOPData(string OPType)
        {
            return MvcResultInfo.Execute(() =>
            {
                try
                {
                    Config con = new Config();
                    con._id = "DefaultOP";
                    con.ConfigModule = "WEIS";
                    con.ConfigValue = OPType;
                    con.Save();
                    return "Shared Config Table Default OP Set to : " + OPType;

                }
                catch (Exception ex)
                {
                    return ex.Message;

                }
            });
        }


        public void SetMaturityRiskNewVersionVoid()
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

        public JsonResult GenerateDefaultOPCollections()
        {
            return MvcResultInfo.Execute(() =>
            {
                try
                {
                    List<string> datas = new List<string>();
                    List<BsonDocument> docs = new List<BsonDocument>();
                    docs.Add(new BsonDocument("_id", "OP14"));
                    docs.Add(new BsonDocument("_id", "OP15"));
                    docs.Add(new BsonDocument("_id", "OP16"));

                    DataHelper.Save("WEISOPs", docs);
                    return "Master OP Collections already created";

                }
                catch (Exception ex)
                {
                    return ex.Message;

                }
            });
        }

        public JsonResult SetHistoryForCurrentOP14WellPlan()
        {
            return MvcResultInfo.Execute(() =>
            {
                try
                {
                    var wellPlans = WellActivity.Populate<WellActivity>();
                    //DataHelper.Delete("WEISWellActivities");
                    foreach (var t in wellPlans)
                    {
                        foreach (var g in t.Phases)
                        {
                            //if (g.OPHistories.Any())
                            //{
                            //    if (g.OPHistories.Where(x => x.Type.Equals("OP14")).Any())
                            //    {
                            //        var op14his = g.OPHistories.Where(x => x.Type.Equals("OP14")).FirstOrDefault();
                            //        op14his.Type = op14his.Type;
                            //        op14his.OP = g.CalcOP;
                            //        //op14his.OPSchedule = g.CalcOPSchedule;
                            //    }
                            //}
                            //else
                            //{
                            //    g.OPHistories = new List<OPHistory>();
                            //    var calcOP = g.CalcOP;
                            //    var calcOPSchedule = g.CalcOPSchedule;
                            //    OPHistory op = new OPHistory();
                            //    op.Type = "OP14";
                            //    op.OP = calcOP;
                            //    //op.OPSchedule = calcOPSchedule;
                            //    g.OPHistories.Add(op);
                            //}
                        }
                        t.Save();

                    }
                    return "OP Histories for Current WellPlan already binded";

                }
                catch (Exception ex)
                {
                    return ex.Message;

                }
            });
        }

        public JsonResult SetDefaultAssignOP()
        {
            return MvcResultInfo.Execute(() =>
            {
                List<string> op = new List<string>();
                op.Add("OP14"); op.Add("OP15");
                var wps = WellPIP.Populate<WellPIP>()
                    .SelectMany(d => d.Elements, (d, x) =>
                    {
                        x.AssignTOOps = op;
                        d.Save();
                        return d;
                    });
                var i = 0;
                foreach (var r in wps)
                {
                    r.Elements = r.Elements;
                    r.Save();
                    i++;
                }
                GenerateDefaultAssignOP();
                return "OK";
            });
        }

        public JsonResult ResetBaseOPinWellPIP()
        {
            return MvcResultInfo.Execute(() =>
            {
                var pips = WellPIP.Populate<WellPIP>();
                List<BsonDocument> docs = new List<BsonDocument>();
                foreach (var p in pips)
                {
                    foreach (var e in p.Elements)
                    {
                        e.AssignTOOps = new List<string>();
                    }
                    docs.Add(p.ToBsonDocument());
                }
                DataHelper.Save("WEISWellPIPs", docs);
                return "ResetBaseOPinWellPIP Done";
            });
        }

        public JsonResult GenerateDefaultAssignOP()
        {
            return MvcResultInfo.Execute(() =>
            {
                List<string> newop = new List<string>();
                newop.Add("OP15");
                string param = "Not yet Realized";
                //var q = Query.Matches("Completion", new BsonRegularExpression(new Regex(param.ToString().ToLower(), RegexOptions.IgnoreCase)));

                var getpip = WellPIP.Populate<WellPIP>();
                var getel = getpip.SelectMany(d => d.Elements, (d, x) =>
                {
                    if (x.Completion.Equals(param))
                    {
                        var qs = new List<IMongoQuery>();
                        qs.Add(Query.EQ("UARigSequenceId", d.SequenceId));
                        qs.Add(Query.EQ("WellName", d.WellName));
                        var act = WellActivity.Populate<WellActivity>(q: Query.And(qs));
                        var actdata = act.SelectMany(p => p.Phases, (p, y) =>
                        {
                            if (y.ActivityType.Equals(d.ActivityType) && y.PhSchedule.Start != Tools.DefaultDate)
                            {
                                p.AssignTOOps = newop;
                                p.Save();
                            }
                            return p;
                        });

                    }
                    return d;
                });

                return getel;
            });
        }

        public JsonResult DeleteDoubleOpsInElement()
        {
            return MvcResultInfo.Execute(() =>
            {
                List<WellPIP> ListWell = WellPIP.Populate<WellPIP>();
                foreach (var i in ListWell)
                {
                    var WellPip = i; var check = false;
                    List<PIPElement> ListElement = new List<PIPElement>();
                    foreach (var j in i.Elements)
                    {
                        PIPElement Element = j;
                        var groups = j.AssignTOOps.GroupBy(v => v);
                        foreach (var group in groups)
                        {
                            //Console.WriteLine("Value {0} has {1} items", group.Key, group.Count());
                            if (group.Count() > 1)
                            {
                                check = true;
                                break;

                            }
                        }
                        if (check)
                        {
                            Element.AssignTOOps = j.AssignTOOps.Distinct().ToList();
                        }
                        ListElement.Add(Element);
                    }
                    if (check)
                    {
                        WellPip.Elements = ListElement;
                        WellPip.Save();
                    }
                }

                return "Delete Double Ops In Element Done";
            });
        }

        public JsonResult GenerateDefaultOPForPIP()
        {
            return MvcResultInfo.Execute(() =>
           {
               var pips = WellPIP.Populate<WellPIP>();
               List<string> ass = new List<string>();
               ass.Add("OP15");
               foreach (var p in pips)
               {
                   foreach (var e in p.Elements)
                   {
                       if (!e.AssignTOOps.Any())
                       {
                           e.AssignTOOps.Add("OP14");
                       }
                   }

                   foreach (var nee in p.Elements.Where(x => !x.Completion.Equals("Realized")))
                   {
                       var isWell = WellActivity.Get<WellActivity>(
                       Query.And(
                           Query.EQ("WellName", p.WellName),
                           Query.EQ("UARigSequenceId", p.SequenceId),
                           Query.EQ("Phases.ActivityType", p.ActivityType)
                       ));
                       if (isWell != null)
                       {
                           var ph = isWell.Phases.Where(x => x.ActivityType.Equals(p.ActivityType));
                           if (ph.Any())
                           {
                               var phase = ph.FirstOrDefault();
                               if (phase.PhSchedule.Start != Tools.DefaultDate)
                               {
                                   // set to OP15
                                   nee.AssignTOOps.Add("OP15");
                               }
                           }
                       }
                   }
                   p.Save();
               }

               return "GenerateDefaultOPForPIP Done!";
           });
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public JsonResult RegenerateMasterData()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                OP15.MergeMasterData();
                return "RegenerateMasterData Done!";
            });
        }

        public JsonResult CheckDupicateOP()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
           {
               var op = WellPIP.Populate<WellPIP>().SelectMany(x => x.Elements, (x, p) => new
               {
                   WellName = x.WellName,
                   SequenceId = x.SequenceId,
                   AssignTOOp = p.AssignTOOps
               }).Where(x => x.AssignTOOp.Count() > 2);
               return op;
           });
        }
        public JsonResult ResetDuplicateOP()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
           {
               List<string> aa = new List<string>();
               aa.Add("OP14");
               aa.Add("OP15");
               var op = WellPIP.Populate<WellPIP>();
               List<WellPIP> pipnew = new List<WellPIP>();
               foreach (var r in op)
               {
                   foreach (var q in r.Elements)
                   {
                       if (q.AssignTOOps.Count() > 2)
                       {
                           q.AssignTOOps = aa;

                       }
                   }
                   r.Save();
               }
               return "OK";
           });
        }

        /// <summary>
        /// in database must have : 
        /// 1. _OP15_20160114
        /// 2. _temp_group_op15
        /// </summary>
        /// <returns></returns>
        public JsonResult Load15Merge()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                //OP15.LoadOP15_New2();
                //OP15.MergeMasterData();
                //OP15.GenerateLogWRComparisonLatestMonth();
                return "LoadOP15_New2 and MergeMasterData, done";
            });
        }

        public JsonResult CompareTest(object[] obj1, object[] obj2, string operand = "AND")
        {
            return MvcResultInfo.Execute(() =>
            {
                //var result = FunctionHelper.CompareBaseOP(obj1, obj2, operand);
                var result2 = FunctionHelper.isMatchOP(obj1, obj2, operand);

                return result2;
            });
        }

        public JsonResult LoadSummaryOPWellPlan()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var res = OP15.GetAllHistoriesValue();
                DataHelper.Delete("_WellActivityOPSummaries");
                List<BsonDocument> docs = new List<BsonDocument>();
                foreach (var y in res)
                {
                    BsonDocument d = new BsonDocument();
                    d = y.ToBsonDocument();
                    d.Set("_id", string.Format("{0}-{1}-{2}-{3}-{4}", y.WellName.Replace(" ", ""), y.ActivityType.Replace(" ", ""), y.RigName.Replace(" ", ""), y.SequenceId.Replace(" ", ""), y.PhaseNo));
                    docs.Add(d);
                }
                DataHelper.Save("_WellActivityOPSummaries", docs);

                return "Save to _WellActivityOPSummaries";
            });
        }

        public JsonResult SendEmailToAdmin()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var response = string.Empty;
                List<WEISPerson> Users = WEISPerson.Populate<WEISPerson>();
                var variables = new Dictionary<string, string>();
                var persons = Users.SelectMany(d => d.PersonInfos).Where(x => x.RoleId.Equals("Administrators"))
                .GroupBy(g => g.Email)
                .Select(d => new
                {
                    FullName = d.FirstOrDefault().FullName,
                    Email = d.FirstOrDefault().Email
                }).ToList();
                foreach (var person in persons)
                {
                    variables.Add("Name", person.FullName);
                }
                if (persons != null)
                {
                    try
                    {
                        var e = ECIS.Client.WEIS.Email.Send("WRInitiate",
                        persons.Select(d => d.Email).ToArray(), null,
                        variables: variables,
                        developerModeEmail: WebTools.LoginUser.Email);
                        //developerModeEmail: "mas.muhammad@eaciit.com");
                        response = e.Message;
                    }
                    catch (Exception e)
                    {
                        response = e.Message;
                    }
                }

                return "OK";
            });
        }

        public JsonResult CreateCollectionFundingType()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                List<BsonDocument> docs = new List<BsonDocument>();

                BsonDocument bs1 = new BsonDocument();
                bs1.Set("_id", "EXPEX");
                bs1.Set("Name", "EXPEX");
                bs1.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
                docs.Add(bs1);

                BsonDocument bs2 = new BsonDocument();
                bs2.Set("_id", "CAPEX");
                bs2.Set("Name", "CAPEX");
                bs2.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
                docs.Add(bs2);

                BsonDocument bs3 = new BsonDocument();
                bs3.Set("_id", "ABEX");
                bs3.Set("Name", "ABEX");
                bs3.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
                docs.Add(bs3);

                BsonDocument bs4 = new BsonDocument();
                bs4.Set("_id", "OPEX");
                bs4.Set("Name", "OPEX");
                bs4.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
                docs.Add(bs4);

                BsonDocument bs5 = new BsonDocument();
                bs5.Set("_id", "C2E");
                bs5.Set("Name", "C2E");
                bs5.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
                docs.Add(bs5);

                BsonDocument bs6 = new BsonDocument();
                bs6.Set("_id", "EXPEX SUCCESS");
                bs6.Set("Name", "EXPEX SUCCESS");
                bs6.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
                docs.Add(bs6);

                DataHelper.Delete("WEISFundingTypes");
                DataHelper.Save("WEISFundingTypes", docs);
                return "Save";
            });
        }

        public void InitiateFundingType()
        {
            List<BsonDocument> docs = new List<BsonDocument>();

            BsonDocument bs1 = new BsonDocument();
            bs1.Set("_id", "EXPEX");
            bs1.Set("Name", "EXPEX");
            bs1.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
            docs.Add(bs1);

            BsonDocument bs2 = new BsonDocument();
            bs2.Set("_id", "CAPEX");
            bs2.Set("Name", "CAPEX");
            bs2.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
            docs.Add(bs2);

            BsonDocument bs3 = new BsonDocument();
            bs3.Set("_id", "ABEX");
            bs3.Set("Name", "ABEX");
            bs3.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
            docs.Add(bs3);

            BsonDocument bs4 = new BsonDocument();
            bs4.Set("_id", "OPEX");
            bs4.Set("Name", "OPEX");
            bs4.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
            docs.Add(bs4);

            BsonDocument bs5 = new BsonDocument();
            bs5.Set("_id", "C2E");
            bs5.Set("Name", "C2E");
            bs5.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
            docs.Add(bs5);

            BsonDocument bs6 = new BsonDocument();
            bs6.Set("_id", "EXPEX SUCCESS");
            bs6.Set("Name", "EXPEX SUCCESS");
            bs6.Set("LastUpdate", Tools.ToUTC(DateTime.Now));
            docs.Add(bs6);

            DataHelper.Delete("WEISFundingTypes");
            DataHelper.Save("WEISFundingTypes", docs);
        }

        public JsonResult CleanSheet()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                MasterAlias.LoadDefaultAlias();
                #region done clean data
                //MasterAlias.MasterCleansing("WEISWellActivities", "PerformanceUnit");
                //MasterAlias.MasterCleansing("WEISBizPlanActivities", "PerformanceUnit");
                //MasterAlias.MasterCleansing("WEISWellActivityUpdates", "PerformanceUnit", true, "Elements");
                //MasterAlias.MasterCleansing("WEISWellPIPs", "PerformanceUnit", true, "Elements");
                //MasterAlias.MasterCleansing("_OP15_Master", "Performance_Unit");

                //MasterAlias.MasterCleansing("WEISWellActivities", "FundingType", true, "Phases");
                //MasterAlias.MasterCleansing("WEISWellActivityUpdates", "FundingType", true, "Elements");
                //MasterAlias.MasterCleansing("WEISWellActivityUpdatesMonthly", "FundingType", true, "Elements");
                //MasterAlias.MasterCleansing("WEISBizPlanActivities", "FundingType", true, "Phases");


                //InitiateFundingType();
                //// PerformanceUnit Master
                //var p = WellActivity.Populate<WellActivity>().Select(x => x.PerformanceUnit).Distinct();
                //DataHelper.Delete("WEISPerformanceUnits");
                //List<BsonDocument> bdocsx = new List<BsonDocument>();

                //foreach (var ps in p.Where(x => x != null))
                //{
                //    bdocsx.Add(new BsonDocument().Set("_id", ps));
                //}
                //DataHelper.Delete("WEISPerformanceUnits");
                //DataHelper.Save("WEISPerformanceUnits", bdocsx);

                ////Reset Funding Type
                //DataHelper.Delete("WEISFundingTypes");
                //MasterFundingType.CreateCollectionFundingType();
                #endregion
                #region issue 14/06/2016
                //Performance Unit
                MasterAlias.MasterCleansing("WEISWellActivities", "PerformanceUnit");
                MasterAlias.MasterCleansing("WEISBizPlanActivities", "PerformanceUnit");
                //AssetName
                MasterAlias.MasterCleansing("WEISWellActivities", "AssetName");
                MasterAlias.MasterCleansing("WEISBizPlanActivities", "AssetName");
                MasterAlias.MasterCleansing("WEISWellActivityUpdates", "AssetName");
                MasterAlias.MasterCleansing("WEISWellActivityUpdatesMonthly", "AssetName");
                //clean master
                List<BsonDocument> bspUnit = new List<BsonDocument>();
                DataHelper.Populate("WEISWellActivities").ForEach(d =>
                {
                    var pUnit = d.Get("PerformanceUnit").ToString();
                    if (pUnit != "BsonNull")
                    {
                        bspUnit.Add(new BsonDocument().Set("_id", pUnit).Set("LastUpdate", Tools.ToUTC(DateTime.Now)));
                    }
                });
                DataHelper.Delete("WEISPerformanceUnits");
                DataHelper.Save("WEISPerformanceUnits", bspUnit);
                //AssetName
                List<BsonDocument> bsAssenName = new List<BsonDocument>();
                DataHelper.Populate("WEISWellActivities").ForEach(d =>
                {
                    var assName = d.Get("AssetName").ToString();
                    if (assName != "BsonNull")
                    {
                        bsAssenName.Add(new BsonDocument().Set("_id", assName).Set("LastUpdate", Tools.ToUTC(DateTime.Now)));
                    }
                });
                DataHelper.Delete("WEISAssetNames");
                DataHelper.Save("WEISAssetNames", bsAssenName);

                #endregion
                //MasterAlias.MasterCleansing("WEISWellActivities", "WellName");
                //MasterAlias.MasterCleansing("WEISWellActivityDocuments", "WellName");
                //MasterAlias.MasterCleansing("WEISWellActivityPhaseInfos", "WellName");
                //MasterAlias.MasterCleansing("WEISWellActivityUpdates", "WellName");
                //MasterAlias.MasterCleansing("WEISWellActivityUpdatesMonthly", "WellName");
                //MasterAlias.MasterCleansing("WEISWellPIPs", "WellName");

                //clean master
                //List<BsonDocument> bswellname = new List<BsonDocument>();
                //DataHelper.Populate("WEISWellActivities").ForEach(d =>
                //{
                //    var wellname = d.Get("WellName").ToString();
                //    bswellname.Add(new BsonDocument().Set("_id", wellname).Set("LastUpdate", Tools.ToUTC(DateTime.Now)));
                //});
                //DataHelper.Delete("WEISWellNames");
                //DataHelper.Save("WEISWellNames", bswellname);

                return "OK";
            });
        }

        public JsonResult MaturityRiskMatrixAddYear()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
           {
               DataHelper.Delete("WEISMaturityRisk");

               List<BsonDocument> matbsList = new List<BsonDocument>();
               BsonDocument bs1 = new BsonDocument
                {
                    {"_id", "TType0Y2014"}, 
                    {"Title", "Type 0"}, 
                    {"Year", 2014}, 
                    {"NPTCost", 20.0}, 
                    {"NPTTime", 19.0}, 
                    {"TECOPCost", 10.0}, 
                    {"TECOPTime", 8.0}
                };
               BsonDocument bs2 = new BsonDocument
                {
                    {"_id"        , "TType1Y2014"}, 
                    {"Title"      , "Type 1"}, 
                    {"NPTCost"    , 25.0}, 
                    {"NPTTime"    , 25.0}, 
                    {"TECOPCost"  , 20.0}, 
                    {"TECOPTime"  , 20.0}, 
                    {"Year"       , 2014}
                };
               BsonDocument bs3 = new BsonDocument
                {
                    {"_id"           , "TType2Y2014"},  
                    {"Title"         , "Type 2"}, 
                    {"NPTCost"       , 25.0}, 
                    {"NPTTime"       , 25.0}, 
                    {"TECOPCost"     , 15.0}, 
                    {"TECOPTime"     , 15.0}, 
                    {"Year"          , 2014}
                };
               BsonDocument bs4 = new BsonDocument
                {
                    {"_id"           , "TType3Y2014"}, 
                    {"Title"         , "Type 3"}, 
                    {"NPTCost"       , 25.0}, 
                    {"NPTTime"       , 25.0}, 
                    {"TECOPCost"     , 10.0}, 
                    {"TECOPTime"     , 10.0}, 
                    {"Year"          , 2014}
                };
               BsonDocument bs5 = new BsonDocument
                {
                    {"_id"           , "TType4Y2014"}, 
                    {"Title"         , "Type 4"}, 
                    {"NPTCost"       , 25.0}, 
                    {"NPTTime"       , 25.0}, 
                    {"TECOPCost"     , 10.0}, 
                    {"TECOPTime"     , 10.0}, 
                    {"Year"          , 2014}
                };
               matbsList.Add(bs1); matbsList.Add(bs2); matbsList.Add(bs3); matbsList.Add(bs4); matbsList.Add(bs5);
               DataHelper.Save("WEISMaturityRisk", matbsList);
               return "OK";
           });
        }

        public JsonResult TestProratingLEDashboard(WaterfallBase wb)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                wb.GetWhatData = "OP";
                wb.PeriodView = "Project View";
                var getRaw = wb.GetActivitiesAndPIPs(false);
                List<int> Years = new List<int>();
                for (var i = wb.FiscalYearStart; i <= wb.FiscalYearFinish; i++)
                {
                    Years.Add(i);
                }
                var RangeFilter = new DateRange(new DateTime(wb.FiscalYearStart, 1, 1), new DateTime(wb.FiscalYearFinish, 12, 31));

                List<DataProrateDashboard> result = new List<DataProrateDashboard>();
                var actvs = getRaw.Activities;
                if (actvs.Any())
                {
                    foreach (var x in actvs)
                    {
                        if (x.Phases.Any())
                        {
                            foreach (var p in x.Phases)
                            {
                                var each = new DataProrateDashboard();
                                each.WellName = x.WellName;
                                each.ActivityType = p.ActivityType;
                                each.LSSchedule = p.PhSchedule;
                                var proportion = DateRangeToMonth.ProportionNumDaysPerYear(p.PhSchedule, wb.FiscalYearStart, wb.FiscalYearFinish);
                                var DetailByYears = new List<DetailByYear>();
                                if (Years.Any())
                                {
                                    foreach (var y in Years)
                                    {
                                        var ratio = proportion.Where(d => d.Key.Equals(y)).FirstOrDefault().Value;
                                        var OPDays = ratio * p.OP.Days;
                                        var LEDays = Tools.Div(OPDays, p.OP.Days) * p.LE.Days;
                                        var OPCost = Tools.Div(OPDays, p.OP.Days) * p.OP.Cost;
                                        var LECost = Tools.Div(LEDays, p.LE.Days) * p.LE.Cost;
                                        var PerfDays = Tools.Div(OPDays, p.OP.Days) * (p.OP.Days - p.LE.Days);
                                        var PerfCost = Tools.Div(OPDays, p.OP.Days) * (p.OP.Cost - p.LE.Cost);

                                        var wau = new WellActivityUpdate();
                                        wau.WellName = x.WellName;
                                        wau.SequenceId = x.UARigSequenceId;
                                        wau.Phase.ActivityType = p.ActivityType;
                                        wau.Phase.PhaseNo = p.PhaseNo;
                                        wau.UpdateVersion = new DateTime(y, 12, 31);
                                        var getActual = wau.GetLatestActual();
                                        var ActualDays = getActual.Days;
                                        var ActualCost = getActual.Cost;

                                        var ActualPerfDays = Tools.Div(ActualDays, p.LE.Days) * (p.OP.Days - p.LE.Days);
                                        var ActualPerfCost = (Tools.Div(ActualCost, p.LE.Cost) * p.OP.Cost) - ActualCost;

                                        var eachYear = new DetailByYear();
                                        eachYear.Year = y;

                                        eachYear.OP = new WellDrillData() { Days = OPDays, Cost = Tools.Div(OPCost, 1000000) };
                                        eachYear.LE = new WellDrillData() { Days = LEDays, Cost = Tools.Div(LECost, 1000000) };
                                        eachYear.Actual = new WellDrillData() { Days = ActualDays, Cost = Tools.Div(ActualCost, 1000000) };
                                        eachYear.Performance = new WellDrillData() { Days = PerfDays, Cost = Tools.Div(PerfCost, 1000000) };
                                        eachYear.ActualPerformance = new WellDrillData() { Days = ActualPerfDays, Cost = Tools.Div(ActualPerfCost, 1000000) };
                                        DetailByYears.Add(eachYear);
                                    }
                                }
                                each.DetailsByYear = DetailByYears;
                                result.Add(each);
                            }
                        }
                    }
                }

                return new { result, Years };
            });
        }

        public JsonResult SetDefaultOPForRFM()
        {
            return MvcResultInfo.Execute(() =>
            {
                DeleteObjectIdRFM();
                AddBaseOP();
                var rfm = WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>();
                foreach (var x in rfm)
                {
                    if (x.BaseOP == null || x.BaseOP == "")
                    {
                        var id = x._id;
                        x.BaseOP = "OP16";
                        x.Save();
                        var qs = new List<IMongoQuery>();
                        qs.Add(Query.EQ("_id", id.ToString()));
                        qs.Add(Query.EQ("WellName", x.WellName));
                        qs.Add(Query.EQ("ActivityType", x.ActivityType));
                        qs.Add(Query.EQ("Country", x.Country));
                        qs.Add(Query.EQ("GroupCase", x.GroupCase));
                        DataHelper.Delete(x.TableName, Query.And(qs));
                    }
                }
                addLearningCurveSubjectToRFM();
                SetUnitedStatesToAllRFM();
                addDefaultVisibleCurrencyInBizPlan();

                AdministrationInput.RecalculateBusPlan();

                return "OK";
            });
        }


        public void SetDefaultOPForRFMVoid()
        {
            DeleteObjectIdRFM();
            AddBaseOP();
            var rfm = WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>();
            foreach (var x in rfm)
            {
                if (x.BaseOP == null || x.BaseOP == "")
                {
                    var id = x._id;
                    x.BaseOP = "OP15";
                    x.Save();
                    var qs = new List<IMongoQuery>();
                    qs.Add(Query.EQ("_id", id.ToString()));
                    qs.Add(Query.EQ("WellName", x.WellName));
                    qs.Add(Query.EQ("ActivityType", x.ActivityType));
                    qs.Add(Query.EQ("Country", x.Country));
                    qs.Add(Query.EQ("GroupCase", x.GroupCase));
                    DataHelper.Delete(x.TableName, Query.And(qs));
                }
            }
            addLearningCurveSubjectToRFM();
            SetUnitedStatesToAllRFM();
            addDefaultVisibleCurrencyInBizPlan();

            AdministrationInput.RecalculateBusPlan();

        }


        public void addDefaultVisibleCurrencyInBizPlan()
        {
            var VisibleCurrency = new List<string>() { "USD", "GBP", "EUR", "FRF" };
            var config3 = Config.Get<Config>("VisibleCurrency") ?? new Config() { _id = "VisibleCurrency", ConfigModule = "BizPlan" };
            config3.ConfigValue = string.Join(",", VisibleCurrency);
            config3.Save();
        }

        public void addLearningCurveSubjectToRFM()
        {
            var rfm = WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>();
            foreach (var x in rfm)
            {
                var SubjectMatter = x.SubjectMatters;
                var newDictionaryValue = new Dictionary<string, double>();
                var years = SubjectMatter.Where(d => d.Key.Equals("CSO Factors")).FirstOrDefault().Value;
                foreach (var y in years)
                {
                    var year = y.Key;
                    newDictionaryValue.Add(year, 10);
                }
                x.SubjectMatters.Remove("Learning Curve");
                if (!x.SubjectMatters.Where(d => d.Key.Equals("Learning Curve Factors")).Any())
                {
                    x.SubjectMatters.Add("Learning Curve Factors", newDictionaryValue);
                    x.Save();
                }
            }
        }

        public void DeleteObjectIdRFM()
        {
            var rfm = WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>();
            foreach (var x in rfm)
            {
                var id = x._id;
                if (id.GetType().Name == "ObjectId")
                {
                    DataHelper.Delete(x.TableName, Query.EQ("_id", new ObjectId(id.ToString())));
                }
            }

        }


        public void SetUnitedStatesToAllRFM()
        {
            var crfm = new CountryReferenceFactor();
            crfm.Country = "United States";
            crfm.ReferenceFactorModels = new List<string>();
            var rfm = MasterRFMs.Populate<MasterRFMs>();
            foreach (var x in rfm)
            {
                var id = x._id.ToString();
                crfm.ReferenceFactorModels.Add(id);
            }
            crfm.Save();

        }

        public void AddBaseOP()
        {
            var masterops = MasterOP.Populate<MasterOP>();
            if (!masterops.Any())
            {
                List<string> BaseOP = new List<string> { "OP14", "OP15", "OP16" };
                foreach (var Name in BaseOP)
                {
                    new MasterOP() { _id = Name, Name = Name }.Save();
                }
            }
        }


        public JsonResult TestPerformanceMacroData()
        {
            return MvcResultInfo.Execute(() =>
            {
                //var result = FunctionHelper.CompareBaseOP(obj1, obj2, operand);
                var result2 = MacroEconomic.Populate<MacroEconomic>();

                return result2;
            });
        }

        public JsonResult GenerateMasterCountryAndRFMJSON()
        {

            return MvcResultInfo.Execute(() =>
           {

               DataHelper.Delete(new MasterCountry().TableName);
               DataHelper.Delete(new MasterRFMs().TableName);


               var contries = MacroEconomic.Populate<MacroEconomic>().Select(x => x.Country).Distinct();
               var modelnames = WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>().Select(x => x.GroupCase).Distinct();

               //var dataRFM = WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>();
               //List<string> dataCountry = dataRFM.GroupBy(x => x.Country).Select(x => x.Key.ToString()).ToList();
               //List<string> dataModel = dataRFM.GroupBy(x => x.GroupCase).Select(x => x.Key.ToString()).ToList();
               foreach (var c in contries)
               {
                   if (c != "*" && !c.Trim().Equals(""))
                   {
                       var country = new MasterCountry() { _id = c };
                       country.Save();
                   }
               }

               List<string> modelname = new List<string>();
               foreach (var m in modelnames)
               {
                   var model = new MasterRFMs() { _id = m };
                   modelname.Add(m);
                   model.Save();
               }

               if (modelname.Any())
               {
                   DataHelper.Delete(new CountryReferenceFactor().TableName);

                   CountryReferenceFactor ctrf = new CountryReferenceFactor();
                   ctrf.Country = "United States";
                   ctrf.ReferenceFactorModels = modelname;
                   ctrf.Save();
               }
               return "OK";
           });


        }


        public void GenerateMasterCountryAndRFM()
        {
            DataHelper.Delete(new MasterCountry().TableName);
            DataHelper.Delete(new MasterRFMs().TableName);


            var contries = MacroEconomic.Populate<MacroEconomic>().Select(x => x.Country).Distinct();
            var modelnames = WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>().Select(x => x.GroupCase).Distinct();

            //var dataRFM = WEISReferenceFactorModel.Populate<WEISReferenceFactorModel>();
            //List<string> dataCountry = dataRFM.GroupBy(x => x.Country).Select(x => x.Key.ToString()).ToList();
            //List<string> dataModel = dataRFM.GroupBy(x => x.GroupCase).Select(x => x.Key.ToString()).ToList();
            foreach (var c in contries)
            {
                if (c != "*" && !c.Trim().Equals(""))
                {
                    var country = new MasterCountry() { _id = c };
                    country.Save();
                }
            }

            List<string> modelname = new List<string>();
            foreach (var m in modelnames)
            {
                var model = new MasterRFMs() { _id = m };
                modelname.Add(m);
                model.Save();
            }

            if (modelname.Any())
            {
                DataHelper.Delete(new CountryReferenceFactor().TableName);

                CountryReferenceFactor ctrf = new CountryReferenceFactor();
                ctrf.Country = "United States";
                ctrf.ReferenceFactorModels = modelname;
                ctrf.Save();
            }


        }

        public WellDrillData GetBICFromPhaseInfos(string WellActivityId, string WellName, string ActivityType, string SequenceId, string RigName, Int32 PhaseNo)
        {
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellActivityId", WellActivityId));
            qs.Add(Query.EQ("WellName", WellName));
            qs.Add(Query.EQ("ActivityType", ActivityType));
            qs.Add(Query.EQ("RigName", RigName));
            qs.Add(Query.EQ("SequenceId", SequenceId));
            qs.Add(Query.EQ("PhaseNo", PhaseNo));

            var phinfos = WellActivityPhaseInfo.Get<WellActivityPhaseInfo>(Query.And(qs));
            if (phinfos != null)
            {
                return phinfos.BIC;
            }
            else
            {
                return new WellDrillData();
            }
        }

        public JsonResult SyncBICFromPhaseInfos()
        {
            return MvcResultInfo.Execute(() =>
            {
                List<WellActivity> was = WellActivity.Populate<WellActivity>();
                foreach (var wa in was)
                {
                    if (wa.Phases.Any())
                    {
                        foreach (var ph in wa.Phases)
                        {
                            var getBIC = GetBICFromPhaseInfos(wa._id.ToString(), wa.WellName, ph.ActivityType, wa.UARigSequenceId, wa.RigName, ph.PhaseNo);
                            ph.BIC = getBIC;
                        }
                    }
                    DataHelper.Save(wa.TableName, wa.ToBsonDocument());
                    //wa.Save();
                }
                return "OK";
            });
        }

        public JsonResult SyncInflationRateCountry()
        {
            return MvcResultInfo.Execute(() =>
            {
                var macrodata = MacroEconomic.Populate<MacroEconomic>();
                foreach (var d in macrodata)
                {
                    var qs = new List<IMongoQuery>();
                    qs.Add(Query.EQ("Currency", d.Currency));
                    qs.Add(Query.Matches("Country", d.Country));
                    if (d.BaseOP != null)
                        qs.Add(Query.EQ("BaseOP", d.BaseOP));

                    var get = MacroEconomic.Populate<MacroEconomic>(Query.And(qs));
                    if (get.Any() && get.Count > 1)
                    {
                        foreach (var g in get)
                        {
                            if (g.Country != d.Country)
                            {
                                g.Inflation = d.Inflation;
                                if (g.BaseOP == null) g.BaseOP = "";
                                g.Save();
                            }
                        }
                    }
                }
                return "OK";
            });
        }


        public JsonResult SyncPhaseInfosFromMasterOP15()
        {
            return MvcResultInfo.Execute(() =>
            {
                var was = DataHelper.Populate("_OP15_Master");
                var pis = WellActivityPhaseInfo.Populate<WellActivityPhaseInfo>(Query.EQ("OPType", "OP15"));

                foreach (var p in pis)
                {
                    var ttt = was.Where(x =>
                        x.GetString("Well_Name").Equals(p.WellName) &&
                        x.GetString("Activity_Type").Equals(p.ActivityType) &&
                        x.GetString("Rig_Name").Equals(p.RigName) &&
                        x.GetInt32("Sequence_#_\r\non_Rig") == p.RigSequenceId
                        );
                    if (ttt.Any())
                    {
                        p.ScopeDescription = ttt.FirstOrDefault().GetString("Scope_Description");
                        p.PlanningClassification = ttt.FirstOrDefault().GetString("Planning_\r\nClassification");
                        p.WorkingInterest = ttt.FirstOrDefault().GetDouble("Shell_\r\nWorking_\r\nInterest");


                        p.USDCost.TroubleFree = ttt.FirstOrDefault().GetDouble("Trouble_Free\r\nCost_USD_(Millions)");
                        p.USDCost.NPT = ttt.FirstOrDefault().GetDouble("NPT_\r\nCost_USD_(Millions)");
                        p.USDCost.TECOP = ttt.FirstOrDefault().GetDouble("TECOP_\r\nCost_USD_(Millions)");
                        p.USDCost.MeanCostEDM = ttt.FirstOrDefault().GetDouble("Mean_Cost_EDM_USD_(Millions)");
                        p.USDCost.Escalation = ttt.FirstOrDefault().GetDouble("Escalation_\r\nCost_USD_(Millions)");
                        p.USDCost.CSO = ttt.FirstOrDefault().GetDouble("CSO_\r\nCost_USD_(Millions)");
                        p.USDCost.Inflation = ttt.FirstOrDefault().GetDouble("Inflation_\r\nCost_USD_(Millions)");
                        p.USDCost.MeanCostMOD = ttt.FirstOrDefault().GetDouble("Mean_Cost_MOD_USD_(Millions)");

                        p.Save();
                    }
                }
                return "OK";
            });
        }


        public JsonResult RebindNullFundingTypeFromHeader()
        {
            return MvcResultInfo.Execute(() =>
            {
                var pis = WellActivity.Populate<WellActivity>();

                foreach (var p in pis)
                {
                    bool isSave = false;
                    foreach (var ps in p.Phases)
                    {
                        if (string.IsNullOrEmpty(ps.FundingType))
                        {
                            if (p.EXType != null)
                            {
                                ps.FundingType = p.EXType;
                                isSave = true;
                            }
                        }
                    }
                    if (isSave)
                        DataHelper.Save(p.TableName, p.ToBsonDocument());
                }
                return "OK";
            });
        }

        private void BatchMacroDataWithBaseOP()
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
        }

        public JsonResult BatchUserUpdatePIP()
        {
            return MvcResultInfo.Execute(() =>
            {
                var wpip = WellPIP.Populate<WellPIP>();
                foreach (var r in wpip)
                {
                    if (r.UserName == null)
                    {
                        r.UserName = "eaciit";
                    }
                    WellPIP newwpip = new WellPIP();
                    newwpip = r;
                    DataHelper.Save(r.TableName, r.ToBsonDocument());
                }
                return "OK";
            });
        }

        public JsonResult MakeDataChecker()
        {
            return MvcResultInfo.Execute(() =>
            {
                var DataChecker = DataHelper.Populate("MetaDataChecker", Query.EQ("CollectionName", "WEISWellActivities"));

                var wellactivity = WellActivity.Populate<WellActivity>();
                var populate = DataHelper.Populate("WEISWellActivities");
                var Datas = new List<BsonDocument>();
                var wellphases = new List<WellActivityPhase>();
                foreach (var well in populate)
                {
                    var phases = well["Phases"].AsBsonArray.ToList();

                    if (phases.Any())
                    {
                        foreach (var phase in phases)
                        {

                            foreach (var bsonElementPhase in phase.ToBsonDocument().Elements)
                            {
                                foreach (var bsonElementInChecker in DataChecker.FirstOrDefault().ToBsonDocument().Elements)
                                {
                                    if (bsonElementPhase.Name.ToLower().Equals(bsonElementInChecker.Value.ToString().ToLower()))
                                    {
                                        if (bsonElementPhase.Name.GetType().Equals(typeof(string)))
                                        {
                                            if (bsonElementPhase.Value == null || bsonElementPhase.Value == string.Empty || bsonElementPhase.Value == BsonNull.Value)
                                            {
                                                BsonDocument data = new BsonDocument();
                                                data.Set("WellName", well.GetString("WellName"));
                                                data.Set("RigName", well.GetString("RigName"));
                                                data.Set("WorkingInterest", well.GetString("WorkingInterest"));
                                                //data.Set("ActivityType", phase.get);
                                                Datas.Add(data);

                                            }
                                        }

                                    }

                                }
                            }

                        }
                    }

                }

                return new { Count = Datas.Count, Data = DataHelper.ToDictionaryArray(Datas) };
            });
        }

        public JsonResult BatchLobs()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var check = WellInfo.Populate<WellInfo>().FirstOrDefault();
                if (check.LoBs.Count() == 0)
                {
                    var d = WellInfo.Populate<WellInfo>();
                    var lob = new List<string>(); lob.Add("DEEPWATER");
                    foreach (var r in d)
                    {
                        r.LoBs = lob;
                        DataHelper.Save("WEISWellNames", r.ToBsonDocument());
                    }
                }

                return "OK";
            });
        }

        public JsonResult FillProjectName()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var queries = new List<IMongoQuery>();
                queries.Add(Query.EQ("ProjectName", BsonNull.Value));
                //var rfms = datas.Select(x => BsonHelper.GetString(x, "Reference_Factor_Model")).Distinct().ToList();
                var populateweis = DataHelper.Populate("WEISWellActivities", Query.And(queries));

                if (populateweis != null || populateweis.Any())
                {
                    foreach (var weis in populateweis)
                    {
                        string wellname = weis.GetString("WellName");
                        if (wellname == "APPO CWI EQUIPMENT 2017")
                        {
                            var xx = 0;
                        }
                        string rigname = weis.GetString("RigName");
                        string seqid = weis.GetString("UARigSequenceId");
                        queries = new List<IMongoQuery>();
                        queries.Add(Query.NE("ProjectName", BsonNull.Value));
                        queries.Add(Query.EQ("WellName", wellname));
                        queries.Add(Query.EQ("RigName", rigname));
                        queries.Add(Query.EQ("UARigSequenceId", seqid));
                        var biz = DataHelper.Populate("WEISBizPlanActivities", Query.And(queries)).FirstOrDefault();
                        if (biz != null)
                        {
                            weis.Set("ProjectName", biz.GetString("ProjectName"));
                            DataHelper.Save("WEISWellActivities", weis);
                        }

                    }
                }
                return "Oke";
            });
        }

        public JsonResult FillProjectNametoBisPlanAllocation()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                List<string> status = new List<string>();
                status.Add("Draft"); status.Add("Complete");
                var p = BizPlanActivity.Populate<BizPlanActivity>();
                if (p != null || p.Any())
                {
                    foreach (var r in p)
                    {
                        foreach (var get in r.Phases.Where(x => status.Contains(x.Estimate.Status)))
                        {
                            var s = r.ProjectName;
                            var aloc = BizPlanAllocation.Get<BizPlanAllocation>(Query.And(Query.EQ("WellName", r.WellName), Query.EQ("RigName", r.RigName), Query.EQ("UARigSequenceId", r.UARigSequenceId), Query.EQ("ActivityType", get.ActivityType), Query.EQ("SaveToOP", get.Estimate.Status)));
                            if (aloc != null)
                            {
                                aloc.ProjectName = s;
                                DataHelper.Save("WEISBizPlanAllocation", aloc.ToBsonDocument());
                            }
                        }
                    }
                }
                return "Oke";
            });
        }

        public JsonResult Main_Menu()
        {
            //hide Plan Role Up 
            return MvcResultInfo.Execute(null, document =>
            {
                var admin = DataHelper.Get<Menu>("Main_Menu", Query.EQ("Title", "Business Plan"));
                var bizplanmaster = admin.Submenus.Where(x => x.Title.Equals("Plan Roll Up"));
                if (bizplanmaster.Any())
                {
                    admin.Submenus.Remove(bizplanmaster.FirstOrDefault());
                    DataHelper.Save("Main_Menu", admin.ToBsonDocument());
                }
                return bizplanmaster;
            });
        }

        public JsonResult Remove_Move_Menu()
        {
            //hide Plan Role Up 
            return MvcResultInfo.Execute(null, document =>
            {
                //Move Data Extractor to System Tools

                var menuAdd = DataHelper.Get<Menu>("Main_Menu", Query.EQ("Title", "Data Extractor"));
                var findLoc = DataHelper.Get<Menu>("Main_Menu", Query.EQ("Title", "Administration"));
                if (menuAdd != null)
                    findLoc.Submenus.Where(x => x.Title.Equals("System Tools")).FirstOrDefault().Submenus.Add(menuAdd);
                var menuAdd2 = findLoc.Submenus.Where(x => x.Title.Equals("Spotfire Report"));
                if (menuAdd2.Any())
                {
                    findLoc.Submenus.Where(x => x.Title.Equals("Planning Report Tool")).FirstOrDefault().Submenus.Add(menuAdd2.FirstOrDefault());
                    findLoc.Submenus.Remove(menuAdd2.FirstOrDefault());
                }

                DataHelper.Save("Main_Menu", findLoc.ToBsonDocument());

                findLoc = DataHelper.Get<Menu>("Main_Menu", Query.EQ("Title", "Business Plan"));
                findLoc.Submenus.Add(new Menu
                {
                    _id = "M00496",
                    Order = 118,
                    Submenus = new List<Menu>(),
                    Title = "New Business Plan - Input",
                    Url = "~/shell/busplansp"
                });
                DataHelper.Save("Main_Menu", findLoc.ToBsonDocument());

                findLoc = DataHelper.Get<Menu>("Main_Menu", Query.EQ("Title", "Sequence Chart"));
                findLoc.Submenus.Add(new Menu
                {
                    _id = "M00497",
                    Order = 81,
                    Submenus = new List<Menu>(),
                    Title = "New Data Browser",
                    Url = "~/shell/datamodelsp/browser"
                });

                DataHelper.Save("Main_Menu", findLoc.ToBsonDocument());

                //Hide Data Model
                DataHelper.Delete("Main_Menu", Query.Or(Query.EQ("Title", "Data Model"), Query.EQ("Title", "Data Extractor")));

                return "OK";
            });
        }

        public JsonResult RemoveMultiPerformanceUnit()
        {
            return MvcResultInfo.Execute(() =>
            {
                DataHelper.Delete("WEISPerformanceUnits", Query.Or(Query.EQ("_id", "MARS/URSA"), Query.EQ("_id", "Mars / Ursa")));

                MasterAlias.LoadDefaultAlias();

                // cleansing wellactivity

                MasterAlias.MasterCleansing("WEISWellActivities", "PerformanceUnit");

                // cleansing wellactivityupdate
                MasterAlias.MasterCleansing("WEISWellActivityUpdates", "PerformanceUnit", true, "Elements");
                MasterAlias.MasterCleansing("WEISWellActivityUpdates", "PerformanceUnit", true, "CRElements");


                // cleansing wellactivityupdatemonthly
                MasterAlias.MasterCleansing("WEISWellActivityUpdatesMonthly", "PerformanceUnit", true, "CRElements");
                MasterAlias.MasterCleansing("WEISWellActivityUpdatesMonthly", "PerformanceUnit", true, "Elements");


                // cleansing wellpip
                MasterAlias.MasterCleansing("WEISWellPIPs", "PerformanceUnit", true, "Elements");
                MasterAlias.MasterCleansing("WEISWellPIPs", "PerformanceUnit", true, "CRElements");

                // cleansing bizplan
                MasterAlias.MasterCleansing("WEISBizPlanActivities", "PerformanceUnit");

                return "OK";
            });

        }

        public JsonResult SetDataQCEmail()
        {
            return MvcResultInfo.Execute(() =>
            {
                var Email = new Email();
                Email._id = "MODDiffChecker";
                Email.Body = "This is an email about the Data QC Checker";
                Email.Subject = "WEIS Data QC";
                Email.SMTPConfig = "Default";
                Email.Title = "MODDiffChecker";
                var existingEmail = Email.Get<Email>(Email._id);
                if (existingEmail != null) throw new Exception(String.Format("Email {0} already exist", Email._id));
                Email.SMTPConfig = "Default";
                Email.Save();
                return Email;
            });
        }
        public JsonResult CheckDiffBusplanExportDetailAndBPIT()
        {
            return MvcResultInfo.Execute(() =>
            {
                var wb = new WaterfallBase();
                #region wb initiation
                wb.Activities = null;
                wb.ActivitiesCategory = null;
                wb.AllocationYear = 0;
                wb.BaseOP = "OP14";
                wb.CumulativeDataType = null;
                wb.Currency = null;
                wb.DataFor = null;
                wb.DateFinish = null;
                wb.DateFinish2 = null;
                wb.DateRelation = "OR";
                wb.DateStart = null;
                wb.DateStart2 = null;
                wb.DayOrCost = null;
                wb.ExType = null;
                wb.firmoption = "All";
                wb.FiscalYearFinish = 0;
                wb.FiscalYearStart = 0;
                wb.GetWhatData = null;
                wb.GroupBy = null;
                wb.IncludeCR = false;
                wb.IncludeGaps = false;
                wb.IncludeZero = false;
                wb.inlastuploadls = false;
                wb.MoneyType = null;
                wb.OperatingUnits = null;
                wb.opRelation = "AND";
                wb.OPs = null;
                wb.PerformanceUnits = null;
                wb.PeriodBase = "By Last Sequence";
                wb.PeriodView = "Project View";
                wb.ProjectNames = null;
                wb.Regions = null;
                wb.RigNames = null;
                wb.RigTypes = null;
                wb.ShellShare = false;
                wb.showdataby = "0";
                wb.SSorTG = null;
                wb.Status = null;
                wb.ValidLSOnly = false;
                wb.WellNames = null;
                #endregion

                var accessibleWells = WebTools.GetWellActivities();
                var email = WebTools.LoginUser.Email;
                var raw = wb.GetActivitiesForBizPlanForFiscalYear(email); //BizPlanActivity.Populate<BizPlanActivity>();

                var busplan = new BusplanController();
                var OPstate = busplan.GetMasterOP();
                string PreviousOP = "";
                string NextOP = "";
                string DefaultOP = busplan.getBaseOP(out PreviousOP, out NextOP);

                List<Excelbusplan> edetail = busplan.GenerateExportDetailData(raw, PreviousOP, accessibleWells, wb);
                string xlst = System.IO.Path.Combine(Server.MapPath("~/App_Data/Temp"), "BlankTemplate.xlsx");
                Aspose.Cells.Workbook wbook = new Aspose.Cells.Workbook(xlst);
                Aspose.Cells.Worksheet ws = wbook.Worksheets[0];
                Aspose.Cells.Cells cells = ws.Cells;

                cells["A1"].PutValue("Well Name");
                cells["B1"].PutValue("Activity Type");
                cells["C1"].PutValue("Rig Name");
                cells["D1"].PutValue("MOD in Export Detail");
                cells["E1"].PutValue("MOD in BPIT");

                var rownum = 2;

                foreach (var r in edetail)
                {
                    if (r.WellName == "VITO VS1" && r.ActivityType == "WHOLE COMPLETION EVENT")
                    {

                    }
                    cells["A" + rownum.ToString()].PutValue(r.WellName);
                    cells["B" + rownum.ToString()].PutValue(r.ActivityType);
                    cells["C" + rownum.ToString()].PutValue(r.RigName);
                    cells["D" + rownum.ToString()].PutValue(r.MeanCostMOD);


                    var maturityLevel = "0";
                    if (r.MaturityLevel != null)
                    {
                        maturityLevel = r.MaturityLevel.Substring(r.MaturityLevel.Length - 1, 1);
                    }

                    BizPlanSummary res = new BizPlanSummary(r.WellName, r.RigName, r.ActivityType, r.Country,
                    r.ShellShare, r.Event, Convert.ToInt32(maturityLevel), r.Services, r.Materials, r.TroubleFree.Days,
                    r.RFM,
                    Tools.Div(r.NPT.PercentTime, r.NPT.PercentTime <= 1 ? 1 : 100), Tools.Div(r.TECOP.PercentTime, r.TECOP.PercentTime <= 1 ? 1 : 100),
                    Tools.Div(r.NPT.PercentCost, r.NPT.PercentCost <= 1 ? 1 : 100), Tools.Div(r.TECOP.PercentCost, r.TECOP.PercentCost <= 1 ? 1 : 100),
                    r.LLRequiredMonth, Tools.Div(r.PercOfMaterialsLongLead, 100), baseOP: r.SaveToOP, isGetExcRateByCurrency: true,
                    lcf: r.LCFParameter
                    );

                    res.GeneratePeriodBucket();
                    res.GenerateAnnualyBucket(res.MonthlyBuckets);


                    cells["E" + rownum.ToString()].PutValue(res.MeanCostMOD);


                    rownum++;
                }

                var newFileNameSingle = System.IO.Path.Combine(Server.MapPath("~/App_Data/Temp"),
                     String.Format("ExportDetail_BPIT_Diff-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy")));

                wbook.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

                var toMails = new List<string>() { "yoga.bangkit@eaciit.com", "raulie@eaciit.com", "Ian.pirsch@eaciit.com", "akash.jain@eaciit.com" };
                var attachments = new List<string> { newFileNameSingle };
                ECIS.Client.WEIS.Email.Send("MODDiffChecker", toMails.ToArray(), ccMails: null, variables: null, attachments: attachments, developerModeEmail: "eky.pradhana@eaciit.com");
                //Email.Send();

                return "OK";
            });
        }
    }

    internal class DataProrateDashboard
    {
        public string WellName { get; set; }
        public string ActivityType { get; set; }
        public DateRange LSSchedule { get; set; }
        public WellDrillData OP { get; set; }
        public WellDrillData LE { get; set; }
        public List<DetailByYear> DetailsByYear { get; set; }

    }
    internal class DetailByYear
    {
        public int Year { get; set; }
        public double Days { get; set; }
        public WellDrillData OP { get; set; }
        public WellDrillData LE { get; set; }
        public WellDrillData Actual { get; set; }
        public WellDrillData Performance { get; set; }
        public WellDrillData ActualPerformance { get; set; }
    }
}