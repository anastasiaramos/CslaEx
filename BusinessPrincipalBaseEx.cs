using System;
using System.Collections;
using System.Security.Principal;

using Csla.Security;

namespace CslaEx
{
    public interface IIdentityEx : IIdentity
    {
		long Oid { get; }

		string GetName(); 
		string GetPassword();

		bool IsReadable(long elemento);
        bool IsCreable(long elemento);
        bool IsModifiable(long elemento);
        bool IsRemovable(long elemento);

		bool IsSuperUser { get; }
		bool IsAdmin { get; }
		bool IsPartner { get; }
		bool IsClient { get; }
	}

    [Serializable()]
    public class BusinessPrincipalBaseEx : BusinessPrincipalBase, IPrincipalEx 
    {
        /// <summary>
        /// Returns the user's Identity object.
        /// </summary>
		public new IIdentityEx Identity { get { return (IIdentityEx)base.Identity; } }

        protected BusinessPrincipalBaseEx(IIdentityEx identity) 
			: base(identity) {}

		public override bool IsInRole(string role)
		{
			switch (role)
			{
				case "ADMIN": return Identity.IsAdmin;
				case "SUPERUSER": return Identity.IsSuperUser;
				case "PARTNER": return Identity.IsPartner;
				case "CLIENT": return Identity.IsClient;

				default: return false;
			}
		}

        public virtual bool CanReadObject(long tipo_elemento) { return false; }
        public virtual bool CanCreateObject(long tipo_elemento) { return false; }
        public virtual bool CanModifyObject(long tipo_elemento) { return false; }
        public virtual bool CanRemoveObject(long tipo_elemento) { return false; }
	}
}