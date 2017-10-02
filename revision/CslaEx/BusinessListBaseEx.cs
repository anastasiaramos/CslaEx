using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Runtime.Serialization;

using Csla;

using NHibernate;

namespace CslaEx
{

    [Serializable()]
    public abstract class BusinessListBaseEx<T, C> :
        BusinessListBase<T, C>
        where T : BusinessListBaseEx<T, C>
        where C : BusinessBaseEx<C>
    {

        #region Atributes And Properties

        protected int _sessCode;
        protected bool _childs = true;
        //protected SortedDictionary<long, C> _key_value_list = new SortedDictionary<long, C>();
        protected Hashtable _hash_list = new Hashtable();

        public virtual int SessionCode
        {
            get { return _sessCode; }
            set { _sessCode = value; }
        }

        //public SortedDictionary<long, C> KeyValueList { get { return _key_value_list; }}
        public Hashtable HashList {get{return _hash_list;}}

        /// <summary>
        /// Indica si se quiere que el objeto cargue los hijos
        /// </summary>
        public virtual bool Childs
        {
            get { return _childs; }
            set { _childs = value; }
        }

        /// <summary>
        /// Manejador del motor de persistencia
        /// </summary>
        /// <returns></returns>
        public virtual nHManager nHMng { get { return nHManager.Instance; } }

        #endregion

        #region Business Methods

        /// <summary>
        /// Marca toda la lista como nueva
        /// </summary>
        public void MarkAsNew() { foreach (C item in Items) item.MarkItemNew(); }

        public new void Add(C item) { this.NewItem(item); }

        /// <summary>
        /// Añade un elemento a la lista principal y a la de busqueda HASH
        /// El elemento SE CREARA en la tabla correspondiente
        /// </summary>
        /// <param name="item">Objeto a añadir</param>
        protected void NewItem(C item)
        {
            this.AddItem(item);

            //Lo  marcamos como nuevo
            item.MarkItemNew();
        }

        public virtual void AddItem(C item)
        {
            PropertyDescriptor prop = TypeDescriptor.GetProperties(item).Find("Oid", false);

            this.Add(item);

            long oid = (long)prop.GetValue(item);

            while (HashList.Contains(oid) && item.IsNew)
            {
                Random r = new Random();
                oid = (long)r.Next();
                item.Oid = oid;
            }

            HashList.Add(oid, item);
        }

        /// <summary>
        /// Devuelve una lista de los elementos del criterio
        /// </summary>
        /// <returns>Lista de elementos</returns>
        public static T GetList(CriteriaEx criteria)
        {
            BusinessListBaseEx<T, C>.BeginTransaction(criteria.SessionCode);
            return DataPortal.Fetch<T>(criteria);
        }

        /// <summary>
        /// Devuelve un elemento a partir de los datos de la lista actual
        /// </summary>
        /// <param name="criteria">Filtro</param>
        /// <returns>Objeto C</returns>
        public virtual C GetItem(FCriteria criteria)
        {
            if (Items.Count == 0) return default(C);

            PropertyDescriptor property = TypeDescriptor.GetProperties(Items[0]).Find(criteria.GetProperty(), false);

            foreach (C item in Items)
            {
                foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                    if (prop.Name == property.Name)
                    {
                        object value = prop.GetValue(item);
                        if (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
                            return item;
                    }
            }

            return default(C);
        }

        public C GetItem(long oid)
        {
            if (Items.Count == 0) return default(C);

            try
            {
                return (C)HashList[oid];
            }
            catch
            {
                return default(C);
            }
        }

        /// <summary>
        /// Elimina un elemento de la lista principal y de la de HASH
        /// </summary>
        /// <param name="oid"></param>
        public void Remove(long oid)
        {
            C obj = this.GetItem(oid);

            if (obj != null)
            {
                Remove(obj);
                HashList.Remove(obj);
            }
        }

        /// <summary>
        /// Consulta si existe un elemento
        /// </summary>
        /// <param name="oid">Oid del elemento</param>
        /// <returns></returns>
        public bool Contains(long oid) { return (this.GetItem(oid) != null); }

        /// <summary>
        /// Consulta si existe un elemento borrado
        /// </summary>
        /// <param name="oid"></param>
        /// <returns></returns>
        public bool ContainsDeleted(long oid)
        {
            foreach (C obj in DeletedList)
                return (((BusinessBaseEx<C>)obj).Oid == oid);

            return false;
        }

        /// <summary>
        /// Devuelve una lista a partir de los datos de la lista actual
        /// </summary>
        /// <param name="criteria">Filtro</param>
        /// <returns>Lista</returns>
        public virtual List<C> GetSubList(FCriteria criteria)
        {
            List<C> list = new List<C>();

            if (Items.Count == 0) return list;

            PropertyDescriptor property = TypeDescriptor.GetProperties(Items[0]).Find(criteria.GetProperty(), false);

            foreach (C item in Items)
            {
                foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                    if (prop.Name == property.Name)
                    {
                        object value = prop.GetValue(item);
                        if (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
                            list.Add(item);
                    }
            }

            return list;
        }

        /// <summary>
        /// Ordena una lista
        /// </summary>
        /// <param name="list">Lista a ordenar</param>
        /// <param name="sortProperty">Campo de ordenación</param>
        /// <param name="sortDirection">Sentido de ordenación</param>
        /// <returns>Lista ordenada</returns>
        public static SortedBindingList<C> SortList(BusinessListBaseEx<T, C> list,
                                                    string sortProperty,
                                                    ListSortDirection sortDirection)
        {
            SortedBindingList<C> sortedList = new SortedBindingList<C>(list);
            sortedList.ApplySort(sortProperty, sortDirection);
            return sortedList;
        }

        /// <summary>
        /// Carga los valores del registro en el objeto
        /// </summary>
        /// <param name="source"></param>
        /*protected IList<C> CopyValues(IDataReader source)
        {

            System.Collections.ICollection col;
            IList<C> list = new List<C>() ;

            col = nHMng.Cfg.GetClassMapping(typeof(C)).PropertyCollection;
            
            Type objType = Type.GetType(C);
            Type[] ctorParams = new Type[] { typeof(C) };

            while (source.Read())
            {

                C objeto = objType.GetConstructor(typeof(C));

                foreach (Property prop in col)
                {
                    Column columna = (Column)(((IList)(prop.ColumnCollection))[0]);
                    object value = source[columna.Text];
                    this.SetPropertyValue(prop.Name, value);
                }
            }
        }*/

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

        #region Data Access

        /// <summary>
        /// Construye el SELECT de la lista y lo ejecuta
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="sesion"></param>
        /// <returns></returns>
        public static IDataReader DoSELECT(string schema, ISession sesion)
        {
            return DoSELECT(typeof(C), schema, sesion);
        }

        /// <summary>
        /// Construye el SELECT de la lista y lo ejecuta
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="sesion"></param>
        /// <returns></returns>
        public static IDataReader DoNativeSELECT(string query, ISession sesion)
        {
            return nHManager.Instance.SQLNativeSelect(query, sesion);
        }

        /// <summary>
        /// Construye el SELECT de la lista y lo ejecuta
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <param name="sesion"></param>
        /// <returns></returns>
        public static IDataReader DoSELECT(Type type, string schema, ISession sesion)
        {
            string query = SELECT(type, schema);
            return nHManager.Instance.SQLNativeSelect(query, sesion);
        }

        /// <summary>
        /// Construye el SELECT de la lista y lo ejecuta
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="sesion"></param>
        /// <returns></returns>
        public static IDataReader DoSELECT_ORDERED(string schema, ISession sesion, string order_field)
        {
            return DoSELECT_ORDERED(typeof(C), schema, sesion, order_field);
        }

        /// <summary>
        /// Construye el SELECT de la lista y lo ejecuta
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <param name="sesion"></param>
        /// <returns></returns>
        public static IDataReader DoSELECT_ORDERED(Type type, string schema, ISession sesion, string order_field)
        {
            string query = SELECT_ORDERED(type, schema, order_field);
            return nHManager.Instance.SQLNativeSelect(query, sesion);
        }

        #endregion

        #region SQL

        /// <summary>
        /// Construye el SELECT de la lista
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <param name="sesion"></param>
        /// <returns></returns>
        public static string SELECT(string schema)
        {
            return SELECT(typeof(C), schema);
        }

        /// <summary>
        /// Construye el SELECT de la lista
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <param name="sesion"></param>
        /// <returns></returns>
        public static string SELECT(Type type, string schema)
        {
            string tabla = nHManager.Instance.Cfg.GetClassMapping(type).Table.Name;

            schema = (schema == "COMMON") ? schema : Convert.ToInt32(schema).ToString("0000");

            string query = "SELECT * " +
                            "FROM \"" + schema + "\".\"" + tabla + "\" FOR UPDATE NOWAIT;";

            return query;
        }

        /// <summary>
        /// Construye el SELECT de la lista y lo ejecuta
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <param name="sesion"></param>
        /// <returns></returns>
        public static string SELECT_ORDERED(string schema, string order_field)
        {
            return SELECT_ORDERED(typeof(C), schema, order_field);
        }

        /// <summary>
        /// Construye el SELECT de la lista y lo ejecuta
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <param name="sesion"></param>
        /// <returns></returns>
        public static string SELECT_ORDERED(Type type, string schema, string order_field)
        {
            string tabla = nHManager.Instance.Cfg.GetClassMapping(type).Table.Name;
            string columna = nHManager.Instance.GetTableField(typeof(C), order_field);
            schema = (schema == "COMMON") ? schema : Convert.ToInt32(schema).ToString("0000");

            string query = "SELECT * " +
                            "FROM \"" + schema + "\".\"" + tabla + "\" " +
                            "ORDER BY \"" + columna + "\";";

            return query;
        }

        public static string SELECT_BY_FIELD(string schema, string parent_field, object field_value)
        {

            string tabla = nHManager.Instance.Cfg.GetClassMapping(typeof(C)).Table.Name;
            string columna = nHManager.Instance.GetTableField(typeof(C), parent_field);
            string query;

            schema = (schema == "COMMON") ? schema : Convert.ToInt32(schema).ToString("0000");

            query = "SELECT * " +
                    "FROM \"" + schema + "\".\"" + tabla + "\" " +
                    "WHERE \"" + columna + "\" = " + field_value.ToString() + ";";

            return query;
        }

        public static string SELECT_BY_FIELD(string schema,
                                                string parent_field,
                                                object field_value,
                                                string order_field)
        {

            string query;
            string columna = nHManager.Instance.GetTableField(typeof(C), order_field);

            query = SELECT_BY_FIELD(schema, parent_field, field_value);

            query = query.Substring(0, query.Length - 1);

            query += "ORDER BY \"" + columna + "\";";

            return query;
        }

        #endregion
    }
}