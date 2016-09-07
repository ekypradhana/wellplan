using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS
{
    public class YearWizard
    {
        public string Country { get; set; }
        public int StartYear { get; set; }
        public int FinishYear { get; set; }
        public string BaseOP { get; set; }
        public string ModelName { get; set; }

        public List<YearWizardRFM> Details { get; set; }

        public YearWizard()
        {
            Details = new List<YearWizardRFM>();
        }

        public WEISReferenceFactorModel AddYearWizard(List<Dictionary<string, object>> changes)
        {

            var yw = this;
            var q = Query.And(
                Query.EQ("GroupCase", yw.ModelName),
                Query.EQ("BaseOP", yw.BaseOP),
                Query.EQ("Country", yw.Country)
                );
            var rfm = WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(q);
            if (rfm == null)
            {
                rfm = new WEISReferenceFactorModel();
                rfm.BaseOP = BaseOP;
                rfm.GroupCase = ModelName;
                changes.ForEach(d =>
                {
                    var subject = Convert.ToString(d["Subject"]);

                    if (!rfm.SubjectMatters.ContainsKey(subject))
                        rfm.SubjectMatters[subject] = new Dictionary<string, double>();

                    foreach (KeyValuePair<string, object> each in d)
                    {
                        if (!each.Key.Contains("Year_"))
                            continue;

                        rfm.SubjectMatters[subject][each.Key] = Convert.ToDouble(each.Value);
                    }
                });
            }
            string[] refmodel = new string[] { "Material Escalation Factors", "Service Escalation Factors", "CSO Factors", "Inflation Factors", "Learning Curve Factors" };

            var qM = Query.And(
                Query.EQ("BaseOP", yw.BaseOP),
                Query.EQ("Country", yw.Country)
                );

            var inflationMacros = MacroEconomic.Get<MacroEconomic>(qM);
            List<AnnualHelper> infs = new List<AnnualHelper>();
            if (inflationMacros != null)
            {
                infs = inflationMacros.Inflation.AnnualValues;
            }

            if (rfm != null)
            {
                foreach (var t in refmodel)
                {
                    var mat = rfm.SubjectMatters.Where(x => x.Key.Equals(t));
                    if (mat.Any())
                    {
                        var escValMat = mat.FirstOrDefault();
                        for (int i = yw.StartYear; i <= yw.FinishYear; i++)
                        {
                            var yearValue = escValMat.Value.ToList().Where(x => x.Key.Equals("Year_" + i));
                            var refUpdater = yw.Details.Where(x => x.Name.Equals(t));
                            if (refUpdater.Any())
                            {
                                if (refUpdater.FirstOrDefault().isUpdate == true) // isUpdate == true
                                {
                                    if (refUpdater.FirstOrDefault().isOvverideCurrent == true) // isOvveride == true
                                    {
                                        #region Ovveride current
                                        if (refUpdater.FirstOrDefault().isCompound == true) // isCoumpounded
                                        {
                                            #region compounded
                                            // compounded
                                            var cpds = refUpdater.FirstOrDefault().CompoundValue;
                                            var yearstartCom = refUpdater.FirstOrDefault().YearToCompound;
                                            var replacewith = refUpdater.FirstOrDefault().Value;

                                            if (yearValue.Any())
                                            {
                                                // thn tsb sudah ada, ovveride dan compound
                                                if (i >= yearstartCom)
                                                {
                                                    // masuk tahun compound, ovveride
                                                    var valFromMacro = 0.0;

                                                    if (t.Equals("Inflation Factors"))
                                                    {
                                                        if (infs.Where(x => x.Year == i).Any())
                                                        {
                                                            valFromMacro = infs.Where(x => x.Year == i).FirstOrDefault().Value;
                                                            escValMat.Value.Remove("Year_" + i);
                                                            escValMat.Value.Add("Year_" + i, valFromMacro);
                                                        }
                                                        else
                                                        {
                                                            escValMat.Value.Remove("Year_" + i);
                                                            escValMat.Value.Add("Year_" + i, 0);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var prevVal = escValMat.Value.ToList().Where(x => x.Key.Equals("Year_" + (i - 1))) == null ? 0 : escValMat.Value.ToList().Where(x => x.Key.Equals("Year_" + (i - 1))).FirstOrDefault().Value;
                                                        //((prev year  + 1) * (factor  + 1)) - 1
                                                        
                                                        var coumpondValue = ((prevVal + 1) * (cpds + 1)) -1;




                                                        escValMat.Value.Remove("Year_" + i);
                                                        escValMat.Value.Add("Year_" + i, coumpondValue);
                                                    }
                                                }
                                                else
                                                {
                                                    var valFromMacro = 0.0;

                                                    // klo inflasi ambilkan dari tabel
                                                    if (t.Equals("Inflation Factors"))
                                                    {
                                                        if (infs.Where(x => x.Year == i).Any())
                                                        {
                                                            valFromMacro = infs.Where(x => x.Year == i).FirstOrDefault().Value;
                                                            escValMat.Value.Remove("Year_" + i);
                                                            escValMat.Value.Add("Year_" + i, valFromMacro);
                                                        }
                                                        else
                                                        {
                                                            escValMat.Value.Remove("Year_" + i);
                                                            escValMat.Value.Add("Year_" + i, 0);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var curVval = yearValue.FirstOrDefault().Value;
                                                        escValMat.Value.Remove("Year_" + i);
                                                        escValMat.Value.Add("Year_" + i, replacewith);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // add
                                                if (i >= yearstartCom)
                                                {
                                                    // masuk tahun compound, ovveride
                                                    if (t.Equals("Inflation Factors"))
                                                    {
                                                        var valFromMacro = 0.0;

                                                        if (infs.Where(x => x.Year == i).Any())
                                                        {
                                                            valFromMacro = infs.Where(x => x.Year == i).FirstOrDefault().Value;
                                                            escValMat.Value.Remove("Year_" + i);
                                                            escValMat.Value.Add("Year_" + i, valFromMacro);
                                                        }
                                                        else
                                                        {
                                                            escValMat.Value.Remove("Year_" + i);
                                                            escValMat.Value.Add("Year_" + i, 0);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var prevVal = escValMat.Value.ToList().Where(x => x.Key.Equals("Year_" + (i - 1))) == null ? 0 : escValMat.Value.ToList().Where(x => x.Key.Equals("Year_" + (i - 1))).FirstOrDefault().Value;
                                                        //var coumpondValue = prevVal + (prevVal * cpds);

                                                        var coumpondValue = ((prevVal + 1) * (cpds + 1)) - 1;

                                                        
                                                        escValMat.Value.Add("Year_" + i, coumpondValue);
                                                    }
                                                }
                                                else
                                                {

                                                    if (t.Equals("Inflation Factors"))
                                                    {
                                                        var valFromMacro = 0.0;

                                                        if (infs.Where(x => x.Year == i).Any())
                                                        {
                                                            valFromMacro = infs.Where(x => x.Year == i).FirstOrDefault().Value;
                                                            escValMat.Value.Remove("Year_" + i);
                                                            escValMat.Value.Add("Year_" + i, valFromMacro);
                                                        }
                                                        else
                                                        {
                                                            escValMat.Value.Remove("Year_" + i);
                                                            escValMat.Value.Add("Year_" + i, 0);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        escValMat.Value.Add("Year_" + i, replacewith);
                                                    }
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            #region not compounded
                                            // compounded
                                            var replacewith = refUpdater.FirstOrDefault().Value;

                                            if (yearValue.Any())
                                            {
                                                if (t.Equals("Inflation Factors"))
                                                {
                                                    var valFromMacro = 0.0;

                                                    if (infs.Where(x => x.Year == i).Any())
                                                    {
                                                        valFromMacro = infs.Where(x => x.Year == i).FirstOrDefault().Value;
                                                        escValMat.Value.Remove("Year_" + i);
                                                        escValMat.Value.Add("Year_" + i, valFromMacro);
                                                    }
                                                    else
                                                    {
                                                        escValMat.Value.Remove("Year_" + i);
                                                        escValMat.Value.Add("Year_" + i, 0);
                                                    }
                                                }
                                                else
                                                {
                                                    // thn tsb sudah ada, ovveride, not compound
                                                    var curVval = yearValue.FirstOrDefault().Value;
                                                    escValMat.Value.Remove("Year_" + i);
                                                    escValMat.Value.Add("Year_" + i, replacewith);
                                                }

                                            }
                                            else
                                            {
                                                if (t.Equals("Inflation Factors"))
                                                {
                                                    var valFromMacro = 0.0;
                                                    if (infs.Where(x => x.Year == i).Any())
                                                    {
                                                        valFromMacro = infs.Where(x => x.Year == i).FirstOrDefault().Value;
                                                        escValMat.Value.Add("Year_" + i, valFromMacro);
                                                    }
                                                    else
                                                    {
                                                        escValMat.Value.Add("Year_" + i, 0);
                                                    }
                                                }
                                                else
                                                {
                                                    // add
                                                    escValMat.Value.Add("Year_" + i, replacewith);
                                                }
                                            }
                                            #endregion
                                        }
                                        #endregion
                                    }
                                    else
                                    {
                                        #region not ovveride
                                        if (refUpdater.FirstOrDefault().isCompound == true) // isCoumpounded
                                        {
                                            #region compounded
                                            // compounded
                                            var cpds = refUpdater.FirstOrDefault().CompoundValue;
                                            var yearstartCom = refUpdater.FirstOrDefault().YearToCompound;
                                            var replacewith = refUpdater.FirstOrDefault().Value;

                                            if (!yearValue.Any())
                                            {
                                                // add
                                                if (i >= yearstartCom)
                                                {
                                                    if (t.Equals("Inflation Factors"))
                                                    {
                                                        var valFromMacro = 0.0;
                                                        if (infs.Where(x => x.Year == i).Any())
                                                        {
                                                            valFromMacro = infs.Where(x => x.Year == i).FirstOrDefault().Value;
                                                            escValMat.Value.Add("Year_" + i, valFromMacro);
                                                        }
                                                        else
                                                        {
                                                            escValMat.Value.Add("Year_" + i, 0);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // masuk tahun compound, ovveride
                                                        var prevVal = escValMat.Value.ToList().Where(x => x.Key.Equals("Year_" + (i - 1))) == null ? 0 : escValMat.Value.ToList().Where(x => x.Key.Equals("Year_" + (i - 1))).FirstOrDefault().Value;
                                                        //var coumpondValue = prevVal + (prevVal * cpds);

                                                        var coumpondValue = ((prevVal + 1) * (cpds + 1)) - 1;

                                                        escValMat.Value.Add("Year_" + i, coumpondValue);
                                                    }
                                                }
                                                else
                                                {

                                                    if (t.Equals("Inflation Factors"))
                                                    {
                                                        var valFromMacro = 0.0;
                                                        if (infs.Where(x => x.Year == i).Any())
                                                        {
                                                            valFromMacro = infs.Where(x => x.Year == i).FirstOrDefault().Value;
                                                            escValMat.Value.Add("Year_" + i, valFromMacro);
                                                        }
                                                        else
                                                        {
                                                            escValMat.Value.Add("Year_" + i, 0);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        escValMat.Value.Add("Year_" + i, replacewith);
                                                    }
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            #region not compounded
                                            // compounded
                                            var replacewith = refUpdater.FirstOrDefault().Value;

                                            if (yearValue.Any())
                                            {
                                                //// thn tsb sudah ada, not ovveride dan not compound
                                                //var curVval = yearValue.FirstOrDefault().Value;
                                                //escValMat.Value.Remove("Year_" + i);
                                                //escValMat.Value.Add("Year_" + i, replacewith);
                                            }
                                            else
                                            {
                                                // add

                                                if (t.Equals("Inflation Factors"))
                                                {
                                                    var valFromMacro = 0.0;
                                                    if (infs.Where(x => x.Year == i).Any())
                                                    {
                                                        valFromMacro = infs.Where(x => x.Year == i).FirstOrDefault().Value;
                                                        escValMat.Value.Add("Year_" + i, valFromMacro);
                                                    }
                                                    else
                                                    {
                                                        escValMat.Value.Add("Year_" + i, 0);
                                                    }
                                                }
                                                else
                                                {
                                                    escValMat.Value.Add("Year_" + i, replacewith);
                                                }
                                            }
                                            #endregion
                                        }
                                        #endregion
                                    }
                                }
                            }
                        }
                    }
                }


                List<double> maxyear = new List<double>();
                foreach (var t in refmodel)
                {
                    var mat = rfm.SubjectMatters.Where(x => x.Key.Equals(t));

                    var escValMat = mat.FirstOrDefault();
                    var yearValue = escValMat.Value.ToList().Select(x => Convert.ToInt32(x.Key.Replace("Year_", ""))).Max();
                    maxyear.Add(yearValue);
                }
                var myear = maxyear.Max();

                foreach (var t in refmodel)
                {
                    var mat = rfm.SubjectMatters.Where(x => x.Key.Equals(t));
                    var escValMat = mat.FirstOrDefault();
                    var yearValue = escValMat.Value.ToList().Select(x => Convert.ToInt32(x.Key.Replace("Year_", ""))).Max();
                    var yearmin = escValMat.Value.ToList().Select(x => Convert.ToInt32(x.Key.Replace("Year_", ""))).Min();

                    var startadd = Convert.ToInt32(yearValue) + 1;

                    for (int i = startadd; i <= myear; i++)
                    {

                        if (t.Equals("Inflation Factors"))
                        {
                            var valFromMacro = 0.0;
                            if (infs.Where(x => x.Year == i).Any())
                            {
                                valFromMacro = infs.Where(x => x.Year == i).FirstOrDefault().Value;
                                escValMat.Value.Add("Year_" + i, valFromMacro);
                            }
                            else
                            {
                                escValMat.Value.Add("Year_" + i, 0);
                            }
                        }
                        else
                        {
                            escValMat.Value.Add("Year_" + i, 0);
                            maxyear.Add(yearValue);
                            yearValue++;
                        }


                    }
                }

                return rfm;
            }
            else
                return null;
        }

        public double GetInflation(string BaseOP, string Country, int Year)
        {
            var qM = Query.And(
              Query.EQ("BaseOP", BaseOP),
              Query.EQ("Country", Country)
              );

            var inflationMacros = MacroEconomic.Get<MacroEconomic>(qM);
            if (inflationMacros != null)
            {
                var infs = inflationMacros.Inflation.AnnualValues;
                if (infs.Where(x => x.Year == Year).Any())
                {
                    var ttt = infs.Where(x => x.Year == Year);
                    if (ttt.Any())
                    {
                        return ttt.FirstOrDefault().Value;
                    }
                    else
                        return 0;
                }
                else
                {
                    return 0;
                }
            }
            else
                return 0;
        }
    }

    public class YearWizardRFM
    {
        public string Name { get; set; }
        public bool isUpdate { get; set; }
        public double Value { get; set; }
        public bool isCompound { get; set; }
        public int YearToCompound { get; set; }
        public double CompoundValue { get; set; }
        public bool isOvverideCurrent { get; set; }
    }

}
