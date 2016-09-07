using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using MongoDB.Bson.Serialization;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using ECIS.Client.WEIS;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class ActivityActualController : Controller
    {
        //
        // GET: /Shell/ActivityActual/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult LoadAndSaveActivityActual()
        {
            return MvcResultInfo.Execute(null, (BsonDocument xx) =>
            {

                //List<BsonDocument> results = new List<BsonDocument>();

                string Directory = System.Configuration.ConfigurationSettings.AppSettings["actualdatapath"];
                string[] folders = System.IO.Directory.GetDirectories(Directory, "*", System.IO.SearchOption.AllDirectories);

                foreach (string f in folders)
                {
                    string[] filePaths = System.IO.Directory.GetFiles(f);

                    foreach (string file in filePaths)
                    {
                        using (StreamReader sr = new StreamReader(file))
                        {
                            String line = sr.ReadToEnd();
                            var doc = BsonDocument.Parse(line);
                            /*  Check on WellId on WEISWellJunction  */
                            var welljunct = DataHelper.Populate("WEISWellJunction", Query.EQ("SHELL_WELL_ID", BsonHelper.GetString(doc, "WELLID")));

                            string ourWellName = BsonHelper.GetString(welljunct.FirstOrDefault().ToBsonDocument(), "OUR_WELL_ID");
                            string EventCode = BsonHelper.GetString(doc.ToBsonDocument(), "EVENTCODE");


                            var wellactvy = DataHelper.Populate<WellActivity>("WEISWellActivities", Query.EQ("WellName", ourWellName));

                            var eventCOde = DataHelper.Populate("WEISActivities", Query.EQ("EDMActivityId", EventCode)); // bisa lebih dari 1
                            if (welljunct != null)
                            {
                                if (eventCOde.Count() > 0)
                                {
                                    WellActivityActual ac = new WellActivityActual();
                                    ac.WellName = BsonHelper.GetString(wellactvy.FirstOrDefault().ToBsonDocument(), "WellName");
                                    ac.SequenceId = BsonHelper.GetString(wellactvy.FirstOrDefault().ToBsonDocument(), "UARigSequenceId");
                                    ac.Actual = new List<WellActivityActualItem>();

                                    ac.UpdateVersion = Tools.ToUTC(BsonHelper.GetDateTime(doc, "DATEOPS"));
                                    ac.LastUpdate = Tools.ToUTC(DateTime.Now);

                                   

                                    List<string> _ids = new List<string>();
                                    foreach (var ev in eventCOde)
                                    {
                                        _ids.Add(BsonHelper.GetString(ev, "_id"));
                                    }

                                    var phasesameType = wellactvy.FirstOrDefault().Phases.Where(x => _ids.Contains(x.ActivityType));

                                    if (phasesameType.Count() > 0)
                                    {
                                        // ada yg sama 
                                        WellActivityActualItem item = new WellActivityActualItem(0);
                                        int phaseNO = phasesameType.FirstOrDefault().PhaseNo;
                                        item.PhaseNo = phaseNO;
                                        
                                        WellDrillData dt = new WellDrillData();
                                        dt.Days = BsonHelper.GetInt32(doc, "DAYSONLOCATION");
                                        dt.Cost = BsonHelper.GetInt32(doc, "ACTUALCOST");
                                        item.Data = dt;
                                        ac.Actual.Add(item);  
                                    }
                                    else
                                    {
                                        // tidak ada yg sama 
                                        // Event Code tidak ada di master, jadi tambahkan phaseNo + 1 dari max
                                        WellActivityActualItem item = new WellActivityActualItem(0);
                                        int phaseno = wellactvy.FirstOrDefault().Phases.OrderByDescending(x => x.PhaseNo).FirstOrDefault().PhaseNo;
                                        item.PhaseNo = phaseno + 1;
                                        WellDrillData dt = new WellDrillData();
                                        dt.Days = BsonHelper.GetInt32(doc, "DAYSONLOCATION");
                                        dt.Cost = BsonHelper.GetInt32(doc, "ACTUALCOST");
                                        item.Data = dt;
                                        ac.Actual.Add(item);  
                                    }
                                      

                                    ac.Save(ac.TableName);

                                    //results.Add(ac.PreSave(ac.ToBsonDocument()));
                                }

                            }

                        }

                    }

                }
                return "OK";
            });
        }
    }
}