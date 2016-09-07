using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ECIS.Biz.Common;

namespace ECIS.Client.WEIS
{
    //public enum AliasId
    //{
    //    OperatingUnit,
    //    RigType,
    //    ActivitiesType,
    //    Projects,
    //    RigName
    //}

    public class MasterAlias : ECIS.Core.ECISModel
    {
        public override string TableName
        {
            get { return "WEISMasterAliases"; }
        }
        public string MasterId { get; set; }
        public string AliasId { set; get; }
        public List<string> Aliases { get; set; }

        public override MongoDB.Bson.BsonDocument PreSave(MongoDB.Bson.BsonDocument doc, string[] references = null)
        {
            this._id = String.Format("{0}-{1}", this.MasterId.ToString(), this.AliasId);
            return this.ToBsonDocument();
        }

        public MasterAlias()
        {
            Aliases = new List<string>();
        }

        public IMongoQuery GetQuery()
        {
            if (this.Aliases != null)
            {
                List<IMongoQuery> queries = new List<IMongoQuery>();
                BsonArray qactivity = new BsonArray();
                foreach (var activity in this.Aliases) qactivity.Add(activity);
                var q = Query.In("ActivityType", qactivity);
                return q;
            }
            else
                return null;
        }

        public static void LoadDRLTOPHOLE()
        {
            List<MasterAlias> result = new List<MasterAlias>();
            // WellName
            result.Add(new MasterAlias
            {
                MasterId = "ActivityType",
                AliasId = "DRL - TOP HOLE ONLY",
                Aliases = new List<string> { "DRL - TOP HOLE ONLY", "DRL – TOP HOLE ONLY" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "Activity_Type",
                AliasId = "DRL - TOP HOLE ONLY",
                Aliases = new List<string> { "DRL - TOP HOLE ONLY", "DRL – TOP HOLE ONLY" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "ActivityType",
                AliasId = "DRL - INTERM.",
                Aliases = new List<string> { "DRL - INTERM. ", "DRL - INTERM." }
            });

            result.Add(new MasterAlias
            {
                MasterId = "Activity_Type",
                AliasId = "DRL - INTERM.",
                Aliases = new List<string> { "DRL - INTERM. ", "DRL - INTERM." }
            });


            foreach (var r in result)
            {
                r.Save();
            }
        }


        public static void LoadDefaultAlias()
        {
            List<MasterAlias> result = new List<MasterAlias>();
            // WellName
            result.Add(new MasterAlias
            {
                MasterId = "WellName",
                AliasId = "CHESHIRE L-97",
                Aliases = new List<string> { "CHESHIRE"}
            });

            result.Add(new MasterAlias
            {
                MasterId = "Well_Name",
                AliasId = "CHESHIRE L-97",
                Aliases = new List<string> { "CHESHIRE" }
            });

            // CHESHIRE
            result.Add(new MasterAlias
            {
                MasterId = "WellName",
                AliasId = "OLYMPUS PLATFORM SHUTDOWN 2018",
                Aliases = new List<string> { "OLYMPUS PLATFORM SHUTDOWN 2018 " }
            });

            result.Add(new MasterAlias
            {
                MasterId = "Well_Name",
                AliasId = "OLYMPUS PLATFORM SHUTDOWN 2018",
                Aliases = new List<string> { "OLYMPUS PLATFORM SHUTDOWN 2018 " }
            });

            result.Add(new MasterAlias
            {
                MasterId = "WellName",
                AliasId = "URSA UI4 FALLBACK",
                Aliases = new List<string> { "URSA UI4 Fallback" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "Well_Name",
                AliasId = "URSA UI4 FALLBACK",
                Aliases = new List<string> { "URSA UI4 Fallback" }
            });

            #region OP15
            // Perormance Unit
            result.Add(new MasterAlias
            {
                MasterId = "Performance_Unit",
                AliasId = "UIX",
                Aliases = new List<string> { "UIX", "UI-X" }
            });


            // operationg unit
            result.Add(new MasterAlias
            {
                MasterId = "Operating_Unit",
                AliasId = "GABON",
                Aliases = new List<string> { "Gabon", "GABON" }
            });

            // rigtype
            result.Add(new MasterAlias
            {
                MasterId = "Rig_Type",
                AliasId = "MODU",
                Aliases = new List<string> { "modu", "MODU", "Modu" }
            });

            // rigname
            result.Add(new MasterAlias
            {
                MasterId = "Rig_Name",
                AliasId = "Ursa",
                Aliases = new List<string> { "Ursa", "Ursa " }
            });

            result.Add(new MasterAlias
            {
                MasterId = "Rig_Name",
                AliasId = "Noble Globetrotter 1",
                Aliases = new List<string> { "Noble Globetrotter 1", "Noble Globetrotter 1\r\n" }
            });

            // project
            result.Add(new MasterAlias
            {
                MasterId = "Project_Name",
                AliasId = "Bonga PH 2",
                Aliases = new List<string> { "Bonga PH 2", "Bonga Ph 2" }
            });
            result.Add(new MasterAlias
            {
                MasterId = "Project_Name",
                AliasId = "Bonga PH 2B",
                Aliases = new List<string> { "Bonga PH 2B", "Bonga Ph 2b" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "Project_Name",
                AliasId = "Bonga PH 3A",
                Aliases = new List<string> { "Bonga Ph 3a", "Bonga PH 3A", "Bonga Ph 3A" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "Project_Name",
                AliasId = "DEIMOS",
                Aliases = new List<string> { "DEIMOS", "Deimos" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "Project_Name",
                AliasId = "Gabon",
                Aliases = new List<string> { "Gabon", "GABON" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "Project_Name",
                AliasId = "GOM EXPL NOV",
                Aliases = new List<string> { "GOM EXPL NOV", "GOM Expl NOV" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "Project_Name",
                AliasId = "GOM EXPLORATION",
                Aliases = new List<string> { "GOM EXPLORATION", "GOM Exploration" }
            });

            // Activity

            result.Add(new MasterAlias
            {
                MasterId = "Activity_Type",
                AliasId = "DRL - INTERM.",
                Aliases = new List<string> { "DRL - INTERM.", "DRL - INTERM. " }
            });

            result.Add(new MasterAlias
            {
                MasterId = "Activity_Type",
                AliasId = "DRL - INTERM. PROD ONLY",
                Aliases = new List<string> { "DRL - INTERM. PROD ONLY", "DRL - INTERM. AND PRO  D ONLY", "DRL - INTERM.  PROD ONLY" }
            });


            result.Add(new MasterAlias
            {
                MasterId = "Activity_Type",
                AliasId = "RIG MAINTENANCE",
                Aliases = new List<string> { "RIG MAINTENANCE", "RIG - MAINTENANCE", "RIG - MAINTENAINCE" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "Activity_Type",
                AliasId = "RIG DELAY",
                Aliases = new List<string> { "RIG DELAY", "RIG - DELAY" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "Activity_Type",
                AliasId = "RIG RAMP UP/DOWN",
                Aliases = new List<string> { "RIG – RAMP UP/DOWN", "RIG RAMP UP/DOWN" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "Activity_Type",
                AliasId = "RIG FARM OUT",
                Aliases = new List<string> { "RIG FARM OUT", "RIG – FARM OUT" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "Activity_Type",
                AliasId = "RIG PREPARATION AND INSPECTION",
                Aliases = new List<string> { "RIG PREPARATION AND INSPECTION", "RIG - PREPARATION AND INSPECTION" }
            });
            #endregion

            // Perormance Unit
            result.Add(new MasterAlias
            {
                MasterId = "PerformanceUnit",
                AliasId = "UIX",
                Aliases = new List<string> { "UIX", "UI-X" }
            });

            // Perormance Unit
            result.Add(new MasterAlias
            {
                MasterId = "PerformanceUnit",
                AliasId = "Mars/Ursa",
                Aliases = new List<string> { "Mars / Ursa", "Mars/Ursa", "MARS/URSA" }
            });

            // operationg unit
            result.Add(new MasterAlias
            {
                MasterId = "OperatingUnit",
                AliasId = "GABON",
                Aliases = new List<string> { "Gabon", "GABON" }
            });

            // rigtype
            result.Add(new MasterAlias
            {
                MasterId = "RigType",
                AliasId = "MODU",
                Aliases = new List<string> { "modu", "MODU", "Modu" }
            });

            // rigname
            result.Add(new MasterAlias
            {
                MasterId = "RigName",
                AliasId = "Ursa",
                Aliases = new List<string> { "Ursa", "Ursa " }
            });

            result.Add(new MasterAlias
            {
                MasterId = "RigName",
                AliasId = "Noble Globetrotter 1",
                Aliases = new List<string> { "Noble Globetrotter 1", "Noble Globetrotter 1\r\n" }
            });

            // project
            result.Add(new MasterAlias
           {
               MasterId = "ProjectName",
               AliasId = "Bonga PH 2",
               Aliases = new List<string> { "Bonga PH 2", "Bonga Ph 2" }
           });
            result.Add(new MasterAlias
            {
                MasterId = "ProjectName",
                AliasId = "Bonga PH 2B",
                Aliases = new List<string> { "Bonga PH 2B", "Bonga Ph 2b" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "ProjectName",
                AliasId = "Bonga PH 3A",
                Aliases = new List<string> { "Bonga Ph 3a", "Bonga PH 3A", "Bonga Ph 3A" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "ProjectName",
                AliasId = "DEIMOS",
                Aliases = new List<string> { "DEIMOS", "Deimos" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "ProjectName",
                AliasId = "Gabon",
                Aliases = new List<string> { "Gabon", "GABON" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "ProjectName",
                AliasId = "GOM EXPL NOV",
                Aliases = new List<string> { "GOM EXPL NOV", "GOM Expl NOV" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "ProjectName",
                AliasId = "GOM EXPLORATION",
                Aliases = new List<string> { "GOM EXPLORATION", "GOM Exploration" }
            });

            // Activity

            result.Add(new MasterAlias
            {
                MasterId = "ActivityType",
                AliasId = "DRL - INTERM.",
                Aliases = new List<string> { "DRL - INTERM.", "DRL - INTERM. " }
            });

            result.Add(new MasterAlias
            {
                MasterId = "ActivityType",
                AliasId = "DRL - INTERM. PROD ONLY",
                Aliases = new List<string> { "DRL - INTERM. PROD ONLY", "DRL - INTERM. AND PROD ONLY", "DRL - INTERM.  PROD ONLY", "DRL - INTERM. & PROD ONLY" }
            });


            result.Add(new MasterAlias
            {
                MasterId = "ActivityType",
                AliasId = "RIG MAINTENANCE",
                Aliases = new List<string> { "RIG MAINTENANCE", "RIG - MAINTENANCE", "RIG - MAINTENAINCE" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "ActivityType",
                AliasId = "RIG DELAY",
                Aliases = new List<string> { "RIG DELAY", "RIG - DELAY" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "ActivityType",
                AliasId = "RIG RAMP UP/DOWN",
                Aliases = new List<string> { "RIG – RAMP UP/DOWN", "RIG RAMP UP/DOWN" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "ActivityType",
                AliasId = "RIG FARM OUT",
                Aliases = new List<string> { "RIG FARM OUT", "RIG – FARM OUT" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "ActivityType",
                AliasId = "RIG PREPARATION AND INSPECTION",
                Aliases = new List<string> { "RIG PREPARATION AND INSPECTION", "RIG - PREPARATION AND INSPECTION" }
            });

            //Funding Type
            result.Add(new MasterAlias {
                MasterId = "FundingType",
                AliasId  = "OPEX",
                Aliases = new List<string> { "Opex", "Opex - WRFM", "Opex - Idle Rig", "Opex - FMS" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "FundingType",
                AliasId = "ABEX",
                Aliases = new List<string> { "Abex" }
            });

            result.Add(new MasterAlias
            {
                MasterId = "FundingType",
                AliasId = "EXPEX",
                Aliases = new List<string> { "Expex"}
            });

            result.Add(new MasterAlias
            {
                MasterId = "FundingType",
                AliasId = "CAPEX",
                Aliases = new List<string> { "Capex", "Development Capex" }
            });
            //Asset name
            result.Add(new MasterAlias{
                MasterId = "AssetName",
                AliasId = "Bonga Ph 2",
                Aliases = new List<string> { "Bonga PH 2" }
            });
            result.Add(new MasterAlias
            {
                MasterId = "AssetName",
                AliasId = "Bonga Ph 3a",
                Aliases = new List<string> { "Bonga PH 3A" }
            });
            result.Add(new MasterAlias
            {
                MasterId = "AssetName",
                AliasId = "Bonga Ph 2",
                Aliases = new List<string> { "Bonga PH 2" }
            });
            result.Add(new MasterAlias
            {
                MasterId = "AssetName",
                AliasId = "Bonga Ph 2b",
                Aliases = new List<string> { "Bonga PH 2B" }
            });
            foreach (var r in result)
            {
                r.Save();
            }
        }


        public static void MasterCleansing(string collections, string fieldToCleansing, bool fieldArray = false, string headerArray = "")
        {
            var aliases = MasterAlias.Populate<MasterAlias>();
            var datas = DataHelper.Populate(collections);
            foreach (var d in datas)
            {
                bool isSaveData = false;
                if (fieldArray)
                {
                    if (d.GetElement(headerArray).Value.AsBsonArray.Any())
                    {
                        var phs = d.GetElement(headerArray).Value.AsBsonArray;
                        foreach (var t in phs)
                        {
                            var aliasData = aliases.Where(x => x.MasterId.Equals(fieldToCleansing));
                            var dataToReplace = t.AsBsonDocument.Get(fieldToCleansing).ToString();

                            bool isReplace = false;
                            string replaceWith = "";
                            foreach (var alias in aliasData)
                            {
                                if (alias.Aliases.Contains(dataToReplace))
                                {
                                    isReplace = true;
                                    replaceWith = alias.AliasId;
                                    isSaveData = true;
                                }
                            }
                            if (isReplace)
                            {
                                t.AsBsonDocument.Set(fieldToCleansing, replaceWith);
                            }
                        }
                    }
                    // dalam Phase
                    if (isSaveData)
                        DataHelper.Save(collections, d.ToBsonDocument());
                }
                else
                {
                    string actvType = "";
                    if (fieldToCleansing.Contains("."))
                    {
                        var t = fieldToCleansing.Split('.');
                        if (d.GetElement(t[0]) != null)
                        {
                            var y = d.GetElement(t[0]);
                            if (y.Value.ToBsonDocument().GetElement(t[1]) != null)
                            {
                                var phs = y.Value.ToBsonDocument().GetElement(t[1]);
                                actvType  = phs.ToString();
                                var aliasData = aliases.Where(x => x.MasterId.Equals(phs.Name.ToString()));
                                var dataToReplace = phs.Value;

                                bool isReplace = false;
                                string replaceWith = "";
                                foreach (var alias in aliasData)
                                {
                                    if (alias.Aliases.Contains(dataToReplace.ToString()))
                                    {
                                        isReplace = true;
                                        replaceWith = alias.AliasId;
                                        isSaveData = true;
                                    }
                                }
                                if (isReplace)
                                {
                                   var eel = d.Get(y.Name).ToBsonDocument().Set(phs.Name, replaceWith);
                                }
                            }
                        }

                        if (isSaveData)
                            DataHelper.Save(collections, d.ToBsonDocument());
                    }
                    else
                    {
                        // single data 
                        if (!d.Get(fieldToCleansing).ToString().Equals("BsonNull") )
                        {
                            var phs = d.GetElement(fieldToCleansing);

                            var aliasData = aliases.Where(x => x.MasterId.Equals(fieldToCleansing));
                            var dataToReplace = phs.Value;

                            bool isReplace = false;
                            string replaceWith = "";
                            foreach (var alias in aliasData)
                            {
                                if (alias.Aliases.Contains(dataToReplace.ToString()))
                                {
                                    isReplace = true;
                                    replaceWith = alias.AliasId;
                                    isSaveData = true;
                                }
                            }
                            if (isReplace)
                            {
                                d.Set(fieldToCleansing, replaceWith);
                            }
                        }
                        if (isSaveData)
                            DataHelper.Save(collections, d.ToBsonDocument());
                    }

                   
                }
            }
        }
    }
}
