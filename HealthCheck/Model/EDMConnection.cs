using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;

namespace HealthCheck.Model
{
    public class EDMConnection
    {
        public int Status { get; set; } //0 is fail. 1 success
        public string Message { get; set; }
        public string OraServerShell { get; set; }

        public EDMConnection()
        {
            OraServerShell = ConfigurationSettings.AppSettings["OraServerShell"].ToString();
        }

        public EDMConnection TestingEDMConnection(DateTime dt)
        {
            var st = new EDMConnection();
            try
            {
                new DataHealth().WriteLog("Checking EDM Connection", dt);
                string connectionString = ConfigurationSettings.AppSettings["OraServerShell"];
                OracleConnection connection = new OracleConnection(connectionString);
                connection.Open();
                st.Status = 1;
                st.Message = "";
                new DataHealth().WriteLog("Success EDM Connection", dt);
            }
            catch (Exception e)
            {
                st.Status = 0;
                st.Message = e.Message + " " + (e.InnerException == null ? "" : e.StackTrace.ToString());
                new DataHealth().WriteLog(e.Message + " " + (e.InnerException == null ? "" : e.StackTrace.ToString()), dt);
            }
            return st;
        }
    }
}
