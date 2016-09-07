using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WEISFiscalYear.WriteLog
{
    class Log
    {
        public void WriteLog(string Text = null)
        {
            var Now = DateTime.Now;
            var ST = String.Format("{0} : {1}", String.Format("{0:dd-MMM-yyyy  HH:mm:ss}", Now), Text);
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

            Console.WriteLine(ST);
            File.AppendAllText(DataPath + filename, ST + Environment.NewLine);
            //System.IO.File.WriteAllText(DataPath + dir + "\\" + filename, json);
        }
    }
}
