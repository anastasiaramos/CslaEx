using System;
using System.Reflection;
using System.Collections;
using System.Data;

using Csla;
using Csla.Core;

using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping;


namespace CslaEx
{
    [Serializable()]
	public abstract class ReadOnlyBaseEx<T> : ReadOnlyBase<T> 
        where T : ReadOnlyBaseEx<T>
    {

		#region Business Methods

		protected int _sessCode;
		protected long _oid;
        protected bool _childs = false;
        protected AttributeMng attMng = new AttributeMng();

		[System.ComponentModel.DataObjectField(true)]
		public long Oid
		{
			get { return _oid; }
			set { _oid = value; }
		}

		public int SessionCode
		{
			get { return _sessCode; }
			set { _sessCode = value; }
		}

        /// <summary>
        /// Indica si se quiere que el objeto cargue los hijos
        /// </summary>
        public bool Childs
        {
            get { return _childs; }
            set { _childs = value; }
        }

        /// <summary>
		/// Manejador del motor de persistencia
		/// </summary>
		/// <returns></returns>
		public virtual nHManager nHMng { get { return nHManager.Instance; } }

		protected override object GetIdValue() { return _oid; }

        /// <summary>
        /// Obtiene un <see cref="ReadOnlyBaseEx"/> a partir de un registro de la base de datos
        /// </summary>
        /// <returns>Objeto <see cref="ReadOnlyBaseEx"/> construido a partir del registro</returns>
		public static T Get(CriteriaEx criteria)
		{
			T obj = DataPortal.Fetch<T>(criteria);
			CloseSession(criteria.SessionCode);
			return obj;
		}

        /// <summary>
        /// Devuelve el tipo de una propiedad a partir de su nombre
        /// </summary>
        /// <param name="name">Nombre de la propiedad</param>
        /// <returns></returns>
        public Type GetPropertyType(string name)
        {
            Type type = typeof(T);
            System.Reflection.PropertyInfo prop = type.GetProperty(name);

            return prop.PropertyType;
        }

        /// <summary>
        /// Devuelve el valor de una propiedad a partir de su nombre
        /// </summary>
        /// <param name="name">Nombre de la propiedad</param>
        /// <returns></returns>
        //public virtual object GetPropertyValue(string name)
        //{
        //    Type type = typeof(T);
        //    System.Reflection.PropertyInfo prop = type.GetProperty(name);

        //    return prop.GetValue(this, null);
        //}

        /// <summary>
        /// Asigna el valor de una propiedad a partir de su nombre
        /// </summary>
        /// <param name="name">Nombre de la propiedad</param>
        /// <param name="value">Valor</param>
        //public virtual void SetPropertyValue(string name, object value)
        //{
        //    Type type = typeof(T);
        //    System.Reflection.PropertyInfo prop = type.GetProperty(name);

        //    // Para no rellenar los campos de los inner join que no están mapeados
        //    if (prop != null) prop.SetValue(this, value, null);
        //}

        protected virtual void CopyValues(IDataReader source){}

        /// <summary>
        /// Asigna a cada atributo de la clase el valor correspondiente del campo de la base de datos
        /// </summary>
        /// <param name="source"></param>
        public unsafe virtual void CopyValues(Type type, IDataReader source)
        {
            object value;

            //Se trata independientemente 
            _oid = Convert.ToInt64(source[nHMng.GetTableID(type)]);

            foreach (AttributeMng.TAttribute atri in attMng.Lista)
            {
                value = source[nHMng.GetTableField(type, (atri.propiedad).ToString())];

                switch (GetPropertyType(atri.propiedad).ToString())
                {
                    case "System.Int32":
                        {
                            *((int*)(atri.atributo)) = (DBNull.Value.Equals(value)) ? 0 : Convert.ToInt32(value);
                        } break;
                    case "System.Int64":
                        {
                            *((long*)(atri.atributo)) = (DBNull.Value.Equals(value)) ? 0 : Convert.ToInt64(value);
                        } break;
                    case "System.Boolean":
                        {
                            *((bool*)(atri.atributo)) = (DBNull.Value.Equals(value)) ? false : Convert.ToBoolean(value);
                        } break;
                    case "System.DateTime":
                        {
                            *((DateTime*)(atri.atributo)) = (DBNull.Value.Equals(value)) ? DateTime.MinValue : Convert.ToDateTime(value);
                        } break;
                    case "System.Decimal":
                        {
                            *((decimal*)(atri.atributo)) = (DBNull.Value.Equals(value)) ? 0 : Convert.ToDecimal(value);
                        } break;
                    case "System.Double":
                        {
                            *((double*)(atri.atributo)) = (DBNull.Value.Equals(value)) ? 0 : Convert.ToDouble(value);
                        } break;
                    default: break;
                }

            }
        }

        //Esto fue un intento de copiar automáticamente los valores de un info en un print
        //pero no se puede porque las propiedades no tiene método get y los atributos son private
        /// <summary>
        /// Carga los valores de un registro apuntado por un IDataReader en el objeto.
        /// Consulta el fichero de mapeo de tablas de nHibernate para rellenar las propiedades.
        /// </summary>
        /// <param name="source"></param>
        //protected virtual void CopyPrintingValues(Type type, T source)
        //{
        //    //Bucle de resto de columnas
        //    foreach (TypeAttributes atrib in type.)
        //    {
        //        //si es una lista no se copia porque esta función se usa en los print, que no tienen listas
        //        if (atrib is ICollection) continue;
        //        object value = source.GetPropertyValue(atrib..Name);
        //        if (!DBNull.Value.Equals(value))
        //            this.SetPropertyValue(prop.Name, value);
        //    }
        //}

		#endregion

        #region SQL

        /// <summary>
        /// Construye un SELECT para el esquema dado
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="sesion">sesión abierta para la transacción</param>
        /// <returns></returns>
        public static string SELECT(Type type, string schema, long oid)
        {
            string tabla = nHManager.Instance.Cfg.GetClassMapping(typeof(T)).Table.Name;
            string query;
            schema = (schema == "COMMON") ? schema : Convert.ToInt32(schema).ToString("0000");

            query = "SELECT * " +
                   "FROM \"" + schema + "\".\"" + tabla + "\" " +
                   "WHERE \"OID\" = " + oid.ToString() + ";";

            return query;
        }

        #endregion

        #region NHibernate Default Interface

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
		/// <returns></returns>
		public static int OpenSession()
		{
			return nHManager.Instance.OpenSession();
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
    }
}
