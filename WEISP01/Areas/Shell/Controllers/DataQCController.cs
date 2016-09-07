using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core;
using ECIS.Identity;
using ECIS.AppServer.Models;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization;
using ECIS.AppServer.Controllers;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class DataQCController : Controller
    {
        //
        // GET: /Shell/MODChecker/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult MODCheckerBatch(string UserName, string Password)
        {

            #region Login Process
            //try
            //{
                UserName = UserName.ToLower();
                Microsoft.Owin.Security.IAuthenticationManager auth = HttpContext.GetOwinContext().Authentication;
                if (!User.Identity.IsAuthenticated)
                {

                    var ctx = new ECIS.Identity.IdentityContext(DataHelper.PopulateCollection("Users"));
                    var userMgr = new UserManager<IdentityUser>(new UserStore<IdentityUser>(ctx));
                    var user = userMgr.Find(UserName, Password);
                    if (user == null) throw new Exception("Invalid UserName or Password");

                    var identity = userMgr.CreateIdentity(user, DefaultAuthenticationTypes.ApplicationCookie);
                    auth.SignIn(new AuthenticationProperties
                    {
                        IsPersistent = true,
                        IssuedUtc = Tools.ToUTC(DateTime.Now),
                        ExpiresUtc = Tools.ToUTC(DateTime.Now).AddDays(1)
                    },
                        identity);

                }
            //}
            //catch (Exception e)
            //{
                
            //}
            #endregion
            //WebTools.LoginUser.Email = "arief@eaciit.com";
            //SendEmailMODChecker(toMailString: "eky.pradhana@eaciit.com", isByBatch: true, FileNameDownload: "DataQCFromBatch");
            //return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            return RedirectToAction("SendEmailMODChecker", "DataQC", new { toMailString = "eky.pradhana@eaciit.com,yoga.bangkit@eaciit.com,mas.muhammad@eaciit.com", FileNameDownload = "DataQCFromBatch", isByBatch = true });
        }

        public JsonResult SendEmailMODChecker(WaterfallBase wb = null, List<string> toMails = null, 
            string FileNameDownload = "", bool onlyExport = false, 
            bool isByBatch = false, string toMailString = "", bool onlyDiscrepancies = true)
        {
            return MvcResultInfo.Execute(() =>
            {
                if (WebTools.LoginUser.Email == null)
                {
                    return "Authentication Error!";
                }

                if (isByBatch)
                {
                    wb = new WaterfallBase();
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
                    wb.OPs = new List<string>() { "OP16" };
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
                    wb.Status = new List<string>() { "Complete", "Draft", "Modified" };
                    wb.ValidLSOnly = false;
                    wb.WellNames = null;
                    #endregion
                    var splitEmail = toMailString.Split(',');
                    toMails = new List<string>();
                    for (var i = 0; i < splitEmail.Count();i++ )
                        toMails.Add(splitEmail[i]);
                }

                var accessibleWells = WebTools.GetWellActivities();
                var email = WebTools.LoginUser.Email;
                var raw = wb.GetActivitiesForBizPlanForFiscalYear(email);

                var busplan = new BusplanController();
                var OPstate = busplan.GetMasterOP();
                string PreviousOP = "";
                string NextOP = "";
                string DefaultOP = busplan.getBaseOP(out PreviousOP, out NextOP);

                List<Excelbusplan> edetail = busplan.GenerateExportDetailData(raw, PreviousOP, accessibleWells, wb);
                string xlst = System.IO.Path.Combine(Server.MapPath("~/App_Data/Temp"), "BlankTemplate.xlsx");
                Aspose.Cells.Workbook wbook = new Aspose.Cells.Workbook(xlst);
                Aspose.Cells.Worksheet ws = wbook.Worksheets[0];
                ws.Name = "MOD";
                Aspose.Cells.Worksheet ws2 = wbook.Worksheets[1];
                ws2.Name = "EDM";
                Aspose.Cells.Worksheet ws3 = wbook.Worksheets[2];
                ws3.Name = "OP16 Mean Days";
                Aspose.Cells.Worksheet ws4 = wbook.Worksheets[3];
                ws4.Name = "OP16 Start";
                Aspose.Cells.Worksheet ws5 = wbook.Worksheets[4];
                ws5.Name = "TECOP Time";
                Aspose.Cells.Worksheet ws6 = wbook.Worksheets[5];
                ws6.Name = "NPT Time";
                Aspose.Cells.Cells sheet1 = ws.Cells;
                Aspose.Cells.Cells sheet2 = ws2.Cells;
                Aspose.Cells.Cells sheet3 = ws3.Cells;
                Aspose.Cells.Cells sheet4 = ws4.Cells;
                Aspose.Cells.Cells sheet5 = ws5.Cells;
                Aspose.Cells.Cells sheet6 = ws6.Cells;

                var bpc = new BusplanController();
                var wellnames = edetail.GroupBy(x => x.WellName).Select(x => x.Key).ToList();
                var activities = edetail.GroupBy(x => x.ActivityType).Select(x => x.Key).ToList();

                wb.WellNames = new List<string>();
                wb.WellNames.AddRange(wellnames);

                wb.Activities = new List<string>();
                wb.Activities.AddRange(activities);

                #region getDataFYV
                var getAllFYV = bpc.GetDataFiscalYear2(wb, "Year", 0, 0);
                var dataFYV = new List<DataFYVDetail>();
                if (getAllFYV.Data != null)
                {

                    var detailFYV =   getAllFYV.Data.ToBsonDocument().Get("Data").ToBsonDocument().Get("Detail").AsBsonArray;
                    foreach(var args in detailFYV)
                    {
                        var musmus = args.ToBsonDocument().Get("Data");
                        var musmus2 = musmus.ToBsonDocument().Get("_v").AsBsonArray.ToList(); //.SelectMany(x=>x.ToBsonDocument().Get("_v")).ToList();//.SelectMany(x => x.ToBsonDocument().Get("_v").AsBsonArray.ToList());
                        if (musmus2.Any())
                        {
                            foreach (var m in musmus2)
                            {
                                var mumu = new DataFYVDetail();
                                mumu.ActivityType = m.ToBsonDocument().Get("_v").ToBsonDocument().GetString("ActivityType");
                                mumu.WellName = m.ToBsonDocument().Get("_v").ToBsonDocument().GetString("WellName");
                                mumu.UARigSequenceId = m.ToBsonDocument().Get("_v").ToBsonDocument().GetString("UARigSequenceId");
                                mumu.MeanCostMOD = m.ToBsonDocument().Get("_v").ToBsonDocument().GetDouble("MeanCostMOD");
                                mumu.MeanCostEDM = m.ToBsonDocument().Get("_v").ToBsonDocument().GetDouble("MeanCostEDM");
                                //mumu.OPSchedule.Start = m.ToBsonDocument().Get("_v").ToBsonDocument().GetDateTime("EstimatePeriod.Start");
                                //mumu.OPSchedule.Finish = m.ToBsonDocument().Get("_v").ToBsonDocument().GetDateTime("EstimatePeriod.Finish");
                                mumu.OPSchedule.Start = m.ToBsonDocument().Get("_v").ToBsonDocument().Get("LESchedule").ToBsonDocument().Get("_v").ToBsonDocument().GetDateTime("Start");
                                mumu.OPSchedule.Finish = m.ToBsonDocument().Get("_v").ToBsonDocument().Get("LESchedule").ToBsonDocument().Get("_v").ToBsonDocument().GetDateTime("Finish");
                                dataFYV.Add(mumu);
                            }

                        }
                    }
                }
                #endregion

                #region getDataSpotfire2

                var spotfire = new Spotfire2Controller();
                var getDataSpotfire = spotfire.GetDataFiscalYear2(wb,"");
                var ListSpotfire = new List<Spotfire>();
                if (getDataSpotfire.Data != null)
                {
                    var dataSpotfilre = getDataSpotfire.Data.ToBsonDocument().Get("Data").ToBsonDocument().Get("Data").AsBsonArray;
                    if (dataSpotfilre != null && dataSpotfilre.Count > 0) {
                        foreach (var s in dataSpotfilre)
                        {
                            var sp = BsonSerializer.Deserialize<Spotfire>(s.ToBsonDocument());
                            ListSpotfire.Add(sp);
                        }
                    }
                }

                #endregion

                var grpFYV = dataFYV.GroupBy(x => new { x.WellName, x.ActivityType, x.UARigSequenceId });
                var grpSpotFire = ListSpotfire.GroupBy(x => new { x.WellName, x.ActivityType, x.SequenceId });
                var rownum = 2;
                var rownum1 = 2;
                var rownum2 = 2;
                var rownum3 = 2;
                var rownum4 = 2;
                var rownum5 = 2;
                var rownum6 = 2;
                foreach (var r in edetail)
                {

                    #region get BPIT
                    var MeanCostMODInBusPlan = 0.0;
                    var MeanCostEDMInBusPlan = 0.0;
                    var MeanDaysInBusPlan = 0.0;
                    var OPStartInBusPlan = Tools.DefaultDate;
                    var TECOPDaysInBusPlan = 0.0;
                    var NPTDaysInBusPlan = 0.0;
                    try
                    {
                        var getBusplan = bpc.GetDataFiscalYearOnBizPlanEntry2(r.Phase, r.ShellShare, r.RFM, r.Country, "USD", r.WellName, r.ActivityType, r.Phase.PhaseNo, r.RigSeqId, r.Phase.Estimate.SaveToOP); //bpc.Select(r.BusplanId, null, "", r.PhaseNo);
                        MeanCostMODInBusPlan = getBusplan.Data.ToBsonDocument().Get("Data").ToBsonDocument().GetDouble("MeanCostMOD");
                        MeanCostEDMInBusPlan = getBusplan.Data.ToBsonDocument().Get("Data").ToBsonDocument().GetDouble("MeanCostEDM");
                        MeanDaysInBusPlan = getBusplan.Data.ToBsonDocument().Get("Data").ToBsonDocument().GetDouble("MeanTime");
                        OPStartInBusPlan = getBusplan.Data.ToBsonDocument().Get("Data").ToBsonDocument().Get("EventDate").ToBsonDocument().GetDateTime("Start");
                        TECOPDaysInBusPlan = getBusplan.Data.ToBsonDocument().Get("Data").ToBsonDocument().GetDouble("TECOPDays");
                        NPTDaysInBusPlan = getBusplan.Data.ToBsonDocument().Get("Data").ToBsonDocument().GetDouble("NPTDays");
                    }
                    catch (Exception e) { }
                    #endregion

                    #region Get FYV
                    var MeanCostMODInFYV = 0.0;
                    var MeanCostEDMInFYV = 0.0;
                    var MeanDaysInFYV = 0.0;
                    var OPStartInFYV = Tools.DefaultDate;
                    try
                    {
                        var filterFYV = grpFYV.Where(x => x.Key.WellName == r.WellName && x.Key.ActivityType == r.ActivityType && x.Key.UARigSequenceId == r.RigSeqId);
                        MeanCostMODInFYV = filterFYV.Sum(x => x.Sum(y => y.MeanCostMOD));
                        MeanCostEDMInFYV = filterFYV.Sum(x => x.Sum(y => y.MeanCostEDM));
                        OPStartInFYV = filterFYV.Min(x => x.Min(y => y.OPSchedule.Start));
                        var OPFinish = filterFYV.Max(x => x.Max(y => y.OPSchedule.Finish));
                        if (OPStartInFYV != Tools.DefaultDate && OPFinish != Tools.DefaultDate)
                        {
                            MeanDaysInFYV = new DateRange() { Start = OPStartInFYV, Finish = OPFinish }.Days;
                        }
                        //MeanCostEDMInFYV = grpFYV.Sum(x => x.ToBsonDocument().GetDouble("MeanCostEDM"));

                        //var getFYV = bpc.GetDataFiscalYear2(wb, "Year", 0, 0);
                        //MeanCostMODInFYV = getFYV.Data.ToBsonDocument().Get("Data").ToBsonDocument().Get("Summary").AsBsonArray.Where(x => BsonHelper.GetString(x.ToBsonDocument(), "Title") == "MeanCostMOD").FirstOrDefault().ToBsonDocument().GetDouble("TotalAllYears");
                        //MeanCostEDMInFYV = getFYV.Data.ToBsonDocument().Get("Data").ToBsonDocument().Get("Summary").AsBsonArray.Where(x => BsonHelper.GetString(x.ToBsonDocument(), "Title") == "MeanCostEDM").FirstOrDefault().ToBsonDocument().GetDouble("TotalAllYears");
                    }
                    catch (Exception e) { }
                    #endregion

                    #region Get DataBrowser
                    var databrowser = new DataModel2Controller();
                    var MODinDataBrowser = 0.0;
                    var MeanDaysInDataBrowser = 0.0;
                    var OPStartInDataBrowser = Tools.DefaultDate;
                    try
                    {
                        var qsWa = new List<IMongoQuery>();
                        qsWa.Add(Query.EQ("WellName", r.WellName));
                        qsWa.Add(Query.EQ("Phases.ActivityType", r.ActivityType));
                        qsWa.Add(Query.EQ("UARigSequenceId", r.RigSeqId));
                        qsWa.Add(Query.EQ("RigName", r.RigName));
                        var getWa = WellActivity.Get<WellActivity>(Query.And(qsWa));
                        if (getWa != null)
                        {
                            var getDB = databrowser.SelectNewOP(wb, Convert.ToInt32(getWa._id));
                            var dataDB = getDB.Data.ToBsonDocument().Get("Data").ToBsonDocument();
                            var deser = BsonSerializer.Deserialize<WellActivity>(dataDB);
                            if (deser.Phases.Any())
                            {
                                var filterPhases = deser.Phases.Where(x => x.ActivityType == r.ActivityType).FirstOrDefault();

                                //pastikan PLAN di body sudah di OP16
                                if (filterPhases.BaseOP != null && filterPhases.BaseOP.Count > 0 && filterPhases.BaseOP.OrderByDescending(x => x).FirstOrDefault() == "OP16")
                                {
                                    MODinDataBrowser = filterPhases.Plan.Cost * 1000000;
                                    MeanDaysInDataBrowser = filterPhases.Plan.Days;
                                    OPStartInDataBrowser = filterPhases.PlanSchedule.Start;
                                }
                            }
                        }
                    }
                    catch (Exception e) { }
                    #endregion


                    #region Get SpotFire
                    var NPTDaysInSpotFire = 0.0;
                    var TECOPDaysInSpotFire = 0.0;
                    var EDMInSpotFire = 0.0;
                    try
                    {
                        var filterSpotFire = grpSpotFire.Where(x => x.Key.WellName == r.WellName && x.Key.ActivityType == r.ActivityType && x.Key.SequenceId == r.RigSeqId);
                        NPTDaysInSpotFire = filterSpotFire.Sum(x => x.Sum(y => y.NPT.Days));
                        TECOPDaysInSpotFire = filterSpotFire.Sum(x => x.Sum(y => y.TECOP.Days));
                        EDMInSpotFire = filterSpotFire.Sum(x => x.Sum(y => y.MeanCostEDM));
                    }
                    catch (Exception e) { }
                    #endregion



                    #region get BusplanSP

                    var MODInBusplanSP = 0.0;
                    var EDMInBusplanSP = 0.0;
                    var MeanDaysInBusPlanSP = 0.0;
                    var OPStartInBusPlanSP = Tools.DefaultDate;
                    try
                    {
                        var bpcSP = new BusplanSPController();
                        var getBusplan = bpcSP.GetDataFiscalYearOnBizPlanEntry2(r.Phase, r.ShellShare, r.RFM, r.Country, "USD", r.WellName, r.ActivityType, r.Phase.PhaseNo, r.RigSeqId, r.Phase.Estimate.SaveToOP); //bpc.Select(r.BusplanId, null, "", r.PhaseNo);
                        MODInBusplanSP = getBusplan.Data.ToBsonDocument().Get("Data").ToBsonDocument().GetDouble("MeanCostMOD");
                        EDMInBusplanSP = getBusplan.Data.ToBsonDocument().Get("Data").ToBsonDocument().GetDouble("MeanCostEDM");
                        MeanDaysInBusPlanSP = getBusplan.Data.ToBsonDocument().Get("Data").ToBsonDocument().GetDouble("MeanTime");
                        OPStartInBusPlanSP = getBusplan.Data.ToBsonDocument().Get("Data").ToBsonDocument().Get("EventDate").ToBsonDocument().GetDateTime("Start");
                    }
                    catch (Exception e) { }
                    #endregion


                    #region Get DataBrowser SP
                    var databrowserSP = new DataModelSPController();
                    var MODInDataBrowserSP = 0.0;
                    var MeanDaysInDataBrowserSP = 0.0;
                    var OPStartInDataBrowserSP = Tools.DefaultDate;
                    try
                    {
                        var qsWa = new List<IMongoQuery>();
                        qsWa.Add(Query.EQ("WellName", r.WellName));
                        qsWa.Add(Query.EQ("Phases.ActivityType", r.ActivityType));
                        qsWa.Add(Query.EQ("UARigSequenceId", r.RigSeqId));
                        qsWa.Add(Query.EQ("RigName", r.RigName));
                        var getWa = WellActivity.Get<WellActivity>(Query.And(qsWa));
                        if (getWa != null)
                        {
                            var getDB = databrowserSP.SelectNewOP(wb, Convert.ToInt32(getWa._id));
                            var dataDB = getDB.Data.ToBsonDocument().Get("Data").ToBsonDocument();
                            var deser = BsonSerializer.Deserialize<WellActivity>(dataDB);
                            if (deser.Phases.Any())
                            {
                                var filterPhases = deser.Phases.Where(x => x.ActivityType == r.ActivityType).FirstOrDefault();

                                //pastikan PLAN di body sudah di OP16
                                if (filterPhases.BaseOP != null && filterPhases.BaseOP.Count > 0 && filterPhases.BaseOP.OrderByDescending(x => x).FirstOrDefault() == "OP16")
                                {
                                    MODInDataBrowserSP = filterPhases.Plan.Cost * 1000000;
                                    MeanDaysInDataBrowserSP = filterPhases.Plan.Days;
                                    OPStartInDataBrowserSP = filterPhases.PlanSchedule.Start;
                                }
                            }
                        }
                    }
                    catch (Exception e) { }
                    #endregion

                    bool isSheet1_Different = false;
                    bool isSheet2_Different = false;
                    bool isSheet3_Different = false;
                    bool isSheet4_Different = false;
                    bool isSheet5_Different = false;
                    bool isSheet6_Different = false;

                    #region diff in sheet1 : MOD
                    var baseMOD = MeanCostMODInBusPlan;
                    var batasAtasMOD = baseMOD + 1;
                    var batasBawahMOD = baseMOD - 1;
                    var diff = new List<string>();
                    var Reason = new List<string>();

                    if (baseMOD != r.MeanCostMOD || baseMOD != MODinDataBrowser || baseMOD != MeanCostMODInFYV || baseMOD != MODInBusplanSP || baseMOD != MODInDataBrowserSP)
                    {
                        if (
                            (batasAtasMOD > MeanCostMODInBusPlan && batasBawahMOD < MeanCostMODInBusPlan) &&
                            (batasAtasMOD > MODinDataBrowser && batasBawahMOD < MODinDataBrowser) &&
                            (batasAtasMOD > MeanCostMODInFYV && batasBawahMOD < MeanCostMODInFYV) &&
                            (batasAtasMOD > MODInBusplanSP && batasBawahMOD < MODInBusplanSP) &&
                            (batasAtasMOD > MODInDataBrowserSP && batasBawahMOD < MODInDataBrowserSP)
                            )
                        {
                            
                        }
                        else
                        {
                            //find which columns are different
                            if (MODinDataBrowser > batasAtasMOD || MODinDataBrowser < batasBawahMOD) diff.Add("Data Browser");
                            if (MeanCostMODInFYV > batasAtasMOD || MeanCostMODInFYV < batasBawahMOD) diff.Add("FYV");
                            if (MODInBusplanSP > batasAtasMOD || MODInBusplanSP < batasBawahMOD) diff.Add("Busplan SP");
                            if (MODInDataBrowserSP > batasAtasMOD || MODInDataBrowserSP < batasBawahMOD) diff.Add("Data Browser SP");

                            isSheet1_Different = true;
                            if (r.Status == "Draft") {
                                Reason.Add("Status is Draft");
                            }
                            if (MODInDataBrowserSP != MODinDataBrowser) Reason.Add("Code in Data Browser SP is not updated yet!");
                        }
                    }
                    #endregion

                    #region diff in sheet2 : EDM

                    var baseEDM = MeanCostEDMInBusPlan;
                    var batasAtasEDM = baseEDM + 1;
                    var batasBawahEDM = baseEDM - 1;

                    var diff2 = new List<string>();
                    var Reason2 = new List<string>();
                    EDMInSpotFire = EDMInSpotFire * 1000000;
                    if (baseEDM != r.MeanCostEDM || baseEDM != MeanCostEDMInBusPlan || baseEDM != MeanCostEDMInFYV || baseEDM != EDMInBusplanSP || baseEDM != EDMInSpotFire)
                    {
                        if (
                            (batasAtasEDM > MeanCostEDMInBusPlan && batasBawahEDM < MeanCostEDMInBusPlan) &&
                            (batasAtasEDM > MeanCostEDMInFYV && batasBawahEDM < MeanCostEDMInFYV) &&
                            (batasAtasEDM > EDMInBusplanSP && batasBawahEDM < EDMInBusplanSP) &&
                            (batasAtasEDM > EDMInSpotFire && batasBawahEDM < EDMInSpotFire)
                            )
                        {
                            
                        }
                        else
                        {
                            //find which columns are different
                            if (MeanCostEDMInFYV > batasAtasEDM || MeanCostEDMInFYV < batasBawahEDM) diff2.Add("FYV");
                            if (EDMInBusplanSP > batasAtasEDM || EDMInBusplanSP < batasBawahEDM) diff2.Add("Busplan SP");
                            if (EDMInSpotFire > batasAtasEDM || EDMInSpotFire < batasBawahEDM) diff2.Add("Spotfire");

                            isSheet2_Different = true;
                            if (r.TroubleFree.Days == 0)
                            {
                                Reason2.Add("Trouble free days is 0");
                            }
                        }
                    }

                    #endregion

                    #region diff in sheet3 : Mean Days

                    var baseMeanDays = MeanDaysInBusPlan;
                    var batasAtasMeanDays = baseMeanDays + 1;
                    var batasBawahMeanDays = baseMeanDays - 1;
                    var diff3 = new List<string>();
                    var Reason3 = new List<string>();
                    if (baseMeanDays != MeanDaysInDataBrowser)
                    {
                        if (
                            (batasAtasMeanDays > MeanDaysInDataBrowser && batasBawahMeanDays < MeanDaysInDataBrowser)
                            )
                        {

                        }
                        else
                        {
                            //find which columns are different
                            if (MeanDaysInDataBrowser > batasAtasMeanDays || MeanDaysInDataBrowser < batasBawahMeanDays) diff3.Add("Data Browser");

                            isSheet3_Different = true;
                            if (r.Status == "Draft") Reason3.Add("Status is Draft");
                            if (MeanDaysInDataBrowser != MeanDaysInDataBrowserSP) Reason3.Add("Code in Data Browser SP is not updated");
                            if (MeanDaysInBusPlan != MeanDaysInBusPlanSP) Reason3.Add("Code in Busplan SP is not updated");

                        }
                    }

                    #endregion

                    #region diff in sheet4 : OP Start

                    var baseOPStart = OPStartInBusPlan.ToString("yyyy-MM-dd");
                    var diff4 = new List<string>();
                    var Reason4 = new List<string>();
                    if (baseOPStart != OPStartInDataBrowser.ToString("yyyy-MM-dd"))
                    {
                            //find which columns are different
                            diff4.Add("Data Browser");
                            isSheet4_Different = true;
                            if (r.TroubleFree.Days == 0)
                            {
                                Reason4.Add("Trouble free days is 0");
                            }
                            if (r.Status == "Draft")
                            {
                                Reason4.Add("Status is Draft");
                            }
                    }

                    #endregion

                    #region diff in sheet5 : TECOP Days

                    var baseTECOPDays = TECOPDaysInBusPlan; //r.TECOP.Days;
                    var batasAtasTECOPDays = baseTECOPDays + 1;
                    var batasBawahTECOPDays = baseTECOPDays - 1;
                    var diff5 = new List<string>();
                    var Reason5 = new List<string>();
                    if (baseTECOPDays != r.TECOP.Days || baseTECOPDays != TECOPDaysInSpotFire)
                    {
                        if (
                            (batasAtasTECOPDays > TECOPDaysInBusPlan && batasBawahTECOPDays < TECOPDaysInBusPlan) &&
                            (batasAtasTECOPDays > TECOPDaysInSpotFire && batasBawahTECOPDays < TECOPDaysInSpotFire)
                            )
                        {

                        }
                        else
                        {
                            //find which columns are different
                            if (TECOPDaysInSpotFire > batasAtasTECOPDays || TECOPDaysInSpotFire < batasBawahTECOPDays) diff5.Add("Spotfire");

                            isSheet5_Different = true;
                            if (r.TroubleFree.Days == 0)
                            {
                                Reason5.Add("Trouble free days is 0");
                            }
                        }
                    }

                    #endregion

                    #region diff in sheet6 : NPT Days

                    var baseNPTDays = NPTDaysInBusPlan;
                    var batasAtasNPTDays = baseNPTDays + 1;
                    var batasBawahNPTDays = baseNPTDays - 1;
                    var diff6 = new List<string>();
                    if ((baseNPTDays != r.NPT.Days || baseNPTDays != NPTDaysInSpotFire))
                    {
                        if (
                            (batasAtasNPTDays > NPTDaysInBusPlan && batasBawahNPTDays < NPTDaysInBusPlan) &&
                            (batasAtasNPTDays > NPTDaysInSpotFire && batasBawahNPTDays < NPTDaysInSpotFire)
                            )
                        {

                        }
                        else
                        {
                            //find which columns are different
                            if (NPTDaysInSpotFire > batasAtasNPTDays || NPTDaysInSpotFire < batasBawahNPTDays) diff6.Add("Spotfire");

                            isSheet6_Different = true;
                        }
                    }

                    #endregion

                    #region write to sheet1 : MOD Cost
                    if ((onlyDiscrepancies && isSheet1_Different) || !onlyDiscrepancies)
                    {
                        sheet1["A" + rownum1.ToString()].PutValue(r.WellName);
                        sheet1["B" + rownum1.ToString()].PutValue(r.ActivityType);
                        sheet1["C" + rownum1.ToString()].PutValue(r.RigName);
                        sheet1["D" + rownum1.ToString()].PutValue(r.RigSeqId);
                        sheet1["E" + rownum1.ToString()].PutValue(r.Status);
                        sheet1["F" + rownum1.ToString()].PutValue(MeanCostMODInBusPlan);
                        sheet1["G" + rownum1.ToString()].PutValue(r.MeanCostMOD);
                        sheet1["H" + rownum1.ToString()].PutValue(MeanCostMODInBusPlan);
                        sheet1["I" + rownum1.ToString()].PutValue(MODInBusplanSP);
                        sheet1["J" + rownum1.ToString()].PutValue(MeanCostMODInFYV);
                        sheet1["K" + rownum1.ToString()].PutValue(MeanCostMODInFYV);
                        sheet1["L" + rownum1.ToString()].PutValue(MODinDataBrowser);
                        sheet1["M" + rownum1.ToString()].PutValue(MODInDataBrowserSP);
                        sheet1["N" + rownum1.ToString()].PutValue(String.Join(",", diff.ToArray()));
                        sheet1["O" + rownum1.ToString()].PutValue(String.Join(",",Reason.ToArray()));
                        rownum1++;
                    }
                    #endregion

                    #region write to sheet2 : EDM Cost
                    if ((onlyDiscrepancies && isSheet2_Different) || !onlyDiscrepancies)
                    {
                        sheet2["A" + rownum2.ToString()].PutValue(r.WellName);
                        sheet2["B" + rownum2.ToString()].PutValue(r.ActivityType);
                        sheet2["C" + rownum2.ToString()].PutValue(r.RigName);
                        sheet2["D" + rownum2.ToString()].PutValue(r.RigSeqId);
                        sheet2["E" + rownum2.ToString()].PutValue(r.Status);
                        sheet2["F" + rownum2.ToString()].PutValue(MeanCostEDMInBusPlan);
                        sheet2["G" + rownum2.ToString()].PutValue(r.MeanCostEDM);
                        sheet2["H" + rownum2.ToString()].PutValue(r.MeanCostEDM);
                        sheet2["I" + rownum2.ToString()].PutValue(EDMInSpotFire);
                        sheet2["J" + rownum2.ToString()].PutValue(MeanCostEDMInFYV);
                        sheet2["K" + rownum2.ToString()].PutValue(MeanCostEDMInFYV);
                        sheet2["L" + rownum2.ToString()].PutValue(String.Join(",", diff2.ToArray()));
                        sheet2["M" + rownum2.ToString()].PutValue(String.Join(",",Reason2.ToArray()));
                        rownum2++;
                        //sheet2["I" + rownum.ToString()].PutValue(Reason2);
                    }
                    #endregion

                    #region write to sheet3 : MeanDays
                    if ((onlyDiscrepancies && isSheet3_Different) || !onlyDiscrepancies)
                    {
                        sheet3["A" + rownum3.ToString()].PutValue(r.WellName);
                        sheet3["B" + rownum3.ToString()].PutValue(r.ActivityType);
                        sheet3["C" + rownum3.ToString()].PutValue(r.RigName);
                        sheet3["D" + rownum3.ToString()].PutValue(r.RigSeqId);
                        sheet3["E" + rownum3.ToString()].PutValue(r.Status);
                        sheet3["F" + rownum3.ToString()].PutValue(MeanDaysInBusPlan);
                        sheet3["G" + rownum3.ToString()].PutValue(r.Mean.Days);
                        sheet3["H" + rownum3.ToString()].PutValue(MeanDaysInBusPlan);
                        sheet3["I" + rownum3.ToString()].PutValue(MeanDaysInBusPlanSP);
                        sheet3["J" + rownum3.ToString()].PutValue(MeanDaysInFYV);
                        sheet3["K" + rownum3.ToString()].PutValue(MeanDaysInFYV);
                        sheet3["L" + rownum3.ToString()].PutValue(MeanDaysInDataBrowser);
                        sheet3["M" + rownum3.ToString()].PutValue(MeanDaysInDataBrowserSP);
                        sheet3["N" + rownum3.ToString()].PutValue(String.Join(",", diff3.ToArray()));
                        sheet3["O" + rownum3.ToString()].PutValue(String.Join(",",Reason3.ToArray()));
                        rownum3++;
                    }
                    #endregion

                    #region write to sheet4 : OP Start
                    if ((onlyDiscrepancies && isSheet4_Different) || !onlyDiscrepancies)
                    {
                        sheet4["A" + rownum4.ToString()].PutValue(r.WellName);
                        sheet4["B" + rownum4.ToString()].PutValue(r.ActivityType);
                        sheet4["C" + rownum4.ToString()].PutValue(r.RigName);
                        sheet4["D" + rownum4.ToString()].PutValue(r.RigSeqId);
                        sheet4["E" + rownum4.ToString()].PutValue(r.Status);
                        sheet4["F" + rownum4.ToString()].PutValue(OPStartInBusPlan.ToString("yyyy-MM-dd"));
                        sheet4["G" + rownum4.ToString()].PutValue(r.Event.Start.ToString("yyyy-MM-dd"));
                        sheet4["H" + rownum4.ToString()].PutValue(OPStartInBusPlan.ToString("yyyy-MM-dd"));
                        sheet4["I" + rownum4.ToString()].PutValue(OPStartInBusPlanSP.ToString("yyyy-MM-dd"));
                        sheet4["J" + rownum4.ToString()].PutValue(OPStartInFYV.ToString("yyyy-MM-dd"));
                        sheet4["K" + rownum4.ToString()].PutValue(OPStartInFYV.ToString("yyyy-MM-dd"));
                        sheet4["L" + rownum4.ToString()].PutValue(OPStartInDataBrowser.ToString("yyyy-MM-dd"));
                        sheet4["M" + rownum4.ToString()].PutValue(OPStartInDataBrowserSP.ToString("yyyy-MM-dd"));
                        sheet4["N" + rownum4.ToString()].PutValue(String.Join(",", diff4.ToArray()));
                        sheet4["O" + rownum4.ToString()].PutValue(String.Join(",", Reason4.ToArray()));
                        rownum4++;
                    }
                    #endregion

                    #region write to sheet5 : TECOP

                    if ((onlyDiscrepancies && isSheet5_Different) || !onlyDiscrepancies)
                    {
                        sheet5["A" + rownum5.ToString()].PutValue(r.WellName);
                        sheet5["B" + rownum5.ToString()].PutValue(r.ActivityType);
                        sheet5["C" + rownum5.ToString()].PutValue(r.RigName);
                        sheet5["D" + rownum5.ToString()].PutValue(r.RigSeqId);
                        sheet5["E" + rownum5.ToString()].PutValue(r.Status);
                        sheet5["F" + rownum5.ToString()].PutValue(Math.Round(TECOPDaysInBusPlan, 2));
                        sheet5["G" + rownum5.ToString()].PutValue(Math.Round(r.TECOP.Days,2));
                        sheet5["H" + rownum5.ToString()].PutValue(Math.Round(TECOPDaysInSpotFire, 2));
                        sheet5["I" + rownum5.ToString()].PutValue(String.Join(",", diff5.ToArray()));
                        sheet5["J" + rownum5.ToString()].PutValue(String.Join(",", Reason5.ToArray()));
                        rownum5++;
                    }
                    #endregion

                    #region write to sheet6 : NPT Days
                    if ((onlyDiscrepancies && isSheet6_Different) || !onlyDiscrepancies)
                    {
                        sheet6["A" + rownum6.ToString()].PutValue(r.WellName);
                        sheet6["B" + rownum6.ToString()].PutValue(r.ActivityType);
                        sheet6["C" + rownum6.ToString()].PutValue(r.RigName);
                        sheet6["D" + rownum6.ToString()].PutValue(r.RigSeqId);
                        sheet6["E" + rownum6.ToString()].PutValue(r.Status);
                        sheet6["F" + rownum6.ToString()].PutValue(NPTDaysInBusPlan);
                        sheet6["G" + rownum6.ToString()].PutValue(r.NPT.Days);
                        sheet6["H" + rownum6.ToString()].PutValue(NPTDaysInSpotFire);
                        sheet6["I" + rownum6.ToString()].PutValue(String.Join(",", diff6.ToArray()));
                        rownum6++;
                    }
                    #endregion
                    

                    rownum++;
                }


                var filename = String.Format("DataQC-{0}.xlsx", FileNameDownload);

                var newFileNameSingle = System.IO.Path.Combine(Server.MapPath("~/App_Data/Temp"), filename);

                wbook.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

                if (!onlyExport)
                {
                    //var toMails = new List<string>() { "yoga.bangkit@eaciit.com", "raulie@eaciit.com", "Ian.pirsch@eaciit.com", "akash.jain@eaciit.com" };
                    var attachments = new List<string> { newFileNameSingle };
                    ECIS.Client.WEIS.Email.SendEmailMODChecker("MODDiffChecker", toMails.ToArray(), ccMails: null, variables: null, attachments: attachments, developerModeEmail: "eky.pradhana@eaciit.com");
                }

                return filename;
            });
        }

        public FileResult DownloadModCheckerFile(string stringName)
        {
            var res = System.IO.Path.Combine(Server.MapPath("~/App_Data/Temp"), stringName);
            return File(res, Tools.GetContentType(".xlsx"), stringName);
        }

        public static async Task RunAsync()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://localhost/ShellDev/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // HTTP GET
                HttpResponseMessage response = await client.GetAsync("LoginProcess?username=eaciit&password=W31sAdmin&rememberMe=false");
            }
        }


        public JsonResult TestDulu(List<string> toMails, bool onlyExport = false)
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

                wb.WellNames = new List<string>() { "APPO AC006" };
                wb.Activities = new List<string>() { "DRL - INTERM." };

                var accessibleWells = WebTools.GetWellActivities();
                var email = WebTools.LoginUser.Email;
                var raw = wb.GetActivitiesForBizPlanForFiscalYear(email); //BizPlanActivity.Populate<BizPlanActivity>();

                var busplan = new BusplanController();
                var OPstate = busplan.GetMasterOP();
                string PreviousOP = "";
                string NextOP = "";
                string DefaultOP = busplan.getBaseOP(out PreviousOP, out NextOP);

                List<Excelbusplan> edetail = busplan.GenerateExportDetailData(raw, PreviousOP, accessibleWells, wb);

                var databrowser = new DataModel2Controller();

                var a = new WellActivityPhase();
                foreach (var r in edetail)
                {
                    var bpc = new BusplanController();

                    //get BPIT
                    var getBusplan = bpc.GetDataFiscalYearOnBizPlanEntry2(r.Phase, r.ShellShare, r.RFM, r.Country, "USD", r.WellName, r.ActivityType, r.Phase.PhaseNo, r.RigSeqId, r.Phase.Estimate.SaveToOP); //bpc.Select(r.BusplanId, null, "", r.PhaseNo);
                    var MeanCostMODInBusPlan = getBusplan.Data.ToBsonDocument().Get("Data").ToBsonDocument().GetDouble("MeanCostMOD");
                    var MeanCostEDMInBusPlan = getBusplan.Data.ToBsonDocument().Get("Data").ToBsonDocument().GetDouble("MeanCostEDM");

                    //Get FYV
                    wb.WellNames = new List<string>() { r.WellName };
                    wb.Activities = new List<string>() { r.ActivityType };
                    //var getFYV = bpc.GetDataFiscalYear2(wb, "Year", 0, 0);
                    //var MeanCostMODInFYV = getFYV.Data.ToBsonDocument().Get("Data").ToBsonDocument().Get("Summary").AsBsonArray.Where(x => BsonHelper.GetString(x.ToBsonDocument(), "Title") == "MeanCostMOD").FirstOrDefault().ToBsonDocument().GetDouble("TotalAllYears");
                    //var MeanCostEDMInFYV = getFYV.Data.ToBsonDocument().Get("Data").ToBsonDocument().Get("Summary").AsBsonArray.Where(x => BsonHelper.GetString(x.ToBsonDocument(), "Title") == "MeanCostEDM").FirstOrDefault().ToBsonDocument().GetDouble("TotalAllYears");

                    //Get DataBrowser
                    var getDB = databrowser.SelectNewOP(wb, r.BusplanId);
                    var dataDB = getDB.Data.ToBsonDocument().Get("Data").ToBsonDocument();
                    var deser = BsonSerializer.Deserialize<WellActivity>(dataDB);
                    if (deser.Phases.Any())
                    {
                        var filterPhases = deser.Phases.Where(x => x.ActivityType == r.ActivityType).FirstOrDefault();
                        a = filterPhases;
                    }
                }

                return a;
            });
        }



    }


    //[BsonIgnoreExtraElements]
    //internal class FYVDetail
    //{
    //    public List<FYVDetailHeader> Detail { get; set; }

    //    public FYVDetail()
    //    {
    //        Detail = new List<FYVDetailHeader>();
    //    }
    //}

    [BsonIgnoreExtraElements]
    internal class FYVDetail
    {
        public List<DataFYVDetail> Data { get; set; }
        public Int32 Year { get; set; }

        public FYVDetail()
        {
            Data = new List<DataFYVDetail>();
        }
    }

    [BsonIgnoreExtraElements]
    internal class DataFYVDetail
    {
        public string WellName { get; set; }
        public string ActivityType { get; set; }
        public string UARigSequenceId { get; set; }
        public double MeanCostMOD { get; set; }
        public double MeanCostEDM { get; set; }
        public DateRange OPSchedule { get; set; }
        public DataFYVDetail()
        {
            OPSchedule = new DateRange();
        }
    }
}