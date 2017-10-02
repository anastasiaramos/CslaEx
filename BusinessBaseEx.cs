using System;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Reflection;
using System.ComponentModel;

using Csla;
using Csla.Core;

using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping;

namespace CslaEx
{
    [Serializable()]  
    public abstract class BusinessBaseEx<T> :
		BusinessBase<T> where T : BusinessBaseEx<T>
	{
		#region Attributes

        protected long _oid;
        protected bool _g_childs = true;
        protected bool _save_childs = true;
        protected bool _is_root_clon = false;
		protected bool _selected;
		protected bool _close_sessions = true;

		private int _sessCode = -1;

        #endregion

        #region Properties

        /// <summary>
        /// Indica si se quiere que el objeto cargue los hijos
        /// </summary>
        public virtual bool IsRootClon
        {
            get { return _is_root_clon; }
            set { _is_root_clon = value; /*MarkAsRoot();*/ }
        }

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

		public virtual bool CloseSessions { get { return _close_sessions; } set { _close_sessions= value; } }
		public virtual int SessionCode { get { return _sessCode; } set { _sessCode = value; } }

		/// <summary>
		/// Indica si se quiere guardar como parte de una transaccion externa
		/// </summary>
		public virtual bool SharedTransaction { get; set; }

		/// <summary>
        /// Indica si se quiere que el objeto cargue los hijos
        /// </summary>
        public virtual bool Childs { get; set; }

        /// <summary>
        /// Indica si se quiere que el objeto cargue los nietos
        /// </summary>
        public virtual bool GChilds { get { return _g_childs; } set { _g_childs = value; } }

        /// <summary>
        /// Indica si se quiere que el objeto guarde los hijos
        /// </summary>
        public virtual bool SaveChilds { get { return _save_childs; } set { _save_childs = value; } }

		public virtual bool IsSelected { get { return _selected; } set { _selected = value; } } 

        /// <summary>
        /// Manejador del motor de persistencia
        /// </summary>
        /// <returns></returns>
        public virtual nHManager nHMng { get { return nHManager.Instance; } }

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
		
        #endregion

        #region Business Methods

        protected override object GetIdValue() { return _oid; }

        /// <summary>
        /// Compara todas las propiedades salvo el OID
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual bool IsLike(object obj)
        {
            T item = (T)(obj);

            if (this.Oid == item.Oid) return false;

            PersistentClass pclass;
            System.Collections.ICollection cols;

            pclass = nHMng.Cfg.GetClassMapping(typeof(T));
            cols = pclass.PropertyCollection;

            foreach (Property prop in cols)
            {
               if (this.GetPropertyValue(prop.Name) != item.GetPropertyValue(prop.Name))
                  return false;
            }

            return true;
        }

        public override int GetHashCode() { return base.GetHashCode(); }
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

			object value;

            //Bucle de resto de columnas
            foreach (Property prop in cols)
            {
#if !DEBUG
				try { columna = (Column)(((IList)(prop.ColumnCollection))[0]); }
				catch { throw new Exception("BusinessBaseEx::CopyValues: Error mapeando la propiedad " + prop.Name + " en el objeto"); }

				try { value = source[columna.Text]; }
				catch { throw new Exception("BusinessBaseEx::CopyValues: No se ha encontrado el campo " + columna.Text + " en el resultado de la consulta"); }
#endif
#if DEBUG
				columna = (Column)(((IList)(prop.ColumnCollection))[0]); 
				value = source[columna.Text];
#endif
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

        /// <summary>
        /// Performs processing required when the current
        /// property has changed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method calls CheckRules(propertyName), MarkDirty and
        /// OnPropertyChanged(propertyName). MarkDirty is called such
        /// that no event is raised for IsDirty, so only the specific
        /// property changed event for the current property is raised.
        /// </para><para>
        /// This implementation uses System.Diagnostics.StackTrace to
        /// determine the name of the current property, and so must be called
        /// directly from the property to be checked.
        /// </para>
        /// </remarks>
        [System.Runtime.CompilerServices.MethodImpl(
          System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        protected void PropertyHasChangedEx(string property)
        {
            try
            {
                string propertyName =
                  new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name.Substring(4);
                
                PropertyHasChanged(propertyName);
            }
            catch
            {
                string tipo = this.GetType().Name;
                throw new Exception("BusinessBaseEx::PropertyHasChanged: Error mapeando la propiedad '" + property + "' en el objeto " + tipo);
            }

        }

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
		
		public static T Get(string query, bool childs = false, int sessionCode = -1)
		{
			CriteriaEx criteria = GetCriteria((sessionCode != -1) ? sessionCode : OpenSession());
			criteria.Childs = childs;

			if (nHManager.Instance.UseDirectSQL) criteria.Query = query;

			if (sessionCode == -1) BeginTransaction(criteria.Session);

			T obj = DataPortal.Fetch<T>(criteria);

			obj.SharedTransaction = (sessionCode != -1);

			return (obj.Oid != 0) ? obj : null;
		}

		public static T Get(long oid, bool childs, int sessionCode)
		{
            string query = (string)typeof(T).InvokeMember("SELECT"
                                                    , BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public
                                                    , null, null, new object[1] { oid });
			return Get(query, childs, sessionCode);
		}

        /// <summary>
        /// Saves the object to the database when is a list child.
        /// </summary>
        /// <remarks>
        public virtual T SaveAsChild()
        {
            if (IsDirty)
                return (T)DataPortal.Update(this);
            else
                return (T)this;
        }

		public virtual T SharedSave(int sessionCode)
		{
			SessionCode = sessionCode;
			return SharedSave();
		}
		protected virtual T SharedSave() { throw new NotImplementedException(); }

		protected virtual void SetSharedSession(int sessionCode)
		{
			SessionCode = sessionCode;
			SharedTransaction = (sessionCode != -1);
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
        public static void DoLOCK(ISession session)
        {
            string query = nHManager.Instance.LOCK(typeof(T), null);
            nHManager.Instance.SQLNativeExecute(query, session);
        }

        /// <summary>
        /// Construye y ejecuta un LOCK para el esquema dado
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="sesion">sesión abierta para la transacción</param>
        public static void DoLOCK(string schema, ISession session)
        {
            string query = nHManager.Instance.LOCK(typeof(T), null);
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
			string query = SELECT_DEPRECATED(schema, oid);
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

		public static void ExecuteSQL(CriteriaEx criteria)
		{
			nHManager.Instance.SQLNativeExecute(criteria.Query, Session(criteria.SessionCode));
		}

        #endregion

		#region SQL

        public static string LIMIT(PagingInfo pagingInfo)
        {
            return @"
				LIMIT " + pagingInfo.ItemsPerPage + " OFFSET " + pagingInfo.CurrentPage * pagingInfo.ItemsPerPage;
        }

        /// <summary>
        /// Construye un LOCK para el esquema dado
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="sesion">sesión abierta para la transacción</param>
        public static string LOCK()
        {
            return nHManager.Instance.LOCK(typeof(T), null);
        }
		public static string LOCK(string schema)
		{
            return nHManager.Instance.LOCK(typeof(T), schema);
		}

        public static string ORDER(OrderList orders, string tableAlias, Dictionary<String, ForeignField> foreignFields)
        {
            return FilterMng.GET_ORDERS_SQL(orders, tableAlias, foreignFields);
        }

        /// <summary>
        /// Construye un SELECT para el esquema dado
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="sesion">sesión abierta para la transacción</param>
        /// <returns></returns>
        public static string SELECT(long oid)
        {
            return nHManager.Instance.SELECT(typeof(T), null, true, "Oid", oid, null);
        }
		public static string SELECT_DEPRECATED(string schema, long oid)
		{
            return nHManager.Instance.SELECT(typeof(T), schema, true, "Oid", oid, null);
		}

        public static string SELECT_COUNT(CriteriaEx criteria)
        {
            criteria.Select = @"SELECT COUNT(*) AS ""TOTAL_ROWS""";
            criteria.From = criteria.Query.Substring(criteria.Query.IndexOf("FROM"));

            int orderPos = criteria.From.IndexOf("ORDER BY ");

            orderPos = (orderPos < 0) ? 0 : orderPos;

            criteria.From = criteria.From.Substring(0, orderPos);

            return criteria.Query;
        }

		#endregion

		#region NHibernate Default Interface

		/// <summary>
		/// Devuelve un criterio de búsqueda para este tipo asociado a la sesión abierta
		/// </summary>
		/// <returns></returns>
		public virtual CriteriaEx GetCriteria() { return GetCriteria(SessionCode); }

		public virtual ITransaction BeginTransaction() { return BeginTransaction(SessionCode); }

		public virtual ISession Session() { return Session(SessionCode); }

		public virtual ITransaction Transaction() { return Transaction(_sessCode); }

		public virtual void CloseSession()
		{
			if (_sessCode == -1) return;

			CloseSession(_sessCode);
			_sessCode = -1;
		}

		#endregion

		#region NHibernate By Code Interface

		public virtual void NewSession() { SessionCode = nHManager.Instance.OpenSession(); }
		public virtual void NewTransaction() { NewSession(); BeginTransaction(SessionCode); }

		/// <summary>
		/// Abre una nueva sesión 
		/// </summary>
		/// <returns>Código de la sesión</returns>
		public static int OpenSession() { return nHManager.Instance.OpenSession(); }

		/// <summary>
		/// Devuelve un criterio de búsqueda para este tipo asociado a una sesión abierta
		/// </summary>
		/// <returns></returns>
		public static CriteriaEx GetCriteria(int sessionCode) { return nHManager.Instance.GetCriteria(typeof(T), sessionCode); }

		/// <summary>
		/// Inicia una transacción para la sessión actual
		/// </summary>
		/// <returns></returns>
		public static ITransaction BeginTransaction(int sessionCode) { return nHManager.Instance.BeginTransaction(sessionCode); }

		/// <summary>
		/// Devuelve la sesión correspondiente a este objeto
		/// </summary>
		/// <returns></returns>
		public static ISession Session(int sessionCode)
		{
            try
            {
                return nHManager.Instance.GetSession(sessionCode);
            }
            catch (CslaSessionException)
            {
                throw new CslaSessionException(typeof(T), sessionCode);
            }
		}

		/// <summary>
		/// Devuelve la transacción correspondiente a este objeto
		/// </summary>
		/// <returns></returns>
		public static ITransaction Transaction(int sessionCode) { return nHManager.Instance.GetTransaction(sessionCode); }

		/// <summary>
		/// Cierra la sesión que se creó para el objeto
		/// </summary>
		/// <returns></returns>
		public static void CloseSession(int sessionCode) { nHManager.Instance.CloseSession(sessionCode); }

		#endregion

		#region NHibernate By Session Interface

		/// <summary>
		/// Devuelve un criterio de búsqueda para este tipo asociado a la sesión abierta
		/// </summary>
		/// <returns></returns>
		public static CriteriaEx GetCriteria(ISession sess) { return nHManager.Instance.GetCriteria(sess, typeof(T)); }

		/// <summary>
		/// Inicia una transacción para una sessión
		/// </summary>
		/// <returns></returns>
		public static ITransaction BeginTransaction(ISession sess) { return nHManager.Instance.BeginTransaction(sess); }

		/// <summary>
		/// Devuelve la transacción correspondiente a una sesión
		/// </summary>
		/// <returns></returns>
		public static ITransaction Transaction(ISession sess) { return nHManager.Instance.GetTransaction(sess); }

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
