using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net.Mime;
using System.Configuration;
using System.IO;

using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Web.Mvc;
using System.Net;
using System.Threading;
using System.ComponentModel;

namespace ECIS.Client.WEIS
{
    [BsonIgnoreExtraElements]
    public class Email : ECISModel
    {
        public override string TableName
        {
            get { return "WEISEmailTemplates"; }
        }

        public string Subject { get; set; }
        public string Body { get; set; }
        public string SMTPConfig { get; set; }

        public static ResultInfo SendMailNew(string emailId, string[] toMails, string[] ccMails = null,
            Dictionary<string, string> variables = null,
            IEnumerable<string> attachments = null,
            string developerModeEmail = null)
        {
            
            return ResultInfo.Execute(null, (BsonDocument doc) =>
            {
                var email = Email.Get<Email>(Query.EQ("_id", emailId));
                string messsageResponse = string.Empty;
                string msgSubject = string.Empty;
                string msgBody = string.Empty;
                try
                {
                    MailMessage messagebox = new MailMessage();
                    MailAddress sender = new MailAddress(ConfigurationManager.AppSettings["SMTP_UserName_Default"]);
                    //MailAddress receive = new MailAddress("mas.muhammad@eaciit.com");
                    //Attachment AttachFile = new Attachment(attachments, MediaTypeNames.Application.Octet);
                    SmtpClient smtp = new SmtpClient()
                    {
                        Host = ConfigurationManager.AppSettings["SMTP_Host_Default"],
                        Port = Convert.ToInt32(ConfigurationManager.AppSettings["SMTP_Port_Default"]),
                        EnableSsl = true,
                        Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["SMTP_UserName_Default"], ConfigurationManager.AppSettings["SMTP_Password_Default"])
                    };
                    messagebox.From = sender;
                    var developerMode = ConfigurationManager.AppSettings["DevelopmentMode"].ToLower() == "true";
                    string ret = email.Body;
                    string subject = email.Subject;
                    if (developerMode)
                    {
                         
                        messagebox.To.Add(developerModeEmail);
                        string pre = "This email is sent from developer mode. On production it would have been send to: ";
                        foreach (var tomail in toMails) pre += tomail + ",";
                        pre += "\n";
                        if (ccMails != null)
                        {
                            pre += "And CC-ed to: ";
                            foreach (var tomail in ccMails) pre += tomail + ",";
                        }
                        pre += "\n\n\n";
                       
                        if (variables != null)
                        {
                            foreach (var key in variables.Keys)
                            {
                                ret = ret.Replace("{" + key + "}", variables[key]);
                                subject = subject.Replace("{" + key + "}", variables[key]);
                            }
                            
                        }
                        msgSubject = subject;
                        msgBody = pre + ret;
                    }
                    else
                     //for production mode
                    {
                        foreach (var i in toMails)
                        {
                            messagebox.To.Add(i);
                        }

                        if (variables != null)
                        {
                            foreach (var key in variables.Keys)
                            {
                                ret = ret.Replace("{" + key + "}", variables[key]);
                                subject = subject.Replace("{" + key + "}", variables[key]);
                            }
                            msgSubject = subject;
                            msgBody = ret;
                        }
                    }

                    messagebox.Subject = msgSubject;
                    messagebox.Body = msgBody;
                    messagebox.IsBodyHtml = false;
                    smtp.Send(messagebox);
                    messsageResponse = "Message Send";
                    LogMail(emailId, toMails, messsageResponse);
                }
                catch (Exception e)
                {
                    messsageResponse = e.Message;
                    LogMail(emailId, toMails, messsageResponse);
                }
                return messsageResponse;
            });
            
        }

        public static void LogMail(string emailId, string[] toMails,string response = null)
        {
            string path = ConfigurationManager.AppSettings["LogEmail"];
            string FilePath = path ;
            TimeZoneInfo localZone = TimeZoneInfo.Local;
            string datenow = DateTime.Now.ToString("yyyyMMdd");
            string FileName = FilePath + "LogMail_" + datenow  + ".txt";
            
            List<string> logdata = new List<string>();
            logdata.Add(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " " + localZone.StandardName);
            logdata.Add(emailId);
            foreach (var i in toMails)
            {
                logdata.Add("Send to:" + i);
            }
            
            
            logdata.Add(response);
            string logresult = string.Join(";",logdata.ToArray());

            if (!File.Exists(FileName))
            {
                System.IO.Directory.CreateDirectory(FilePath);
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(FileName))
                {
                    //sw.WriteLine(logresult);
                }
            }
 
            //sw.WriteLine(logresult);
            string tempfile = Path.GetTempFileName();
            using (var writer = new StreamWriter(tempfile))
            using (var reader = new StreamReader(FileName))
            {
                writer.WriteLine(logresult);
                while (!reader.EndOfStream)
                    writer.WriteLine(reader.ReadLine());
            }
            File.Copy(tempfile, FileName, true);
           
        }

        public static ResultInfo Send(string emailId, string[] toMails, string[] ccMails = null, 
            Dictionary<string, string> variables = null, 
            IEnumerable<string> attachments = null,
            string developerModeEmail = null)
        {
            var email = Email.Get<Email>(emailId);
            
            if (email != null)
            {
                //return email.Send(toMails, ccMails, variables, attachments, developerModeEmail);
                ResultInfo x = new ResultInfo();
                try
                {
                    var TaskRes = Task.Run(() => email.Send(toMails, ccMails, variables, attachments, developerModeEmail));
                    Thread.Sleep(1000);
                    x.Message = TaskRes.Result.Message;
                    //LogMail("",toMails,x.Message);
                }
                catch (Exception e)
                {
                    x.Message = e.Message;
                    //LogMail("", toMails, x.Message);
                }
               
                return x;
            }
            else
            {
                return ResultInfo.Execute(() => {
                    throw new Exception("No email template found");
                   
                });
            }
        }
      
        public ResultInfo Send(string[] toMails, string[] ccMails = null,
            Dictionary<string, string> variables = null,
            IEnumerable<string> attachments = null,
            string developerModeEmail = null)
        {


            var developerMode = ConfigurationManager.AppSettings["DevelopmentMode"].ToLower() == "true";
            string msgBody = Body;
            if (developerMode)
            {
                string pre = "This email is sent from developer mode. On production it would have been send to: ";
                foreach (var tomail in toMails) pre += tomail + ",";
                pre += "\n";
                if (ccMails != null)
                {
                    pre += "And CC-ed to: ";
                    foreach (var tomail in ccMails) pre += tomail + ",";
                }
                pre += "\n\n\n";
                msgBody = pre + msgBody;

                toMails = new string[] { developerModeEmail };
                ccMails = new string[] { };
            }

            var xtomail = new List<string>();
            if (toMails != null && toMails.Count() > 0)
            {
                foreach (var t in toMails.ToList())
                {
                    if (!t.Trim().Equals(""))
                    {
                        xtomail.Add(t);
                    }
                }
            }
            var xccmail = new List<string>();
            if (ccMails != null && ccMails.Count() > 0)
            {
                foreach (var t in ccMails.ToList())
                {
                    if (!t.Trim().Equals(""))
                    {
                        xccmail.Add(t);
                    }
                }
            }
            
            ResultInfo x = new ResultInfo();
            string respData;
            try
            {
                var resp = Tools.SendMail(SMTPConfig, Subject, msgBody, xtomail.ToArray(), xccmail.ToArray(),
                attachments == null ? null : attachments.ToArray(),
                (string source) =>
                {
                    string ret = source;
                    if (variables != null)
                    {
                        foreach (var key in variables.Keys)
                        {
                            ret = ret.Replace("{" + key + "}", variables[key]);
                        }
                    }
                    return ret;
                }, false);
                if (resp.Result == "OK") { respData = "Message sent"; } else { respData = resp.Message; }
                x.Message = respData;
                LogMail("", toMails, respData);
               
            }
            catch (Exception e)
            {
                respData = Tools.PushException(e);
                x.Message = respData;
                LogMail("", toMails, respData);
            }
            return x;
        }

        public static ResultInfo SendEmailMODChecker(string emailId, string[] toMails, string[] ccMails = null,
            Dictionary<string, string> variables = null,
            IEnumerable<string> attachments = null,
            string developerModeEmail = null)
        {
            var email = Email.Get<Email>(emailId);

            if (email != null)
            {
                //return email.Send(toMails, ccMails, variables, attachments, developerModeEmail);
                ResultInfo x = new ResultInfo();
                try
                {
                    var TaskRes = Task.Run(() => email.SendEmailMODChecker(toMails, ccMails, variables, attachments, developerModeEmail));
                    Thread.Sleep(1000);
                    x.Message = TaskRes.Result.Message;
                    //LogMail("",toMails,x.Message);
                }
                catch (Exception e)
                {
                    x.Message = e.Message;
                    //LogMail("", toMails, x.Message);
                }

                return x;
            }
            else
            {
                return ResultInfo.Execute(() =>
                {
                    throw new Exception("No email template found");

                });
            }
        }

        public ResultInfo SendEmailMODChecker(string[] toMails, string[] ccMails = null,
            Dictionary<string, string> variables = null,
            IEnumerable<string> attachments = null,
            string developerModeEmail = null)
        {


            var developerMode = false;
            string msgBody = Body;
            if (developerMode)
            {
                string pre = "This email is sent from developer mode. On production it would have been send to: ";
                foreach (var tomail in toMails) pre += tomail + ",";
                pre += "\n";
                if (ccMails != null)
                {
                    pre += "And CC-ed to: ";
                    foreach (var tomail in ccMails) pre += tomail + ",";
                }
                pre += "\n\n\n";
                msgBody = pre + msgBody;

                toMails = new string[] { developerModeEmail };
                ccMails = new string[] { };
            }

            var xtomail = new List<string>();
            if (toMails != null && toMails.Count() > 0)
            {
                foreach (var t in toMails.ToList())
                {
                    if (!t.Trim().Equals(""))
                    {
                        xtomail.Add(t);
                    }
                }
            }
            var xccmail = new List<string>();
            if (ccMails != null && ccMails.Count() > 0)
            {
                foreach (var t in ccMails.ToList())
                {
                    if (!t.Trim().Equals(""))
                    {
                        xccmail.Add(t);
                    }
                }
            }

            ResultInfo x = new ResultInfo();
            string respData;
            try
            {
                var resp = Tools.SendMail(SMTPConfig, Subject, msgBody, xtomail.ToArray(), xccmail.ToArray(),
                attachments == null ? null : attachments.ToArray(),
                (string source) =>
                {
                    string ret = source;
                    if (variables != null)
                    {
                        foreach (var key in variables.Keys)
                        {
                            ret = ret.Replace("{" + key + "}", variables[key]);
                        }
                    }
                    return ret;
                }, false);
                if (resp.Result == "OK") { respData = "Message sent"; } else { respData = resp.Message; }
                x.Message = respData;
                LogMail("", toMails, respData);

            }
            catch (Exception e)
            {
                respData = Tools.PushException(e);
                x.Message = respData;
                LogMail("", toMails, respData);
            }
            return x;
        }

        public void Broadcast(string developerEmail)
        {
            var users = DataHelper.Populate("Users");
            foreach (var user in users)
            {
                Dictionary<string, string> vars = new Dictionary<string, string>();
                vars.Add("FullName", user.GetString("FullName"));
                vars.Add("Email", user.GetString("Email"));
                vars.Add("Password", user.GetString("Password"));
                Send(new string[] { user.GetString("Email") }, null, vars, null, developerEmail);
            }
        }
    }
}
