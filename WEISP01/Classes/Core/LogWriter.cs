using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Configuration;

namespace ECIS.AppServer.Classes.Core
{
    public class LogWriter
    {
        public void Write(string message, string stacktrace)
        {
            string LogPath = ConfigurationManager.AppSettings["LogPath"].ToString();
            if (!Directory.Exists(LogPath))
                Directory.CreateDirectory(LogPath);

            string FileName = "Log_" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + ".txt";

            using (StreamWriter writer = new StreamWriter(LogPath + FileName, true))
            {
                writer.WriteLine(DateTime.Now + " : " + message);
                writer.WriteLine(DateTime.Now + " : " + stacktrace);
                writer.WriteLine("");
            }
        }
    }
}