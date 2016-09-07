using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.Management;

namespace ECIS.Client.WEIS
{
    public class StorageConnection
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
        /// WebAPP Size FOLDER
        /// </summary>
        public double WebAppSize { get; set; }

        /// <summary>
        /// Physical WebAPP Drive Size
        /// </summary>
        public double TotalWebAppDriveSize { get; set; }

        /// <summary>
        /// Used Physical WebAPP Drive Size
        /// </summary>
        public double UsedWebAppDriveSize { get; set; }

        /// <summary>
        /// Remaining WebAPP Drive Size
        /// </summary>
        public double AvailableWebAppDriveSize { get; set; }


        /// <summary>
        /// TOTAL RAM
        /// </summary>
        public double TotalRAM { get; set; }

        /// <summary>
        /// USED RAM
        /// </summary>
        public double UsedRAM { get; set; }

        /// <summary>
        /// AVAILABLE RAM
        /// </summary>
        public double AvailableRAM { get; set; }


        public double TotalPhysicalMemory { get; set; }
        public double AvailablePhysicalMemory { get; set; }

        public void CheckingStorage()
        {
            string storageToCheck = ConfigurationManager.AppSettings["StorageToCheck"].ToString();
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            
            foreach (var d in allDrives)
            {
                if (d.Name.ToLower().Contains(storageToCheck.ToLower()))
                {
                    TotalWebAppDriveSize = d.TotalSize;
                    AvailableWebAppDriveSize = d.TotalFreeSpace;
                    UsedWebAppDriveSize = d.TotalSize - d.TotalFreeSpace; //TotalWebAppDriveSize - AvailableWebAppDriveSize;
                }
            }
        }

        public void CheckingStorageDB()
        {
            string storageToCheck = ConfigurationSettings.AppSettings["DBStorageToCheck"].ToString();
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (var d in allDrives)
            {
                if (d.Name.ToLower().Contains(storageToCheck.ToLower()))
                {
                    TotalDBDriveSize = d.TotalSize;
                    AvailableDBDriveSize = d.TotalFreeSpace;
                    UsedDBDriveSize = d.TotalSize - d.TotalFreeSpace;
                }
            }
        }
        public  string GetStorage(DBConnection DBConn, out StorageConnection stcon, DateTime? dt = null)//(string host, string db, string port, string username = "", string pass = "")
        {
            var StorageMessage = ""; var DBConnectionStatus = true;
            try
            {
                CheckingStorage();
                CheckingStorageDB();
                new DataHealth().WriteLog("Checking Storage Database...", dt);
                if (DBConn.UserName == null) DBConn.UserName = "";
                if (DBConn.Password == null) DBConn.Password = "";
                //string isNeedAuthentication = ConfigurationSettings.AppSettings["AuthenticationDatabase"].ToString();
                MongoClient client = new MongoClient();
                if (DBConn.UserName != "")
                {
                    //"mongodb://myUserName:myPassword@linus.mongohq.com:myPortNumber/";
                    try
                    {
                        string conStr = DBConn.UserName + ":" + DBConn.Password + "@" + DBConn.ServerHost;

                        client = new MongoClient("mongodb://" + conStr + ":" + DBConn.Port);
                    }
                    catch (Exception ex)
                    {

                        this.DBSize = 0;
                        DBConnectionStatus = false;
                    }
                    
                }
                else
                {
                    try
                    {
                        string conStr = DBConn.ServerHost;
                        client = new MongoClient("mongodb://" + conStr + ":" + DBConn.Port);
                    }
                    catch (Exception ex)
                    {

                        this.DBSize = 0;
                        DBConnectionStatus = false;
                    }
                    
                }
                if (DBConnectionStatus) {
                    MongoServer server = client.GetServer();
                    MongoDatabase t = server.GetDatabase(DBConn.ServerDB);
                    var DbSize = MinimalizeStorage(t.GetStats().StorageSize);
                    this.DBSize = t.GetStats().StorageSize;

                }
                

                var ComputerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                

                this.AvailableRAM = ComputerInfo.AvailablePhysicalMemory;
                this.TotalRAM = ComputerInfo.TotalPhysicalMemory;
                UsedRAM = TotalRAM - AvailableRAM;

                var webAppSize = GetDirectorySize(ConfigurationSettings.AppSettings["WebAppLocation"].ToString());
                this.WebAppSize = webAppSize;
                
                stcon = this;

                StorageMessage = " DataBase Size : " + MinimalizeStorage(this.DBSize) 
                    + " \nAvailable Physical Memory : " + MinimalizeStorage(ComputerInfo.AvailablePhysicalMemory) 
                    + " \nTotal Physical Memory : " + MinimalizeStorage(ComputerInfo.TotalPhysicalMemory) 
                    + " \nTotal WebApp Drive Storage : " + MinimalizeStorage(TotalWebAppDriveSize) 
                    + " \nAvailable WebApp Drive Size : " + MinimalizeStorage(AvailableWebAppDriveSize)
                    + " \nUsed WebApp Drive Size : " + MinimalizeStorage(UsedWebAppDriveSize)
                +" \nWebApp Size : " + MinimalizeStorage(UsedWebAppDriveSize)
                + " \nDB Drive Available Size : " + MinimalizeStorage(AvailableDBDriveSize)
                +" \nDB Drive Used Size : " + MinimalizeStorage(UsedDBDriveSize);
            }
            catch (Exception e)
            {
                
                new DataHealth().WriteLog(e.Message + " " + (e.InnerException == null ? "" : e.StackTrace.ToString()), dt);
                var ComputerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                //StorageMessage = e.Message + " " + (e.InnerException == null ? "" : e.StackTrace.ToString());
                StorageMessage = " DataBase Size : " + 0 + " \nAvailable Physical Memory : " + MinimalizeStorage(ComputerInfo.AvailablePhysicalMemory) + " \nTotal Physical Memory : " + MinimalizeStorage(ComputerInfo.TotalPhysicalMemory) + " \nTotal WebApp Drive Storage : " + MinimalizeStorage(TotalWebAppDriveSize) + " \nAvailable WebApp Drive Size : " + MinimalizeStorage(AvailableWebAppDriveSize) + " \nUsed WebApp Drive Size : " + MinimalizeStorage(UsedWebAppDriveSize);

                this.DBSize = 0;
                stcon = this;

                stcon = new StorageConnection();
                stcon.StorageMessage = e.Message + " " + (e.InnerException == null ? "" : e.StackTrace.ToString());

                return StorageMessage;
            }
            return StorageMessage;
        }

        static long GetDirectorySize(string p)
        {
            string[] a = Directory.GetFiles(p, "*.*");

            long b = 0;
            foreach (string name in a)
            {
                FileInfo info = new FileInfo(name);
                b += info.Length;
            }
            return b;
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

    }
}
