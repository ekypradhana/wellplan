using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ECIS.Core.DataSerializer;
using ECIS.Core;
using ECIS.Client.WEIS;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.IO;

namespace ECIS.AppServer.Areas.Shell.Controllers
{
    [ECAuth()]
    public class ExportController : Controller
    {
        static IMongoQuery ProcessOperand(string fields, string Operand, string Value)
        {
            IMongoQuery qWellAc = Query.Null;
            //List<IMongoQuery> qs1 = new List<IMongoQuery>();
            switch (Operand)
            {
                case "Equal":
                    {
                        qWellAc = Query.EQ(fields, Value);
                        break;
                    }
                case "Not Equal":
                    {
                        qWellAc = Query.NE(fields, Value);
                        break;
                    }
                case "Greater Than":
                    {
                        qWellAc = Query.GT(fields, Value);
                        break;
                    }
                case "Lower Than":
                    {
                        qWellAc = Query.LT(fields, Value);
                        break;
                    }
                default: // Like
                    {
                        qWellAc = Query.Matches(fields, BsonRegularExpression.Create(new Regex(Value, RegexOptions.IgnoreCase)));
                        break;
                    }
            }

            return qWellAc;
        }


        public IMongoQuery GetQuery(string scr)
        {
            IMongoQuery qWellAc = Query.Null;

            List<IMongoQuery> list = new List<IMongoQuery>();

            int i = 1;

            List<string> Operand = new List<string>();
            foreach (string x in scr.Split(','))
            {
                if (i % 2 != 0)
                {
                    if (x.Trim() != string.Empty && !x.Trim().Contains("|||"))
                    {
                        string frst = x.Split('|')[0];
                        string scnd = x.Split('|')[1];
                        string thrd = x.Split('|')[2];

                        qWellAc = ProcessOperand(frst, scnd, thrd);

                        list.Add(qWellAc);
                    }
                }
                else
                {
                    if (x.Trim() != string.Empty)
                    {
                        // get and/or operator
                        Operand.Add(x);
                    }
                }
                i++;
            }

            int y = 0;

            if (Operand.Count > 0)
            {
                IMongoQuery qsNew = Query.Null;
                foreach (string x in Operand)
                {
                    if (x.Equals("AND"))
                    {
                        qsNew = qsNew == null ? Query.And(list[y], list[y + 1]) : Query.And(qsNew, list[y], list[y + 1]);
                    }
                    else
                    {
                        qsNew = qsNew == null ? Query.Or(list[y], list[y + 1]) : Query.Or(qsNew, list[y], list[y + 1]);
                    }

                    y++;
                }

                return qsNew;
            }
            else
                return qWellAc;
        }

        private static bool _isReadOnly()
        {
            var Email = WebTools.LoginUser.Email;
            List<string> Roles = WEISPerson.GetRolesByEmail(Email);
            var ro = Roles.Where(x => x.ToUpper() == "RO-ALL");
            bool isRO = false;
            if (ro.Count() > 0)
            {
                isRO = true;
            }
            return isRO;
        }

        //
        // GET: /Shell/Export/
        public ActionResult CreateDefinition()
        {
            ViewBag.isReadOnly = "0";
            if (_isReadOnly())
            {
                ViewBag.isRO = "1";
            }
            return PartialView();
            //return View();
        }
        //
        // GET: /Shell/Export/
        public ActionResult Index()
        {
            ViewBag.isReadOnly = "0";
            if (_isReadOnly())
            {
                ViewBag.isRO = "1";
            }
            return View();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ActionResult Save(string Name, string CollectioName, int? _id = -1, string OutputPath = "", string Query = "", List<string> Fields = null)
        {
            Fields = (Fields == null ? new List<string>() : Fields);
            Definition d = new Definition();

            d._id = _id <= 0 ? 0 : _id;
            d.Name = Name;
            d.OutputPath = OutputPath;
            d.CollectioName = CollectioName;
            d.Fields = Fields.ToArray();
            d.Query = Query;

            if (_id == 0)
            {
                if (Definition.Populate<Definition>(MongoDB.Driver.Builders.Query.EQ("Name", Name)).Count > 0)
                    return Json(new { Result = "NOK" }, JsonRequestBehavior.AllowGet);

                DataHelper.Save(d.TableName, d.ToBsonDocument());
                return Json(new
                {
                    Result = "OK",
                    Data = Definition.Get<Definition>(MongoDB.Driver.Builders.Query.EQ("Name", Name))
                }, JsonRequestBehavior.AllowGet);
            }

            DataHelper.Save(d.TableName, d.PreSave(d.ToBsonDocument()));
            return Json(new
            {
                Result = "OK",
                Data = Definition.Get<Definition>(MongoDB.Driver.Builders.Query.EQ("Name", Name))
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 
        /// </summary>
        public JsonResult Preview(int id)
        {
            Definition definition = DataHelper.Populate<Definition>("ExportDef", Query.EQ("_id", id)).FirstOrDefault();
            var data = new List<BsonDocument>();

            try
            {
                data = DataExporter.UnwindData(ECIS.Core.DataSerializer.DataExporter.ExecuteDef(definition));
                if (data == null) data = new List<BsonDocument>();
                if (data.Count > 0) data = data.Take(10).ToList();
            }
            catch (Exception e)
            {

            }

            var result = new
            {
                Columns = definition.Fields,
                Data = DataHelper.ToDictionaryArray(data)
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

         /// <summary>
        /// 
        /// </summary>
        public JsonResult CreateQuery(string scratch)
        {

            //string scr = "Gov ID,Greater Than,12312,AND,EDM ID,Greater Than,314234,AND,OPS Sequence ID,Like,235235,,,,,,,,,";
            //var ret = "";
            IMongoQuery qs =  GetQuery(scratch);



            return Json(qs.ToString(), JsonRequestBehavior.AllowGet);
        }

        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult Detail(int id)
        {
            Definition t = DataHelper.Populate<Definition>("ExportDef", Query.EQ("_id", id)).FirstOrDefault();
            List<object> objQ = new List<object>();

            string q =  t.Query;

            if (!q.Trim().Equals(""))
            {
                BsonDocument query = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(q);
                QueryDocument queryDoc = new QueryDocument(query);
            }


           

            //var yy =  queryDoc.HasElement("$or");
            //var result = collection.FindAs<TypeOfResultExpected>(queryDoc);

            return Json(t , JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Download
        /// </summary>
        public ActionResult Download(int id)
        {
            Definition definition = DataHelper.Populate<Definition>("ExportDef", Query.EQ("_id", id)).FirstOrDefault();
            var data = new List<BsonDocument>();
            string csv = "";

            try
            {
                data = DataExporter.UnwindData(ECIS.Core.DataSerializer.DataExporter.ExecuteDef(definition));
                if (data == null) data = new List<BsonDocument>();
                //if (data.Count > 0) data = data.Take(10).ToList();
                if (data.Count > 0) data = data.ToList();
            }
            catch (Exception e)
            {

            }

            try
            {
                csv = DataExporter.ToCSVFormat(data, definition.Fields.ToList());
            }
            catch (Exception e)
            {
                csv = string.Join(",", definition.Fields.ToArray());
            }

            Response.Clear();
            Response.ContentType = "text/CSV";

            if (definition.Name == null || definition.Name.Trim() == "")
            {
                Response.AddHeader("Content-Disposition", "attachment;filename=" + DateTime.Now.ToString("yyyyMMdd") + ".csv");
            }
            else
            {
                Response.AddHeader("Content-Disposition", "attachment;filename=" + definition.Name.Trim().Replace(" ", "") + ".csv");
            }

            Response.Write(csv);
            Response.Flush();
            Response.End();

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="definitions"></param>
        public void Delete(int id)
        {
            var data = DataHelper.Populate("ExportDef", Query.EQ("_id", id)) ;
            if (data != null)
                DataHelper.Delete("ExportDef", Query.EQ("_id", id));
        }

        /// <summary>
        /// FilterIn / Contains
        /// </summary>
        /// <param name="filters"></param>
        /// <param name="fieldname"></param>
        /// <returns></returns>
        public static BsonDocument FilterIn(List<string> filters, string fieldname)
        {
            BsonArray arr = new BsonArray();
            foreach (string s in filters)
            {
                arr.Add(s);
            }
            var match = new BsonDocument
                                {
                                    { "$match", new BsonDocument 
                                         {{ fieldname, new BsonDocument {
                                                {"$in", arr }
                                          }}}
                                    }
                                };
            return match;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_id"></param>
        /// <returns></returns>
        public JsonResult Search(string ListName, DateTime? CreateDate)
        {

            List<IMongoQuery> qs = new List<IMongoQuery>();

            List<string> fName = new List<string>();
            if (ListName != null && !string.IsNullOrEmpty(ListName))
            qs.Add(Query.Matches("Name", BsonRegularExpression.Create(new Regex(ListName, RegexOptions.IgnoreCase))));

            if (CreateDate != null)
            {

                qs.Add(

                Query.And(
                    Query.GTE("CreateDate", Tools.ToUTC(new DateTime(Convert.ToDateTime(CreateDate).Year, Convert.ToDateTime(CreateDate).Month, Convert.ToDateTime(CreateDate).Day))),
                    Query.LTE("CreateDate", Tools.ToUTC(new DateTime(Convert.ToDateTime(CreateDate).Year, Convert.ToDateTime(CreateDate).Month, Convert.ToDateTime(CreateDate).AddDays(1).Day)))
                    )
                    );
            }
            List<Definition> ret = new List<Definition>(); ;
            if (qs.Count > 0)
            {
                ret = DataHelper.Populate<Definition>("ExportDef", Query.Or(qs.ToArray()));
            }
            else
            {
                ret = DataHelper.Populate<Definition>("ExportDef");
            }

            var result = new ResultInfo();

            result.Data = new
            {
                Data = ret.ToArray()
            };

            return Json(new { Data = ret.ToArray(), Total = ret.Count() }, JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="TableName"></param>
        /// <returns></returns>
        public JsonResult GetFields(string TableName)
        {
            if (TableName.Trim() != "")
            {
                var t = DataHelper.Populate("TablesAndFields", Query.EQ("_id", TableName));

                BsonValue val = t[0]["Fields"].AsBsonValue;

                

                List<string> res = new List<string>();
                foreach (string data in val.ToString().Split(','))    // BsonHelper.GetDoc(  t.ToArray())
                {
                    res.Add(  data.Replace("{", "").Replace("}", "").Replace("[", "").Replace("]", "").Trim().ToString());
                }

                return Json(new { Data = res.ToArray() }, JsonRequestBehavior.AllowGet);
            }
            else
                return Json(new { Data = "" }, JsonRequestBehavior.AllowGet);

        }
    }
}