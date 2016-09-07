using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECIS.Client.WEIS;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace MonthlyReportScheduler
{
    class Program
    {
        static void Main(string[] args)
        {
            var lcc = ConfigurationSettings.AppSettings["License"].ToString();
            ECIS.Core.License.ConfigFolder = lcc;//System.IO.Directory.GetCurrentDirectory() + @"\License";
            ECIS.Core.License.LoadLicense();

            DateTime Now = Tools.ToUTC(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1), true);
            var lastUpdateVersion = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(q: null, take: 1, sort: SortBy.Descending("UpdateVersion"));

            if (lastUpdateVersion.Any())
            {
                var LastUpdate = lastUpdateVersion.FirstOrDefault().UpdateVersion;
                while (LastUpdate < Now)
                {
                    #region New Log
                    var DataPath = ConfigurationSettings.AppSettings["Log"].ToString();
                    string filename = "Log-MonthlyReport" + String.Format("{0:yyyyMMdd}", LastUpdate) + ".txt";

                    if (!Directory.Exists(DataPath))
                        Directory.CreateDirectory(DataPath);

                    if (!File.Exists(DataPath + filename))
                    {
                        using (FileStream fs = File.Create(DataPath + filename))
                        {
                            Byte[] info = new UTF8Encoding(true).GetBytes("");
                            fs.Write(info, 0, info.Length);
                        }
                    }

                    var log = new Log()
                    {
                        LogPath = filename
                    };
                    #endregion


                    log.WriteLog("====================== BATCH MONTHLY REPORT ======================");
                    var prevMonthlyReports = WellActivityUpdateMonthly.Populate<WellActivityUpdateMonthly>(Query.EQ("UpdateVersion", LastUpdate));
                    log.WriteLog(String.Format("Found {0} Monthly Document. On ", prevMonthlyReports.Count()));

                    foreach (var xx in prevMonthlyReports)
                    {
                        log.WriteLog(String.Format("Update Info: \n" +
                            "WellName      : {0}\n" +
                            "Rig Name      : {2}\n" +
                            "Activity Type : {1}\n"
                        , xx.WellName, xx.Phase.ActivityType, xx.RigName));

                        xx.UpdateVersion = LastUpdate.AddMonths(1);
                        xx._id = String.Format("W{0}S{1}A{2}D{3:yyyyMMdd}",
                        xx.WellName.Replace(" ", "").Replace("-", ""),
                        xx.SequenceId,
                        xx.Phase.ActivityType.Replace(" ", "").Replace("-", ""),
                        xx.UpdateVersion);
                        DataHelper.Save(xx.TableName, xx.ToBsonDocument());
                    }
                    log.WriteLog(String.Format("{0} Monthly Document Updated!.", prevMonthlyReports.Count()));

                    LastUpdate = LastUpdate.AddMonths(1);
                    log.WriteLog("====================== DONE ======================");
                }
            }
            else
            {
                Console.WriteLine("====================== No Data in Monthly Report ======================");
            }

        }
    }

    class Log
    {
        public string LogPath { get; set; }
        public void WriteLog(string Text = null)
        {
            var Now = DateTime.Now;
            var ST = String.Format("{0} : {1}", String.Format("{0:dd-MMM-yyyy  HH:mm:ss}", Now), Text);

            Console.WriteLine(ST);
            File.AppendAllText(LogPath, ST + Environment.NewLine);
        }
    }
}
