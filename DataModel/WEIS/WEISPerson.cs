using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

using ECIS.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ECIS.Biz.Common;

namespace ECIS.Client.WEIS
{
    [BsonIgnoreExtraElements]
    public class WEISPerson : ECISModel
    {
        public override string TableName
        {
            get { return "WEISPersons"; }
        }

        public WEISPerson()
        {
            WellName = string.Empty;
            SequenceId = string.Empty;
            ActivityType = string.Empty;
            PhaseNo = 0;
            PersonInfos = new List<WEISPersonInfo>();
        }

        public override BsonDocument PreSave(BsonDocument doc, string[] references=null)
        {
            _id = String.Format("W{0}S{1}A{2}",WellName.Replace(" ",""),SequenceId,ActivityType.Replace(" ",""));
            return this.ToBsonDocument();
        }

        public string WellName { get; set; }
        public string SequenceId { get; set; }
        public string ActivityType { get; set; }
        public int PhaseNo { get; set; }

        public static WEISPerson GetByKey(string wellName, string sequenceId, string activityType){
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", wellName));
            qs.Add(Query.EQ("SequenceId", sequenceId));
            qs.Add(Query.EQ("ActivityType", activityType));
            return WEISPerson.Get<WEISPerson>(Query.And(qs));
        }

        public List<WEISPersonInfo> PersonInfos { get; set; }

        public static WEISPerson GetByKey(string wellName, string sequenceId, int phaseNo)
        {
            var qs = new List<IMongoQuery>();
            qs.Add(Query.EQ("WellName", wellName));
            qs.Add(Query.EQ("SequenceId", sequenceId));
            qs.Add(Query.EQ("PhaseNo", phaseNo));
            return WEISPerson.Get<WEISPerson>(Query.And(qs));
        }

        public static List<string> GetRolesByEmail(string email)
        {
            return Populate<WEISPerson>(Query.EQ("PersonInfos.Email", email))
                .SelectMany(d => d.PersonInfos)
                .Where(d => d.Email.Equals(email))
                .Select(d => d.Role._id.ToString())
                .ToList<string>();
        }
    }

    [BsonIgnoreExtraElements]
    public class WEISPersonInfo
    {
        public WEISPersonInfo()
        {
            FullName = string.Empty;
            Email = string.Empty;
            RoleId = string.Empty;
            //LineOfBusiness = string.Empty;
        }

        public string FullName { get; set; }
        public string Email { get; set; }
        public string RoleId { get; set; }
        public string LineOfBusiness { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public WEISRole Role {
            get
            {
                WEISRole role = null;
                if (String.IsNullOrEmpty(RoleId) == false)
                {
                    role = WEISRole.Get<WEISRole>(RoleId);
                }
                return role;
            }
        }



    }

    public class WEISPic
    {
        private List<WEISPersonInfo> _PIC;
        public List<WEISPersonInfo> PIC
        {
            get
            {
                if (_PIC == null) _PIC = new List<WEISPersonInfo>();
                return _PIC;
            }
            set { _PIC = value; }
        }
    }



    public class WEISRole : ECISModel
    {
        public WEISRole()
        {
            RoleName = string.Empty;
        }

        public string RoleName { set; get; }

        [Newtonsoft.Json.JsonIgnore]
        public bool HasPersons
        {
            get
            {
                return (DataHelper.Populate("WEISPersons", Query.EQ("PersonInfos.RoleId", _id.ToString())).Count() > 0);
            } 
        }

        public override string TableName
        {
            get { return "WEISRoles"; }
        }
    }

    public class PersonOnWell : WEISPersonInfo {
        public string WellName;
    }



}
