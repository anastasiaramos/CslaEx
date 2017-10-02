using System;
using System.Collections.Generic;
using System.Collections;
using System.Data;

using Csla;
using Csla.Core;

using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping;

using System.Reflection;
using System.ComponentModel;
using Csla.Properties;

namespace CslaEx
{
    [Serializable()]  
    public abstract class BusinessBaseEx<T> :
		BusinessBase<T> where T : BusinessBaseEx<T>
	{

		#region Business Methods

        protected long _oid;
        protected bool _childs = true;

		private int _sessCode;

        [System.ComponentModel.DataObjectField(true)]
        public virtual long Oid
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            get
            {
                CanReadProperty(true);
                return _oid;
            }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            set
            {
                CanWriteProperty(true);
                _oid = value;
            }
        }

		public virtual int SessionCode
		{
			get { return _sessCode; }
			set { _sessCode = value; }
		}

		protected override object GetIdValue() { return _oid; }

		/// <summary>
        /// Indica si se quiere que el objeto cargue los hijos
        /// </summary>
        public virtual bool Childs
        {
            get { return _childs; }
            set { _childs = value; }
        }

		/// <summary>
		/// Devuelve el valor de una propiedad a partir de su nombre
		/// </summary>
		/// <param name="name">Nombre de la propiedad</param>
		/// <returns></returns>
		public virtual object GetPropertyValue(string name)
		{
			Type type = typeof(T);
			System.Reflection.PropertyInfo prop = type.GetProperty(name);

			return prop.GetValue(this, null);
		}
		
		/// <summary>
		/// Asigna el valor de una propiedad a partir de su nombre
		/// </summary>
		/// <param name="name">Nombre de la propiedad</param>
		/// <param name="value">Valor</param>
		public virtual void SetPropertyValue(string name, object value)
		{
			Type type = typeof(T);
			System.Reflection.PropertyInfo prop = type.GetProperty(name);

            // Para no rellenar los campos de los inner join que no están mapeados
			if (prop != null) prop.SetValue(this, value, null);
		}
		
		/// <summary>
		/// Manejador del motor de persistencia
		/// </summary>
		/// <returns></returns>
		public virtual nHManager nHMng { get { return nHManager.Instance; } }

        /// <summary>
        /// Copia los atributos del objeto a partir de otro objeto
        /// </summary>
        /// <param name="source">Objeto origen</param>
        protected virtual void CopyValues(T source)
        {
            PersistentClass pclass;
            System.Collections.ICollection cols;

            //Obtencion de la información de mapeo
            pclass = nHMng.Cfg.GetClassMapping(typeof(T));
            cols = pclass.PropertyCollection;

            //Bucle de resto de columnas
            foreach (Property prop in cols)
                this.SetPropertyValue(prop.Name, source.GetPropertyValue(prop.Name));
        }

        /// <summary>
        /// Carga los valores de un registro apuntado por un IDataReader en el objeto.
        /// Consulta el fichero de mapeo de tablas de nHibernate para rellenar las propiedades.
        /// </summary>
        /// <param name="source"></param>
        protected virtual void CopyValues(IDataReader source)
        {
            PersistentClass pclass;
            System.Collections.ICollection cols;

            //Obtencion de la información de mapeo
            pclass = nHMng.Cfg.GetClassMapping(typeof(T));
            cols = pclass.PropertyCollection;

            //Columna de Primary Key
            Column columna = (Column)((IList)(pclass.Identifier.ColumnCollection))[0];
            this.SetPropertyValue("Oid", source[columna.Text]);

            //Bucle de resto de columnas
            foreach (Property prop in cols)
            {
                columna = (Column)(((IList)(prop.ColumnCollection))[0]);
                object value = source[columna.Text];
                if (!DBNull.Value.Equals(value))
                    this.SetPropertyValue(prop.Name, value);
            }
        }

		// Para las listas de objetos hijo
		public virtual void MarkItemNew() { MarkNew(); }

		// Interfaz pública para poder hacer listas de objetos root
		public virtual void MarkItemChild() { MarkAsChild(); }
		public virtual void MarkItemOld() { MarkOld(); }

		// Interfaz pública para poder agregar elementos a una lista hija
		public virtual void MarkItemDirty() { MarkDirty(); }

		#endregion

		#region Factory Methods

		/// <summary>
		/// Clausura la transaccion y sesion actual 
		/// </summary>
		public virtual void CloseDBObject()
		{
			if (Transaction() != null)
			{
				Transaction().Rollback();
				Transaction().Dispose();
			}

			CloseSession();
		}

		#endregion

		#region Common Data Access

		[Serializable()]
		protected class CriteriaCs : CriteriaBase
		{
			private struct Exp
			{
				public string Name;
				public object Value;
			}

			private List<Exp> _exps = new List<Exp>();
			
			public long Oid
			{
				get { return (long)GetValue("Oid"); }
				set { Add("Oid", value); }
			}

			public CriteriaCs(long oid)
				: base(typeof(T))
			{
				Oid = oid;
			}

			public CriteriaCs(string name, object value)
				: base(typeof(T))
			{
				Add(name, value);
			}

			public void Add(string name, object value)
			{
				Exp exp = new Exp();

				exp.Name = name;
				exp.Value = value;

				_exps.Add(exp); 
			}

			public object GetValue(string name)
			{
				foreach (Exp exp in _exps)
					if (exp.Name.Equals(name))
						return (long)exp.Value;

				return 0;
			}
		}

		#endregion

        #region Data Access

        /// <summary>
        /// Construye y ejecuta un LOCK para el esquema dado
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="sesion">sesión abierta para la transacción</param>
        public static void DoLOCK(string schema, ISession session)
        {
			string query = LOCK(schema);
			nHManager.Instance.SQLNativeExecute(query, session);
        }

        /// <summary>
        /// Construye y ejecuta un SELECT para el esquema dado
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="sesion">Sesión abierta para la transacción</param>
        /// <returns></returns>
        public static IDataReader DoSELECT(string schema, ISession session, long oid)
        {
			string query = SELECT(schema, oid);
            return nHManager.Instance.SQLNativeSelect(query, session);
        }

		/// <summary>
		/// Construye y ejecuta un SELECT para el esquema dado
		/// </summary>
		/// <param name="schema"></param>
		/// <param name="sesion">sesión abierta para la transacción</param>
		/// <returns></returns>
		public static IDataReader DoNativeSELECT(string query, ISession session)
		{
			return nHManager.Instance.SQLNativeSelect(query, session);
		}

        #endregion

		#region SQL

		/// <summary>
		/// Construye un LOCK para el esquema dado
		/// </summary>
		/// <param name="schema"></param>
		/// <param name="sesion">sesión abierta para la transacción</param>
		public static string LOCK(string schema)
		{
			string tabla = nHManager.Instance.Cfg.GetClassMapping(typeof(T)).Table.Name;
			string query;
			schema = (schema == "COMMON") ? schema : Convert.ToInt32(schema).ToString("0000");

			query = "LOCK TABLE \"" + schema + "\".\"" + tabla + "\"" + " IN ROW EXCLUSIVE MODE NOWAIT;";

			return query;
		}

		/// <summary>
		/// Construye un SELECT para el esquema dado
		/// </summary>
		/// <param name="schema"></param>
		/// <param name="sesion">sesión abierta para la transacción</param>
		/// <returns></returns>
		public static string SELECT(string schema, long oid)
		{
			string tabla = nHManager.Instance.Cfg.GetClassMapping(typeof(T)).Table.Name;
			string query;
			schema = (schema == "COMMON") ? schema : Convert.ToInt32(schema).ToString("0000");

			query = "SELECT * " +
				   "FROM \"" + schema + "\".\"" + tabla + "\" " +
				   "WHERE \"OID\" = " + oid.ToString() + 
                   " FOR UPDATE NOWAIT;";

			return query;
		}

		#endregion

		#region NHibernate Default Interface

		/// <summary>
		/// Devuelve un criterio de búsqueda para este tipo asociado a la sesión abierta
		/// </summary>
		/// <returns></returns>
		public virtual CriteriaEx GetCriteria()
		{
			return GetCriteria(SessionCode);
		}

		public virtual ITransaction BeginTransaction()
		{
			return BeginTransaction(SessionCode);
		}

		public virtual ISession Session()
		{
			return Session(SessionCode);
		}

		public virtual ITransaction Transaction()
		{
			return Transaction(_sessCode);
		}

		public virtual void CloseSession()
		{
			CloseSession(_sessCode);
		}

		#endregion

		#region NHibernate By Code Interface

		/// <summary>
		/// Abre una nueva sesión 
		/// </summary>
		/// <returns>Código de la sesión</returns>
		public static int OpenSession()
		{
			return nHManager.Instance.OpenSession();
		}

		/// <summary>
		/// Devuelve un criterio de búsqueda para este tipo asociado a una sesión abierta
		/// </summary>
		/// <returns></returns>
		public static CriteriaEx GetCriteria(int sessionCode)
		{
			return nHManager.Instance.GetCriteria(typeof(T), sessionCode);
		}

		/// <summary>
		/// Inicia una transacción para la sessión actual
		/// </summary>
		/// <returns></returns>
		public static ITransaction BeginTransaction(int sessionCode)
		{
			return nHManager.Instance.BeginTransaction(sessionCode);
		}

		/// <summary>
		/// Devuelve la sesión correspondiente a este objeto
		/// </summary>
		/// <returns></returns>
		public static ISession Session(int sessionCode)
		{
			return nHManager.Instance.GetSession(sessionCode);
		}

		/// <summary>
		/// Devuelve la transacción correspondiente a este objeto
		/// </summary>
		/// <returns></returns>
		public static ITransaction Transaction(int sessionCode)
		{
			return nHManager.Instance.GetTransaction(sessionCode);
		}

		/// <summary>
		/// Cierra la sesión que se creó para el objeto
		/// </summary>
		/// <returns></returns>
		public static void CloseSession(int sessionCode)
		{
			nHManager.Instance.CloseSession(sessionCode);
		}

		#endregion

		#region NHibernate By Session Interface

		/// <summary>
		/// Devuelve un criterio de búsqueda para este tipo asociado a la sesión abierta
		/// </summary>
		/// <returns></returns>
		public static CriteriaEx GetCriteria(ISession sess)
		{
			return nHManager.Instance.GetCriteria(sess, typeof(T));
		}

		/// <summary>
		/// Inicia una transacción para una sessión
		/// </summary>
		/// <returns></returns>
		public static ITransaction BeginTransaction(ISession sess)
		{
			return nHManager.Instance.BeginTransaction(sess);
		}

		/// <summary>
		/// Devuelve la transacción correspondiente a una sesión
		/// </summary>
		/// <returns></returns>
		public static ITransaction Transaction(ISession sess)
		{
			return nHManager.Instance.GetTransaction(sess);
		}

		#endregion

		#region NHibernate By Oid Interface (No tira)

		/// <summary>
		/// Devuelve la sesión correspondiente a este objeto
		/// </summary>
		/// <returns></returns>
		public static ISession Session(long oid)
		{
			return nHManager.Instance.GetSession(typeof(T), oid);
		}

		/// <summary>
		/// Inicia una transacción para la sessión actual
		/// </summary>
		/// <returns></returns>
		public static ITransaction BeginTransaction(long oid)
		{
			return nHManager.Instance.BeginTransaction(Session(oid));
		}

		/// <summary>
		/// Devuelve la transacción correspondiente a este objeto
		/// </summary>
		/// <returns></returns>
		public static ITransaction Transaction(long oid)
		{
			return nHManager.Instance.GetTransaction(typeof(T), oid);
		}

		#endregion

    }
}
