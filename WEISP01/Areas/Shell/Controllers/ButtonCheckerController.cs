using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.IO;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class ButtonCheckerController : Controller
    {
        public string ViewBy { get; set; }
        public ButtonCheckerController() {
            ViewBy = "Year";
        }

        // GET: /Shell/ButtonChecker/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult CheckAllButton()
        {
            return MvcResultInfo.Execute(() =>
            {
                return "OK";
            });
        }

        public JsonResult RefreshBusplan(WaterfallBase wb, List<string> Status)
        {
            return MvcResultInfo.Execute(() =>
            {
                var refresh = new BusplanController();
                var hit = refresh.GetBizPlanActivity(wb,Status);
                var result = ((ECIS.Core.ResultInfo)(hit.Data)).Result;
                return result.ToString();
            });
        }

        public JsonResult ExportDetailBusplan(WaterfallBase wb)
        {
           return MvcResultInfo.Execute(() =>
            {
                var wellNames = new List<string>();
                wellNames.Add("AUGER A10");
                wb.WellNames = wellNames;
                var export = new BusplanController();
                var hit = export.ExportDetail(wb);
                bool res = hit.Data.ToBsonDocument().FirstOrDefault().Value.ToBoolean();
                var result = "NOK";
                if (res)
                {
                    result = "OK";
                }
                return result.ToString();
           });
        }

        public JsonResult RefreshFYV(WaterfallBase wb)
        {
            return MvcResultInfo.Execute(() =>
            {
                var wellNames = new List<string>();
                wellNames.Add("AUGER A10");
                wb.WellNames = wellNames;
                int yearStart = 0; int yearFinish = 0;
                var refresh = new BusplanController();
                var hit = refresh.GetDataFiscalYear2(wb, ViewBy);
                var result = ((ECIS.Core.ResultInfo)(hit.Data)).Result;
                return result.ToString();
            });
        }

        public JsonResult ExportFYV(WaterfallBase wb)
        {
            return MvcResultInfo.Execute(() =>
           {
               var refresh = new BusplanController();
               var hit = refresh.GenerateFiscalYearExcel(wb, ViewBy);
               bool res = hit.Data.ToBsonDocument().FirstOrDefault().Value.ToBoolean();
               var result = "NOK";
               if (res)
               {
                   result = "OK";
               }
               return result.ToString();
           });
        }

        public JsonResult RefreshActGapAnalysis(WaterfallBase wb)
        {
            return MvcResultInfo.Execute(() =>
          {
              //string[] wellNames, string[] rigNames, string dateStart, string dateFinish, bool riskcheck = true, bool inlastuploadls = true, string[] OPs = null, string opRelation = "AND"
              string[] wellNames;
              if (wb.WellNames != null)
              {
                  wellNames = wb.WellNames.ToArray();
              }
              else
              {
                  wellNames=null;
              }
              string[] rigNames;
              if (wb.RigNames != null)
              {
                  rigNames = wb.RigNames.ToArray();
              }
              else
              {
                  rigNames = null;
              }
              string[] OPs;
              if (wb.OPs != null)
              {
                  OPs = wb.OPs.ToArray();
              }
              else
              {
                  OPs = null;
              }
              wb.DateStart = "2016-01-01";
              wb.DateFinish = "2016-12-31";
              var refresh = new BusinessPlanSmartAlertController();
              var hit = refresh.GetData(wellNames, rigNames, wb.DateStart, wb.DateFinish, false, wb.inlastuploadls, OPs, wb.opRelation);
              return "OK";
          });
        }

        public JsonResult RefreshBizPlanSeq()
        {
            return MvcResultInfo.Execute(() =>
            {
                SequenceParam param = new SequenceParam();
                var rigNames = new List<string>();
                rigNames.Add("Auger");
                param.rigNames = rigNames;
                var refresh = new BizPlanSequenceController();
                var hit = refresh.GetData(param);
                var result = ((ECIS.Core.ResultInfo)(hit.Data)).Result;
                return result;
            });
        }

        public JsonResult RefreshWeeklyReportWorkflow()
        {
            return MvcResultInfo.Execute(() =>
            {
                var SearchDate = new DateTime();
                var SearchStatus = "In-Progress";
                var refresh = new WeeklyReportController();
                var hit = refresh.Search(SearchDate, null, SearchStatus);
                var result = ((ECIS.Core.ResultInfo)(hit.Data)).Result;
                return result.ToString();
            });
        }

        public JsonResult RefreshWeeklyReportArchive()
        {
            return MvcResultInfo.Execute(() =>
            {
                var SearchDateFrom = new DateTime(2015,4,1);
                var SearchDateTo = new DateTime(2016,8,21);
                var Ops = new List<string>();
                Ops.Add("OP15");
                var refresh = new WeeklyReportController();
                var hit = refresh.SearchDistributed(SearchDateFrom, SearchDateTo, null, null, "", "", Ops.ToArray(),1,0);
                var result = ((ECIS.Core.ResultInfo)(hit.Data)).Result;
                return result.ToString();
            });
        }

        public JsonResult RefreshActivityDoc(WaterfallBase wb)
        {
            return MvcResultInfo.Execute(() =>
            {
                var SearchDateFrom = "2016-01-01";
                var SearchDateTo = "2016-12-31";
                var SearchDateFrom2 = "2016-01-01";
                var SearchDateTo2 = "2016-12-31";
                var Ops = new List<string>();
                Ops.Add("OP15");
                var refresh = new DashboardController();
                var hit = refresh.GetWellForDocument(SearchDateFrom, SearchDateTo, SearchDateFrom2, SearchDateTo2,"OR",null,null,null,null,null,null);
                return "OK";
            });
        }

        public JsonResult RefreshActualWeis(WaterfallBase wb)
        {
            var ri = new ResultInfo();
            try
            {
                var dateStart = "2016-01-01";
                var dateFinish = "2016-12-31";
                string[] wellNames = new string[1];
                wellNames[0] = "AUGER A10";
                var refresh = new ActualController();
                var hit = refresh.GetData(dateStart, dateFinish, wellNames, null);
                ri.Result = ((ECIS.Core.ResultInfo)(hit.Data)).Result.ToString();
                if (((ECIS.Core.ResultInfo)(hit.Data)).Message != null) {
                    ri.Message = ((ECIS.Core.ResultInfo)(hit.Data)).Message.ToString();
                }
            }
            catch (Exception e)
            {
                ri.Result = "NOK";
                ri.Message = e.Message;
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RefreshActualOracle(WaterfallBase wb)
        {
            var ri = new ResultInfo();
            try
            {
                var dateStart = "2016-01-01";
                var dateFinish = "2016-12-31";
                var refresh = new ActualController();
                var hit = refresh.LoadFromOracle(dateStart, dateFinish, "");
                bool res = hit.Data.ToBsonDocument().FirstOrDefault().Value.ToBoolean();
                var Msg = hit.Data.ToBsonDocument()[2];
                var Result = "NOK";
                if (res)
                {
                    Result = "OK";
                }
                ri.Result = Result;
                ri.Message = Msg.ToString();
            }
            catch (Exception e)
            {
                ri.Result = "NOK";
                ri.Message = e.Message;
            }
            
            //return Json(new { Result = Result, Message = Msg.ToString() }, JsonRequestBehavior.AllowGet);
            return MvcTools.ToJsonResult(ri); 
        }

        public JsonResult RefreshRollingWeeklyData()
        {
            var ri = new ResultInfo();
            try
            {
                
                var AsAtDate  = new DateTime(2016,8,22);
                string[] wellNames = new string[1];
                wellNames[0] = "AUGER A10";
                var refresh = new HistoricalDataModelController();
                var hit = refresh.Calculate(AsAtDate,null,null,null,wellNames,"Week",4,null,"AND");
                ri.Result = ((ECIS.Core.ResultInfo)(hit.Data)).Result.ToString();
                if (((ECIS.Core.ResultInfo)(hit.Data)).Message != null)
                {
                    ri.Message = ((ECIS.Core.ResultInfo)(hit.Data)).Message.ToString();

                }
            }
            catch (Exception e)
            {
                ri.Result = "NOK";
                ri.Message = e.Message;
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RefreshMonthlyLEWorkflow()
        {
            var ri = new ResultInfo();
            try
            {
                string Date = "Apr-2016";
                var wellNames = new List<string>();
                wellNames.Add("AUGER A10");
                var refresh = new MonthlyReportController();
                var hit = refresh.Search(Date:Date, WellNames:wellNames);
                ri.Result = ((ECIS.Core.ResultInfo)(hit.Data)).Result.ToString();
                if (((ECIS.Core.ResultInfo)(hit.Data)).Message != null)
                {
                    ri.Message = ((ECIS.Core.ResultInfo)(hit.Data)).Message.ToString();
                }
            }
            catch (Exception e)
            {
                ri.Result = "NOK";
                ri.Message = e.Message;
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RefreshMonthlyLETracker()
        {
            var ri = new ResultInfo();
            try
            {
                var projectNames = new List<string>();
                projectNames.Add("Auger");
                var refresh = new MonthlyReportController();
                var hit = refresh.GetDataDashboardLE(false,"2014","2016","",projectNames,null,"");
                bool res = hit.Data.ToBsonDocument().FirstOrDefault().Value.ToBoolean();
                var Result = "NOK";
                if (res)
                {
                    Result = "OK";
                }
                ri.Result = Result;
            }
            catch (Exception e)
            {
               ri.Result = "NOK";
               ri.Message = e.Message;
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RefreshMonthlyLEHistory()
        {
            var ri = new ResultInfo();
            try
            {
                var AsAtDate = new DateTime(2016,8,1);
                string[] projectNames = new string[1];
                projectNames[0] = "Auger";
                string[] Ops = new string[1];
                Ops[0] = "OP16";
                var refresh = new HistoricalDataMonthlyController();
                var hit = refresh.Calculate(AsAtDate,null,null,null,projectNames,null,"Week",1,Ops,"");
                ri.Result = ((ECIS.Core.ResultInfo)(hit.Data)).Result.ToString();
                if (((ECIS.Core.ResultInfo)(hit.Data)).Message != null)
                {
                    ri.Message = ((ECIS.Core.ResultInfo)(hit.Data)).Message.ToString();
                }
            }
            catch (Exception e)
            {
               ri.Result = "NOK";
               ri.Message = e.Message;
            }
            return MvcTools.ToJsonResult(ri); 
        }

        public JsonResult RefreshMonthlyLEReport(WaterfallBase wb)
        {
            var ri = new ResultInfo();
            try
            {
                var wellNames = new List<string>();
                wellNames.Add("AUGER A10");
                wb.WellNames    = wellNames;
                wb.BaseOP       = "OP15";
                wb.DateStart    = null;
                wb.DateFinish   = null;
                wb.DateStart2   = null;
                wb.DateFinish2  = null;
                var refresh = new MonthlyReportNewController();
                var hit = refresh.GetData(wb);
                ri.Result = ((ECIS.Core.ResultInfo)(hit.Data)).Result.ToString();
                if (((ECIS.Core.ResultInfo)(hit.Data)).Message != null)
                {
                    ri.Message = ((ECIS.Core.ResultInfo)(hit.Data)).Message.ToString();
                }
            }
            catch (Exception e)
            {
               ri.Result = "NOK";
               ri.Message = e.Message;
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RefreshWellPIP(SearchParams sp)
        {
            var ri = new ResultInfo();
            try
            {
                var wellNames = new List<string>();
                wellNames.Add("AUGER A10");
                sp.wellNames = wellNames;
                sp.PIPType = "All";
                var refresh = new WellPIPController();
                var hit = refresh.Search(sp,1,0);
                ri.Result = ((ECIS.Core.ResultInfo)(hit.Data)).Result.ToString();
                if (((ECIS.Core.ResultInfo)(hit.Data)).Message != null)
                {
                    ri.Message = ((ECIS.Core.ResultInfo)(hit.Data)).Message.ToString();
                }
            }
            catch (Exception e)
            {
                ri.Result = "NOK";
                ri.Message = e.Message;
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult DownloadPIP(SearchParams sp)
        {
            var ri = new ResultInfo();
            try
            {
                var wellNames = new List<string>();
                wellNames.Add("AUGER A10");
                sp.wellNames = wellNames;
                sp.PIPType = "All";
                var refresh = new WellPIPController();
                var hit = refresh.DownloadPIP(sp);
                var result = hit;
            }
            catch (Exception e)
            {
                ri.Result = "NOK";
                ri.Message = e.Message;
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RefreshWaterfallCumulative(WaterfallBase wb)
        {
            var ri = new ResultInfo();
            try
            {
                var refresh = new WaterfallReportController();
                var hit = refresh.GetData(wb,"Waterfall","TQ");
                ri.Result = ((ECIS.Core.ResultInfo)(hit.Data)).Result.ToString();
                if (((ECIS.Core.ResultInfo)(hit.Data)).Message != null)
                {
                    ri.Message = ((ECIS.Core.ResultInfo)(hit.Data)).Message.ToString();
                }
            }
            catch (Exception e)
            {
                ri.Result = "NOK";
                ri.Message = e.Message;
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RefeshPIPComment()
        {
            var ri = new ResultInfo();
            try
            {
                var wellNames = new List<string>();
                wellNames.Add("AUGER A10");
                var refresh = new CommentController();
                var hit = refresh.GetAllComments(null,null,wellNames,null,null,null,"",1,0);
                var result = hit;

            }
            catch (Exception e)
            {
                ri.Result = "NOK";
                ri.Message = e.Message;
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RefreshHistoricalPIP()
        {
            var ri = new ResultInfo();
            try
            {
                var AsAtDate = new DateTime(2016,08,22);
                string[] wellNames = new string[1];
                wellNames[0] = "AUGER A10";
                var refresh = new HistoricalPIPModelController();
                var hit = refresh.Calculate(AsAtDate:AsAtDate,WellNames:wellNames,RollingType:"Week",RollingNo:1);
                ri.Result = ((ECIS.Core.ResultInfo)(hit.Data)).Result.ToString();
                if (((ECIS.Core.ResultInfo)(hit.Data)).Message != null)
                {
                    ri.Message = ((ECIS.Core.ResultInfo)(hit.Data)).Message.ToString();
                }
            }
            catch (Exception e)
            {
                ri.Result = "NOK";
                ri.Message = e.Message;
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RefreshPIPAnalyst()
        {
            var ri = new ResultInfo();
            try
            {
                var f = new PIPAnalysisFilter();
                f.PIPType = "Project PIP";
                f.GroupBy = "rig";
                f.PeriodStart = new DateTime(2016,01,01);
                f.PeriodFinish = new DateTime(2016,12,01);
                f.TakeDataFor = "total";
                var refresh = new PIPAnalysisController();
                var hit = refresh.GetDataScatter(f);
                ri.Result = ((ECIS.Core.ResultInfo)(hit.Data)).Result.ToString();
                if (((ECIS.Core.ResultInfo)(hit.Data)).Message != null)
                {
                    ri.Message = ((ECIS.Core.ResultInfo)(hit.Data)).Message.ToString();
                }
                var result = hit;
            }
            catch (Exception e)
            {
                ri.Result = "NOK";
                ri.Message = e.Message;
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult RefreshDataBrowser(WaterfallBase wb)
        {
            var ri = new ResultInfo();
            try
            {
                wb.BaseOP = "OP16";
                var refresh = new DataModel2Controller();
                var hit = refresh.GetWellSequenceInfo2(wb);
                ri.Result = ((ECIS.Core.ResultInfo)(hit.Data)).Result.ToString();
                if (((ECIS.Core.ResultInfo)(hit.Data)).Message != null)
                {
                    ri.Message = ((ECIS.Core.ResultInfo)(hit.Data)).Message.ToString();
                }
                var result = hit;
            }
            catch (Exception e)
            {
                ri.Result = "NOK";
                ri.Message = e.Message;
            }
            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult DownloadDataBrowser(WaterfallBase wb)
        {
            var ri = new ResultInfo();
            try
            {
                wb.BaseOP = "OP16";
                List<string> ops = new List<string>();
                ops.Add("OP16");
                var refresh = new DataModel2Controller();
                var hit = refresh.Export(wbs:wb,OPs:ops);
                bool res = hit.Data.ToBsonDocument().FirstOrDefault().Value.ToBoolean();
                var result = "NOK";
                if (res)
                {
                    result = "OK";
                }
                ri.Result = result.ToString();
            }
            catch (Exception e)
            {
                ri.Result = "NOK";
                ri.Message = e.Message;
            }
            return MvcTools.ToJsonResult(ri);
        }
	}
}