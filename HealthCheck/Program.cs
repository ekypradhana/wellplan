using ECIS.Client.WEIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization;
namespace HealthCheck
{
    class Program
    {
        static void Main(string[] args)
        {
            ECIS.Core.License.ConfigFolder = System.IO.Directory.GetCurrentDirectory() + @"\License";
            ECIS.Core.License.LoadLicense();
            try
            {
                DataHealth d = new DataHealth();


                new DataHealth().WriteLog("===============Health Check Running===============", DateTime.Now);
                d.GenData(DateTime.Now);//
                var timeSend = ConfigurationManager.AppSettings["HealthCheckSendEmailTime"].ToString();
                var roleId = ConfigurationManager.AppSettings["HealthCheckReceiverEmailRole"];


                var isExecToday = DataHelper.Populate("_temp_health_daily", Query.EQ("_id", DateTime.Now.ToString("yyyyMMdd")));
                //var isExecToday = "";

                //if (DateTime.Now.ToString("HH").Equals(timeSend) && !isExecToday.Any())
                if (DateTime.Now.ToString("HH").Equals(timeSend))
                {
                    new DataHealth().WriteLog("================  Create Summary ================", DateTime.Now);
                    var template = ConfigurationManager.AppSettings["SummaryTemplate"].ToString();
                    var pathReport = d.CreateReport(template);
                    new DataHealth().WriteLog("================ Summary Created ================", DateTime.Now);

                    BsonDocument t = new BsonDocument();
                    t.Set("_id", DateTime.Today.ToString("yyyyMMdd"));
                    DataHelper.Save("_temp_health_daily", t);

                    // send Email

                    new DataHealth().WriteLog("================  Send Email ================", DateTime.Now);
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
                    //Get User PIC Health Check
                    List<BsonDocument> docs = new List<BsonDocument>();
                    string aggregateCond1 = "{ $unwind: '$PersonInfos' }";
                    string projectio = @"{$project :  { 
                                'Email' : '$PersonInfos.Email','Role':'$PersonInfos.RoleId'
                                } }";

                    List<BsonDocument> pipelines = new List<BsonDocument>();
                    pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(aggregateCond1));
                    pipelines.Add(BsonSerializer.Deserialize<BsonDocument>(projectio));

                    List<BsonDocument> aggregates = DataHelper.Aggregate(new WEISPerson().TableName, pipelines);
                    
                    if(!roleId.Trim().Equals("") && aggregates.Any())
                    {

                        List<string> to = new List<string>();
                        if (aggregates.Any())
                        {
                            var listuser = aggregates.Where(x => x.GetString("Role").Equals(roleId));
                            if(listuser.Any())
                            {
                                to = listuser.Select(x => x.GetString("Email")).ToList<string>();
                                d.SendHealthCheckEmail(pathReport, to);
                                new DataHealth().WriteLog("================  Email Sent ================", DateTime.Now);
                            }
                            
                        }

                        
                    }
                    


                }

                new DataHealth().WriteLog("================Health Check Done================", DateTime.Now);

            }
            catch (Exception ex)
            {
                string msg = string.Format("{0}\n{1}\n{2}", ex.Message, ex.InnerException, ex.StackTrace);
                Console.WriteLine(msg);
                Console.Read();
            }
        }
    }
}
