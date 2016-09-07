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

namespace ECIS.Client.WEIS
{

    public class WellSimulation : ECISModel
    {
        public override string TableName
        {
            get { return "WEISWellSimulation"; }
        }

        public string CopyFrom { get; set; }

        public List<AnnualDetail> Annuals { get; set; }
        public List<AnnualDetail> Comparisons { get; set; }

        public WellSimulation()
        {
            Annuals = new List<AnnualDetail>();
            Comparisons = new List<AnnualDetail>();
        }

        public static Dictionary<int, double> SplitNoOfDaysAnnualy(DateRange dr)
        {
            Dictionary<int, double> result = new Dictionary<int, double>();
            do
            {
                if (dr.Finish.Year == dr.Start.Year)
                {
                    result.Add(dr.Finish.Year, (dr.Finish - dr.Start).Days + 1);
                }
                else
                {
                    DateTime lastThisYear = new DateTime(dr.Start.Year, 12, 31);
                    result.Add(dr.Start.Year, (lastThisYear - dr.Start).Days + 1);
                }
                dr.Start = new DateTime(dr.Start.Year, 1, 1).AddYears(1);

            } while (dr.Finish.Year >= dr.Start.Year);

            return result;
        }

        public static List<AnnualDetail> CalcCurrentWellPlanAnnualy(string type = "CurrentOP")
        {
            var curactv = WellActivity.Populate<WellActivity>();
            List<AnnualDetail> rx = new List<AnnualDetail>();
            List<AnnualDetail> ans = new List<AnnualDetail>();

            if (type.Equals("CurrentOP"))
            {
                #region Current OP
                foreach (var t in curactv)
                {
                    foreach (var p in t.Phases)
                    {
                        if (p.PlanSchedule.Start.Year <= 1900 || p.PlanSchedule.Finish.Year <= 1900)
                        { }
                        else
                        {
                            var res = SplitNoOfDaysAnnualy(p.PlanSchedule);
                            foreach (var r in res)
                            {

                                var pembagi = Tools.Div(r.Value, (p.PlanSchedule.Finish - p.PlanSchedule.Start).Days + 1);

                                ExpenseDetail ed = new ExpenseDetail();

                                ed.Title = p.FundingType == null ? "" : p.FundingType;
                                ed.Cost = pembagi * p.Plan.Cost;
                                ed.Days = pembagi * p.Plan.Days;

                                ans.Add(new AnnualDetail
                                {
                                    Days = pembagi * p.Plan.Days,
                                    Cost = pembagi * p.Plan.Cost,
                                    Year = r.Key,
                                    ExpenseDetail = ed
                                });

                            }

                        }
                    }
                }
                #endregion
            }
            else if (type.Equals("CurrentLE"))
            {
                #region Current LE
                foreach (var t in curactv)
                {
                    foreach (var p in t.Phases)
                    {
                        if (p.LESchedule.Start.Year <= 1900 || p.LESchedule.Finish.Year <= 1900)
                        { }
                        else
                        {
                            var res = SplitNoOfDaysAnnualy(p.LESchedule);
                            foreach (var r in res)
                            {

                                var pembagi = Tools.Div(r.Value, (p.LESchedule.Finish - p.LESchedule.Start).Days + 1);

                                ExpenseDetail ed = new ExpenseDetail();

                                ed.Title = p.FundingType == null ? "" : p.FundingType;
                                ed.Cost = pembagi * p.LE.Cost;
                                ed.Days = pembagi * p.LE.Days;

                                ans.Add(new AnnualDetail
                                {
                                    Days = pembagi * p.LE.Days,
                                    Cost = pembagi * p.LE.Cost,
                                    Year = r.Key,
                                    ExpenseDetail = ed
                                });

                            }

                        }
                    }
                }

                #endregion
            }
            else if (type.Equals("CurrentLS"))
            {
                #region Current LS
                foreach (var t in curactv)
                {
                    foreach (var p in t.Phases)
                    {
                        if (p.PhSchedule.Start.Year <= 1900 || p.PhSchedule.Finish.Year <= 1900)
                        { }
                        else
                        {
                            var res = SplitNoOfDaysAnnualy(p.PhSchedule);
                            foreach (var r in res)
                            {

                                var pembagi = Tools.Div(r.Value, (p.PhSchedule.Finish - p.PhSchedule.Start).Days + 1);

                                ExpenseDetail ed = new ExpenseDetail();

                                ed.Title = p.FundingType == null ? "" : p.FundingType;
                                ed.Cost = pembagi * p.OP.Cost;
                                ed.Days = pembagi * p.OP.Days;

                                ans.Add(new AnnualDetail
                                {
                                    Days = pembagi * p.OP.Days,
                                    Cost = pembagi * p.OP.Cost,
                                    Year = r.Key,
                                    ExpenseDetail = ed
                                });

                            }

                        }
                    }
                }

                #endregion
            }
            foreach (var t in ans.GroupBy(x => x.Year))
            {
                List<ExpenseDetail> details = new List<ExpenseDetail>();
                foreach (var y in t.ToList().GroupBy(x => x.ExpenseDetail.Title))
                {
                    details.Add(new ExpenseDetail { Title = y.Key, Year = y.FirstOrDefault().Year, Cost = y.Sum(p => p.Cost), Days = y.Sum(p => p.Days) });
                }
                rx.Add(new AnnualDetail
                {
                    Year = t.Key,
                    Cost = t.ToList().Sum(x => x.Cost),
                    Days = t.ToList().Sum(x => x.Days),
                    ExpenseDetails = details,
                    ExpenseDetail = null
                });
            }

            // funding Type recalc
            List<AnnualDetail> result = new List<AnnualDetail>();

            foreach (var r in rx)
            {
                AnnualDetail ad = new AnnualDetail();
                ad.Year = r.Year;
                ad.Days = r.Days;
                ad.Cost = r.Cost;


                ExpenseDetail EXPEX = new ExpenseDetail();
                EXPEX.Title = "EXPEX";
                EXPEX.Cost = r.ExpenseDetails.Where(x => x.Title.Trim().ToUpper().Contains("EXPEX")).Sum(x => x.Cost);
                EXPEX.Days = r.ExpenseDetails.Where(x => x.Title.Trim().ToUpper().Contains("EXPEX")).Sum(x => x.Days);
                EXPEX.Year = r.Year;
                ad.ExpenseDetails.Add(EXPEX);

                ExpenseDetail ABEX = new ExpenseDetail();
                ABEX.Title = "ABEX";
                ABEX.Cost = r.ExpenseDetails.Where(x => x.Title.Trim().ToUpper().Contains("ABEX")).Sum(x => x.Cost);
                ABEX.Days = r.ExpenseDetails.Where(x => x.Title.Trim().ToUpper().Contains("ABEX")).Sum(x => x.Days);
                ABEX.Year = r.Year;
                ad.ExpenseDetails.Add(ABEX);

                ExpenseDetail CAPEX = new ExpenseDetail();
                CAPEX.Title = "CAPEX";
                CAPEX.Cost = r.ExpenseDetails.Where(x => x.Title.Trim().ToUpper().Contains("CAPEX")).Sum(x => x.Cost);
                CAPEX.Days = r.ExpenseDetails.Where(x => x.Title.Trim().ToUpper().Contains("CAPEX")).Sum(x => x.Days);
                CAPEX.Year = r.Year;
                ad.ExpenseDetails.Add(CAPEX);

                ExpenseDetail OPEX = new ExpenseDetail();
                OPEX.Title = "OPEX";
                OPEX.Cost = r.Cost - (EXPEX.Cost + ABEX.Cost + CAPEX.Cost);
                OPEX.Days = r.Days - (EXPEX.Days + ABEX.Days + CAPEX.Days);
                OPEX.Year = r.Year;
                ad.ExpenseDetails.Add(OPEX);


                result.Add(ad);
            }


            return result.OrderBy(x => x.Year).ToList();
        }

        public List<BsonDocument> WellPlans { get; set; }

        public override void PostGet()
        {
            if (WellPlans == null)
            {
                // load current
                this.WellPlans = new List<BsonDocument>();
                var wellPlans = WellActivity.Populate<WellActivity>();

                foreach (var w in wellPlans)
                {
                    WellDrillData hsim = new WellDrillData();

                    foreach (var p in w.Phases)
                    {
                        WellDrillData sim = new WellDrillData();
                        DateRange simSchedule = new DateRange(Tools.DefaultDate, Tools.DefaultDate);

                        

                    }
                }
            }

            // bind Simulation Data per Phase and For WellActivities

            // bind comparison data



            base.PostGet();
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            if (_id == null)
            {
                _id = String.Format("U{0}", LastUpdate.ToString("yyyyMMddhhmmss"));
                return this.ToBsonDocument();
            }
            else
                return this.ToBsonDocument();
        }


        public override BsonDocument PostSave(BsonDocument doc, string[] references = null)
        {
            if (references != null && references.Contains("saveSIM"))
            {
                var currentDB = this.WellPlans.Select(x => x.ToBsonDocument());
                DataHelper.Delete("_SIM_" + this._id.ToString());
                DataHelper.Save("_SIM_" + this._id.ToString(), currentDB);
            }
            return doc;
        }


    }

    public class AnnualDetail
    {
        // comparison attribute
        public string Id { get; set; }
        public string Title { get; set; }
        public string CreateBy { get; set; }


        public double Days { get; set; }
        public double Cost { get; set; }
        public int Year { get; set; }

        public ExpenseDetail ExpenseDetail { get; set; }

        public List<ExpenseDetail> ExpenseDetails { get; set; }

        public AnnualDetail()
        {
            ExpenseDetail = new ExpenseDetail();
            ExpenseDetails = new List<ExpenseDetail>();
        }
    }

    public class ExpenseDetail
    {
        public double Days { get; set; }
        public double Cost { get; set; }
        public int Year { get; set; }
        public string Title { get; set; }
    }

   
}
