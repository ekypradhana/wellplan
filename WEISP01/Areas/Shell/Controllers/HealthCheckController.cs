using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ECIS.Core;
using ECIS.Client.WEIS;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Newtonsoft.Json;
using ECIS.Identity;
using System.Configuration;
using System.IO;
using System.Management;
using Microsoft.Win32;
using Aspose.Cells;


namespace ECIS.AppServer.Areas.Shell.Controllers
{
    public class HealthCheckController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public string MinimalizeStorage(double size)
        {
            var ret = "";
            if (size < 1000)
                ret = Math.Round(size, 2) + "";
            else if (size < 1000000)
                ret = Math.Round(size / 1000, 2).ToString() + "KB";
            else if (size < 1000000000)
                ret = Math.Round(size / 1000000, 2).ToString() + "MB";
            else if (size < 1000000000000)
                ret = Math.Round(size / 1000000000, 2).ToString() + "GB";
            else if (size < 1000000000000000)
                ret = Math.Round(size / 1000000000000, 2).ToString() + "TB";
            return ret;
        }

        public JsonResult GetLatestConfig()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                var ret = new OverviewHealthCheck();
                var fileInfos = new List<FileInfo>();
                var baseDirHealthCheck = ConfigurationManager.AppSettings["HealthCheckDataPath"].ToString();
                foreach (string d in Directory.GetDirectories(baseDirHealthCheck))
                {
                    DirectoryInfo info = new DirectoryInfo(d);
                    FileInfo[] files = info.GetFiles().OrderByDescending(p => p.CreationTime).ToArray();
                    foreach (var x in files)
                    {
                        if (x.Name.Contains(".json"))
                        {
                            fileInfos.Add(x);
                        }
                    }
                }

                if (fileInfos.Any())
                {
                    fileInfos = fileInfos.OrderByDescending(x => x.CreationTime).ToList();
                    var text = System.IO.File.ReadAllText(fileInfos.FirstOrDefault().FullName);//fileInfos.FirstOrDefault().DirectoryName + 
                    var a = JsonConvert.DeserializeObject<DataHealthConfig>(text);
                    ret.isDBUp = a.IsDBUp;
                    ret.isEDMUp = a.isEDMUp;
                    ret.isPROXYUp = a.IsProxyUp;
                    ret.isSMTPUp = a.IsSMTPUp;

                    ret.DBSize = a.StorageInfo.DBSize;
                    ret.DBDrive.Available = a.StorageInfo.AvailableDBDriveSize;
                    ret.DBDrive.Total = a.StorageInfo.TotalDBDriveSize;

                    ret.WebAppSize = a.StorageInfo.WebAppSize;
                    ret.WebAppDrive.Available = a.StorageInfo.AvailableWebAppDriveSize;
                    ret.WebAppDrive.Total = a.StorageInfo.TotalWebAppDriveSize;

                    ret.RAM.Available = a.StorageInfo.AvailableRAM;//Tools.ToDouble(sp2[1].ToLower().Replace("tb", "").Replace("gb", "").Replace("mb", ""));
                    ret.RAM.Total = a.StorageInfo.TotalRAM;

                    ret.RAM.Percentage = Math.Round(Tools.Div(ret.RAM.Total - ret.RAM.Available, ret.RAM.Total) * 100, 0);
                    ret.DBDrive.Percentage = Math.Round(Tools.Div(ret.DBDrive.Total - ret.DBDrive.Available, ret.DBDrive.Total) * 100, 0);
                    ret.WebAppDrive.Percentage = Math.Round(Tools.Div(ret.WebAppDrive.Total - ret.WebAppDrive.Available, ret.WebAppDrive.Total) * 100, 0);


                    //var sm = a.StorageMessage;
                    //if (sm.Contains("DataBase Size"))
                    //{
                    //    var sp1 = sm.Split('\n');
                    //    foreach (var x in sp1)
                    //    {
                    //        var sp2 = x.Split(':');
                    //        if (sp2.Any())
                    //        {
                    //            if (sp2[0].Contains("DataBase Size"))
                    //                ret.DBSize = sp2[1];
                    //            if (sp2[0].Contains("Total WebApp Drive Storage"))
                    //                ret.TotalWebAppDriveStorage = sp2[1];

                    //            var dbl = sp2[1].ToLower().Replace("tb", "").Replace("gb", "").Replace("mb", "");
                    //            var ggg = Tools.ToDouble(dbl);

                    //            if (sp2[0].Contains("Available Physical Memory"))
                    //                ret.PhisicalMemory.Available = ggg;//Tools.ToDouble(sp2[1].ToLower().Replace("tb", "").Replace("gb", "").Replace("mb", ""));
                    //            if (sp2[0].Contains("Total Physical Memory"))
                    //                ret.PhisicalMemory.Total = ggg;

                    //            if (sp2[0].Contains("Available WebApp Drive Size"))
                    //                ret.WebAppDrive.Available = ggg;
                    //            if (sp2[0].Contains("Total WebApp Drive Storage"))
                    //                ret.WebAppDrive.Total = ggg;

                    //            if (sp2[0].Contains("DB Drive Available Size"))
                    //                ret.DBDrive.Available = ggg;
                    //            if (sp2[0].Contains("DB Drive Used Size"))
                    //                ret.DBDrive.Total = ggg;

                    //        }
                    //    }
                    //}

                }


                return ret;
            });
        }

        public JsonResult SaveDBHealth(ECIS.Client.WEIS.DBConnection DBConn)
        {
            return MvcResultInfo.Execute(() =>
            {
                var DataPath = ConfigurationManager.AppSettings["HealthCheckDataPath"].ToString();
                var ConfigPath = DataPath + "config.json";
                DataHealthConfig Config = new DataHealthConfig();
                try
                {
                    var text = System.IO.File.ReadAllText(ConfigPath);
                    Config = JsonConvert.DeserializeObject<DataHealthConfig>(text);
                    //Set DB Config
                    Config.DB_ServerHost = DBConn.ServerHost;
                    Config.DB_ServerDB = DBConn.ServerDB;
                    Config.DB_UserName = DBConn.UserName;
                    Config.DB_Password = DBConn.Password;
                    Config.DB_Port = DBConn.Port.ToString();
                    //Config.DB_UseSSL = DBConn.UseSSL.ToString() ;
                    string config_json = JsonConvert.SerializeObject(Config);
                    System.IO.File.WriteAllText(ConfigPath, config_json);
                }
                catch (Exception e)
                {
                    Config.Error_Message = e.Message;
                    return Tools.PushException(e);
                }

                return "OK";
            });
        }

        public JsonResult SaveProxyHealth(ECIS.Client.WEIS.ProxyConnection ProxyConn)
        {
            return MvcResultInfo.Execute(() =>
            {
                var DataPath = ConfigurationManager.AppSettings["HealthCheckDataPath"].ToString();
                var ConfigPath = DataPath + "config.json";
                DataHealthConfig Config = new DataHealthConfig();
                try
                {
                    var text = System.IO.File.ReadAllText(ConfigPath);
                    Config = JsonConvert.DeserializeObject<DataHealthConfig>(text);
                    //Set Proxy Config
                    Config.Proxy_Host = ProxyConn.Host;
                    Config.Proxy_Port = ProxyConn.Port.ToString();
                    string config_json = JsonConvert.SerializeObject(Config);
                    System.IO.File.WriteAllText(ConfigPath, config_json);
                }
                catch (Exception e)
                {
                    Config.Error_Message = e.Message;
                    return Tools.PushException(e);
                }

                return "OK";
            });
        }

        public JsonResult SaveSMTPHealth(ECIS.Client.WEIS.SMTPConnection SMTPConn)
        {
            return MvcResultInfo.Execute(() =>
            {
                var DataPath = ConfigurationManager.AppSettings["HealthCheckDataPath"].ToString();
                var ConfigPath = DataPath + "config.json";
                DataHealthConfig Config = new DataHealthConfig();
                try
                {
                    var text = System.IO.File.ReadAllText(ConfigPath);
                    Config = JsonConvert.DeserializeObject<DataHealthConfig>(text);
                    //Set SMTP Config

                    Config.SMTP_Host = SMTPConn.Host;
                    Config.SMTP_UserName = SMTPConn.UserName;
                    Config.SMTP_Password = SMTPConn.Password;
                    Config.SMTP_Port = SMTPConn.Port.ToString();
                    Config.SMTP_From = SMTPConn.From;
                    Config.SMTP_To = SMTPConn.To;
                    Config.SMTP_TLS = SMTPConn.TLS.ToString();
                    Config.SMTP_Subject = SMTPConn.Subject;
                    Config.SMTP_Message = SMTPConn.Message;

                    string config_json = JsonConvert.SerializeObject(Config);
                    System.IO.File.WriteAllText(ConfigPath, config_json);
                }
                catch (Exception e)
                {
                    Config.Error_Message = e.Message;
                    return Tools.PushException(e);
                };

                return "OK";
            });
        }

        public void CheckHealth(SysHealthCheck healthCheck)
        {
            SysHealthCheck LogHC = new SysHealthCheck();

            string er = "";

            var DBConn = healthCheck.DBConHealth;
            DBConn.ServerHost = "localhost";
            DBConn.ServerDB = "ecshell";
            DBConn.Port = "27012";
            DBConn.UserName = string.Empty;
            DBConn.Password = string.Empty;
            var CheckDB = ECIS.Client.WEIS.DBConnection.CheckDBConnection(DBConn.ServerHost, DBConn.ServerDB, Convert.ToInt32(DBConn.Port),
                out er, DBConn.UserName, DBConn.Password);
            LogHC.DBConHealth = DBConn;
            LogHC.DBConHealthyFailedMsg = er;
            LogHC.isDBConHealthy = CheckDB;

            var ProxyConn = healthCheck.ProxyConHealth;
            var CheckProxy = ECIS.Client.WEIS.ProxyConnection.CheckProxyConnection(ProxyConn.Host, ProxyConn.Port, out er);
            LogHC.ProxyConHealth = ProxyConn;
            LogHC.isProxyHealthy = CheckProxy;
            LogHC.ProxyConHealthFailedMsg = er;

            var SMTPConn = healthCheck.SMTPConHealth;
            var To = SMTPConn.To.Split(',');
            var CheckSMTP = ECIS.Client.WEIS.SMTPConnection.SendTestEmail(SMTPConn.From, SMTPConn.Subject, SMTPConn.Message, SMTPConn.Host, SMTPConn.Port, SMTPConn.UserName, SMTPConn.Password, SMTPConn.TLS, To, out er);
            LogHC.SMTPConHealth = SMTPConn;
            LogHC.isSMTPHealthy = CheckSMTP;
            LogHC.SMTPConHealthFailedMsg = er;

            LogHC.Save();
        }

        private SysHealthCheck GetHealthCheck(SysHealthCheck healthCheck, bool isCheckHealth)
        {
            ECIS.Client.WEIS.DBConnection DBConHealth;
            ECIS.Client.WEIS.SMTPConnection SMTPConHealth;
            ECIS.Client.WEIS.ProxyConnection ProxyConHealth;

            if (healthCheck == null)
            {
                DBConHealth = DataHelper.Get<ECIS.Client.WEIS.DBConnection>("SharedConfigTable", Query.EQ("_id", "WEISDBDefault")) ?? new ECIS.Client.WEIS.DBConnection();
                SMTPConHealth = DataHelper.Get<ECIS.Client.WEIS.SMTPConnection>("SharedConfigTable", Query.EQ("_id", "WEISSMTPDefault")) ?? new ECIS.Client.WEIS.SMTPConnection();
                ProxyConHealth = DataHelper.Get<ECIS.Client.WEIS.ProxyConnection>("SharedConfigTable", Query.EQ("_id", "WEISProxyDefault")) ?? new ECIS.Client.WEIS.ProxyConnection();
            }
            else
            {
                DBConHealth = healthCheck.DBConHealth;
                SMTPConHealth = healthCheck.SMTPConHealth;
                ProxyConHealth = healthCheck.ProxyConHealth;
            }

            if (isCheckHealth)
                CheckHealth(new SysHealthCheck() { DBConHealth = DBConHealth, SMTPConHealth = SMTPConHealth, ProxyConHealth = ProxyConHealth });

            var latestCheckHealth = DataHelper.Populate<SysHealthCheck>(new SysHealthCheck().TableName, sort: SortBy.Descending("LastUpdate"), take: 1)
                .FirstOrDefault() ?? new SysHealthCheck();

            latestCheckHealth.DBConHealth = DBConHealth;
            latestCheckHealth.ProxyConHealth = ProxyConHealth;
            latestCheckHealth.SMTPConHealth = SMTPConHealth;

            return latestCheckHealth;
        }

        private Dictionary<string, List<HistoricalPingData>> GetHistoricalData()
        {
            var date = DateTime.Now;
            var queryForPings = Query.And(
                Query.GTE("LastUpdate", new DateTime(date.Year, date.Month, 1)),
                Query.LT("LastUpdate", new DateTime(date.Year, date.Month + 1, 1))
            );
            var pings = DataHelper.Populate<SysHealthCheck>(new SysHealthCheck().TableName, q: queryForPings);

            var pingsOfDB = pings.Select(d => new
            {
                Date = d.LastUpdate,
                IsHealthy = d.isDBConHealthy
            })
            .GroupBy(d => new DateTime(d.Date.Year, d.Date.Month, d.Date.Day))
            .Select(d => new HistoricalPingData
            {
                Date = d.Key,
                Up = d.Where(e => e.IsHealthy).Count(),
                Down = d.Where(e => !e.IsHealthy).Count(),
                Total = d.Count()
            })
            .ToList();

            var pingsOfSMTP = pings.Select(d => new
            {
                Date = d.LastUpdate,
                IsHealthy = d.isSMTPHealthy
            })
            .GroupBy(d => new DateTime(d.Date.Year, d.Date.Month, d.Date.Day))
            .Select(d => new HistoricalPingData
            {
                Date = d.Key,
                Up = d.Where(e => e.IsHealthy).Count(),
                Down = d.Where(e => !e.IsHealthy).Count(),
                Total = d.Count()
            })
            .ToList();

            var pingsOfProxy = pings.Select(d => new
            {
                Date = d.LastUpdate,
                IsHealthy = d.isProxyHealthy
            })
            .GroupBy(d => new DateTime(d.Date.Year, d.Date.Month, d.Date.Day))
            .Select(d => new HistoricalPingData
            {
                Date = d.Key,
                Up = d.Where(e => e.IsHealthy).Count(),
                Down = d.Where(e => !e.IsHealthy).Count(),
                Total = d.Count()
            })
            .ToList();

            for (var i = 0; i < 5; i++)
            {
                var emptyHistoricalData = new HistoricalPingData() { Date = date.AddDays(-1 * (i + 1)) };

                if (pingsOfDB.Count() < 5)
                    pingsOfDB.Insert(0, emptyHistoricalData);
                if (pingsOfProxy.Count() < 5)
                    pingsOfProxy.Insert(0, emptyHistoricalData);
                if (pingsOfSMTP.Count() < 5)
                    pingsOfSMTP.Insert(0, emptyHistoricalData);
            }

            var res = new Dictionary<string, List<HistoricalPingData>>();
            res.Add("database", pingsOfDB);
            res.Add("proxy", pingsOfProxy);
            res.Add("smtp", pingsOfSMTP);

            return res;
        }

        public JsonResult RefreshData(SysHealthCheck healthCheck, bool isCheckHealth, DateTime StartDate, DateTime EndDate)
        {
            return MvcResultInfo.Execute(() =>
            {
                var Now = Tools.ToUTC(DateTime.Now);
                StartDate = Tools.ToUTC(StartDate, true);
                EndDate = Tools.ToUTC(EndDate, true);
                var DataPath = ConfigurationManager.AppSettings["HealthCheckDataPath"].ToString();
                var ConfigPath = DataPath + "config.json";

                var latestCheckHealth = new SysHealthCheck();
                DataHealthConfig Config = new DataHealthConfig();
                try
                {
                    var text = System.IO.File.ReadAllText(ConfigPath);
                    Config = JsonConvert.DeserializeObject<DataHealthConfig>(text);

                    //Set DB Config
                    latestCheckHealth.DBConHealth.ServerHost = Config.DB_ServerHost;
                    latestCheckHealth.DBConHealth.ServerDB = Config.DB_ServerDB;
                    latestCheckHealth.DBConHealth.UserName = Config.DB_UserName;
                    latestCheckHealth.DBConHealth.Password = Config.DB_Password;
                    latestCheckHealth.DBConHealth.Port = Config.DB_Port;
                    //latestCheckHealth.DBConHealth.UseSSL = Convert.ToBoolean(Config.DB_UseSSL);

                    //Set SMTP Config
                    latestCheckHealth.SMTPConHealth.Host = Config.SMTP_Host;
                    latestCheckHealth.SMTPConHealth.UserName = Config.SMTP_UserName;
                    latestCheckHealth.SMTPConHealth.Password = Config.SMTP_Password;
                    latestCheckHealth.SMTPConHealth.Port = Convert.ToInt32(Config.SMTP_Port);
                    latestCheckHealth.SMTPConHealth.From = Config.SMTP_From;
                    latestCheckHealth.SMTPConHealth.To = Config.SMTP_To;
                    latestCheckHealth.SMTPConHealth.TLS = Convert.ToBoolean(Config.SMTP_TLS);
                    latestCheckHealth.SMTPConHealth.Subject = Config.SMTP_Subject;
                    latestCheckHealth.SMTPConHealth.Message = Config.SMTP_Message;

                    //Set Proxy Config
                    latestCheckHealth.ProxyConHealth.Host = Config.Proxy_Host;
                    latestCheckHealth.ProxyConHealth.Port = Convert.ToInt32(Config.Proxy_Port);

                    //Set Storage Config
                    latestCheckHealth.StorageInfo.DBSize = Config.StorageInfo.DBSize;
                    latestCheckHealth.StorageInfo.TotalPhysicalMemory = Config.StorageInfo.TotalDBDriveSize;
                    latestCheckHealth.StorageInfo.AvailablePhysicalMemory = Config.StorageInfo.AvailableDBDriveSize;

                    //Set EDM Config
                    latestCheckHealth.EDMConHealth.OraServerShell = Config.EDMConnectionString;

                }
                catch (Exception e)
                {
                    Config.Error_Message = e.Message;
                    return Tools.PushException(e);
                }

                #region Get Historical Data
                var historicalData = new List<DataHealthConfig>();
                var selectedDate = new List<string>();
                for (var i = StartDate; i <= EndDate; i = i.AddDays(1))
                {
                    selectedDate.Add(i.ToString("yyyyMMdd"));
                }
                string latest_dir = Directory.GetDirectories(DataPath).Where(x => Now.ToString("yyyyMMdd") == x.Substring(x.Length - 8, 8)).FirstOrDefault();
                if (latest_dir.Length > 0)
                {
                    string[] file_list = Directory.GetFiles(latest_dir).Where(x => x.Contains(".json")).ToArray();
                    if (file_list.Length > 0)
                    {
                        var latest_file = file_list[file_list.Length - 1];
                        try
                        {
                            string text = System.IO.File.ReadAllText(latest_file);
                            var latest_data = JsonConvert.DeserializeObject<DataHealthConfig>(text);
                            latestCheckHealth.isDBConHealthy = latest_data.IsDBUp;// == 1 ? true : false;
                            latestCheckHealth.DBConHealthyFailedMsg = latest_data.DBMessage;
                            latestCheckHealth.isProxyHealthy = latest_data.IsProxyUp;// == 1 ? true : false;
                            latestCheckHealth.ProxyConHealthFailedMsg = latest_data.ProxyMessage;
                            latestCheckHealth.isSMTPHealthy = latest_data.IsSMTPUp;// == 1 ? true : false;
                            latestCheckHealth.SMTPConHealthFailedMsg = latest_data.SMTPMessage;
                            latestCheckHealth.LastUpdate = Tools.ToUTC(new DateTime(Now.Year, Now.Month, Now.Day, Now.Hour, 0, 0));
                        }
                        catch (Exception e)
                        {
                            return Tools.PushException(e);
                        }
                    }

                }
                string[] data_list = Directory.GetDirectories(DataPath).Where(x => selectedDate.Contains(x.Substring(x.Length - 8, 8))).ToArray();
                if (data_list.Length > 0)
                {
                    foreach (var d in data_list)
                    {
                        string[] file_list = Directory.GetFiles(d).Where(x => x.Contains(".json")).ToArray();
                        if (file_list.Length > 0)
                        {
                            foreach (var f in file_list)
                            {
                                string text = "";
                                try
                                {
                                    text = System.IO.File.ReadAllText(f);
                                    var data = JsonConvert.DeserializeObject<DataHealthConfig>(text);
                                    data.Day = new DateTime(data.Date.Year, data.Date.Month, data.Date.Day);
                                    data.Series = new Dictionary<string, double>();
                                    data.Series.Add("database" + data.Date.Hour, data.IsDBUp ? 1 : 1.001);
                                    data.Series.Add("proxy" + data.Date.Hour, data.IsProxyUp ? 1 : 1.001);
                                    data.Series.Add("smtp" + data.Date.Hour, data.IsSMTPUp ? 1 : 1.001);
                                    data.Series.Add("storage" + data.Date.Hour, 1);
                                    data.Series.Add("edm" + data.Date.Hour, data.isEDMUp ? 1 : 1.001);
                                    var storageMessage = " WebApp Folder Size: " + MinimalizeStorage(data.StorageInfo.WebAppSize)
                                    + " \nWebApp Drive Available: " + MinimalizeStorage(data.StorageInfo.AvailableWebAppDriveSize)
                                    + " \nWebApp Drive Used: " + MinimalizeStorage(data.StorageInfo.UsedWebAppDriveSize)
                                    + " \nWebApp Drive Total: " + MinimalizeStorage(data.StorageInfo.TotalWebAppDriveSize)
                                    + " Database Size: " + MinimalizeStorage(data.StorageInfo.DBSize)
                                    + " \nDatabase Drive Available: " + MinimalizeStorage(data.StorageInfo.AvailableDBDriveSize)
                                    + " \nDatabase Drive Used: " + MinimalizeStorage(data.StorageInfo.UsedDBDriveSize)
                                    + " \nDatabase Drive Total: " + MinimalizeStorage(data.StorageInfo.TotalDBDriveSize)
                                    + " \nRAM Available: " + MinimalizeStorage(data.StorageInfo.AvailableRAM)
                                    + " \nRAM Used: " + MinimalizeStorage(data.StorageInfo.UsedRAM)
                                    + " \nRAM Total: " + MinimalizeStorage(data.StorageInfo.TotalRAM);
                                    data.StorageMessage = storageMessage;
                                    historicalData.Add(data);
                                }
                                catch (Exception e)
                                {
                                    return Tools.PushException(e);
                                }
                            }
                        }

                    }
                }

                #endregion

                var result = new
                {
                    healthCheck = latestCheckHealth,
                    historicalData = historicalData,
                    lastPing = new
                    {
                        database = latestCheckHealth.LastUpdate,
                        smtp = latestCheckHealth.LastUpdate,
                        proxy = latestCheckHealth.LastUpdate,
                    },
                };
                return result;
            });
        }

        public JsonResult RunBatchWithBasicConfig()
        {
            return MvcResultInfo.Execute(null, document =>
            {
                //var batchLocation = ConfigurationManager.AppSettings["BatchHealthCheckLocation"].ToString();
                //System.Diagnostics.Process.Start("CMD.exe", batchLocation);
                var temp = DataHelper.Populate("WEISEmailTemplates", Query.EQ("_id", "HealthCheckDaily"));
                if (!temp.Any())
                {
                    BsonDocument s = new BsonDocument();
                    s.Set("_id", "HealthCheckDaily");
                    s.Set("LastUpdate", DateTime.Now);
                    s.Set("Title", "Report Health Check");
                    s.Set("Subject", "Report Health Check On " + DateTime.Now.ToString("dd-MMM-yyyy"));
                    s.Set("Body", "Please find attachements to check daily actual data");
                    s.Set("SMTPConfig", "Default");
                    DataHelper.Save("WEISEmailTemplates", s);
                }
                DataHealth data = new DataHealth();
                List<string> emails = new List<string>();
                emails .Add(
                WebTools.LoginUser.Email
                    );
                emails.Add("arfan.pantua@eaciit.com");

                data.GenData(DateTime.Now, true, emails);
                return null;
            });
        }

        public JsonResult RefreshCustom(SysHealthCheck healthCheck)
        {
            return MvcResultInfo.Execute(() =>
            {
                DataHealth check = new DataHealth();
                check.GenLastData(DateTime.Now, healthCheck);

                ////////////////////////////////////
                var Now = Tools.ToUTC(DateTime.Now);
                var DataPath = ConfigurationManager.AppSettings["HealthCheckDataPath"].ToString();
                var ConfigPath = DataPath + "customConfig.json";

                //var latestCheckHealth = GetHealthCheck(healthCheck, isCheckHealth);
                var latestCheckHealth = new SysHealthCheck();
                DataHealthConfig Config = new DataHealthConfig();
                try
                {
                    var text = System.IO.File.ReadAllText(ConfigPath);
                    Config = JsonConvert.DeserializeObject<DataHealthConfig>(text);
                    //Set DB Config
                    latestCheckHealth.DBConHealth.ServerHost = Config.DB_ServerHost;
                    latestCheckHealth.DBConHealth.ServerDB = Config.DB_ServerDB;
                    latestCheckHealth.DBConHealth.UserName = Config.DB_UserName;
                    latestCheckHealth.DBConHealth.Password = Config.DB_Password;
                    latestCheckHealth.DBConHealth.Port = Config.DB_Port;
                    //latestCheckHealth.DBConHealth.UseSSL = Convert.ToBoolean(Config.DB_UseSSL);

                    //Set SMTP Config
                    latestCheckHealth.SMTPConHealth.Host = Config.SMTP_Host;
                    latestCheckHealth.SMTPConHealth.UserName = Config.SMTP_UserName;
                    latestCheckHealth.SMTPConHealth.Password = Config.SMTP_Password;
                    latestCheckHealth.SMTPConHealth.Port = Config.SMTP_Port == "" ? 0 : Convert.ToInt32(Config.SMTP_Port);
                    latestCheckHealth.SMTPConHealth.From = Config.SMTP_From;
                    latestCheckHealth.SMTPConHealth.To = Config.SMTP_To;
                    latestCheckHealth.SMTPConHealth.TLS = Convert.ToBoolean(Config.SMTP_TLS);
                    latestCheckHealth.SMTPConHealth.Subject = Config.SMTP_Subject;
                    latestCheckHealth.SMTPConHealth.Message = Config.SMTP_Message;

                    //Set Proxy Config
                    latestCheckHealth.ProxyConHealth.Host = Config.Proxy_Host;
                    latestCheckHealth.ProxyConHealth.Port = Config.Proxy_Port == "" ? 0 : Convert.ToInt32(Config.Proxy_Port);

                    //Set Storage Config
                    latestCheckHealth.StorageInfo.DBSize = Config.StorageInfo.DBSize;
                    latestCheckHealth.StorageInfo.TotalPhysicalMemory = Config.StorageInfo.TotalDBDriveSize;
                    latestCheckHealth.StorageInfo.AvailablePhysicalMemory = Config.StorageInfo.AvailableDBDriveSize;

                    //Set EDM Config
                    latestCheckHealth.EDMConHealth.OraServerShell = Config.EDMConnectionString;

                }
                catch (Exception e)
                {
                    Config.Error_Message = e.Message;
                    return Tools.PushException(e);
                }

                #region Get Historical Data
                var historicalData = new List<DataHealthConfig>();
                var selectedDate = new List<string>();
                for (var i = Now; i <= Now; i = i.AddDays(1))
                {
                    selectedDate.Add(i.ToString("yyyyMMddHH"));
                }
                string latest_dir2 = Directory.GetDirectories(DataPath).FirstOrDefault();
                string latest_dir = Directory.GetDirectories(DataPath).Where(x => Now.ToString("yyyyMMddHH") == x.Substring(x.Length - 10, 10)).FirstOrDefault();
                if (latest_dir != null)
                {
                    string[] file_list = Directory.GetFiles(latest_dir);
                    if (file_list.Length > 0)
                    {
                        var latest_file = file_list[file_list.Length - 1];
                        try
                        {
                            string text = System.IO.File.ReadAllText(latest_file);
                            var latest_data = JsonConvert.DeserializeObject<DataHealthConfig>(text);
                            latestCheckHealth.isDBConHealthy = latest_data.IsDBUp;// == 1 ? true : false;
                            latestCheckHealth.DBConHealthyFailedMsg = latest_data.DBMessage;
                            latestCheckHealth.isProxyHealthy = latest_data.IsProxyUp;// == 1 ? true : false;
                            latestCheckHealth.ProxyConHealthFailedMsg = latest_data.ProxyMessage;
                            latestCheckHealth.isSMTPHealthy = latest_data.IsSMTPUp;// == 1 ? true : false;
                            latestCheckHealth.SMTPConHealthFailedMsg = latest_data.SMTPMessage;
                            latestCheckHealth.isEDMHealthy = latest_data.isEDMUp;// == 1 ? true : false;
                            latestCheckHealth.EDMConHealthFailedMsg = latest_data.EDMMessage;
                            latestCheckHealth.LastUpdate = Tools.ToUTC(new DateTime(Now.Year, Now.Month, Now.Day, Now.Hour, 0, 0));
                        }
                        catch (Exception e)
                        {
                            return Tools.PushException(e);
                        }
                    }

                }
                //string[] data_list = Directory.GetDirectories(DataPath).Where(x => selectedDate.Contains(x.Substring(x.Length - 10, 10))).ToArray();
                string[] data_list = Directory.GetDirectories(DataPath).Where(x => Now.ToString("yyyyMMddHH").Equals(x.Substring(x.Length - 10, 10))).ToArray();
                if (data_list.Length > 0)
                {
                    string[] file_list = Directory.GetFiles(data_list.FirstOrDefault());
                    foreach (var f in file_list)
                    {
                        string text = "";
                        try
                        {
                            text = System.IO.File.ReadAllText(f);
                            var data = JsonConvert.DeserializeObject<DataHealthConfig>(text);
                            data.Day = new DateTime(data.Date.Year, data.Date.Month, data.Date.Day);
                            data.Series = new Dictionary<string, double>();
                            data.Series.Add("database" + data.Date.Hour, data.IsDBUp ? 1 : 1.001);
                            data.Series.Add("proxy" + data.Date.Hour, data.IsProxyUp ? 1 : 1.001);
                            data.Series.Add("smtp" + data.Date.Hour, data.IsSMTPUp ? 1 : 1.001);
                            data.Series.Add("storage" + data.Date.Hour, 1);
                            data.Series.Add("edm" + data.Date.Hour, data.isEDMUp ? 1 : 1.001);
                            var storageMessage = " WebApp Folder Size: " + MinimalizeStorage(data.StorageInfo.WebAppSize)
                                + " \nWebApp Drive Available: " + MinimalizeStorage(data.StorageInfo.AvailableWebAppDriveSize)
                                + " \nWebApp Drive Used: " + MinimalizeStorage(data.StorageInfo.UsedWebAppDriveSize)
                                + " \nWebApp Drive Total: " + MinimalizeStorage(data.StorageInfo.TotalWebAppDriveSize)
                                + " Database Size: " + MinimalizeStorage(data.StorageInfo.DBSize)
                                + " \nDatabase Drive Available: " + MinimalizeStorage(data.StorageInfo.AvailableDBDriveSize)
                                + " \nDatabase Drive Used: " + MinimalizeStorage(data.StorageInfo.UsedDBDriveSize)
                                + " \nDatabase Drive Total: " + MinimalizeStorage(data.StorageInfo.TotalDBDriveSize)
                                + " \nRAM Available: " + MinimalizeStorage(data.StorageInfo.AvailableRAM)
                                + " \nRAM Used: " + MinimalizeStorage(data.StorageInfo.UsedRAM)
                                + " \nRAM Total: " + MinimalizeStorage(data.StorageInfo.TotalRAM);
                            data.StorageMessage = storageMessage;
                            historicalData.Add(data);
                        }
                        catch (Exception e)
                        {
                            return Tools.PushException(e);
                        }
                    }
                }

                #endregion

                var result = new
                {
                    healthCheck = latestCheckHealth,
                    historicalData = historicalData,
                    lastPing = new
                    {
                        database = latestCheckHealth.LastUpdate,
                        smtp = latestCheckHealth.LastUpdate,
                        proxy = latestCheckHealth.LastUpdate,
                    },
                };
                return result;
            });
        }

        //public JsonResult CreateReport()//
        //{
        //    string xlst = Path.Combine(Server.MapPath("~/App_Data/Temp"), "DataBrowserExportTemplate.xlsx");
        //    #region Get Historical Data
        //    var DataPath = ConfigurationManager.AppSettings["HealthCheckDataPath"].ToString();
        //    var historicalData = new List<HistoricalData>();
        //    var PopulateHours = new List<string>();
        //    var Now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
        //    for (var i = Now; i <= new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 24, 0, 0); i = i.AddHours(1))
        //    {
        //        PopulateHours.Add(i.ToString("yyyyMMddHH"));
        //    }
        //    //string latest_dir2 = Directory.GetDirectories(DataPath).FirstOrDefault();
        //    //string latest_dir = Directory.GetDirectories(DataPath).Where(x => Now.ToString("yyyyMMddHH") == x.Substring(x.Length - 10, 10)).FirstOrDefault();
        //    //if (latest_dir != null)
        //    //{
        //    //    string[] file_list = Directory.GetFiles(latest_dir);
        //    //    if (file_list.Length > 0)
        //    //    {
        //    //        var latest_file = file_list[file_list.Length - 1];
        //    //        try
        //    //        {
        //    //            string text = System.IO.File.ReadAllText(latest_file);
        //    //            var latest_data = JsonConvert.DeserializeObject<HistoricalData>(text);
        //    //            latestCheckHealth.isDBConHealthy = latest_data.DBStatus == 1 ? true : false;
        //    //            latestCheckHealth.DBConHealthyFailedMsg = latest_data.DBMessage;
        //    //            latestCheckHealth.isProxyHealthy = latest_data.ProxyStatus == 1 ? true : false;
        //    //            latestCheckHealth.ProxyConHealthFailedMsg = latest_data.ProxyMessage;
        //    //            latestCheckHealth.isSMTPHealthy = latest_data.SMTPStatus == 1 ? true : false;
        //    //            latestCheckHealth.SMTPConHealthFailedMsg = latest_data.SMTPMessage;
        //    //            latestCheckHealth.isEDMHealthy = latest_data.EDMStatus == 1 ? true : false;
        //    //            latestCheckHealth.EDMConHealthFailedMsg = latest_data.EDMMessage;
        //    //            latestCheckHealth.LastUpdate = Tools.ToUTC(new DateTime(Now.Year, Now.Month, Now.Day, Now.Hour, 0, 0));
        //    //        }
        //    //        catch (Exception e)
        //    //        {
        //    //            return Tools.PushException(e);
        //    //        }
        //    //    }

        //    //}
        //    string[] data_list = Directory.GetDirectories(DataPath).Where(x => PopulateHours.Contains(x.Substring(x.Length - 10, 10))).ToArray();
        //    if (data_list.Length > 0)
        //    {
        //        foreach (var d in data_list)
        //        {
        //            string[] file_list = Directory.GetFiles(d);
        //            foreach (var f in file_list)
        //            {
        //                string text = "";
        //                try
        //                {
        //                    text = System.IO.File.ReadAllText(f);
        //                    var data = JsonConvert.DeserializeObject<HistoricalData>(text);
        //                    data.Day = new DateTime(data.Date.Year, data.Date.Month, data.Date.Day);
        //                    data.Series = new Dictionary<string, double>();
        //                    data.Series.Add("database" + data.Date.Hour, data.DBStatus == 0 ? 1.0000000000001 : data.DBStatus);
        //                    data.Series.Add("proxy" + data.Date.Hour, data.ProxyStatus == 0 ? 1.0000000000001 : data.ProxyStatus);
        //                    data.Series.Add("smtp" + data.Date.Hour, data.SMTPStatus == 0 ? 1.0000000000001 : data.SMTPStatus);
        //                    data.Series.Add("storage" + data.Date.Hour, data.DBStatus == 0 ? 1.0000000000001 : 2);
        //                    data.Series.Add("edm" + data.Date.Hour, data.EDMStatus == 0 ? 1.0000000000001 : data.EDMStatus);
        //                    historicalData.Add(data);
        //                }
        //                catch (Exception e)
        //                {
        //                    return Tools.PushException(e);
        //                }
        //            }
        //        }
        //    }

        //    #endregion
        //    //List<WellActivity> wellactiv = new List<WellActivity>();
        //    //if (ID != null)
        //    //{
        //    //    foreach (var id in ID)
        //    //    {
        //    //        WellActivity well = GetWellActivity(wbs, id, OPs, opRelation);
        //    //        wellactiv.Add(well);

        //    //    }
        //    //}

        //    Workbook wb = new Workbook(xlst);
        //    //var ws = wb.Worksheets[0];
        //    //int startRow = 3;
        //    //var PrevDays = 0.0; var PrevCost = 0.0; var PlanDays = 0.0; var PlanCost = 0.0;
        //    //var OpDays = 0.0; var OpCost = 0.0; var LeDays = 0.0; var LeCost = 0.0;
        //    //var AfeDays = 0.0; var AfeCost = 0.0; var TqDays = 0.0; var TqCost = 0.0;
        //    //foreach (var well in wellactiv)
        //    //{
        //    //    if (well.Phases != null)
        //    //    {

        //    //        foreach (var phs in well.Phases)
        //    //        {
        //    //            ws.Cells["A" + startRow.ToString()].Value = well._id;
        //    //            ws.Cells["B" + startRow.ToString()].Value = well.RigName;
        //    //            ws.Cells["C" + startRow.ToString()].Value = well.WellName;
        //    //            ws.Cells["D" + startRow.ToString()].Value = well.UARigSequenceId;


        //    //            ws.Cells["E" + startRow.ToString()].Value = phs.ActivityType;
        //    //            ws.Cells["F" + startRow.ToString()].Value = ConvertBaseOP(phs.BaseOP);

        //    //            ws.Cells["G" + startRow.ToString()].Value = phs.PreviousOPSchedule.Start.ToString("dd-MMM-yyyy");
        //    //            ws.Cells["H" + startRow.ToString()].Value = phs.PreviousOPSchedule.Finish.ToString("dd-MMM-yyyy");
        //    //            ws.Cells["I" + startRow.ToString()].Value = phs.PreviousOP.Days;
        //    //            ws.Cells["J" + startRow.ToString()].Value = phs.PreviousOP.Cost;
        //    //            PrevDays += phs.PreviousOP.Days; PrevCost += phs.PreviousOP.Cost;


        //    //            startRow++;
        //    //        }

        //    //    }


        //    //}

        //    var newFileNameSingle = Path.Combine(Server.MapPath("~/App_Data/Temp"),
        //             String.Format("DataBrowser-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss")));

        //    string returnName = String.Format("DataBrowser-{0}.xlsx", Tools.ToUTC(DateTime.Now).ToString("dd-MMM-yyyy-hhmmss"));

        //    wb.Save(newFileNameSingle, Aspose.Cells.SaveFormat.Xlsx);

        //    return Json(new { Success = true, Path = returnName }, JsonRequestBehavior.AllowGet);
        //}
    }

    public class HistoricalPingData
    {
        public DateTime Date { get; set; }
        public int Up { get; set; }
        public int Down { get; set; }
        public int Total { get; set; }
    }

    //public class HistoricalData
    //{
    //    public string Id{get; set;}
    //    public DateTime Date { get; set; }
    //    public int DBStatus { get; set; }
    //    public string DBMessage { get; set; }
    //    public int ProxyStatus { get; set; }
    //    public string ProxyMessage { get; set; }
    //    public int SMTPStatus { get; set; }
    //    public string SMTPMessage { get; set; }       
    //    public string StorageMessage { get; set; }
    //    public int EDMStatus { get; set; }
    //    public string EDMMessage { get; set; }

    //    public Dictionary<string,double> Series { get; set; }
    //    public DateTime Day { get; set; }
    //}

    //class DataHealthConfig
    //{
    //    //DB Config
    //    public string DB_ServerHost { get; set; }
    //    public string DB_ServerDB { get; set; }
    //    public string DB_Port { get; set; }
    //    public string DB_UserName { get; set; }
    //    public string DB_Password { get; set; }
    //    public string DB_UseSSL { get; set; }

    //    //Proxy Config
    //    public string Proxy_Host { get; set; }
    //    public string Proxy_Port { get; set; }

    //    //SMTP Config
    //    public string SMTP_Host { get; set; }
    //    public string SMTP_UserName { get; set; }
    //    public string SMTP_Password { get; set; }
    //    public string SMTP_From { get; set; }
    //    public string SMTP_To { get; set; }
    //    public string SMTP_Subject { get; set; }
    //    public string SMTP_Port { get; set; }
    //    public string SMTP_TLS { get; set; }
    //    public string SMTP_Message { get; set; }

    //    //Storage Config

    //    private string StorageMessage { get; set; }        
    //    public double DBSize { get; set; }
    //    public double TotalDBDriveSize { get; set; }
    //    public double UsedDBDriveSize { get; set; }
    //    public double AvailableDBDriveSize { get; set; }

    //    //EDM Config
    //    public string OraServerShell { get; set; }

    //    //Error Message
    //    public string Error_Message { get; set; }
    //}
}