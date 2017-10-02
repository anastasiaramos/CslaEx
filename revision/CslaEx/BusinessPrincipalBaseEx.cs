using System;
using System.Security.Principal;

using Csla;
using Csla.Security;

namespace CslaEx
{
    public interface IIdentityEx : IIdentity
    {
		string GetName(); 
		string GetPassword();

		bool IsReadable(string elemento);
        bool IsWritable(string elemento);

		bool IsAdmin { get; }
	}

    /// <summary>
    /// Base class from which custom principal
    /// objects should inherit to operate
    /// properly with the data portal.
    /// </summary>
    [Serializable()]
    public class BusinessPrincipalBaseEx : BusinessPrincipalBase, IPrincipalEx 
    {
        private IIdentityEx _identity;

        /// <summary>
        /// Returns the user's identity object.
        /// </summary>
        public new IIdentityEx Identity
        {
            get { return _identity; }
        }

        protected BusinessPrincipalBaseEx(IIdentityEx identity) 
			: base(identity)
        { 
            _identity = identity; 
        }

        public virtual bool CanReadObject(string nombre_elemento)
        {
            return false;
        }

        public virtual bool CanWriteObject(string nombre_elemento)
        {
            return false;
        }

  }
}