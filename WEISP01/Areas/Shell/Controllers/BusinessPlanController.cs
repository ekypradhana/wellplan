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
using System.Text.RegularExpressions;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class BusinessPlanController : Controller
    {
        //
        // GET: /Shell/BusinessPlan/
        public ActionResult Index()
        {
            ViewBag.UserName = WebTools.LoginUser.UserName;
            return View();
        }

        public ActionResult SequenceChart()
        {
            return View();
        }

        public JsonResult DeleteSimulation(string ID)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                DataHelper.Delete(new PlanSimulationHeader().TableName, q: Query.EQ("_id", ID));
                DataHelper.Delete(new PlanSimulation().TableName, q: Query.EQ("SimulationId", ID));
                DataHelper.Delete(new PlanSimulationBucket().TableName, q: Query.EQ("SimulationId", ID));
                return null;
            });
        }

        public JsonResult ChangeNameSimulation(string ID, string NewTitle)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var ret = PlanSimulationHeader.Get<PlanSimulationHeader>(ID);
                ret.Title = NewTitle;
                ret.Save();
                return null;
            });
        }
        public JsonResult GetSimulationId()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var getSeqNumber = DataHelper.Get("SequenceNos", "SimulationId");
                int newSimulationID = 0;
                if (getSeqNumber != null && getSeqNumber.Count() > 0)
                {
                    newSimulationID = BsonHelper.GetInt32(getSeqNumber, "NextNo");
                }
                else
                {
                    //new Seq Number
                    var bd = new BsonDocument();
                    bd.Set("_id", "SimulationId");
                    bd.Set("Title", "SimulationId");
                    bd.Set("NextNo", 1);
                    bd.Set("Format", "");
                    DataHelper.Save("SequenceNos", bd);
                    newSimulationID = 1;
                }
                var Simulation = String.Format("SIM{0}{1}{2}", DateTime.Now.Year, DateTime.Now.Month, newSimulationID);
                return Simulation;
            });
        }

        public JsonResult SaveDetailSimulation(List<PlanSimulationBucket> ListBucket, bool MoveToNearestDate = false)
        {
            var ri = new ResultInfo();

            try
            {
                
                foreach (var x in ListBucket)
                {
                    x.Sim.Cost = x.Sim.Cost * 1000000;
                    x.OP.Cost = x.OP.Cost * 1000000;
                    x.LE.Cost = x.LE.Cost * 1000000;
                    x.LS.Cost = x.LS.Cost * 1000000;
                    if (x._id != null)
                    {
                        var _id = Convert.ToString(x._id);
                        var dataChecker = PlanSimulationBucket.Populate<PlanSimulationBucket>(Query.And(
                            Query.NE("_id", _id),
                            Query.EQ("SimulationId", x.SimulationId),
                            Query.EQ("RigName", x.RigName),
                            Query.LT("SimSchedule.Start", Tools.ToUTC(x.SimSchedule.Finish)),
                            Query.GT("SimSchedule.Finish", Tools.ToUTC(x.SimSchedule.Start))
                        ));

                        if (!MoveToNearestDate && dataChecker.Any())
                        {
                            ri.PushException(new Exception("Can't save changes, because the date is crossing other activity date"));
                            return MvcTools.ToJsonResult(ri);
                        }
                        else if (MoveToNearestDate)
                        {
                            var originalBucket = PlanSimulationBucket.Get<PlanSimulationBucket>(_id);
                            var dateDiff = 0;
                            var isFirst = true;

                            if (!x.RigName.Equals(originalBucket.RigName))
                            {
                                PlanSimulationBucket.Populate<PlanSimulationBucket>(Query.And(
                                    Query.NE("_id", _id),
                                    Query.EQ("SimulationId", x.SimulationId),
                                    Query.EQ("RigName", x.RigName),
                                    Query.GTE("SimSchedule.Start", Tools.ToUTC(x.SimSchedule.Start))
                                ), sort: new SortByBuilder().Ascending("SimSchedule.Start")).ForEach(d =>
                                {
                                    if (isFirst)
                                    {
                                        var newActivityDiff = (Tools.ToUTC(x.SimSchedule.Finish) - Tools.ToUTC(x.SimSchedule.Start)).Days;

                                        x.Delete();
                                        x.SimSchedule.Start = d.SimSchedule.Start;
                                        x.SimSchedule.Finish = d.SimSchedule.Start.AddDays(newActivityDiff);
                                        x.Save();

                                        var oldNearestActivityDiff = (Tools.ToUTC(d.SimSchedule.Finish) - Tools.ToUTC(d.SimSchedule.Start)).Days;

                                        d.SimSchedule.Start = x.SimSchedule.Finish;
                                        d.SimSchedule.Finish = d.SimSchedule.Start.AddDays(oldNearestActivityDiff);
                                        d.Delete();
                                        d.Save();

                                        dateDiff = (Tools.ToUTC(d.SimSchedule.Start) - Tools.ToUTC(x.SimSchedule.Start)).Days;
                                        isFirst = false;
                                        return;
                                    }

                                    d.SimSchedule.Start = d.SimSchedule.Start.AddDays(dateDiff);
                                    d.SimSchedule.Finish = d.SimSchedule.Finish.AddDays(dateDiff);
                                    d.Delete();
                                    d.Save();
                                });
                            }
                            else
                            {
                                PlanSimulationBucket.Populate<PlanSimulationBucket>(Query.And(
                                    Query.NE("_id", _id),
                                    Query.EQ("SimulationId", x.SimulationId),
                                    Query.EQ("RigName", x.RigName),
                                    Query.GTE("SimSchedule.Start", Tools.ToUTC(originalBucket.SimSchedule.Start))
                                ), sort: new SortByBuilder().Ascending("SimSchedule.Start")).ForEach(d =>
                                {
                                    if (isFirst)
                                    {
                                        dateDiff = -(d.SimSchedule.Start - x.SimSchedule.Finish).Days;
                                        isFirst = false;
                                    }

                                    d.SimSchedule.Start = d.SimSchedule.Start.AddDays(dateDiff);
                                    d.SimSchedule.Finish = d.SimSchedule.Finish.AddDays(dateDiff);
                                    d.Delete();
                                    d.Save();
                                });
                            }
                        }

                        DataHelper.Delete(new PlanSimulationBucket().TableName, Query.EQ("_id", _id));
                        x.Save();

                        continue;
                    }

                   
                    x.Delete();
                    x.Save();
                }
            }
            catch (Exception e)
            {
                ri.PushException(e);
            }

            return MvcTools.ToJsonResult(ri);
        }

        public JsonResult GetDetailSimulation(string BucketId)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var bucket = PlanSimulationBucket.Get<PlanSimulationBucket>(BucketId);
                return bucket;
            });
        }

        public JsonResult DeleteDetailSimulation(string BucketId)
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var Bucket = PlanSimulationBucket.Get<PlanSimulationBucket>(BucketId);
                Bucket.Delete();
                return null;
            });
        }

        public JsonResult SaveComparison(string CurId, string CompareId)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var curSim = PlanSimulationHeader.Get<PlanSimulationHeader>(Query.EQ("_id", CurId));
                curSim.BindComparison(CompareId);
                return "OK";
            });
        }
        public JsonResult SaveSimulation(string SimulationId, string Title, string CopyFrom)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var simHead = new PlanSimulationHeader();
                if (CopyFrom == "Current OP" || CopyFrom == "Current LS" || CopyFrom == "Current LE")
                {

                    
                    simHead._id = SimulationId;
                    simHead.Title = Title;
                    simHead.CopyFrom = CopyFrom == null ? "No Copy" : CopyFrom;
                    simHead.CreatedBy = WebTools.LoginUser.UserName;
                    simHead.Locked = false;
                    simHead.Save();
                    

                    //add seq nos
                    var getSeqNumber = DataHelper.Get("SequenceNos", "SimulationId");
                    getSeqNumber.Set("NextNo", BsonHelper.GetInt32(getSeqNumber, "NextNo") + 1);
                    DataHelper.Save("SequenceNos", getSeqNumber);

                    var wps = WellActivity.Populate<WellActivity>();
                    List<BsonDocument> bdocs = new List<BsonDocument>();
                    foreach (var t in wps)
                    {
                        BsonDocument bdoc = new BsonDocument();
                        bdoc = t.ToBsonDocument();

                        var cur_id = t._id.ToString();
                        string newId = string.Format("{0}-{1}", cur_id, SimulationId);

                        bdoc.Set("_id", newId);
                        bdoc.Set("SimulationId", SimulationId);
                        bdocs.Add(bdoc);
                    }

                    //List<PlanSimulation> results = new List<PlanSimulation>();
                    foreach (var b in bdocs)
                    {
                        PlanSimulation myObj = BsonSerializer.Deserialize<PlanSimulation>(b);
                        //results.Add(myObj);
                        myObj.Save();
                    }
                }
                else if (CopyFrom != "No Copy" && CopyFrom != null)
                {
                    string[] param = CopyFrom.Split('|');
                    string id = param[0].Trim();

                    var tem = PlanSimulationHeader.Get<PlanSimulationHeader>(Query.EQ("_id", id));
                    simHead._id = SimulationId;
                    simHead.Title = Title;
                    simHead.CopyFrom = tem.CopyFrom ; //== null ? "No Copy" : CopyFrom; //"Copy From " + tem.Title; // 
                    simHead.CreatedBy = WebTools.LoginUser.UserName;
                    simHead.Comparisons = tem.Comparisons;
                    simHead.AnnualValues = tem.AnnualValues;
                    simHead.Save();

                    //add seq nos
                    var getSeqNumber = DataHelper.Get("SequenceNos", "SimulationId");
                    getSeqNumber.Set("NextNo", BsonHelper.GetInt32(getSeqNumber, "NextNo") + 1);
                    DataHelper.Save("SequenceNos", getSeqNumber);

                    var wps = PlanSimulation.Populate<PlanSimulation>(Query.EQ("SimulationId", id));
                    List<BsonDocument> bdocs = new List<BsonDocument>();
                    foreach (var t in wps)
                    {
                        var tm = t.ToBsonDocument();

                        var cur_id = t._id.ToString().Split('-');
                        string newId = string.Format("{0}-{1}", cur_id[0], SimulationId);

                        tm.Set("_id", newId);
                        tm.Set("SimulationId", SimulationId);
                        bdocs.Add(tm);
                    }

                    //List<PlanSimulation> results = new List<PlanSimulation>();
                    foreach (var b in bdocs)
                    {
                        PlanSimulation myObj = BsonSerializer.Deserialize<PlanSimulation>(b);
                        //results.Add(myObj);
                        myObj.Save();
                    }

                }
                else
                {
                    simHead._id = SimulationId;
                    simHead.Title = Title;
                    simHead.CopyFrom = "No Copy";
                    simHead.CreatedBy = WebTools.LoginUser.UserName;
                    simHead.Save();

                    //add seq nos
                    var getSeqNumber = DataHelper.Get("SequenceNos", "SimulationId");
                    getSeqNumber.Set("NextNo", BsonHelper.GetInt32(getSeqNumber, "NextNo") + 1);
                    DataHelper.Save("SequenceNos", getSeqNumber);
                }

                simHead.AnnualValues = simHead.GetAnnualValues();
                simHead.Save();

                return "OK";
            });
        }


        public JsonResult GetDetailByWell(string wellName, string rigName, string sequenceId,string SimulationId)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("WellName", wellName));
                //qs.Add(Query.EQ("RigName", rigName));
                qs.Add(Query.EQ("UARigSequenceId", sequenceId));
                qs.Add(Query.EQ("SimulationId", SimulationId));
                var p = PlanSimulationBucket.Populate<PlanSimulationBucket>(q: Query.And(qs));
                foreach (var x in p)
                {
                    x.Sim.Cost = Tools.Div(x.Sim.Cost, 1000000);
                    x.Sim.Days = (x.SimSchedule.Finish - x.SimSchedule.Start).TotalDays + 1;
                    x.OP.Cost = Tools.Div(x.OP.Cost, 1000000);
                    x.OP.Days = (x.OPSchedule.Finish - x.OPSchedule.Start).TotalDays + 1;
                    x.LE.Cost = Tools.Div(x.LE.Cost, 1000000);
                    x.LE.Days = (x.LESchedule.Finish - x.LESchedule.Start).TotalDays + 1;
                    x.LS.Cost = Tools.Div(x.LS.Cost, 1000000);
                    x.LS.Days = (x.LSSchedule.Finish - x.LSSchedule.Start).TotalDays + 1;
                }
                return p;
            });
        }

        public JsonResult DeleteWellActivity(string SimulationId, string WellName, string RigName)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var qs = new List<IMongoQuery>();
                qs.Add(Query.EQ("SimulationId", SimulationId));
                qs.Add(Query.EQ("WellName", WellName));
                qs.Add(Query.EQ("RigName", RigName));
                DataHelper.Delete("WEISPlanSimulationBuckets", Query.And(qs));
                return "OK";
            });
        }

        public JsonResult GetWellActivityForSequenceChart(string id, string param = "", List<string> rigNames = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                List<IMongoQuery> queries = new List<IMongoQuery>();
                queries.Add(Query.EQ("SimulationId", id));
                queries.Add(Query.Matches("WellName", new BsonRegularExpression(new Regex(param.ToString().ToLower(), RegexOptions.IgnoreCase))));
                if (rigNames != null && rigNames.Count > 0)
                {
                    queries.Add(Query.In("RigName", new BsonArray(rigNames)));
                }

                var bpa = new List<PlanSimulationBucket>();
                var hdr = PlanSimulationHeader.Populate<PlanSimulationHeader>();

                var header = PlanSimulationHeader.Get<PlanSimulationHeader>(Query.EQ("_id", id));
                var bucket = PlanSimulationBucket.Populate<PlanSimulationBucket>(Query.And(queries));
                bpa.AddRange(bucket);

                var comprasionCounter = 0;
                (header.Comparisons ?? new List<PlanSimulationComparison>()).ForEach(d =>
                {
                    var comparisonBucket = PlanSimulationBucket.Populate<PlanSimulationBucket>(Query.And(queries));
                    comprasionCounter++;
                    comparisonBucket.ForEach(e =>
                    {
                        e.SimulationId = Convert.ToString(e.SimulationId);
                        e.Counter = d.CompNo;
                        bpa.Add(e);
                    });
                });

                var data = bpa.GroupBy(d => d.RigName)
                    .Select(d =>
                    {
                        var simulations = d.GroupBy(e => new { SimulationId = Convert.ToString(e.SimulationId), Counter = e.Counter })
                            .Select(e =>
                            {
                                var currentheader = hdr.FirstOrDefault(f => Convert.ToString(f._id).Equals(e.Key.SimulationId));


                                var isCurrent = ((e.FirstOrDefault() ?? new PlanSimulationBucket()).Counter == 1);
                                if (isCurrent)
                                {
                                    currentheader = header;
                                }

                                var activities = e.GroupBy(f => new
                                {
                                    RigName = d.Key,
                                    SimulationID = e.Key.SimulationId,
                                    WellName = f.WellName,
                                    UARigSequenceId = f.UARigSequenceId
                                })
                                    .Select(f =>
                                    {
                                        var psSchedule = new DateRange()
                                        {
                                            Start = DateTime.Now,
                                            Finish = DateTime.Now
                                        };

                                        var phases = f.Where(g => g.SimSchedule.Start > new DateTime(2010, 1, 1) && g.SimSchedule.Finish > new DateTime(2010, 1, 1))
                                            .GroupBy(g => g.ActivityType)
                                            .Select(g =>
                                            {
                                                var subHeader = g.FirstOrDefault();

                                                if (psSchedule.Start > subHeader.SimSchedule.Start)
                                                    psSchedule.Start = subHeader.SimSchedule.Start;
                                                if (psSchedule.Finish < subHeader.SimSchedule.Finish)
                                                    psSchedule.Finish = subHeader.SimSchedule.Finish;

                                                return new
                                                {
                                                    _id = subHeader._id,
                                                    ActivityType = g.Key,
                                                    LevelOfEstimate = subHeader.LevelOfEstimate,
                                                    PhSchedule = subHeader.SimSchedule
                                                };
                                            }).ToList();

                                        return new
                                        {
                                            WellName = f.Key.WellName,
                                            UARigSequenceId = f.Key.UARigSequenceId,
                                            PsSchedule = psSchedule,
                                            Phases = phases
                                        };
                                    }).ToList();

                                return new
                                {
                                    _id = e.Key.SimulationId,
                                    title = currentheader.Title,
                                    Activities = activities,
                                    Current = isCurrent,
                                    CopyFrom = currentheader.CopyFrom
                                };
                            });

                        return new
                        {
                            RigName = d.Key,
                            Simulations = simulations
                        };
                    }).ToList();

                return data;
            });
        }

        public JsonResult GetWellActivity(string id, List<string> param = null)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                double divider = 1000000.00;
                List<IMongoQuery> queries = new List<IMongoQuery>();
                queries.Add(Query.EQ("SimulationId", id));
                //queries.Add(Query.Matches("WellName", new BsonRegularExpression(new Regex(param.ToString().ToLower(), RegexOptions.IgnoreCase))));
                if (param != null && param.Count > 0)
                {
                    if ( param.Count == 1 && param.FirstOrDefault().Trim().Equals(""))
                    {

                    }
                    else
                    {
                        queries.Add(Query.In("WellName", new BsonArray(param)));
                    }
                }   
                var bpa = PlanSimulationBucket.Populate<PlanSimulationBucket>(Query.And(queries.ToArray())).OrderBy(d => d.RigName);
                List<BsonDocument> row = new List<BsonDocument>();

                foreach (var r in bpa.GroupBy(x => new { x.WellName,x.RigName, x.UARigSequenceId }))
                {
                    BsonDocument document = new BsonDocument { 
                        new BsonDocument{
                            {"_id",r.FirstOrDefault()._id.ToString()},
                            {"SimulationId",r.FirstOrDefault().SimulationId},
                            {"WellName", r.Key.WellName},
                            {"RigName", r.Key.RigName},
                            {"UARigSequenceId", r.Key.UARigSequenceId},
                            {"ExType", r.FirstOrDefault().ExType},
                            {"Simulation", new BsonDocument
                                {
                                    {"Start",r.Min(x=>x.SimSchedule.Start)},
                                    {"Finish",r.Min(x=>x.SimSchedule.Finish)},
                                    {"Days",r.Sum(x=>x.Sim.Days)},
                                    {"Cost",Tools.Div(r.Sum(x=>x.Sim.Cost),divider)},
                                }
                            },
                            {"OP", new BsonDocument
                                {
                                    {"Days",r.Sum(x=>x.OP.Days)},
                                    {"Cost", Tools.Div(r.Sum(x=>x.OP.Cost),divider)}
                                }
                            },
                            {"LS", new BsonDocument
                                {
                                    {"Days",r.Sum(x=>x.LS.Days)},
                                    {"Cost", Tools.Div(r.Sum(x=>x.LS.Cost),divider)}
                                }
                            },
                            {"LE", new BsonDocument
                                {
                                    {"Days",r.Sum(x=>x.LE.Days)},
                                    {"Cost", Tools.Div(r.Sum(x=>x.LE.Cost),divider)}
                                }
                            },
                            {"IsNew",r.FirstOrDefault().isNewWell ? "Yes" : ""}
                        }
                    };
                    row.Add(document);
                }

                return DataHelper.ToDictionaryArray(row);
            });
        }
        internal static MongoDatabase _db;
        internal static MongoDatabase GetDb(string connection, string database)
        {
            if (_db == null)
            {
                var connectionString = connection;
                var client = new MongoClient("mongodb://" + connectionString);//+ ":27017");
                MongoServer server = client.GetServer();
                MongoDatabase t = server.GetDatabase(database);
                _db = t;
            }
            return _db;
        }

        public static List<string> GetUploadedLSCollection(string colNamePrefix = "")
        {
            string host = System.Configuration.ConfigurationSettings.AppSettings["ServerHost"];
            string db = System.Configuration.ConfigurationSettings.AppSettings["ServerDb"];
            MongoDatabase mongo = GetDb(host, db);
            List<string> res = new List<string>();
            foreach (string t in mongo.GetCollectionNames().ToList())
            {
                if (t.Substring(0, 2).Equals("OP") && t.Substring(t.Length - 2, 2).Equals("V2"))
                {
                    res.Add(t);
                }
            }
            return res;
        }

        public JsonResult GetCopyFrom()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var req = PlanSimulationHeader.Populate<PlanSimulationHeader>().Select(x => new
                {
                    Simulation = x._id + " | " + x.Title
                }).Distinct();
                List<string> result = new List<string>();
                result.Add("No Copy");
                result.Add("Current OP");
                result.Add("Current LE");
                result.Add("Current LS");

                //foreach (var y in GetUploadedLSCollection())
                //{
                //    result.Add(y);
                //}
                foreach (var i in req.ToList())
                {
                    result.Add(i.Simulation);
                }
                return result;
            });

        }

        public JsonResult GetComparisonSelect()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var req = PlanSimulationHeader.Populate<PlanSimulationHeader>().Select(x => new
                {
                    Simulation = x._id + "-" + x.Title
                }).Distinct();
                List<string> res = new List<string>();
                res.Add("Current OP");
                res.Add("Current LE");
                res.Add("Current LS");
                foreach (var i in req.ToList())
                {
                    res.Add(i.Simulation);
                }
                return res;
            });
        }
        public JsonResult GetEvent(int WellId)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var queries = Query.EQ("_id", WellId);
                var ev = PlanSimulation.Populate<PlanSimulation>(queries);
                var evData = ev.Select(d => d.Phases);
                return evData;
            });
        }


        public JsonResult SelectSimulationHeader(string filter = "")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var q = Query.EQ("_id", filter);

                var simh = PlanSimulationHeader.Populate<PlanSimulationHeader>(q);

                var data_header = simh.Select(x => new
                {
                    Id = x._id,
                    Title = x.Title,
                    CreatedBy = x.CreatedBy,
                    Copy = x.CopyFrom,
                    values = x.AnnualValues,
                    Locked = x.Locked,
                    Comparison = x.Comparisons
                });

                var period = new List<Int32>();

                int minyear = 0;
                int maxyear = 0;

                List<BsonDocument> bsl = new List<BsonDocument>();
                if (data_header.Any())
                {
                    foreach (var row in data_header)
                    {
                        BsonDocument bs = new BsonDocument();
                        bs.Set("SimulationId", row.Id.ToString());
                        bs.Set("Title", row.Title);
                        bs.Set("CreatedBy", row.CreatedBy);
                        bs.Set("Copy", row.Copy);
                        bs.Set("CompNo", 0);
                        bs.Set("Locked",row.Locked);
                        foreach (var t in row.values)
                        {
                            bs.Set("Days_" + t.DateId, t.Days);
                            bs.Set("Cost_" + t.DateId, t.Cost/1000000);

                            if (minyear == 0 && maxyear == 0)
                            {
                                minyear = t.DateId;
                                maxyear = t.DateId;
                            }
                            else
                            {
                                if (t.DateId < minyear)
                                {
                                    minyear = t.DateId;
                                }
                                if (t.DateId > maxyear)
                                {
                                    maxyear = t.DateId;
                                }
                            }

                        }
                        bsl.Add(bs);

                        if (row.Comparison.Any())
                        {
                            foreach (var comp in row.Comparison)
                            {
                                BsonDocument bsComp = new BsonDocument();
                                int comNo = Convert.ToInt32(comp.CompNo);
                                bsComp.Set("SimulationId", comp._id.ToString());
                                bsComp.Set("Title", comp.Title);
                                bsComp.Set("CreatedBy", comp.CreatedBy);
                                bsComp.Set("Copy", comp.CopyFrom);
                                if (comNo == 0)
                                {
                                    comNo = 0;
                                } 
                                bsComp.Set("CompNo",comNo);
                                if (comp.AnnualValues.Any())
                                {
                                    foreach (var detCom in comp.AnnualValues)
                                    {
                                        bsComp.Set("Days_" + detCom.DateId, detCom.Days);
                                        bsComp.Set("Cost_" + detCom.DateId, detCom.Cost/1000000);

                                        if (minyear == 0 && maxyear == 0)
                                        {
                                            minyear = detCom.DateId;
                                            maxyear = detCom.DateId;
                                        }
                                        else
                                        {
                                            if (detCom.DateId < minyear)
                                            {
                                                minyear = detCom.DateId;
                                            }
                                            if (detCom.DateId > maxyear)
                                            {
                                                maxyear = detCom.DateId;
                                            }
                                        }

                                    }
                                }
                                bsl.Add(bsComp);
                            }
                        }

                    }


                }
                else
                {
                    BsonDocument bs = new BsonDocument();
                    bs.Set("SimulationId", "");
                    bs.Set("Title", "");
                    bs.Set("CreatedBy", "");
                    bs.Set("Copy", "");
                    bs.Set("CompNo", 0);
                    bsl.Add(bs);
                }

                period.Add(minyear);
                period.Add(maxyear);

                return new { Year = period, Detail = DataHelper.ToDictionaryArray(bsl) };
            });
        }
        public JsonResult GetSimulationHeader(string filter = "")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var q = Query.EQ("Title", new BsonRegularExpression(new Regex(filter.ToString().ToLower(), RegexOptions.IgnoreCase)));

                var simh = PlanSimulationHeader.Populate<PlanSimulationHeader>(q);

                var data_header = simh.Select(x => new
                {
                    Id = x._id,
                    Title = x.Title,
                    CreatedBy = x.CreatedBy,
                    Copy = x.CopyFrom,
                    Locked = x.Locked, 
                    values = x.AnnualValues
                }).OrderByDescending(d=>d.Id);

                //var qBucket = PlanSimulationBucket.Populate<PlanSimulationBucket>();
                //var Bucket = qBucket.SelectMany(x => x.Values, (x, p) => new
                //{
                //    Year = p.DateId
                //}).Distinct();
                var period = new List<Int32>();
                //var MaxYear = Bucket.Max(x => Convert.ToInt32(x.Year));
                //var MinYear = Bucket.Min(x => Convert.ToInt32(x.Year));
                //period.Add(MinYear);
                //period.Add(MaxYear);

                int minyear = 0;
                int maxyear = 0;

                List<BsonDocument> bsl = new List<BsonDocument>();
                if (data_header.Any())
                {
                    foreach (var row in data_header)
                    {
                        BsonDocument bs = new BsonDocument();
                        bs.Set("SimulationId", row.Id.ToString());
                        bs.Set("Title", row.Title);
                        bs.Set("CreatedBy", row.CreatedBy);
                        bs.Set("Copy", row.Copy);
                        bs.Set("Locked", row.Locked);
                        foreach (var t in row.values)
                        {
                            bs.Set("Days_" + t.DateId, t.Days);
                            bs.Set("Cost_" + t.DateId, t.Cost/1000000);

                            if (minyear == 0 && maxyear == 0)
                            {
                                minyear = t.DateId;
                                maxyear = t.DateId;
                            }
                            else
                            {
                                if (t.DateId < minyear)
                                {
                                    minyear = t.DateId;
                                }
                                if (t.DateId > maxyear)
                                {
                                    maxyear = t.DateId;
                                }
                            }

                        }
                        bsl.Add(bs);


                    }
                }
                else
                {
                    BsonDocument bs = new BsonDocument();
                    bs.Set("SimulationId", "");
                    bs.Set("Title", "");
                    bs.Set("CreatedBy", "");
                    bs.Set("Copy", "");
                    bs.Set("Locked",false);
                    bsl.Add(bs);
                }
 
                period.Add(minyear);
                period.Add(maxyear);

                return new { Year = period, Detail = DataHelper.ToDictionaryArray(bsl) };
            });
        }


        public JsonResult GetSimulationBuckets(string filter = "")
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var simId = "SIM2016119";
                var qwhere = Query.EQ("Title", new BsonRegularExpression(new Regex(filter.ToString().ToLower(), RegexOptions.IgnoreCase)));
                var header = PlanSimulationHeader.Populate<PlanSimulationHeader>(qwhere);
                var q = PlanSimulationBucket.Populate<PlanSimulationBucket>();
                var Bucket = q.SelectMany(x => x.Values, (x, p) => new
                {
                    Year = p.DateId
                }).Distinct();
                var BucketValue = q.SelectMany(x => x.Values, (x, p) => new
                {
                    SimulationId = x.SimulationId,
                    Year = p.DateId,
                    Days = p.Days,
                    Cost = p.Cost
                });
                var period = new List<Int32>();
                var MaxYear = Bucket.Max(x => Convert.ToInt32(x.Year));
                var MinYear = Bucket.Min(x => Convert.ToInt32(x.Year));
                period.Add(MinYear);
                period.Add(MaxYear);
                double pDays = 0; double pCost = 0;
                List<BsonDocument> bslistdoc = new List<BsonDocument>();


                BsonDocument docx = new BsonDocument();
                //pDays +=row.Days; 
                foreach (var head in header)
                {
                    //docx.Set("Title", head.Title);
                    //docx.Set("CreateBy", head.CreatedBy);
                    //docx.Set("Copy", head.CopyFrom);   

                    docx.Set("SimulationId", head._id.ToString());
                    foreach (var row in Bucket)
                    {

                        docx.Set("Year", row.Year);
                        foreach (var val in BucketValue.Where(d => d.SimulationId.Equals(head._id)))
                        {
                            if (row.Year == val.Year)
                            {
                                pDays += val.Days;
                                pCost += val.Cost;
                                docx.Set("Days", pDays);
                                docx.Set("Cost", pCost);

                            }

                        }
                        bslistdoc.Add(docx);
                    }
                }


                return new { Year = period, Header = header, Detail = DataHelper.ToDictionaryArray(bslistdoc) };
                //return DataHelper.ToDictionaryArray(bslistdoc);
                //return BucketValue;
            });
        }

        public JsonResult SaveWellData(string pSimulationId, bool pIsNew, string pRigName, string pWellName, string pExType, DateTime pSimulationStart, int pSimulationDays, string pActivityType, float pSimulationCost = 0, int pOPDays = 0, float pOPCost = 0, int pLSDays = 0, float pLSCost = 0, int pLEDays = 0, float pLECost = 0)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {


                PlanSimulationBucket psb = new PlanSimulationBucket();
                psb.SimulationId = pSimulationId;
                psb.RigName = pRigName;
                psb.WellName = pWellName;
                psb.ActivityType = pActivityType;
                psb.ExType = pExType;
                psb.SimSchedule = new DateRange() { Start = pSimulationStart, Finish = pSimulationStart.AddDays(pSimulationDays) };
                psb.OP.Days = pOPDays;
                psb.OP.Cost = pOPCost;
                psb.LS.Days = pLSDays;
                psb.LS.Cost = pLSCost;
                psb.LE.Days = pLEDays;
                psb.LE.Cost = pLECost;
                psb.Sim.Days = pSimulationDays;
                psb.Sim.Cost = pSimulationCost;
                psb.isNewWell = pIsNew;

                psb.Save();

                if (pIsNew)
                {
                    var newWell = new WellInfo();
                    newWell._id = pWellName;
                    try
                    {
                        newWell.Save();
                    }
                    catch (Exception e)
                    {

                    }
                }


                //new MasterRigName() { _id = Name, RigType = RigType, isOfficeLocation = isOfficeLocation }.Save();
                //new PlanSimulation() { 
                //    SimulationId = pSimulationId,
                //    RigName = pRigName,
                //    WellName = pWellName,
                //    EXType   = pExType
                //}.Save();

                //simluation
                //var q = Query.EQ("SimulationId", id);
                //List<BsonDocument> bslist = new List<BsonDocument>();
                //var phases = new List<BsonDocument>()
                //{
                //    new BsonDocument
                //        {
                //            {"Simulation", new BsonDocument
                //                {
                //                    {"Days",pSimulationDays},
                //                    {"Cost",pSimulationCost}
                //                }
                //            },
                //            {"OP", new BsonDocument
                //                {
                //                    {"Days",pOPDays},
                //                    {"Cost", pOPCost}
                //                }
                //            },
                //            {"LE", new BsonDocument
                //                {
                //                    {"Days",pLEDays},
                //                    {"Cost", pLECost}
                //                }
                //            }
                //        }
                //};
                //BsonDocument document = new BsonDocument { 
                //    new BsonDocument{
                //        {"SimulationId",pSimulationId},
                //        {"WellName", pWellName},
                //        {"RigName", pRigName},
                //        {"ExType", pExType},
                //        {"Phases", new BsonArray(phases) },
                //        {"IsNew",pIsNew}
                //    }
                //};
                //bslist.Add(document);
                //foreach (var val in bslist)
                //{
                //    PlanSimulation obData = BsonSerializer.Deserialize<PlanSimulation>(val);
                //    //results.Add(myObj);
                //    obData.Save();
                //}

                //else
                //{
                //    var simulation = PlanSimulation.Populate<PlanSimulation>();
                //    var dPhase = simulation.SelectMany(x => x.Phases, (x, p) => new
                //    {
                //        Id = x._id,
                //        SimulationId = x.SimulationId,
                //        RigName = x.RigName,
                //        WellName = x.WellName,
                //        ExType = x.EXType,
                //        Phase = p.Simulation
                //    });
                //    List<BsonDocument> bslist = new List<BsonDocument>();
                //    BsonDocument bs = new BsonDocument();
                //    bs.Set("SimulationId", pSimulationId);
                //    bs.Set("IsNew", pIsNew);
                //    bs.Set("WellName", pWellName);
                //    bs.Set("RigName", pRigName);
                //    foreach (var row in dPhase)
                //    {
                //        bs = row.ToBsonDocument();
                //        bs.Set("Simulation.Days", row.Phase.Days);
                //        bs.Set("Simulation.Cost", row.Phase.Cost);
                //        bslist.Add(bs);
                //    }

                //    foreach (var val in bslist)
                //    {
                //        PlanSimulation obData = BsonSerializer.Deserialize<PlanSimulation>(val);
                //        //results.Add(myObj);
                //        obData.Save();
                //    }
                //}

                return pSimulationId;
            });
        }

        public JsonResult GetWellName()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var req = PlanSimulationBucket.Populate<PlanSimulationBucket>().SelectMany(x => x.Values, (x, p) => new
                {
                    WellName = x.WellName
                }).Distinct();
                List<string> res = new List<string>();
                foreach (var i in req.ToList())
                {
                    res.Add(i.WellName);
                }
                return res;
            });
        }

        public JsonResult GetRigName()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
           {
               var req = PlanSimulationBucket.Populate<PlanSimulationBucket>().SelectMany(x => x.Values, (x, p) => new
               {
                   RigName = x.RigName
               }).Distinct();
               List<string> res = new List<string>();
               foreach (var i in req.ToList())
               {
                   res.Add(i.RigName);
               }
               return res.OrderBy(d => d);
           });
        }

        public JsonResult DeleteComparison(string SimId, int CompNo)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var data = PlanSimulationHeader.Get<PlanSimulationHeader>(SimId);
                List<PlanSimulationComparison> psc = new List<PlanSimulationComparison>();

                if (data != null) {
                    if (data.Comparisons.Any()) {
                        foreach (var oldComp in data.Comparisons) {
                            if (oldComp.CompNo != CompNo) {
                                psc.Add(oldComp);
                            }
                        }
                    }
                }

                data.Comparisons = psc;
                data.Save();


                return "OK";
            });
        }

        public JsonResult LockedSimulation(string SimId, bool locked)
        {
             return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var header = PlanSimulationHeader.Get<PlanSimulationHeader>(SimId);
                List<PlanSimulationComparison> psc = new List<PlanSimulationComparison>();
                foreach(var newComp in header.Comparisons){
                    psc.Add(newComp);
                }
                header._id = SimId;
                header.Comparisons = psc;
                header.Locked = locked;
                header.Save();

            return "OK";
            });
        }
    }
}