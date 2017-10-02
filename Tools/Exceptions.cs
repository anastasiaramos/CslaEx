using System;

using CslaEx.Resources;

namespace CslaEx
{

    /// <summary>
    /// Excepcion genérica
    /// </summary>
	public class CslaException : System.Exception 
	{
        public const string NH_SESSION_NOT_FOUND = "CS_00001";
        
        private string _code;

        /// <summary>
        /// Codigo de error
        /// </summary>
        public virtual string Code
        {
            get { return _code; }
            set { _code = value; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="msg">Mensaje</param>
		public CslaException(string msg) : base(msg) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="msg">Mensaje</param>
        /// <param name="code">Código del mensaje</param>
        public CslaException(string msg, string code) : base(msg) 
        {
            _code = code;
        }

    }
	
    /// <summary>
    /// Exception de error de sesión
    /// </summary>
	public class CslaSessionException : CslaException
	{
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="method">Metodo que provoca la excepcion</param>
		public CslaSessionException(Type type, long session_number)
			: base(String.Format(Messages.SESSION_EXCEPTION, session_number.ToString(), type.Name)) 
        {
            Code = NH_SESSION_NOT_FOUND;
        }

	}

}