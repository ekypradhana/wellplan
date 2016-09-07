using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECIS.Core;
using MongoDB.Driver.Builders;
using WEISFiscalYear.Model;
using WEISFiscalYear.WriteLog;
using ECIS.Client.WEIS;

namespace WEISFiscalYear
{
    class Program
    {
        static void Main(string[] args)
        {
            var lcc = ConfigurationSettings.AppSettings["License"].ToString();
            ECIS.Core.License.ConfigFolder = lcc;//System.IO.Directory.GetCurrentDirectory() + @"\License";
            ECIS.Core.License.LoadLicense();
            var fc = new FinancialCalendar();
            try
            {

                Console.WriteLine("======================Begin Financial Calendar======================");
                Console.WriteLine(String.Format("======================{0:U}======================", DateTime.Now));
                //var Now = DateTime.Now;
                //string filename = "Log-" + String.Format("{0:yyyyMMddHH}", Now) + ".txt";
                var Now = DateTime.Now;//new DateTime(2016, 5, 1);
                var DataPath = ConfigurationSettings.AppSettings["Log"].ToString();
                string filename = "Log-" + String.Format("{0:yyyyMMddHH}", Now) + ".txt";

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

                var pth = DataPath + filename;
                fc.RunFiscal(Now, pth);

                Console.WriteLine("======================End======================");
                //Console.ReadLine();
            }
            catch (Exception e)
            {
                fc.WriteLog(String.Format(e.Message));
                Console.Read();
            }
        }
    }
}
