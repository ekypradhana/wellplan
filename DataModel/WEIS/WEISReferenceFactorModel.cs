using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ECIS.Client.WEIS
{
    public class WEISReferenceFactorModel : ECISModel
    {

        public override string TableName
        {
            get { return "WEISReferenceFactorModel"; }
        }

        public WEISReferenceFactorModel()
        {
            GroupCase = "";
            Country = "";
            WellName = "";
            ActivityType = "";
            SubjectMatters = new Dictionary<string, Dictionary<string, double>>();
        }



        public static WEISReferenceFactorModel CreateDefaultRFM(string GroupCase, string Country, string baseop)
        {
            var t = new WEISReferenceFactorModel()
            {
                GroupCase = GroupCase,
                Country = Country,
                BaseOP = baseop,
                SubjectMatters = new Dictionary<string, Dictionary<string, double>>() {
                    { WEISReferenceFactorModel.GetSubjectMatterOptions()[0], new Dictionary<string,double>() {
                        { "Year_2015", 0.1 },
                        { "Year_2016", 0.1 },
                        { "Year_2017", 0.1 },
                        { "Year_2018", 0.1 },
                        { "Year_2019", 0.1 },
                        { "Year_2020", 0.1 },
                    } },
                    { WEISReferenceFactorModel.GetSubjectMatterOptions()[1], new Dictionary<string,double>() {
                         { "Year_2015", 0.1 },
                        { "Year_2016", 0.1 },
                        { "Year_2017", 0.1 },
                        { "Year_2018", 0.1 },
                        { "Year_2019", 0.1 },
                        { "Year_2020", 0.1 },
                    } },
                    { WEISReferenceFactorModel.GetSubjectMatterOptions()[2], new Dictionary<string,double>() {
                         { "Year_2015", 0.1 },
                        { "Year_2016", 0.1 },
                        { "Year_2017", 0.1 },
                        { "Year_2018", 0.1 },
                        { "Year_2019", 0.1 },
                        { "Year_2020", 0.1 },
                    } },
                    { WEISReferenceFactorModel.GetSubjectMatterOptions()[3], new Dictionary<string,double>() {
                         { "Year_2015", 0.1 },
                        { "Year_2016", 0.1 },
                        { "Year_2017", 0.1 },
                        { "Year_2018", 0.1 },
                        { "Year_2019", 0.1 },
                        { "Year_2020", 0.1 },
                    } },
                    { WEISReferenceFactorModel.GetSubjectMatterOptions()[4], new Dictionary<string,double>() {
                         { "Year_2015", 0.1 },
                        { "Year_2016", 0.1 },
                        { "Year_2017", 0.1 },
                        { "Year_2018", 0.1 },
                        { "Year_2019", 0.1 },
                        { "Year_2020", 0.1 },
                    } }
                }
            };//.Save();
            t.Save();

            //save to WEISCountryReferenceFactor
            var wc = CountryReferenceFactor.Get<CountryReferenceFactor>(Query.EQ("Country", Country));
            if (wc == null)
            {
                var newwc = new CountryReferenceFactor();
                newwc.Country = Country;
                newwc.ReferenceFactorModels.Add(GroupCase);
            }
            else
            {
                if (wc.ReferenceFactorModels.Any())
                {
                    if (wc.ReferenceFactorModels.Where(x => x.Equals(GroupCase)).Count() == 0)
                    {
                        wc.ReferenceFactorModels.Add(GroupCase);
                        wc.Save();
                    }
                }
                else
                {
                    wc.ReferenceFactorModels = new List<string>();
                    wc.ReferenceFactorModels.Add(GroupCase);
                    wc.Save();
                }
            }

            return t;
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references = null)
        {
            Country = (Country == "" || Country == null ? "*" : Country);
            WellName = (WellName == "" || WellName == null ? "*" : WellName);
            ActivityType = (ActivityType == "" || ActivityType == null ? "*" : ActivityType);
            GroupCase = (GroupCase == "" || GroupCase == null ? "*" : GroupCase);

            _id = String.Format("C{0}W{1}A{2}G{3}{4}", Country.Replace(" ", ""), WellName.Replace(" ", ""), ActivityType.Replace(" ", ""), GroupCase.Replace(" ", ""),BaseOP);

            return this.ToBsonDocument();
        }

        public string WellName { get; set; }
        public string ActivityType { get; set; }
        public string Country { get; set; }
        public string GroupCase { get; set; }
        public string BaseOP { get; set; }

        public static string[] GetSubjectMatterOptions()
        {
            return new string[] { "Material Escalation Factors", "Service Escalation Factors", "CSO Factors", "Inflation Factors", "Learning Curve Factors" };
        }

        public Dictionary<string, Dictionary<string, double>> SubjectMatters { set; get; }

        public WEISReferenceFactorModel Get(string GroupCase, string Country, string ActivityType, string WellName, string OP = "")
        {
            var qs = new List<IMongoQuery>(){
                Query.EQ("GroupCase", GroupCase),
                Query.EQ("Country", Country),
                Query.EQ("ActivityType", ActivityType),
                Query.EQ("WellName", WellName),
            };
            
            if(OP != ""){
                qs.Add(Query.EQ("BaseOP",OP));
            }
            var query = Query.And(qs);

            return WEISReferenceFactorModel.Get<WEISReferenceFactorModel>(query);
        }
    }
}
