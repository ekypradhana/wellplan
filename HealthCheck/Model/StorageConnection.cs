using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace HealthCheck.Model
{
    class StorageConnection
    {
        private string StorageMessage { get; set; }
        /// <summary>
        /// DB Size
        /// </summary>
        public double DBSize { get; set; }

        /// <summary>
        /// Physical DB Drive Size
        /// </summary>
        public double TotalDBDriveSize { get; set; }

        /// <summary>
        /// Used Physical DB Drive Size
        /// </summary>
        public double UsedDBDriveSize { get; set; }

        /// <summary>
        /// Remaining DB Drive Size
        /// </summary>
        public double AvailableDBDriveSize { get; set; }


        /// <summary>
        /// DB Size
        /// </summary>
        public double WebAppSize { get; set; }

        /// <summary>
        /// Physical DB Drive Size
        /// </summary>
        public double TotalWebAppDriveSize { get; set; }

        /// <summary>
        /// Used Physical DB Drive Size
        /// </summary>
        public double UsedWebAppDriveSize { get; set; }

        /// <summary>
        /// Remaining DB Drive Size
        /// </summary>
        public double AvailableWebAppDriveSize { get; set; }

        public void CheckingStorage()
        {
            string storageToCheck = ConfigurationSettings.AppSettings["StorageToCheck"].ToString();
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (var d in allDrives)
            {
                if (d.Name.ToLower().Contains(storageToCheck.ToLower()))
                {
                    TotalWebAppDriveSize = d.TotalSize;
                    AvailableWebAppDriveSize = d.TotalFreeSpace;
                    UsedWebAppDriveSize = TotalWebAppDriveSize - AvailableWebAppDriveSize;
                }
            }
        }
        public  string GetStorage(DBConnection DBConn, DateTime? dt = null)//(string host, string db, string port, string username = "", string pass = "")
        {
            var StorageMessage = "";
            try
            {
                CheckingStorage();
                new DataHealth().WriteLog("Checking Storage Database...", dt);
                if (DBConn.UserName == null) DBConn.UserName = "";
                if (DBConn.Password == null) DBConn.Password = "";
                string isNeedAuthentication = ConfigurationSettings.AppSettings["AuthenticationDatabase"].ToString();
                MongoClient client = new MongoClient();
                if (DBConn.UserName != "")
                {
                    //"mongodb://myUserName:myPassword@linus.mongohq.com:myPortNumber/";
                    string conStr = DBConn.UserName + ":" + DBConn.Password + "@" + DBConn.ServerHost;
                    client = new MongoClient("mongodb://" + conStr + ":" + DBConn.Port);
                }
                else
                {
                    string conStr = DBConn.ServerHost;
                    client = new MongoClient("mongodb://" + conStr + ":" + DBConn.Port);
                }

                MongoServer server = client.GetServer();
                MongoDatabase t = server.GetDatabase(DBConn.ServerDB);
                var ComputerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                StorageMessage = "Available Physical Memory : " + ComputerInfo.AvailablePhysicalMemory + " DataBase Size : " + t.GetStats().StorageSize + " Total Physical Memory : " + ComputerInfo.TotalPhysicalMemory + " Total WebApp Drive Storage : " + TotalWebAppDriveSize + " Available WebApp Drive Size : " + AvailableWebAppDriveSize + " Used WebApp Drive Size : " + UsedWebAppDriveSize;
            }
            catch (Exception e)
            {
                new DataHealth().WriteLog(e.Message + " " + (e.InnerException == null ? "" : e.StackTrace.ToString()), dt);
                var ComputerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                //StorageMessage = e.Message + " " + (e.InnerException == null ? "" : e.StackTrace.ToString());
                StorageMessage = "Available Physical Memory : " + ComputerInfo.AvailablePhysicalMemory + " DataBase Size : " + 0 + " Total Physical Memory : " + ComputerInfo.TotalPhysicalMemory + " Total WebApp Drive Storage : " + TotalWebAppDriveSize + " Available WebApp Drive Size : " + AvailableWebAppDriveSize + " Used WebApp Drive Size : " + UsedWebAppDriveSize;
                return StorageMessage;
            }
            return StorageMessage;
        }


    }
}
