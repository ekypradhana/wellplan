using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.RegularExpressions;

namespace ECIS.Client.WEIS
{
    [BsonIgnoreExtraElements]
    public class WellPIP : ECISModel
    {
        public WellPIP()
        {
            CRElements = new List<PIPElement>();
        }
        public static void UpdateFromWellOps(List<WellActivity> WellActivities)
        {
            foreach (var wa in WellActivities)
            {
                foreach (var a in wa.Phases)
                {
                    var pip = WellPIP.GetByOpsActivity(wa.UARigSequenceId, a.PhaseNo);
                    if (pip == null)
                    {
                        pip = new WellPIP();
                        pip.SequenceId = wa.UARigSequenceId;
                        pip.ActivityType = a.ActivityType;
                        pip.WellName = wa.WellName;
                        pip.Version = 1;
                        pip.Status = "Draft";
                        pip.Save();
                    }
                }
            }
        }
        public BsonDocument UpdateMonthlyFromPIP(WellPIP wpip)
        {
            var wau = new WellActivityUpdateMonthly();
            wau.WellName = wpip.WellName;
            wau.SequenceId = wpip.SequenceId;
            wau.Phase.ActivityType = this.ActivityType;
            var resp = "";
            var pElement = wpip.Elements;
            var pCRElement = wpip.CRElements;
            var fiscal = WEISFinancialCalendar.Get<WEISFinancialCalendar>(Query.EQ("Status", "Active"));
            try
            {
                #region element
                if (pElement != null)
                {
                    if (pElement.Any())
                    {

                        foreach (var el in pElement)
                        {
                            var pipelement = new PIPElement
                            {
                                ElementId = el.ElementId,
                                Title = el.Title,
                                DaysPlanImprovement = el.DaysPlanImprovement,
                                CostPlanImprovement = el.CostPlanImprovement,
                                DaysPlanRisk = el.CostPlanRisk,
                                CostPlanRisk = el.CostPlanRisk,
                                Period = new DateRange(el.Period.Start, el.Period.Start),
                                Classification = el.Classification,
                                ActionParty = el.ActionParty,
                                ActionParties = el.ActionParties,
                                PerformanceUnit = el.PerformanceUnit,
                                Completion = el.Completion,
                                Theme = el.Theme,
                                isNewElement = el.isNewElement,
                                isPositive = el.isPositive,
                                AssignTOOps = el.AssignTOOps
                            };
                            wau.Elements.Add(pipelement);
                        }
                    }
                }
                #endregion
                #region cr_element
                if (pCRElement != null)
                {
                    if (pCRElement.Any())
                    {
                        foreach (var cel in pCRElement)
                        {

                            var crelement = new PIPElement
                            {
                                ElementId = cel.ElementId,
                                Title = cel.Title,
                                DaysPlanImprovement = cel.DaysPlanImprovement,
                                CostPlanImprovement = cel.CostPlanImprovement,
                                DaysPlanRisk = cel.CostPlanRisk,
                                CostPlanRisk = cel.CostPlanRisk,
                                Period = new DateRange(cel.Period.Start, cel.Period.Start),
                                Classification = cel.Classification,
                                ActionParty = cel.ActionParty,
                                ActionParties = cel.ActionParties,
                                PerformanceUnit = cel.PerformanceUnit,
                                Completion = cel.Completion,
                                Theme = cel.Theme,
                                isNewElement = cel.isNewElement,
                                isPositive = cel.isPositive,
                                AssignTOOps = cel.AssignTOOps
                            };
                            wau.Elements.Add(crelement);
                        }
                    }
                }
                #endregion

                var preSv = wau.PreSave(wau.ToBsonDocument(), references: new string[] { "SyncPIP" });
                preSv.Set("UpdateVersion", Tools.ToUTC(fiscal.MonthYear));
                DataHelper.Save(new WellActivityUpdateMonthly().TableName, preSv.ToBsonDocument());
                return preSv;
            }
            catch (Exception)
            {
                return null;
            }


        }
        public override BsonDocument PostSave(BsonDocument doc, string[] references = null)
        {
            var ignoreWAU = references != null && references.Count() != 0 && references[0].ToLower().Equals("ignorewau");

            if (!ignoreWAU)
            {
                var wau = WellActivityUpdate.GetById(WellName, SequenceId, PhaseNo, null, true, false);
                if (wau != null)
                {
                    wau.Calc();
                    wau.Save(references: new string[] { "IgnoreWellPIP" });
                }
                else
                {
                    var ignoreWAUM = references != null && references.Count() != 0 && references[0].ToLower().Equals("ignorewaum");
                    if (!ignoreWAUM)
                    {
                        var waum = WellActivityUpdateMonthly.GetById(WellName, SequenceId, PhaseNo, null, true, false);
                        if (waum != null)
                        {
                            waum.Calc();
                            waum.Save(references: new string[] { "IgnoreWellPIP" });
                        }
                    }
                }

            }


            #region 18 sept 2015 - CRElements
            var ignoreResetCRElement = references != null && references.Count() >= 2 && references[1].ToLower().Equals("ignoreresetcrelement");
            if (!ignoreResetCRElement)
            {
                ApplyCRElements(this);
            }
            #endregion


            return this.ToBsonDocument();
        }
        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            //var isUpdate = references != null && references[0].ToLower().Equals("isupdate");
            doc = base.PreSave(doc);
            this._id = String.Format("W{0}S{1}P{2}", WellName, SequenceId, PhaseNo);
            var wau = WellActivityUpdate.GetById(WellName, SequenceId, PhaseNo, null, true);
            var enableVersioning = false;
            if (Status.Equals("Publish") && Elements.Count == 0) throw new Exception("Plan without any PIP element couldn't be published");
            if (Status.Equals("Draft") && enableVersioning)
            {
                var existingPIP = WellPIP.Get<WellPIP>(_id);
                if (existingPIP.Status.Equals("Publish")) Status = Status + 1;
                doc = this.ToBsonDocument();
            }
            foreach (var e in Elements)
            {
                if (e.ElementId == 0)
                {
                    e.ElementId = Elements.Max(e1 => e1.ElementId) + 1;
                }
                bool resetAlloc = false;
                if (e.Allocations.Count > 0)
                {
                    var dtPeriodMin = e.Allocations.Min(d => d.Period);
                    var dtPeriodMax = e.Allocations.Max(d => d.Period);
                    resetAlloc = !(dtPeriodMin.Year.Equals(e.Period.Start.Year) && dtPeriodMin.Month.Equals(e.Period.Start.Month)
                        && dtPeriodMax.Year.Equals(e.Period.Finish.Year) && dtPeriodMax.Month.Equals(e.Period.Finish.Month));
                }
                else
                {
                    resetAlloc = true;
                }

                //if (!isUpdate)
                //{
                //    if (e.isNewElement && !e.isUpdate)//&& (references != null && !references.ToList().Contains("ignoresetlewithop")))
                //    {
                //        // reset zero value to follow OP

                //        if (e.DaysCurrentWeekImprovement == 0) e.DaysCurrentWeekImprovement = e.DaysPlanImprovement;
                //        if (e.DaysCurrentWeekRisk == 0) e.DaysCurrentWeekRisk = e.DaysPlanRisk;
                //        if (e.CostCurrentWeekImprovement == 0) e.CostCurrentWeekImprovement = e.CostPlanImprovement;
                //        if (e.CostCurrentWeekRisk == 0) e.CostCurrentWeekRisk = e.CostPlanRisk;
                //    }
                //}
                //if (e.isUpdate)
                //    e.isUpdate = false; // reset helper isUpdat eattribute

                if (resetAlloc)
                    e.ResetAllocation(true, wau);
            }


            #region 17 sept 2015 - add logic PIP Type

            if (Elements.Count == 0)
                this.Type = "Efficient";
            else
                this.Type = isElementsContainsDaysDuration(Elements);

            #endregion




            doc = this.ToBsonDocument();
            return doc;
        }

        public static void ApplyCRElements(WellPIP pip)
        {
            string RigName = "";
            if (pip.WellName.Length > 3)
            {
                int length = pip.WellName.Length;
                string cr = pip.WellName.Substring((length) - 3, 3);
                if (cr.Equals("_CR"))
                {
                    RigName = pip.WellName.Replace("_CR", "");
                }
                else
                {
                    var q = Query.And(Query.EQ("UARigSequenceId", pip.SequenceId), Query.EQ("WellName", pip.WellName), Query.EQ("Phases.ActivityType", pip.ActivityType));
                    var rgnam = DataHelper.Get<WellActivity>("WEISWellActivities", q);
                    if (rgnam != null)
                        RigName = rgnam.RigName;
                }
            }
            else
            {
                var q = Query.And(Query.EQ("UARigSequenceId", pip.SequenceId), Query.EQ("WellName", pip.WellName), Query.EQ("Phases.ActivityType", pip.ActivityType));
                var rgnam = DataHelper.Get<WellActivity>("WEISWellActivities", q);
                if (rgnam != null)
                    RigName = rgnam.RigName;
            }

            if (RigName != "")
            {
                ResetCRElementsAll(RigName, pip);
            }
        }

        public static void ResetCRElementsAll(string RigName, WellPIP BasePIP)
        {
            string WellName_CR = RigName + "_CR";
            var PIP_CR = WellPIP.Get<WellPIP>(Query.EQ("WellName", WellName_CR));

            //PIP based on RIG is exist
            var usedThisRigs = WellActivity.Populate<WellActivity>(Query.EQ("RigName", RigName));
            List<string> WellNames = usedThisRigs.Select(x => x.WellName).Distinct().ToList();
            List<string> ActivityTypes = usedThisRigs.SelectMany(d => d.Phases, (d, e) => e.ActivityType).Distinct().ToList();
            List<string> SequenceIds = usedThisRigs.Select(x => x.UARigSequenceId).Distinct().ToList();

            var qs = new List<IMongoQuery>();
            qs.Add(Query.In("WellName", new BsonArray(WellNames.ToArray())));
            qs.Add(Query.In("ActivityType", new BsonArray(ActivityTypes.ToArray())));
            qs.Add(Query.In("SequenceId", new BsonArray(SequenceIds.ToArray())));
            var havePIPs = WellPIP.Populate<WellPIP>(Query.And(qs));

            if (PIP_CR != null && PIP_CR.Elements.Count > 0)
            {

                if (PIP_CR.Type.ToLower() == "reduction")
                {
                    if (havePIPs != null && havePIPs.Count() > 0)
                    {
                        //allocation prorate
                        var totalElements = havePIPs.Count;
                        var DayDiff = 0.0;
                        DateRange DayToCalc = new DateRange();
                        foreach (var hpip in havePIPs)
                        {
                            var newCRElements = new List<PIPElement>();

                            // Get Phase / Event Details
                            var q = new List<IMongoQuery>();
                            q.Add(Query.EQ("WellName", hpip.WellName));
                            q.Add(Query.EQ("Phases.ActivityType", hpip.ActivityType));
                            q.Add(Query.EQ("UARigSequenceId", hpip.SequenceId));
                            var gp = WellPIP.Get<WellActivity>(Query.And(q));
                            var OPSchS = gp.Phases.Where(x => x.PhaseNo == hpip.PhaseNo);
                            DateRange OPSch = new DateRange();

                            if (OPSchS != null && OPSchS.Any())
                            {
                                OPSch = OPSchS.FirstOrDefault().PhSchedule;
                            }

                            // ## LOGIC 1 : Find the eventStartDate in Weekly Report ##
                            var WAULatest = WellActivityUpdate.GetById(hpip.WellName, hpip.SequenceId, hpip.PhaseNo, null, true, false);

                            // If event has started (WAU and EventStartDate found)
                            if (WAULatest != null && WAULatest.EventStartDate != null)
                            {
                                DayDiff = WAULatest.CurrentWeek.Days == 0 ? WAULatest.Plan.Days : WAULatest.CurrentWeek.Days;
                                DayToCalc = new DateRange(WAULatest.EventStartDate, WAULatest.EventStartDate.AddDays(DayDiff));
                            }
                            else
                            {
                                var getLEDaysS = gp.Phases.Where(x => x.PhaseNo == hpip.PhaseNo);
                                double getLEDays = 0;
                                if (getLEDaysS != null && getLEDaysS.Any())
                                {
                                    getLEDays = getLEDaysS.FirstOrDefault().LESchedule.Days;
                                }

                                if (getLEDays != 0)
                                {
                                    DayDiff = getLEDays;
                                }
                                else
                                {
                                    var daysPhasesS = gp.Phases.Where(x => x.PhaseNo == hpip.PhaseNo);
                                    double daysPhases = 0;
                                    if (daysPhasesS != null && daysPhasesS.Any())
                                    {
                                        daysPhases = daysPhasesS.FirstOrDefault().PhSchedule.Days;
                                    }

                                    DayDiff = daysPhases;
                                }

                                DayToCalc = OPSch;
                            }

                            foreach (var elementCR in PIP_CR.Elements)
                            {
                                var CRE = new PIPElement();

                                // Check is it overlapping or not?
                                string msg = "";
                                double overlappingDays = 0.0;
                                bool isOverlapping = DateRangeToMonth.isDateRangeOverlaping(elementCR.Period, DayToCalc, out msg, out overlappingDays);
                                if (isOverlapping)
                                {
                                    //CRE.CostPlanImprovement = DayDiff * Tools.Div(elementCR.CostPlanImprovement, overlappingDays);
                                    //CRE.CostPlanRisk = DayDiff * Tools.Div(elementCR.CostPlanRisk, overlappingDays);
                                    //CRE.CostCurrentWeekImprovement = DayDiff * Tools.Div(elementCR.CostCurrentWeekImprovement, overlappingDays);
                                    //CRE.CostCurrentWeekRisk = DayDiff * Tools.Div(elementCR.CostCurrentWeekRisk, overlappingDays);

                                    var RigELementPeriod = elementCR.Period.Days + 1;
                                    CRE.CostPlanImprovement = (overlappingDays + 1) * Tools.Div(elementCR.CostPlanImprovement, RigELementPeriod);
                                    CRE.CostPlanRisk = (overlappingDays + 1) * Tools.Div(elementCR.CostPlanRisk, RigELementPeriod);
                                    CRE.CostCurrentWeekImprovement = (overlappingDays + 1) * Tools.Div(elementCR.CostCurrentWeekImprovement, RigELementPeriod);
                                    CRE.CostCurrentWeekRisk = (overlappingDays + 1) * Tools.Div(elementCR.CostCurrentWeekRisk, RigELementPeriod);
                                }
                                else
                                {
                                    CRE.CostPlanImprovement = 0.0;
                                    CRE.CostPlanRisk = 0.0;
                                    CRE.CostCurrentWeekImprovement = 0.0;
                                    CRE.CostCurrentWeekRisk = 0.0;
                                }
                                CRE.Title = elementCR.Title;
                                CRE.ElementId = elementCR.ElementId;
                                CRE.Realized = elementCR.Realized;
                                CRE.DaysPlanImprovement = elementCR.DaysPlanImprovement;
                                CRE.DaysPlanRisk = elementCR.DaysPlanRisk;
                                CRE.DaysActualImprovement = elementCR.DaysActualImprovement;
                                CRE.DaysActualRisk = elementCR.DaysActualRisk;
                                CRE.DaysLastWeekImprovement = elementCR.DaysLastWeekImprovement;
                                CRE.DaysLastWeekRisk = elementCR.DaysLastWeekRisk;
                                CRE.DaysCurrentWeekImprovement = elementCR.DaysCurrentWeekImprovement;
                                CRE.DaysCurrentWeekRisk = elementCR.DaysCurrentWeekRisk;
                                CRE.CostActualImprovement = elementCR.CostActualImprovement;
                                CRE.CostActualRisk = elementCR.CostActualRisk;
                                CRE.CostLastWeekImprovement = elementCR.CostLastWeekImprovement;
                                CRE.CostLastWeekRisk = elementCR.CostLastWeekRisk;
                                CRE.Classification = elementCR.Classification;
                                CRE.Theme = elementCR.Theme;
                                CRE.ActionParty = elementCR.ActionParty;
                                CRE.Completion = elementCR.Completion;
                                CRE.PerformanceUnit = elementCR.PerformanceUnit;
                                CRE.Period = elementCR.Period;
                                CRE.Allocations = elementCR.Allocations;
                                CRE.ActionParties = elementCR.ActionParties;
                                CRE.Comments = elementCR.Comments;
                                CRE._range = elementCR._range;
                                CRE.AssignTOOps = elementCR.AssignTOOps.Any() ? elementCR.AssignTOOps : new List<string>();

                                newCRElements.Add(CRE);


                            }
                            hpip.CRElements = newCRElements;
                            hpip.Save(references: new string[] { "ignorewau", "ignoreResetCRElement", "ignoresetlewithop" });

                            //save to WAUM newest
                            var waum = WellActivityUpdateMonthly.GetById(hpip.WellName, hpip.SequenceId, hpip.PhaseNo, null, true, false);
                            if (waum != null)
                            {
                                //wau.Calc();
                                waum.CRElements = newCRElements;
                                waum.Elements = new List<PIPElement>();
                                waum.Elements = hpip.Elements;
                                waum.Save(references: new string[] { "IgnoreWellPIP" });
                            }

                            //save to WAU newest
                            var wau = WellActivityUpdate.GetById(hpip.WellName, hpip.SequenceId, hpip.PhaseNo, null, true, false);
                            if (wau != null)
                            {
                                //wau.Calc();
                                wau.CRElements = newCRElements;
                                wau.Elements = new List<PIPElement>();
                                wau.Elements = hpip.Elements;
                                wau.Save(references: new string[] { "IgnoreWellPIP" });
                            }

                           

                        }
                    }

                    #region old
                    //foreach (var well in usedThisRigs)
                    //{
                    //    WellNames.Add(well.WellName);
                    //    SequenceIds.Add(well.UARigSequenceId);
                    //    foreach (var ph in well.Phases)
                    //    {
                    //        ActivityTypes.Add(ph.ActivityType);
                    //        // check have wellPIPs documents 
                    //        var seqId = well.UARigSequenceId;
                    //        var actvType = ph.ActivityType;
                    //        var welln = well.WellName;

                    //        var havePIPs = WellPIP.Populate<WellPIP>(Query.And(Query.EQ("WellName", welln), Query.EQ("SequenceId", seqId), Query.EQ("ActivityType", actvType)));


                    //        if (havePIPs != null && havePIPs.Count() > 0)
                    //        {
                    //            var totalLEDaysAllPIPs = havePIPs.Select(x => x.Elements).Sum(x => x.Sum(y => y.DaysCurrentWeekImprovement + y.DaysCurrentWeekRisk));
                    //            //var totalLEDaysAllPIPs = 10;
                    //            if (totalLEDaysAllPIPs > 0)
                    //            {
                    //                foreach (var hpip in havePIPs)
                    //                {
                    //                    var totalLEDays = 0.0;
                    //                    if (hpip.Elements != null && hpip.Elements.Count > 0)
                    //                    {
                    //                        totalLEDays = hpip.Elements.Sum(x => x.DaysCurrentWeekImprovement + x.DaysCurrentWeekRisk);
                    //                    }
                    //                    var newCRElements = new List<PIPElement>();
                    //                    foreach (var elementCR in PIP_CR.Elements)
                    //                    {
                    //                        var CRE = elementCR;
                    //                        CRE.CostPlanImprovement = Tools.Div(totalLEDays, totalLEDaysAllPIPs) * elementCR.CostPlanImprovement;
                    //                        CRE.CostPlanRisk = Tools.Div(totalLEDays, totalLEDaysAllPIPs) * elementCR.CostPlanRisk;
                    //                        CRE.CostCurrentWeekImprovement = Tools.Div(totalLEDays, totalLEDaysAllPIPs) * elementCR.CostCurrentWeekImprovement;
                    //                        CRE.CostCurrentWeekRisk = Tools.Div(totalLEDays, totalLEDaysAllPIPs) * elementCR.CostCurrentWeekRisk;
                    //                        newCRElements.Add(CRE);
                    //                    }
                    //                    hpip.CRElements = newCRElements;
                    //                    hpip.Save(references: new string[] { "ignoreResetCRElement" });
                    //                }
                    //            }
                    //            else
                    //            {
                    //                // allocation prorate
                    //                var totalElements = havePIPs.Count;
                    //                foreach (var hpip in havePIPs)
                    //                {
                    //                    var newCRElements = new List<PIPElement>();
                    //                    foreach (var elementCR in PIP_CR.Elements)
                    //                    {
                    //                        var CRE = elementCR;
                    //                        CRE.CostPlanImprovement = Tools.Div(elementCR.CostPlanImprovement, totalElements);
                    //                        CRE.CostPlanRisk = Tools.Div(elementCR.CostPlanRisk, totalElements);
                    //                        CRE.CostCurrentWeekImprovement = Tools.Div(elementCR.CostCurrentWeekImprovement, totalElements);
                    //                        CRE.CostCurrentWeekRisk = Tools.Div(elementCR.CostCurrentWeekRisk, totalElements);
                    //                        newCRElements.Add(CRE);
                    //                    }
                    //                    hpip.CRElements = newCRElements;
                    //                    hpip.Save(references: new string[] { "ignoreResetCRElement" });
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                    #endregion
                }
                else
                {
                    // pip type = CE, remove all CRElements in respective WellPIP
                    foreach (var hpip in havePIPs)
                    {
                        var newCRElements = new List<PIPElement>();
                        hpip.CRElements = newCRElements;
                        hpip.Save(references: new string[] { "ignorewau", "ignoreResetCRElement", "ignoresetlewithop" });

                        //save to WAU newest
                        var wau = WellActivityUpdate.GetById(hpip.WellName, hpip.SequenceId, hpip.PhaseNo, null, true, false);
                        if (wau != null)
                        {
                            //wau.Calc();
                            wau.CRElements = newCRElements;
                            wau.Elements = new List<PIPElement>();
                            wau.Elements = hpip.Elements;
                            wau.Save(references: new string[] { "IgnoreWellPIP" });
                        }

                        //save to WAUM newest
                        var waum = WellActivityUpdateMonthly.GetById(hpip.WellName, hpip.SequenceId, hpip.PhaseNo, null, true, false);
                        if (waum != null)
                        {
                            //wau.Calc();
                            waum.CRElements = newCRElements;
                            waum.Elements = new List<PIPElement>();
                            waum.Elements = hpip.Elements;
                            waum.Save(references: new string[] { "IgnoreWellPIP" });
                        }

                    }
                }
            }
            else
            {
                // pip_cr == null, remove all CRElements in respective WellPIP
                foreach (var hpip in havePIPs)
                {
                    var newCRElements = new List<PIPElement>();
                    hpip.CRElements = newCRElements;
                    hpip.Save(references: new string[] { "ignorewau", "ignoreResetCRElement" });

                    //save to WAU newest
                    var wau = WellActivityUpdate.GetById(hpip.WellName, hpip.SequenceId, hpip.PhaseNo, null, true, false);
                    if (wau != null)
                    {
                        //wau.Calc();
                        wau.CRElements = newCRElements;
                        wau.Save(references: new string[] { "IgnoreWellPIP" });
                    }

                }
            }
        }
        public string isElementsContainsDaysDuration(List<PIPElement> elements)
        {
            if (elements.Where(x => x.DaysActualImprovement != 0).Count() > 0 ||
                elements.Where(x => x.DaysActualRisk != 0).Count() > 0 ||
                elements.Where(x => x.DaysCurrentWeekImprovement != 0).Count() > 0 ||
                elements.Where(x => x.DaysCurrentWeekRisk != 0).Count() > 0 ||
                elements.Where(x => x.DaysLastWeekImprovement != 0).Count() > 0 ||
                elements.Where(x => x.DaysLastWeekRisk != 0).Count() > 0 ||
                elements.Where(x => x.DaysPlanImprovement != 0).Count() > 0 ||
                elements.Where(x => x.DaysPlanRisk != 0).Count() > 0
                )
            {
                // punya value days
                return "Efficient";
            }
            else if ((elements.Where(x => x.CostActualImprovement != 0).Count() > 0 && elements.Where(x => x.DaysActualImprovement == 0).Count() > 0) ||
                     (elements.Where(x => x.CostActualRisk != 0).Count() > 0 && elements.Where(x => x.DaysActualRisk == 0).Count() > 0) ||
                     (elements.Where(x => x.CostCurrentWeekImprovement != 0).Count() > 0 && elements.Where(x => x.DaysCurrentWeekImprovement == 0).Count() > 0) ||
                     (elements.Where(x => x.CostCurrentWeekRisk != 0).Count() > 0 && elements.Where(x => x.DaysCurrentWeekRisk == 0).Count() > 0) ||
                     (elements.Where(x => x.CostLastWeekImprovement != 0).Count() > 0 && elements.Where(x => x.DaysLastWeekImprovement == 0).Count() > 0) ||
                     (elements.Where(x => x.CostLastWeekRisk != 0).Count() > 0 && elements.Where(x => x.DaysLastWeekRisk == 0).Count() > 0) ||
                     (elements.Where(x => x.CostPlanImprovement != 0).Count() > 0 && elements.Where(x => x.DaysPlanImprovement == 0).Count() > 0) ||
                     (elements.Where(x => x.CostPlanRisk != 0).Count() > 0 && elements.Where(x => x.DaysPlanRisk == 0).Count() > 0)
                     )
            {
                // gak punya days tapi punya cost
                return "Reduction";
            }
            else
            {
                return "Efficient";
            }
        }

        public string RefreshPIPs()
        {
            try
            {
                // backup current WellPIPs Data
                var wellPIPOlds = DataHelper.Populate<WellPIP>("WEISWellPIPs");
                var dtNow = DateTime.Now.ToString("yyyyMMddhhmmss");
                foreach (var t in wellPIPOlds)
                {
                    DataHelper.Save("_WEISWellPIPs_bck_" + dtNow, t.ToBsonDocument());
                }
                // clear current WEISWellPIPs
                DataHelper.Delete("WEISWellPIPs");
                foreach (var t in wellPIPOlds)
                {
                    t.Save();
                }

                return "OK";
            }
            catch (Exception ex)
            {
                return string.Format("{0}\n{1}", ex.Message, ex.InnerException);
            }
        }

        public static WellPIP GetByOpsActivity(string SequenceId, int phaseNo)
        {
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("SequenceId", SequenceId));
            qs.Add(Query.EQ("PhaseNo", phaseNo));
            //qs.Add(Query.EQ("ActivityType", Activity));
            var q = Query.And(qs);
            return WellPIP.Get<WellPIP>(q);
        }
        public static WellPIP GetByOpsActivity(string SequenceId, string activityType)
        {
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("SequenceId", SequenceId));
            //qs.Add(Query.EQ("Ac", activityType));
            qs.Add(Query.EQ("ActivityType", activityType));
            var q = Query.And(qs);
            return WellPIP.Get<WellPIP>(q);
        }
        public static WellPIP GetByOpsActivity(string SequenceId, string activityType, string WellName)
        {
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("SequenceId", SequenceId));
            //qs.Add(Query.EQ("Ac", activityType));
            qs.Add(Query.EQ("ActivityType", activityType));
            qs.Add(Query.EQ("WellName", WellName));
            var q = Query.And(qs);
            return WellPIP.Get<WellPIP>(q);
        }
        public static WellPIP GetByWellActivity(string wellName, string activityType)
        {
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", wellName));
            //qs.Add(Query.EQ("Ac", activityType));
            qs.Add(Query.EQ("ActivityType", activityType));
            var q = Query.And(qs);
            return WellPIP.Get<WellPIP>(q);
        }
        public static WellPIP GetByWellActivity(string wellName, int phaseNo)
        {
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", wellName));
            //qs.Add(Query.EQ("Ac", activityType));
            qs.Add(Query.EQ("PhaseNo", phaseNo));
            var q = Query.And(qs);
            return WellPIP.Get<WellPIP>(q);
        }
        public override string TableName
        {
            get { return "WEISWellPIPs"; }
        }

        public override void PostGet()
        {
            if (this.Elements != null && this.Elements.Count() > 0)
            {
                this.RealizedDays = this.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.DaysCurrentWeekRisk);
                this.RealizedCost = this.Elements.Where(x => x.Completion.Equals("Realized")).Sum(x => x.CostCurrentWeekRisk);
            }
            base.PostGet();
        }
        public string UserName { set; get; }
        public int Version { get; set; }
        public string Status { get; set; }
        public string SequenceId { get; set; }
        public int PhaseNo { get; set; }
        public string ActivityType { get; set; }
        public string WellName { get; set; }

        private List<PIPElement> _element;
        public List<PIPElement> Elements
        {
            get
            {
                if (_element == null) _element = new List<PIPElement>();
                return _element;
            }
            set { _element = value; }
        }


        //private List<PIPElement> _crelement;
        //public List<PIPElement> CRElements
        //{
        //    get
        //    {
        //        if (_crelement == null) _crelement = new List<PIPElement>();
        //        return _crelement;
        //    }
        //    set { _crelement = value; }
        //}

        public List<PIPElement> CRElements { get; set; }

        private List<PIPPerformanceMetrics> _PerformanceMetrics;
        public List<PIPPerformanceMetrics> PerformanceMetrics
        {
            get
            {
                if (_PerformanceMetrics == null) _PerformanceMetrics = new List<PIPPerformanceMetrics>();
                return _PerformanceMetrics;
            }
            set { _PerformanceMetrics = value; }
        }

        private List<PIPProjectMilestones> _ProjectMilestones;
        public List<PIPProjectMilestones> ProjectMilestones
        {
            get
            {
                if (_ProjectMilestones == null) _ProjectMilestones = new List<PIPProjectMilestones>();
                return _ProjectMilestones;
            }
            set { _ProjectMilestones = value; }
        }
        public double RealizedDays { get; set; }
        public double RealizedCost { get; set; }

        public string ProjectType { get; set; }
        public string Field { get; set; }
        public string Scaled { get; set; }
        public int CostLevel { get; set; }
        public string Type { get; set; }

        private string GetRigName()
        {
            try
            {
                if (Type != "Reduction")
                {
                    var q = Query.And(Query.EQ("UARigSequenceId", SequenceId), Query.EQ("WellName", WellName), Query.EQ("Phases.ActivityType", ActivityType));
                    return DataHelper.Get<WellActivity>("WEISWellActivities", q).RigName;
                }
                else
                {
                    return WellName.Replace("_CR", "");
                }
            }
            catch (Exception e)
            {
                return "";
            }
        }

        [BsonIgnore]
        public string RigName
        {
            get { return GetRigName(); }
        }
        [BsonIgnore]
        public string BaseOP { get; set; }
        [BsonIgnore]
        public DateRange OPSchedule { get; set; }
        [BsonIgnore]
        public DateRange AFESchedule { get; set; }

        [BsonIgnore]
        public DateRange LSSchedule { get; set; }


        [BsonIgnore]
        public DateTime LSStart { get; set; }

        [BsonIgnore]
        public DateTime LSFinish { get; set; }


        [BsonIgnore]
        public DateTime OPStart { get; set; }

        [BsonIgnore]
        public DateTime OPFinish { get; set; }

        [BsonIgnore]
        public DateTime AFEStart { get; set; }

        [BsonIgnore]
        public DateTime AFEFinish { get; set; }



    }

    [BsonIgnoreExtraElements]
    public class PIPElement
    {
        [BsonIgnore]
        public Dictionary<int, double> AnnualProportions { get; set; }
        [BsonIgnore]
        public Dictionary<int, double> MonthlyProportions { get; set; }

        public PIPElement()
        {
            this.isNewElement = true;
            this.isPositive = true;
            this.Classification = string.Empty;
            this.Completion = string.Empty;
            this.AssignTOOps = new List<string>();
            this.isUpdate = false;
            //LoEs = new List<PIPElementLoE>();
        }
        //public PIPElement(DateTime? UpdateVersion = null);
        [BsonIgnore]
        public bool isUpdate { get; set; }

        public int ElementId { get; set; }
        public string Title { get; set; }
        public bool Realized { get; set; }

        //public double RealizedDays { get; set; }
        //public double RealizedCost { get; set; }
        public bool isPositive { get; set; }

        public double DaysPlanImprovement { get; set; }
        public double DaysPlanRisk { get; set; }
        public double DaysActualImprovement { get; set; }
        public double DaysActualRisk { get; set; }
        public double DaysLastWeekImprovement { get; set; }
        public double DaysLastWeekRisk { get; set; }
        public double DaysCurrentWeekImprovement { get; set; }
        public double DaysCurrentWeekRisk { get; set; }
        public double CostPlanImprovement { get; set; }
        public double CostPlanRisk { get; set; }
        public double CostActualImprovement { get; set; }
        public double CostActualRisk { get; set; }
        public double CostCurrentWeekImprovement { get; set; }
        public double CostCurrentWeekRisk { get; set; }
        public double CostLastWeekImprovement { get; set; }
        public double CostLastWeekRisk { get; set; }
        public string Classification { get; set; }
        public string Theme { get; set; }
        public bool CostAvoidance { get; set; }
        public double CostAvoidanceValue { get; set; }
        public string ActionParty { get; set; }
        //public double Completion { get; set; }
        private string _completion;
        public object Completion
        {
            get
            {
                return this._completion;
            }
            set
            {
                this._completion = Convert.ToString(value);
            }
        }


        public string PerformanceUnit { get; set; }
        public DateRange _range;
        public DateRange Period
        {
            get
            {
                if (_range == null) _range = new DateRange { Start = Tools.DefaultDate, Finish = Tools.DefaultDate };
                return _range;
            }

            set
            {
                _range = value;
            }
        }


        [BsonIgnore]
        public bool isNewElement { get; set; }


        private List<PIPAllocation> _allocations;
        public List<PIPAllocation> Allocations
        {
            get
            {
                if (_allocations == null) ResetAllocation();
                return _allocations;
            }

            set
            {
                _allocations = value;
            }
        }

        public void ResetAllocation(bool loadFromWAU = false, WellActivityUpdate wau = null,
            bool resetPlan = true, bool resetLE = true, bool forceLEequalPlan = false)
        {
            if (_allocations == null)
            {
                _allocations = new List<PIPAllocation>();
            }
            if (Period.Finish < Period.Start) Period.Finish = Period.Start;
            var dt = new DateTime(Period.Start.Year, Period.Start.Month, 1);
            //int days = Convert.ToInt32(Period.Days) + 1;
            int mthNumber = 0;
            bool exceed = false;
            while (!exceed)
            {
                mthNumber++;
                if (dt.Year == Period.Finish.Year && dt.Month == Period.Finish.Month)
                    exceed = true;
                else
                    dt = dt.AddMonths(1);
            }
            int days = mthNumber;

            if (forceLEequalPlan)
            {
                DaysCurrentWeekImprovement = DaysPlanImprovement;
                DaysCurrentWeekRisk = DaysPlanRisk;
                CostCurrentWeekImprovement = CostPlanImprovement;
                CostCurrentWeekRisk = CostPlanRisk;
            }

            if (loadFromWAU && wau != null)
            {
                var el = wau.Elements.FirstOrDefault(d => d.ElementId.Equals(ElementId));
                if (el != null)
                {
                    DaysCurrentWeekImprovement = el.DaysCurrentWeekImprovement;
                    DaysCurrentWeekRisk = el.DaysCurrentWeekRisk;
                    CostCurrentWeekImprovement = el.CostCurrentWeekImprovement;
                    CostCurrentWeekRisk = el.CostCurrentWeekRisk;
                }
            }

            if (!resetLE)
            {
                var NotZeroLE = Allocations.FirstOrDefault(d => d.LECost != 0 || d.LEDays != 0);
                if (NotZeroLE == null) resetLE = true;
            }

            var newAllocations = new List<PIPAllocation>();
            var ewau = wau == null ? null : wau.Elements.FirstOrDefault(d => d.ElementId.Equals(ElementId));
            var totalAlloc = new PIPAllocation();
            exceed = false;

            LEDays = DaysCurrentWeekImprovement + DaysCurrentWeekRisk;
            LECost = CostCurrentWeekImprovement + CostCurrentWeekRisk;


            PIPElement modElement = new PIPElement();
            modElement.DaysPlanImprovement = Tools.Div(DaysPlanImprovement, days);
            modElement.DaysPlanRisk = Tools.Div(DaysPlanRisk, days);
            modElement.CostPlanImprovement = Tools.Div(CostPlanImprovement, days);
            modElement.CostPlanRisk = Tools.Div(CostPlanRisk, days);
            modElement.DaysCurrentWeekImprovement = Tools.Div(DaysCurrentWeekImprovement, days);
            modElement.DaysCurrentWeekRisk = Tools.Div(DaysCurrentWeekRisk, days);
            modElement.CostCurrentWeekImprovement = Tools.Div(CostCurrentWeekImprovement, days);
            modElement.CostCurrentWeekRisk = Tools.Div(CostCurrentWeekRisk, days);
            modElement.LEDays = Tools.Div(LEDays, days);
            modElement.LECost = Tools.Div(LECost, days);
            dt = new DateTime(Period.Start.Year, Period.Start.Month, 1);
            var dtInit = dt;
            int idx = 0;
            while (!exceed)
            {
                //var dateEnd = Tools.EndDateOfMonth(dateIdx);
                //if (dateEnd >= Period.Finish)
                //{
                //    exceed = true;
                //    dateEnd = Period.Finish;
                //}
                //var daysInMth = (dateEnd - dateIdx).TotalDays + 1;
                var daysInMth = 1;
                if (idx != 0) dt = dtInit.AddMonths(idx);

                var newAlloc = _allocations.FirstOrDefault(d => d.Period.Year.Equals(dt.Year)
                    && d.Period.Month.Equals(dt.Month));
                if (newAlloc == null)
                {
                    newAlloc = new PIPAllocation
                    {
                        AllocationID = idx,
                        Period = dt,
                        LEDays = 0,
                        LECost = 0
                    };
                }

                //--- get wau elements, and update actual
                if (ewau != null && loadFromWAU && !resetLE)
                {
                    var xAlloc = _allocations.FirstOrDefault(x => x.Period.Year == newAlloc.Period.Year
                        && x.Period.Month == newAlloc.Period.Month);
                    if (xAlloc != null)
                    {
                        newAlloc.LEDays = xAlloc.LEDays;
                        newAlloc.LECost = xAlloc.LECost;
                    }
                }
                else if (ewau == null)
                {
                    newAlloc.LEDays = daysInMth * modElement.LEDays;
                    newAlloc.LECost = daysInMth * modElement.LECost;
                }

                if (resetPlan)
                {
                    newAlloc.CostPlanImprovement = modElement.CostPlanImprovement * daysInMth;
                    newAlloc.CostPlanRisk = modElement.CostPlanRisk * daysInMth;
                    newAlloc.DaysPlanImprovement = modElement.DaysPlanImprovement * daysInMth;
                    newAlloc.DaysPlanRisk = modElement.DaysPlanRisk * daysInMth;
                }

                if (resetLE)
                {
                    newAlloc.LEDays = daysInMth * modElement.LEDays;
                    newAlloc.LECost = daysInMth * modElement.LECost;
                }

                //newAlloc.LEDays = newAlloc.DaysPlanImprovement + newAlloc.DaysPlanRisk;
                //newAlloc.LECost = newAlloc.CostPlanImprovement + newAlloc.CostPlanRisk;
                //totalAlloc.LEDays += newAlloc.LEDays;
                //totalAlloc.LECost += newAlloc.LECost;
                newAllocations.Add(newAlloc);

                //dateIdx = dateEnd.AddDays(1);
                idx++;
                if (idx == days) exceed = true;
            }
            Allocations = newAllocations;
        }
        public List<WEISPersonInfo> ActionParties { get; set; }
        [BsonIgnore]
        public double CompletionPerc { get; set; }
        [BsonIgnore]
        public double LEDays { get; set; }
        [BsonIgnore]
        public double LECost { get; set; }
        [BsonIgnore]
        public double RatioElement { get; set; }

        private List<WEISComment> _Comments;
        public List<WEISComment> Comments
        {
            get
            {
                if (_Comments == null) _Comments = new List<WEISComment>();
                return _Comments;
            }
            set { _Comments = value; }
        }

        [BsonIgnore]
        public int LevelOfEstimate { get; set; }
        //[BsonIgnoreExtraElements]
        //public List<PIPElementLoE> LoEs { get; set; }

        public bool CalculateIsPositive()
        {
            var costPlan = this.CostPlanImprovement + this.CostPlanRisk;
            var costCurrentWeek = this.CostCurrentWeekImprovement + this.CostCurrentWeekRisk;

            return !((costPlan - costCurrentWeek) >= 0);
        }

        //public string[] AssignTOOps { get; set; }

        private List<string> _AssignTOOps;
        public List<string> AssignTOOps
        {
            get
            {
                if (_AssignTOOps == null) _AssignTOOps = new List<string>();
                return _AssignTOOps;
            }
            set { _AssignTOOps = value; }
        }
    }

    [BsonIgnoreExtraElements]
    public class PIPPerformanceMetrics
    {
        public string Title { get; set; }
        public double Schedule { get; set; }
        public double Cost { get; set; }
    }
    [BsonIgnoreExtraElements]
    public class PIPProjectMilestones
    {
        public string Title { get; set; }


        public DateTime? Period
        {
            get
            {
                return this._Period.HasValue ? Tools.ToUTC(this._Period.Value) : (DateTime?)null;
            }
            set
            {
                this._Period = value;
            }
        }

        private DateTime? _Period { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class PIPAllocation
    {
        public int AllocationID { get; set; }
        private DateTime _period;
        public DateTime Period
        {
            get
            {
                return _period;
            }
            set
            {
                _period = Tools.ToUTC(value, true);
            }
        }
        public double DaysPlanImprovement { get; set; }
        public double DaysPlanRisk { get; set; }
        public double CostPlanImprovement { get; set; }
        public double CostPlanRisk { get; set; }
        public double LEDays { get; set; }
        public double LECost { get; set; }
    }
    public class WellPIPClassifications : ECISModel
    {
        public override string TableName
        {
            get { return "WEISPIPClassifications"; }
        }
        public string Name { get; set; }
    }
    public class WellPIPThemes : ECISModel
    {
        public override string TableName
        {
            get { return "WEISPIPThemes"; }
        }
        public string Name { get; set; }
    }
    public class ActivityCategory : ECISModel
    {
        public override string TableName
        {
            get { return "WEISActivityCategory"; }
        }
        public string Name { get; set; }
    }

}