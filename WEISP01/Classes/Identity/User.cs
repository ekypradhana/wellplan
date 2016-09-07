namespace ECIS.Identity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using global::MongoDB.Bson;
    using global::MongoDB.Bson.Serialization.Attributes;
    using Microsoft.AspNet.Identity;

    using ECIS.Core;
    using ECIS.Identity;
    using ECIS.Client.WEIS;
    using MongoDB.Driver;
    using MongoDB.Driver.Builders;

    public class IdentityUser : IUser<string>
    {
        public IdentityUser()
        {
            Id = ObjectId.GenerateNewId().ToString();
            Roles = new List<string>();
            Logins = new List<UserLoginInfo>();
            Claims = new List<IdentityUserClaim>();
        }

        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; private set; }

        public string UserName { get; set; }
        public string FullName { get; set; }
        
        public string LineOfBusiness { get; set; }

        public bool Enable { get; set; }
        public bool ADUser { get; set; }

        public virtual string SecurityStamp { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public virtual DateTime? ConfirmedAtUtc { get; set; }

        public virtual string Email { get; set; }

        public virtual void SetConfirmed(bool confirmed)
        {
            if (!confirmed)
            {
                ConfirmedAtUtc = null;
                return;
            }
            if (ConfirmedAtUtc.HasValue)
            {
                return;
            }
            ConfirmedAtUtc = DateTime.UtcNow;
        }

        [BsonIgnoreIfNull]
        public List<string> Roles { get; set; }

        public virtual void AddRole(string role)
        {
            Roles.Add(role);
        }

        public virtual void RemoveRole(string role)
        {
            Roles.Remove(role);
        }

        [BsonIgnoreIfNull]
        public virtual string PasswordHash { get; set; }

        [BsonIgnoreIfNull]
        public List<UserLoginInfo> Logins { get; set; }

        public virtual void AddLogin(UserLoginInfo login)
        {
            Logins.Add(login);
        }

        public List<BsonDocument> GetRoles()
        {
            var q = Query.EQ("PersonInfos.Email",Email);
            var persons = WEISPerson.Populate<WEISPerson>(q);
            var ret = new List<BsonDocument>();
            if (persons != null)
            {
                ret.AddRange(persons.SelectMany(d=>d.PersonInfos, (d,e) => new
                {
                    d.WellName, d.ActivityType, e.RoleId, e.Role.RoleName
                }.ToBsonDocument()));
            }
            return ret;
        }

        public virtual void RemoveLogin(UserLoginInfo login)
        {
            var loginsToRemove = Logins
                .Where(l => l.LoginProvider == login.LoginProvider)
                .Where(l => l.ProviderKey == login.ProviderKey);

            Logins = Logins.Except(loginsToRemove).ToList();
        }

        public virtual bool HasPassword()
        {
            return false;
        }

        [BsonIgnoreIfNull]
        public List<IdentityUserClaim> Claims { get; set; }

        public virtual void AddClaim(Claim claim)
        {
            Claims.Add(new IdentityUserClaim(claim));
        }

        public virtual void RemoveClaim(Claim claim)
        {
            var claimsToRemove = Claims
                .Where(c => c.Type == claim.Type)
                .Where(c => c.Value == claim.Value);

            Claims = Claims.Except(claimsToRemove).ToList();
        }

        [BsonDefaultValue(false)]
        public bool HasChangePassword { set; get; }

        public string SecretToken { set; get; }
    }
}