using System;

using Csla;

using NHibernate;
using NHibernate.Impl;
using NHibernate.Cfg;
using NHibernate.Expression;

namespace CslaEx
{

	/// <summary>
	/// Criterio de filtrado para una consulta al Fetch de DataPortal
	/// Es heredero del criterio de NHibernate
	/// </summary>
    [Serializable()]
	public class CriteriaEx : CriteriaImpl, ICriteria
	{
		
		#region Bussiness Methods

		private long _oid = -1;
		private ISession _session = null;
		private int _sessCode = -1;
        private bool _get_childs = true;
        private string _query = string.Empty;

		public virtual long Oid
		{
			get { return _oid; }
			set { _oid = value; }
		}

		public virtual ISession Session
		{
			get { return _session; }
			set { _session = value; }
		}
		public virtual int SessionCode
		{
			get { return _sessCode; }
			set { _sessCode = value; }
		}
        public virtual bool Childs
        {
            get { return _get_childs; }
            set { _get_childs = value; }
        }
        public virtual string Query
        {
            get { return _query; }
            set { _query = value; }
        }
 
		public virtual nHManager GetnHMng()
		{
			return nHManager.Instance;
		}

		#endregion

		#region Factory Method

		public CriteriaEx(long oid)
			: base(null, null)
		{
			_oid = oid;
		}

		public CriteriaEx(Type type, SessionImpl session, int code) 
			: base(type, session)
		{
			Session = session;
			SessionCode = code;
		}

		/// <summary>
		/// Expresión genérica
		/// </summary>
		/// <param name="exp"></param>
		public void Add(Expression exp)
		{
			Add(exp);
		}

		/// <summary>
		/// Busqueda por Oid
		/// </summary>
		/// <param name="value">Valor del oid</param>
		public void AddOidSearch(long value)
		{
            _oid = value;
            Add(Expression.Eq("Oid", value));
		}

        /// <summary>
        /// Expresión genérica
        /// </summary>
        /// <param name="exp"></param>
        public void AddBetween(string property, object lo, object hi)
        {
            Add(Expression.Between(property, lo, hi));
        }

		/// <summary>
		/// Búsqueda por Codigo Alfanumérico
		/// </summary>
		/// <param name="value">Valor del código</param>
		public void AddCodeSearch(string value)
		{
			Add(Expression.Eq("Codigo", value));
		}

		/// <summary>
		/// Búsqueda por Codigo Numérico
		/// </summary>
		/// <param name="value">Valor del código</param>
		public void AddCodeSearch(long value)
		{
			Add(Expression.Eq("Codigo", value));
		}

        /// <summary>
        /// Búsqueda por Número Alfanumérico
        /// </summary>
        /// <param name="value">Valor del código</param>
        public void AddNumberSearch(string value)
        {
            Add(Expression.Eq("Numero", value));
        }

        /// <summary>
        /// Búsqueda por Codigo Numérico
        /// </summary>
        /// <param name="value">Valor del código</param>
        public void AddNumberSearch(long value)
        {
            Add(Expression.Eq("Numero", value));
        }

		/// <summary>
		/// Expresión de igualdad
		/// </summary>
		/// <param name="exp"></param>
		public void AddEq(string property, object value)
		{
			Add(Expression.Eq(property, value));
		}

		/// <summary>
		/// Expresión or de igualdad
		/// </summary>
		/// <param name="exp"></param>
		public void AddEqOr(string property, object value, string property2, object value2)
		{
			Add(Expression.Or(Expression.Eq(property, value), Expression.Eq(property2, value2)));
		}

		/// <summary>
		/// Expresión or de igualdad
		/// </summary>
		/// <param name="exp"></param>
		public void AddBetweenOr(string property1, object value11, object value12, string property2,                                         object value21, object value22)
		{
			Add(Expression.Or(Expression.Between(property1, value11, value12), Expression.Between                                    (property2, value21, value22)));
		}

        /// <summary>
        /// Expresión genérica
        /// </summary>
        /// <param name="exp"></param>
        public void AddGT(string property, object value)
        {
            Add(Expression.Gt(property, value));
        }

        /// <summary>
        /// Expresión genérica
        /// </summary>
        /// <param name="exp"></param>
        public void AddLT(string property, object value)
        {
            Add(Expression.Lt(property, value));
        }

		/// <summary>
		/// La propiedad empieza por una subristra
		/// </summary>
		/// <param name="exp"></param>
		public void AddStartsWith(string property, string value)
		{
			Add(Expression.InsensitiveLike(property, value + "%"));
		}

		/// <summary>
		/// Insensitive Like
		/// </summary>
		/// <param name="exp"></param>
		public void AddContains(string property, string value)
		{
			Add(Expression.InsensitiveLike(property, "%" + value + "%"));
		}

		/// <summary>
		/// Añade un orden de filtrado
		/// </summary>
		/// <param name="exp"></param>
		public void AddOrder(string property, bool ascending)
		{
			Order order = new Order(property, ascending);
			AddOrder(order);
		}

		/// <summary>
		/// Expresión genérica
		/// </summary>
		/// <param name="exp"></param>
		public void AddSQL(string query)
		{
			Add(Expression.Sql(query));
		}

        /// <summary>
        /// Búsqueda por campo distinto del valor pasado de tipo long
        /// </summary>
        /// <param name="value">Valor del campo</param>
        public void AddDistinctValue(string propertyName,long value)
        {
            Add(Expression.Not(Expression.Eq(propertyName, value)));
        }

		#endregion

	}
}