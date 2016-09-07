using ECIS.Client.WEIS;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class MetadataCheckerController : Controller
    {
        //
        // GET: /Shell/Spotfire/
        public ActionResult Index()
        {
            return View();
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

        private double GetRatio(DateRange period, WaterfallBase wb)
        {
            if (wb.PeriodView != null && wb.PeriodView.ToLower().Contains("fiscal"))
            {
                wb.DateStart = (wb.DateStart == null ? "" : wb.DateStart);
                wb.DateFinish = (wb.DateFinish == null ? "" : wb.DateFinish);

                string format = "yyyy-MM-dd";
                CultureInfo culture = CultureInfo.InvariantCulture;

                var filter = new DateRange()
                {
                    Start = Tools.ToUTC(wb.DateStart != "" ? DateTime.ParseExact(wb.DateStart, format, culture) : new DateTime(1900, 1, 1)),
                    Finish = Tools.ToUTC(wb.DateFinish != "" ? DateTime.ParseExact(wb.DateFinish, format, culture) : new DateTime(3000, 1, 1))
                };

                var fiscalYears = new List<Int32>();
                if (wb.FiscalYearStart == 0 || wb.FiscalYearFinish == 0)
                {

                }
                else
                {
                    var res = new List<int>();
                    for (var i = wb.FiscalYearStart; i <= wb.FiscalYearFinish; i++)
                        fiscalYears.Add(i);
                }
                var isInvalidFilter = (filter.Start == Tools.DefaultDate);

                if (isInvalidFilter)
                    filter = period;

                var ratios = DateRangeToMonth.ProportionNumDaysPerYear(period, filter).Where(f =>
                {
                    if (fiscalYears.Count > 0)
                        return fiscalYears.Contains(f.Key);

                    return true;
                });

                return ratios.Select(f => f.Value).DefaultIfEmpty(0).Sum(f => f);
            }

            return 1.0;
        }




        public List<Spotfire> GetSpot(WellActivity wa, string BaseOP = "")
        {
            List<Spotfire> spots = new List<Spotfire>();


            foreach (var t in wa.Phases.Where(x => string.IsNullOrEmpty(x.FundingType)))
            {

                #region Query
                IMongoQuery queries = null;
                var queryBasic = new List<IMongoQuery>();
                if (BaseOP.Equals(""))
                {
                    queryBasic.Add(Query.EQ("WellName", wa.WellName));
                    queryBasic.Add(Query.EQ("WellActivityId", wa._id.ToString()));
                    queryBasic.Add(Query.EQ("ActivityType", t.ActivityType));
                    queryBasic.Add(Query.EQ("RigName", wa.RigName));
                    queryBasic.Add(Query.EQ("SequenceId", wa.UARigSequenceId));
                }
                else
                {
                    queryBasic.Add(Query.EQ("WellName", wa.WellName));
                    queryBasic.Add(Query.EQ("WellActivityId", wa._id.ToString()));
                    queryBasic.Add(Query.EQ("ActivityType", t.ActivityType));
                    queryBasic.Add(Query.EQ("RigName", wa.RigName));
                    queryBasic.Add(Query.EQ("SequenceId", wa.UARigSequenceId));
                    queryBasic.Add(Query.EQ("OPType", BaseOP));
                }

                #endregion
                #region Advance
                var queryAdvance = new List<IMongoQuery>();
                var config2 = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "MetaDataConfig"));
                var newconfig = BsonHelper.Deserialize<MetadataConfig>(config2.FirstOrDefault().ToBsonDocument().Get("ConfigValue").ToBsonDocument());
                var rules = new List<string>();
                foreach (var rule in newconfig.Rules)
                {
                    if (rule.Equals("Null"))
                    {
                        rules.Add(null);
                    }
                    else
                    {
                        rules.Add("");
                    }
                }
                foreach (var field in newconfig.Fields)
                {
                    queryAdvance.Add(Query.In(field.Value.Replace("Phases.", ""), new BsonArray(rules)));
                }
                #endregion
                queries = Query.And(queryBasic);
                //queries = Query.And(queryAdvance);

                //    var pi = WellActivityPhaseInfo.Get<WellActivityPhaseInfo>(queries);

                try
                {
                    Spotfire sp = new Spotfire();
                    sp.SequenceId = wa.UARigSequenceId;
                    sp.Region = wa.Region;
                    sp.OperatingUnit = wa.OperatingUnit;
                    sp.Asset = wa.AssetName;
                    sp.Project = wa.ProjectName;
                    sp.WellName = wa.WellName;
                    sp.ActivityType = t.ActivityType;
                    sp.FundingType = t.FundingType;
                    sp.RigName = wa.RigName;
                    sp.RigType = wa.RigType;
                    sp.WorkingInterest = wa.WorkingInterest;
                    sp.PlanSchedule.Start = t.PlanSchedule.Start;
                    sp.PlanSchedule.Finish = t.PlanSchedule.Finish;

                    //sp.PlanYear = y.Year;

                    sp.ScheduleID = wa.WellName + " - " + t.ActivityType;




                    spots.Add(sp);
                }
                catch (Exception ex)
                {

                }


            }
            return spots;
        }

        public JsonResult GetWellSequenceInfo2(WaterfallBase wb)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                string previousOP = "";
                string nextOP = "";
                var DefaultOP = getBaseOP(out previousOP, out nextOP);

                var GetMasterOPs = MasterOP.Populate<MasterOP>();


                var wa = wb.GetMetadataChecker(false);
                var raw = new List<WellActivity>();
                if (wa.Activities.Any()) raw.AddRange(wa.Activities);


                List<Spotfire> spots = new List<Spotfire>();
                foreach (var t in raw)
                {
                    var sps = GetSpot(t);
                    if (sps.Any())
                        spots.AddRange(sps);
                }

                List<BsonDocument> docs = new List<BsonDocument>();

                foreach (var t in spots)
                {
                    docs.Add(t.ToBsonDocument());
                }

                return DataHelper.ToDictionaryArray(docs);
            });
        }

        public void SetOpListDate(List<OPListHelperForDataBrowserGrid> datas, out List<OPListHelperForDataBrowserGrid> result)
        {
            var get = new List<OPListHelperForDataBrowserGrid>();
            var StartDate = datas.Where(x => x.OPSchedule.Start != Tools.DefaultDate);
            DateTime MinStartDate = Tools.DefaultDate;
            if (StartDate.Any())
            {
                MinStartDate = StartDate.Min(x => x.OPSchedule.Start);
            }
            var FinishDate = datas.Where(x => x.OPSchedule.Finish != Tools.DefaultDate);
            DateTime MinFinishDate = Tools.DefaultDate;
            if (FinishDate.Any())
            {
                MinFinishDate = FinishDate.Min(x => x.OPSchedule.Finish);
            }
            foreach (var helper in datas)
            {
                if (MinStartDate != Tools.DefaultDate)
                {
                    if (helper.OPSchedule.Start == Tools.DefaultDate)
                    {
                        helper.OPSchedule.Start = MinStartDate;
                    }
                }
                if (MinFinishDate != Tools.DefaultDate)
                {
                    if (helper.OPSchedule.Finish == Tools.DefaultDate)
                    {
                        helper.OPSchedule.Finish = MinFinishDate;
                    }
                }
                get.Add(helper);

            }
            result = get;
        }

        public JsonResult getFields()
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var q = DataHelper.Populate("WEISWellActivities", take: 1);
                var uw = BsonHelper.Unwind(q.ToList(), "Phases", "", new List<string> { "RigType", "RigName", "Region", "WellName", "UARigSequenceId" });
                var fields = new List<string>();
                var fie = new List<BsonDocument>();
                int index = 0;
                foreach (var r in uw.FirstOrDefault())
                {
                    //fields.Add(r.Name);
                    var fi = new BsonDocument();

                    if (index > 5)
                    {
                        fi.Set("Key", r.Name);
                        fi.Set("Value", "Phases." + r.Name);
                    }
                    else
                    {
                        fi.Set("Key", r.Name);
                        fi.Set("Value", r.Name);
                    }
                    fie.Add(fi);
                    index++;
                }
                return DataHelper.ToDictionaryArray(fie); ;
            });

        }

        public JsonResult MetadataSave(List<FieldsData> fields, List<string> rules)
        {
            return MvcResultInfo.Execute(null, (BsonDocument doc) =>
            {
                MetadataConfig mConfig = new MetadataConfig();
                mConfig.Fields = fields;
                mConfig.Rules = rules;

                Config cfg = new Config();
                cfg._id = "MetaDataConfig";
                cfg.ConfigModule = "MetaDataChecker";
                cfg.ConfigValue = mConfig.ToBsonDocument();
                cfg.Save();
                return "OK";
            });
        }

        public JsonResult getConfig()
        {
            return MvcResultInfo.Execute(() =>
            {
                var config = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "MetaDataConfig"));
                if (config == null || config.Count() == 0)
                {
                    var rules = new List<string>();
                    rules.Add("Null");
                    MetadataConfig mConfig = new MetadataConfig();

                    mConfig.Fields = new List<FieldsData>();
                    mConfig.Fields.Add(new FieldsData
                    {
                        Key = "FundingType",
                        Value = "Phases.FundingType"
                    });
                    mConfig.Fields.Add(new FieldsData
                    {
                        Key = "WorkingInterest",
                        Value = "WorkingInterest"
                    });
                    mConfig.Rules = rules;

                    Config cfg = new Config();
                    cfg._id = "MetaDataConfig";
                    cfg.ConfigModule = "MetaDataChecker";
                    cfg.ConfigValue = mConfig.ToBsonDocument();
                    cfg.Save();
                }

                var config2 = DataHelper.Populate("SharedConfigTable", Query.EQ("_id", "MetaDataConfig"));
                var newconfig = BsonHelper.Deserialize<MetadataConfig>(config2.FirstOrDefault().ToBsonDocument().Get("ConfigValue").ToBsonDocument());
                return new
                {
                    fields = newconfig.Fields,
                    rules = newconfig.Rules
                };
            });
        }

        public JsonResult UpdateChanges(List<Spotfire> update)
        {
            return MvcResultInfo.Execute(() =>
            {
                foreach (var r in update)
                {
                    var s = WellActivity.Populate<WellActivity>(
                    Query.And(
                        Query.EQ("WellName", r.WellName),
                        Query.EQ("RigName", r.RigName),
                        Query.EQ("Phases.ActivityType", r.ActivityType)
                    ));

                    if (s.Any())
                    {

                        var act = s.FirstOrDefault().Phases.Where(x => x.ActivityType.Equals(r.ActivityType));
                        if (act.Any())
                        {
                            var single = act.FirstOrDefault();
                            s.FirstOrDefault().WorkingInterest = r.WorkingInterest;
                            single.FundingType = r.FundingType.ToUpper();
                            DataHelper.Save("WEISWellActivities", s.FirstOrDefault().ToBsonDocument());

                        }
                    }
                }
                return "OK";
            });
        }

    }

}

