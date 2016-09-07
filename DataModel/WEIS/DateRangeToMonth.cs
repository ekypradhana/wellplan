using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ECIS.Core;
namespace ECIS.Client.WEIS
{

    public class NumberOfDayPerYear
    {
        public double NoOfDay { get; set; }
        public int Year { get; set; }
        public double Proportion { get; set; }
        public DateRange OriginalDateRange { get; set; }
        public DateRange SplitedDateRange { get; set; }
        public NumberOfDayPerYear()
        {
            OriginalDateRange = new DateRange();
            SplitedDateRange = new DateRange();
        }
    }

    public class NumberOfDayPerMonth
    {
        public double NoOfDay { get; set; }
        public int MonthId { get; set; }
        public double Proportion { get; set; }
        public DateRange OriginalDateRange { get; set; }
        public DateRange SplitedDateRange { get; set; }
        public NumberOfDayPerMonth()
        {
            OriginalDateRange = new DateRange();
            SplitedDateRange = new DateRange();
        }
    }


    public class DateRangeToMonth
    {


        public static List<NumberOfDayPerMonth> SplitedDateRangeMonthly(DateRange input)
        {
            var totalNumberOFDay = input.Days + 1;

            List<NumberOfDayPerMonth> result = new List<NumberOfDayPerMonth>();

            List<DateIsland> islnads = new List<DateIsland>();
            Dictionary<int,DateTime> groups = new Dictionary<int,DateTime>();
            for (DateTime i = input.Start; i <= input.Finish; )
            {
                islnads.Add(new DateIsland(i));
                i = i.AddDays(1);
            }

            foreach (var y in islnads.GroupBy(x=>x.MonthId))
            {
                NumberOfDayPerMonth r = new NumberOfDayPerMonth();
                r.MonthId = y.Key;
                r.NoOfDay = y.ToList().Count();
                r.OriginalDateRange = input;
                r.SplitedDateRange = new DateRange(
                    y.ToList().OrderBy(x => x.DateId).FirstOrDefault().DateId,
                    y.ToList().OrderByDescending(x => x.DateId).FirstOrDefault().DateId);
                r.Proportion = Tools.Div(y.ToList().Count(), totalNumberOFDay);
                result.Add(r);
            }

            return result;
        }

        public static List<NumberOfDayPerYear> SplitedDateRangeYearly(DateRange input)
        {
            List<NumberOfDayPerYear> annual = new List<NumberOfDayPerYear>();

            var results = NumDaysPerYearSplited(input);
            var sum = results.Sum(x => x.Value.Sum(y => y.Value));
            List<NumberOfDayPerYear> res = new List<NumberOfDayPerYear>();
            foreach (var t in results)
            {
                NumberOfDayPerYear r = new NumberOfDayPerYear();
                r.NoOfDay = t.Value.Sum(x => x.Value);
                r.OriginalDateRange = input;
                r.Proportion = Tools.Div(r.NoOfDay, sum);
                r.SplitedDateRange = t.Value.FirstOrDefault().Key;
                annual.Add(r);
                r.Year = t.Key;
            }
            return annual;
        }

        public static Dictionary<string, double> GetListDayOfMonths(DateRange range)
        {
            Dictionary<string, double> res = new Dictionary<string, double>();
            var listmonths = GetListMonthOfDateRange(range);

            if (range.Start.Month == range.Finish.Month)
            {
                res.Add(range.Start.ToString("yyyyMM"), (range.Finish.Date - range.Start.Date).TotalDays);
            }
            else
            {
                DateTime currentLoop = new DateTime();
                currentLoop = range.Start.Date;
                foreach (var monthId in listmonths.OrderBy(x => x.Key))
                {
                    if (monthId.Value.Equals(range.Start.Date.ToString("yyyyMM")))
                    {
                        var yyy = DateTime.DaysInMonth(currentLoop.Year, range.Start.Month) - range.Start.Day;
                        res.Add(monthId.Value, Convert.ToDouble(yyy));
                        currentLoop = currentLoop.AddMonths(1);
                    }
                    else if (monthId.Value.Equals(range.Finish.Date.ToString("yyyyMM")))
                    {
                        res.Add(monthId.Value, range.Finish.Day);
                    }
                    else
                    {
                        // between 
                        res.Add(currentLoop.ToString("yyyyMM"), Convert.ToDouble(DateTime.DaysInMonth(currentLoop.Year, currentLoop.Month)));
                        currentLoop = currentLoop.AddMonths(1);
                    }
                }

            }
            return res;
        }

        public static Dictionary<int, string> GetListMonthOfDateRange(DateRange range)
        {
            Dictionary<int, string> res = new Dictionary<int, string>();
            //int startMonth = range.Start.Date.Month;
            int i = 1;
            for (DateTime day = range.Start.Date; range.Finish.Date >= day; day = day.AddMonths(1))
            {
                res.Add(i, day.ToString("yyyyMM"));
                i++;
            }
            return res;
        }

        public static bool isDateInsideofRange(DateTime dateCheck, DateRange DateRange)
        {
            if (dateCheck > DateRange.Start && dateCheck < DateRange.Finish)
                return true;
            else
                return false;
        }

        public static double GetDaysIntersect(DateRange dr1, DateRange dr2)
        {
            double ret = 0;
            var dr3 = new DateRange();
            dr3.Start = dr1.Start > dr2.Start ? dr1.Start : dr2.Start;
            dr3.Finish = dr1.Finish < dr2.Finish ? dr1.Finish : dr2.Finish;
            if (dr3.Start > dr3.Finish)
                ret = 0;
            else
                ret = dr3.Days + 1;
            return ret;
        }

        public static bool isDateInsideEqualofRange(DateTime dateCheck, DateRange DateRange)
        {
            if (dateCheck.Date >= DateRange.Start.Date && dateCheck.Date <= DateRange.Finish.Date)
                return true;
            else
                return false;
        }

        public static bool isDateRangeOverlaping(DateRange dateCheck, DateRange DateRange, out string Message, out double OverlappingDays)
        {

            //assumed
            //DateRange == OPSch
            //dateCheck == RIG /SCM Element Period

            bool isTrue = false;
            string msg = "";
            double OverLap = 0.0;

            if (dateCheck.Start > dateCheck.Finish)
            {
                msg = "Start Date Check is more than Finish Date Check, or Start Date equals Finish Date";
                isTrue = false;


                Message = msg;
                OverlappingDays = OverLap;


                return isTrue;
            }
            else
            {
                if ((dateCheck.Start >= DateRange.Start && dateCheck.Start <= DateRange.Finish) && (dateCheck.Finish <= DateRange.Finish))
                {
                    isTrue = true;
                    msg = "Inside or Equal";

                    DateRange dr = new DateRange(dateCheck.Start, dateCheck.Finish);
                    OverLap = dr.Days;


                    Message = msg;
                    OverlappingDays = OverLap;


                    return isTrue;
                }
                if ((dateCheck.Start >= DateRange.Start && dateCheck.Start <= DateRange.Finish) && dateCheck.Finish > DateRange.Finish)
                {
                    isTrue = true;
                    msg = "Finish Date Check overlaping Range Date Finish";
                    DateRange dr = new DateRange(dateCheck.Start, DateRange.Finish);
                    OverLap = dr.Days;


                    Message = msg;
                    OverlappingDays = OverLap;


                    return isTrue;
                }

                if (dateCheck.Start < DateRange.Start && dateCheck.Finish <= DateRange.Finish && dateCheck.Finish > DateRange.Start)
                {
                    isTrue = true;
                    msg = "Start Date Check under Range Date";
                    DateRange dr = new DateRange(DateRange.Start, dateCheck.Finish);
                    OverLap = dr.Days;


                    Message = msg;
                    OverlappingDays = OverLap;


                    return isTrue;
                }

                if (dateCheck.Start < DateRange.Start && dateCheck.Finish < DateRange.Start)
                {
                    isTrue = false;
                    msg = "Date Range Check under Date Range";


                    Message = msg;
                    OverlappingDays = OverLap;


                    return isTrue;
                }
                if (dateCheck.Start > DateRange.Finish && dateCheck.Finish < DateRange.Finish)
                {
                    isTrue = false;
                    msg = "Date Range Check above Date Range";


                    Message = msg;
                    OverlappingDays = OverLap;


                    return isTrue;
                }

                if (dateCheck.Start < DateRange.Start && dateCheck.Finish > DateRange.Finish)
                {
                    isTrue = true;
                    msg = "Start and Finish cross and outside of date range";
                    DateRange dr = new DateRange(DateRange.Start, DateRange.Finish);
                    OverLap = dr.Days;


                    Message = msg;
                    OverlappingDays = OverLap;


                    return isTrue;
                }
                else
                    if ((dateCheck.Start > DateRange.Finish && dateCheck.Finish > DateRange.Finish) ||
                     (dateCheck.Start < DateRange.Start && dateCheck.Finish > DateRange.Start)

                     )
                    {
                        isTrue = false;
                        msg = "Outside Range";

                        Message = msg;
                        OverlappingDays = OverLap;


                        return isTrue;
                    }


            }
            Message = msg;
            OverlappingDays = OverLap;


            return isTrue;
        }

        public static Dictionary<int, Dictionary<DateRange, double>> NumDaysPerYearSplited(DateRange originalPeriod)
        {
            Dictionary<int, Dictionary<DateRange, double>> originalSplit = new Dictionary<int, Dictionary<DateRange, double>>();
            int yearStart = originalPeriod.Start.Year;
            int yearFinish = originalPeriod.Finish.Year;


            for (int i = yearStart; i <= yearFinish; i++)
            {
                if (i == yearStart)
                {
                    if (yearFinish == yearStart)
                    {
                        DateRange splited = new DateRange();

                        splited.Start = originalPeriod.Start;
                        splited.Finish = originalPeriod.Finish;
                        Dictionary<DateRange, double> split = new Dictionary<DateRange, double>();
                        split.Add(splited, (originalPeriod.Finish - originalPeriod.Start).Days + 1);
                        originalSplit.Add(i, split);
                    }
                    else
                    {
                        DateRange splited = new DateRange();

                        splited.Start = originalPeriod.Start;
                        splited.Finish = new DateTime(yearStart, 12, 31);

                        Dictionary<DateRange, double> split = new Dictionary<DateRange, double>();
                        split.Add(splited, (new DateTime(yearStart, 12, 31) - originalPeriod.Start).Days + 1);
                        originalSplit.Add(i, split);

                    }
                }
                else if (i == yearFinish)
                {
                    DateRange splited = new DateRange();

                    splited.Start = new DateTime(yearFinish, 1, 1);
                    splited.Finish = originalPeriod.Finish;

                    Dictionary<DateRange, double> split = new Dictionary<DateRange, double>();
                    split.Add(splited, (originalPeriod.Finish - new DateTime(yearFinish, 1, 1)).Days + 1);
                    originalSplit.Add(i, split);

                }
                else if (yearStart < i && yearFinish > i)
                {
                    DateRange splited = new DateRange();

                    splited.Start = new DateTime(i, 1, 1);
                    splited.Finish = new DateTime(i, 12, 31);

                    Dictionary<DateRange, double> split = new Dictionary<DateRange, double>();
                    split.Add(splited, (new DateTime(i, 12, 31) - new DateTime(i, 1, 1)).Days + 1);
                    originalSplit.Add(i, split);

                }

            }
            return originalSplit;
        }


        public static Dictionary<int, double> NumDaysPerYear(DateRange originalPeriod)
        {
            Dictionary<int, double> originalSplit = new Dictionary<int, double>();
            int yearStart = originalPeriod.Start.Year;
            int yearFinish = originalPeriod.Finish.Year;

            for (int i = yearStart; i <= yearFinish; i++)
            {
                if (i == yearStart)
                {
                    if (yearFinish == yearStart)
                    {
                        originalSplit.Add(i, (originalPeriod.Finish - originalPeriod.Start).Days + 1);
                    }
                    else
                    {
                        originalSplit.Add(i, (new DateTime(yearStart, 12, 31) - originalPeriod.Start).Days + 1);
                    }
                }
                else if (i == yearFinish)
                {
                    originalSplit.Add(i, (originalPeriod.Finish - new DateTime(yearFinish, 1, 1)).Days + 1);
                }
                else if (yearStart < i && yearFinish > i)
                {
                    originalSplit.Add(i, (new DateTime(i, 12, 31) - new DateTime(i, 1, 1)).Days + 1);
                }

            }

            return originalSplit;
        }

        public static Dictionary<int, Dictionary<DateRange, double>> NumDaysPerYear2(DateRange originalPeriod)
        {
            Dictionary<int, Dictionary<DateRange, double>> originalSplit = new Dictionary<int, Dictionary<DateRange, double>>();
            int yearStart = originalPeriod.Start.Year;
            int yearFinish = originalPeriod.Finish.Year;

            for (int i = yearStart; i <= yearFinish; i++)
            {
                if (i == yearStart)
                {
                    if (yearFinish == yearStart)
                    {
                        Dictionary<DateRange, double> dd = new Dictionary<DateRange, double>();
                        dd.Add(new DateRange(originalPeriod.Start, originalPeriod.Finish), (originalPeriod.Finish - originalPeriod.Start).Days + 1);
                        originalSplit.Add(i, dd);
                    }
                    else
                    {
                        Dictionary<DateRange, double> dd = new Dictionary<DateRange, double>();
                        dd.Add(new DateRange(originalPeriod.Start, new DateTime(yearStart, 12, 31)), (new DateTime(yearStart, 12, 31) - originalPeriod.Start).Days + 1);
                        originalSplit.Add(i, dd);
                    }
                }
                else if (i == yearFinish)
                {
                    Dictionary<DateRange, double> dd = new Dictionary<DateRange, double>();
                    dd.Add(new DateRange(new DateTime(yearFinish, 1, 1), originalPeriod.Finish), (originalPeriod.Finish - new DateTime(yearFinish, 1, 1)).Days + 1);
                    originalSplit.Add(i, dd);
                }
                else if (yearStart < i && yearFinish > i)
                {
                    Dictionary<DateRange, double> dd = new Dictionary<DateRange, double>();
                    dd.Add(new DateRange(new DateTime(i, 1, 1), new DateTime(i, 12, 31)), (new DateTime(i, 12, 31) - new DateTime(i, 1, 1)).Days + 1);
                    originalSplit.Add(i, dd);
                }

            }

            return originalSplit;
        }

        public static Dictionary<int, double> ProportionNumDaysPerYear(DateRange originalPeriod, DateRange filterPeriod)
        {
            Dictionary<int, double> filtersplit = new Dictionary<int, double>();
            int yearStart = filterPeriod.Start.Year;
            int yearFinish = filterPeriod.Finish.Year;

            var ttt = DateRangeToMonth.NumDaysPerYear(originalPeriod);
            var filterx = DateRangeToMonth.NumDaysPerYear(filterPeriod);

            double total = ttt.Sum(x => x.Value);

            foreach (var y in filterx)
            {
                if (ttt.Keys.ToList().Contains(y.Key))
                {
                    filtersplit.Add(y.Key,
                    Tools.Div(ttt.Where(x => x.Key == y.Key).FirstOrDefault().Value, total)
                    );
                }
                else
                {
                    filtersplit.Add(y.Key,
                    0//Tools.Div(y.Value, total)
                    );
                }

            }

            return filtersplit;
        }

        public static Dictionary<int, double> NumDaysPerQuarter(DateRange originalPeriod)
        {


            Dictionary<int, double> originalSplit = new Dictionary<int, double>();
            // this.Numb

            var permonths = NumDaysPerMonth(originalPeriod);
            var noDaysInQuarter = 0;
            int prevQuarter = 0;

            int firstInisiate = 0;

            for (DateTime day = originalPeriod.Start; day <= originalPeriod.Finish; day = day.AddDays(1))
            {
                DateIsland dayInside = new DateIsland(day);
                DateIsland finishDay = new DateIsland(originalPeriod.Finish);

                if (firstInisiate == 0)
                {
                    firstInisiate = firstInisiate + 1;
                    prevQuarter = dayInside.QtrId;

                    if (originalPeriod.Start == originalPeriod.Finish && originalPeriod.Start.Date == originalPeriod.Finish.Date)
                    {
                        originalSplit.Add(prevQuarter, 1);
                    }

                }

                if (dayInside.QtrId == prevQuarter)
                {
                    noDaysInQuarter = noDaysInQuarter + 1;

                    if (dayInside.QtrId == finishDay.QtrId && day >= originalPeriod.Finish.Date)
                    {
                        originalSplit.Add(dayInside.QtrId, noDaysInQuarter);
                    }
                }
                else
                {
                    // sudah lain quarter
                    originalSplit.Add(prevQuarter, noDaysInQuarter);
                    noDaysInQuarter = 0;
                    noDaysInQuarter = noDaysInQuarter + 1;
                    prevQuarter = dayInside.QtrId;
                }

            }

            return originalSplit;
        }


        public static Dictionary<int, Dictionary<DateRange, double>> NumDaysPerMonth(DateRange originalPeriod)
        {
            Dictionary<int, Dictionary<DateRange, double>> originalSplit = new Dictionary<int, Dictionary<DateRange, double>>();

            DateTime temp = originalPeriod.Start;
            while (Convert.ToInt32(temp.ToString("yyyyMM")) <= Convert.ToInt32(originalPeriod.Finish.ToString("yyyyMM")))
            {
                // do something with target.Month and target.Year

                string tempx = temp.Year.ToString() + temp.Month.ToString("00");
                string startx = originalPeriod.Start.Year.ToString() + originalPeriod.Start.Month.ToString("00");
                string finishx = originalPeriod.Finish.Year.ToString() + originalPeriod.Finish.Month.ToString("00");


                if (temp.Month == originalPeriod.Start.Month && temp.Year == originalPeriod.Start.Year)
                {
                    DateTime last = new DateTime(originalPeriod.Start.Year,
                        originalPeriod.Start.Month,
                        DateTime.DaysInMonth(originalPeriod.Start.Year, originalPeriod.Start.Month));

                    if(originalPeriod.Finish.Date <= last.Date )
                    {
                        last = originalPeriod.Finish;
                    }

                    DateTime start = originalPeriod.Start;

                    string yearmonth = originalPeriod.Start.Year.ToString() + originalPeriod.Start.Month.ToString("00");
                    DateRange dr = new DateRange(start, last);
                    Dictionary<DateRange, double> drx = new Dictionary<DateRange, double>();
                    drx.Add(dr, (last - start).Days + 1);
                    originalSplit.Add(Convert.ToInt32(yearmonth), drx);
                }
                else if (temp.Month == originalPeriod.Finish.Month && temp.Year == originalPeriod.Finish.Year)
                {
                    DateTime start = new DateTime(originalPeriod.Finish.Year,
                        originalPeriod.Finish.Month,
                        1);

                    DateTime finish = originalPeriod.Finish;

                    string yearmonth = originalPeriod.Finish.Year.ToString() + originalPeriod.Finish.Month.ToString("00");
                    DateRange dr = new DateRange(start, finish);


                    Dictionary<DateRange, double> drx = new Dictionary<DateRange, double>();
                    drx.Add(dr, (finish - start).Days + 1);
                    originalSplit.Add(Convert.ToInt32(yearmonth), drx);


                    // originalSplit.Add(Convert.ToInt32(yearmonth), (finish - start).Days + 1);
                }
                else if ((Convert.ToInt32(tempx) > (Convert.ToInt32(startx))) &&
                   (Convert.ToInt32(tempx) < (Convert.ToInt32(finishx)))
                    )
                {
                    DateTime last = new DateTime(temp.Year,
                       temp.Month,
                       DateTime.DaysInMonth(temp.Year, temp.Month));

                    DateTime start = new DateTime(temp.Year,
                       temp.Month, 1);

                    string yearmonth = last.Year.ToString() + last.Month.ToString("00");
                    DateRange dr = new DateRange(start, last);

                    Dictionary<DateRange, double> drx = new Dictionary<DateRange, double>();
                    drx.Add(dr, (last - start).Days + 1);
                    originalSplit.Add(Convert.ToInt32(yearmonth), drx);

                    // originalSplit.Add(Convert.ToInt32(yearmonth), (last - start).Days + 1);

                }


                temp = temp.AddMonths(1);
            }

            return originalSplit;
        }
        public static Dictionary<int, double> ProportionNumDaysPerMonthYear(DateRange originalPeriod, DateRange filterPeriod)
        {
            Dictionary<int, double> filtersplit = new Dictionary<int, double>();
            //int yearStart = filterPeriod.Start.Year;
            //int yearFinish = filterPeriod.Finish.Year;
            DateRange outDrttt = new DateRange();
            DateRange outDrfilterx = new DateRange();
            var ttt = DateRangeToMonth.NumDaysPerMonth(originalPeriod);
            var filterx = DateRangeToMonth.NumDaysPerMonth(filterPeriod);

            double total = ttt.SelectMany(x => x.Value).Sum(x => x.Value);

            foreach (var y in filterx)
            {
                if (ttt.Keys.ToList().Contains(y.Key))
                {

                    var dic = ttt.Where(x => x.Key == y.Key);

                    var value = dic.ToList().SelectMany(x => x.Value).ToList().Sum(o => o.Value);

                    filtersplit.Add(y.Key,
                    Tools.Div(Convert.ToDouble(value), total)
                    );
                }
                else
                {
                    filtersplit.Add(y.Key,
                    0//Tools.Div(y.Value, total)
                    );
                }

            }

            return filtersplit;
        }

        public static Dictionary<int, double> ProportionNumDaysPerYear(DateRange originalDateRage, List<int> years)
        {
            int start = years.Min();
            int finish = years.Max();

            var filter = new DateRange()
            {
                Start = Tools.ToUTC(new DateTime(start, 1, 1)),
                Finish = Tools.ToUTC(new DateTime(finish, 12, 31))
            };

            var ratios = DateRangeToMonth.ProportionNumDaysPerYear(originalDateRage, filter);

            return ratios;
        }

        public static Dictionary<int, double> ProportionNumDaysPerYear(DateRange originalDateRage, int yearstart, int yearfinish)
        {

            var filter = new DateRange()
            {
                Start = Tools.ToUTC(new DateTime(yearstart, 1, 1)),
                Finish = Tools.ToUTC(new DateTime(yearfinish, 12, 31))
            };

            var ratios = DateRangeToMonth.ProportionNumDaysPerYear(originalDateRage, filter);

            return ratios;
        }

        public static Dictionary<int, double> ProportionNumDaysPerMonthYear(DateRange originalDateRage, int yearstart, int yearfinish)
        {

            var filter = new DateRange()
            {
                Start = Tools.ToUTC(new DateTime(yearstart, 1, 1)),
                Finish = Tools.ToUTC(new DateTime(yearfinish, 12, 31))
            };

            var ratios = DateRangeToMonth.ProportionNumDaysPerYear(originalDateRage, filter);

            return ratios;
        }



        public static Dictionary<int, double> GetWeightEachYear(int year, DateRange range, out int numDays, out DateRange dr, bool isRealized = true)
        {

            Dictionary<int, double> result = new Dictionary<int, double>();
            // between 
            double totalduration = (range.Finish - range.Start).Days + 1;
            //double tdur = (range.Finish - range.Start).TotalDays;
            dr = new DateRange();

            var dt = Tools.ToUTC(range.Start);
            var df = Tools.ToUTC(range.Finish);

            if (isRealized)
            {
                // 2
                if (range.Start.Year == year && range.Finish.Year > year)
                {
                    var startdate = range.Start;
                    var lastdate = new DateTime(range.Start.Year, 12, 31);
                    var diff = (lastdate - startdate).Days + 1;
                    numDays = diff;
                    dr.Start = startdate;
                    dr.Finish = lastdate;
                    var pembagi = Tools.Div(diff, totalduration);
                    result.Add(year, Math.Round(pembagi, 2));
                }
                else
                    // 3
                    if (range.Start.Year < year && range.Finish.Year > year)
                    {
                        var startdate = new DateTime(year, 1, 1);
                        var lastdate = new DateTime(year, 12, 31);
                        var diff = (lastdate - startdate).Days + 1;
                        numDays = diff;
                        dr.Start = startdate;
                        dr.Finish = lastdate;
                        var pembagi = Tools.Div(diff, totalduration);
                        result.Add(year, Math.Round(pembagi, 2));
                    }
                    else
                        // 6
                        if (year == range.Finish.Year && year == range.Start.Year)
                        {
                            var startdate = range.Start;
                            var lastdate = range.Finish;
                            var diff = (lastdate - startdate).Days + 1;
                            numDays = diff;
                            dr.Start = startdate;
                            dr.Finish = lastdate;
                            var pembagi = Tools.Div(diff, totalduration);
                            result.Add(year, Math.Round(pembagi, 2));
                        }
                        else
                            // 4
                            if (range.Finish.Year == year && range.Start.Year != year)
                            {
                                var startdate = new DateTime(year, 1, 1);
                                var lastdate = range.Finish;
                                var diff = (lastdate - startdate).Days + 1;
                                numDays = diff;
                                dr.Start = startdate;
                                dr.Finish = lastdate;
                                var pembagi = Tools.Div(diff, totalduration);
                                result.Add(year, Math.Round(pembagi, 2));
                            }

                            else
                            // 1 dan 5
                            //if (year < range.Start.Year || year > range.Finish.Year)
                            {
                                numDays = 0;
                                dr.Start = Tools.DefaultDate;
                                dr.Finish = Tools.DefaultDate;
                                result.Add(year, 0);
                            }
            }
            else
            {
                numDays = 0;
                dr.Start = Tools.DefaultDate;
                dr.Finish = Tools.DefaultDate;
                result.Add(year, 0);
            }
            return result;
        }

        public static DateRange GetRangeInYear(DateRange originalDateRage, int year)
        {
            int yearMin = originalDateRage.Start.Year;
            int yearMax = originalDateRage.Finish.Year;

            if (year < yearMin || year > yearMax)
            {
                return new DateRange();
            }
            else
            {
                int outnum = 0;
                DateRange dr = new DateRange();
                var yyy = GetWeightEachYear(year, originalDateRage, out outnum, out dr);

                return dr;
            }
        }
    }
}
