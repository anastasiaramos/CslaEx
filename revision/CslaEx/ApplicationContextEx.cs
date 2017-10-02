using System;
using System.Security.Principal;

using Csla;

namespace CslaEx
{
	/// <summary>
	/// Extends the Csla.ApplicationContext class
	/// </summary>
    public static class ApplicationContextEx
    {
        #region User

        /// <summary>
        /// Get or set the current <see cref="IPrincipal" />
        /// object representing the user's identity.
        /// </summary>
        /// <remarks>
        /// This is discussed in Chapter 5. When running
        /// under IIS the HttpContext.Current.User value
        /// is used, otherwise the current Thread.CurrentPrincipal
        /// value is used.
        /// </remarks>
        public static IPrincipalEx User
        {
            get
            {
				if (Csla.ApplicationContext.User is IPrincipalEx)
					return (IPrincipalEx)(Csla.ApplicationContext.User);
				else
					return null;
            }

            set
            {
                Csla.ApplicationContext.User = (IPrincipal)value;
            }
        }

        #endregion

    }
}
