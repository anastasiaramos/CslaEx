using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Data;

using Csla;
using Csla.Core;

using NHibernate;

namespace CslaEx
{
    [Serializable()]
    public abstract class ReadOnlyListBaseEx<T, C> : ReadOnlyListBase<T, C>
        where T : ReadOnlyListBaseEx<T, C>
        where C : ReadOnlyBaseEx<C>
    {

        #region Business Methods

        protected int _sessCode;
        protected bool _childs = false;
        //protected SortedDictionary<long, C> _key_value_list = new SortedDictionary<long, C>();
        protected Hashtable _hash_list = new Hashtable();

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

        //public SortedDictionary<long, C> KeyValueList { get { return _key_value_list; } }
        public Hashtable HashList { get { return _hash_list; } }

        /// <summary>
        /// Manejador del motor de persistencia
        /// </summary>
        /// <returns></returns>
        public virtual nHManager nHMng { get { return nHManager.Instance; } }

        public void AddItem(C item)
        {
            PropertyDescriptor prop = TypeDescriptor.GetProperties(item).Find("Oid", false);

            IsReadOnly = false;
            this.Add(item);
            //KeyValueList.Add((long)prop.GetValue(item), item);
            HashList.Add((long)prop.GetValue(item), item);
            IsReadOnly = true;
        }

        public C GetItem(long oid)
        {
            if (Items.Count == 0) return default(C);

            //PropertyDescriptor prop = TypeDescriptor.GetProperties(Items[0]).Find("Oid", false);
            //int pos = FindCore(prop, oid);

            try
            {
                return (C)HashList[oid];
            }
            catch
            {
                return default(C);
            }
        }

        public virtual C GetItemByProperty(string property, object o)
        {
            if (Items.Count == 0) return default(C);

            PropertyDescriptor prop = TypeDescriptor.GetProperties(Items[0]).Find(property, false);
            int pos = FindCore(prop, o);
            if (pos != -1)
                return Items[pos];

            return default(C);
        }

        public virtual C GetItem(FCriteria criteria)
        {
            if (Items.Count == 0) return default(C);

            PropertyDescriptor prop = TypeDescriptor.GetProperties(Items[0]).Find(criteria.GetProperty(), false);
            int pos = FindCore(prop, criteria.GetValue());
            if (pos != -1)
                return Items[pos];

            return default(C);
        }

        /// <summary>
        /// Devuelve una lista de todos los elementos
        /// </summary>
        /// <returns>Lista de elementos</returns>
        protected static T RetrieveList(Type type, string schema, CriteriaEx criteria)
        {
            criteria.Query = SELECT(type, schema);
            criteria.Query += WHERE(type, criteria);
            T obj = DataPortal.Fetch<T>(criteria);
            CloseSession(criteria.SessionCode);
            return obj;
        }

        /// <summary>
        /// Devuelve una lista ordenada y filtrada
        /// </summary>
        /// <param name="criteria">Filtro</param>
        /// <param name="sortProperty">Campo de ordenación</param>
        /// <param name="sortDirection">Sentido de ordenación</param>
        /// <returns>Lista ordenada</returns>
        public static SortedBindingList<C> GetSortedList(T list,
                                                            string sortProperty,
                                                            ListSortDirection sortDirection)
        {
            SortedBindingList<C> sortedList = new SortedBindingList<C>(list);
            sortedList.ApplySort(sortProperty, sortDirection);
            return sortedList;
        }

        /// <summary>
        /// Ordena una lista
        /// </summary>
        /// <param name="list">Lista a ordenar</param>
        /// <param name="sortProperty">Campo de ordenación</param>
        /// <param name="sortDirection">Sentido de ordenación</param>
        /// <returns>Lista ordenada</returns>
        public static SortedBindingList<C> SortList(ReadOnlyListBaseEx<T, C> list,
                                                    string sortProperty,
                                                    ListSortDirection sortDirection)
        {
            SortedBindingList<C> sortedList = new SortedBindingList<C>(list);
            sortedList.ApplySort(sortProperty, sortDirection);
            return sortedList;
        }

        /// <summary>
        /// Ordena una lista
        /// </summary>
        /// <param name="list">Lista a ordenar</param>
        /// <param name="sortProperty">Campo de ordenación</param>
        /// <param name="sortDirection">Sentido de ordenación</param>
        /// <returns>Lista ordenada</returns>
        public static SortedBindingList<C> SortList(List<C> list,
                                                    string sortProperty,
                                                    ListSortDirection sortDirection)
        {
            SortedBindingList<C> sortedList = new SortedBindingList<C>(list);
            sortedList.ApplySort(sortProperty, sortDirection);
            return sortedList;
        }

        /// <summary>
        /// Devuelve una lista a partir de los datos de la lista actual
        /// </summary>
        /// <param name="criteria">Filtro (Insensitive)</param>
        /// <returns>Lista</returns>
        public List<C> GetSubList(FCriteria criteria)
        {
            List<C> list = new List<C>();

            if (Items.Count == 0) return list;

            PropertyDescriptor property = TypeDescriptor.GetProperties(Items[0]).Find(criteria.GetProperty(), false);

            switch (criteria.Operation)
            {
                case Operation.Contains:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    object value = prop.GetValue(item);
                                    if (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
                                        list.Add(item);
                                    break;
                                }
                        }
                    } break;

                case Operation.Equal:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    object value = prop.GetValue(item);
                                    if (value.ToString().ToLower().Equals(criteria.GetValue().ToString().ToLower()))
                                        list.Add(item);
                                    break;
                                }
                        }
                    } break;

                case Operation.StartsWith:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    object value = prop.GetValue(item);
                                    if (value.ToString().ToLower().StartsWith(criteria.GetValue().ToString().ToLower()))
                                        list.Add(item);
                                    break;
                                }
                        }
                    } break;
                default:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    object value = prop.GetValue(item);
                                    if (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
                                        list.Add(item);
                                    break;
                                }
                        }
                    } break;
            }

            return list;
        }

        /// <summary>
        /// Devuelve una lista a partir de los datos de la lista actual, usando
        /// un criterio para fechas
        /// </summary>
        /// <param name="criteria">Filtro para DateTime</param>
        /// <returns>Lista</returns>
        public List<C> GetSubList(DCriteria criteria)
        {
            List<C> list = new List<C>();

            if (Items.Count == 0) return list;

            PropertyDescriptor property = TypeDescriptor.GetProperties(Items[0]).Find(criteria.GetProperty(), false);

            switch (criteria.Operation)
            {
                case Operation.Less:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    DateTime value = (DateTime)prop.GetValue(item);
                                    if (value < (DateTime)criteria.GetValue())
                                        list.Add(item);
                                    break;
                                }
                        }
                    } break;

                case Operation.LessOrEqual:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    DateTime value = (DateTime)prop.GetValue(item);
                                    if (value <= (DateTime)criteria.GetValue())
                                        list.Add(item);
                                    break;
                                }
                        }
                    } break;

                case Operation.Equal:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    DateTime value = (DateTime)prop.GetValue(item);
                                    if (value == (DateTime)criteria.GetValue())
                                        list.Add(item);
                                    break;
                                }
                        }
                    } break;

                case Operation.GreaterOrEqual:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    DateTime value = (DateTime)prop.GetValue(item);
                                    if (value >= (DateTime)criteria.GetValue())
                                        list.Add(item);
                                    break;
                                }
                        }
                    } break;

                case Operation.Greater:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    DateTime value = (DateTime)prop.GetValue(item);
                                    if (value > (DateTime)criteria.GetValue())
                                        list.Add(item);
                                    break;
                                }
                        }
                    } break;

                default:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    DateTime value = (DateTime)prop.GetValue(item);
                                    if (value == (DateTime)criteria.GetValue())
                                        list.Add(item);
                                    break;
                                }
                        }
                    } break;
            }

            return list;
        }

        /// <summary>
        /// Devuelve una lista a partir de los datos de la lista actual
        /// </summary>
        /// <param name="criteria">Filtro (Insensitive)</param>
        /// <returns>Lista</returns>
        public static List<C> GetSubList(IList lista, FCriteria criteria)
        {
            List<C> list = new List<C>();

            if (lista.Count == 0) return list;

            PropertyDescriptor property = TypeDescriptor.GetProperties(lista[0]).Find(criteria.GetProperty(), false);

            foreach (C item in lista)
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
        /// Función que realiza el filtrado de los formularios Localize
        /// cuando se busca por campos que no pertenecen a la tabla
        /// </summary>
        /// <param name="sublist"></param>
        /// <param name="lista"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static List<C> GetFilteredList(T lista, IList sublist, string property)
        {
            List<C> rlist = new List<C>();

            //T rlist = DataPortal.Create<T>();
            PropertyDescriptor prop, prop2;

            foreach (object item in sublist)
            {
                prop = TypeDescriptor.GetProperties(item).Find("Oid", true);

                FCriteria criteria = new FCriteria<long>(property, (long)prop.GetValue(item));

                List<C> list = null;

                list = GetSubList(lista, criteria);

                if (list.Count > 0)
                {
                    foreach (C dom in list)
                    {
                        prop2 = TypeDescriptor.GetProperties(dom).Find(property, true);
                        if ((long)prop2.GetValue(dom) == (long)prop.GetValue(item))
                            rlist.Add(dom);
                    }
                }

            }

            return rlist;
        }

        /// <summary>
        /// Devuelve una lista ordenada y filtrada a partir de los datos de la lista
        /// actual
        /// </summary>
        /// <param name="criteria">Filtro</param>
        /// <param name="sortProperty">Campo de ordenación</param>
        /// <param name="sortDirection">Sentido de ordenación</param>
        /// <returns>Lista ordenada</returns>
        public SortedBindingList<C> GetSortedSubList(FCriteria criteria,
                                                        string sortProperty,
                                                        ListSortDirection sortDirection)
        {
            List<C> list = new List<C>();

            SortedBindingList<C> sortedList = new SortedBindingList<C>(list);

            if (Items.Count == 0) return sortedList;

            PropertyDescriptor property = TypeDescriptor.GetProperties(Items[0]).Find(criteria.GetProperty(), false);

            foreach (C item in Items)
            {
                foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                    if (prop.Name == property.Name)
                    {
                        object value = prop.GetValue(item);
                        if (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
                            sortedList.Add(item);
                        break;
                    }
            }

            sortedList.ApplySort(sortProperty, sortDirection);
            return sortedList;
        }

        /// <summary>
        /// Devuelve una lista ordenada y filtrada a partir de los datos de la lista
        /// actual, usando un criterio para fechas.
        /// </summary>
        /// <param name="criteria">Criterio para DateTime</param>
        /// <param name="sortProperty">Campo de ordenación</param>
        /// <param name="sortDirection">Sentido de ordenación</param>
        /// <returns>Lista ordenada</returns>
        public SortedBindingList<C> GetSortedSubList(DCriteria criteria,
                                                        string sortProperty,
                                                        ListSortDirection sortDirection)
        {
            List<C> list = new List<C>();

            SortedBindingList<C> sortedList = new SortedBindingList<C>(list);

            if (Items.Count == 0) return sortedList;

            PropertyDescriptor property = TypeDescriptor.GetProperties(Items[0]).Find(criteria.GetProperty(), false);

            switch (criteria.Operation)
            {
                case Operation.Less:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    DateTime value = (DateTime)prop.GetValue(item);
                                    if (value < (DateTime)criteria.GetValue())
                                        sortedList.Add(item);
                                    break;
                                }
                        }
                    } break;

                case Operation.LessOrEqual:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    DateTime value = (DateTime)prop.GetValue(item);
                                    if (value <= (DateTime)criteria.GetValue())
                                        sortedList.Add(item);
                                    break;
                                }
                        }
                    } break;

                case Operation.Equal:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    DateTime value = (DateTime)prop.GetValue(item);
                                    if (value == (DateTime)criteria.GetValue())
                                        sortedList.Add(item);
                                    break;
                                }
                        }
                    } break;

                case Operation.GreaterOrEqual:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    DateTime value = (DateTime)prop.GetValue(item);
                                    if (value >= (DateTime)criteria.GetValue())
                                        sortedList.Add(item);
                                    break;
                                }
                        }
                    } break;

                case Operation.Greater:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    DateTime value = (DateTime)prop.GetValue(item);
                                    if (value > (DateTime)criteria.GetValue())
                                        sortedList.Add(item);
                                    break;
                                }
                        }
                    } break;

                default:
                    {
                        foreach (C item in Items)
                        {
                            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                                if (prop.Name == property.Name)
                                {
                                    DateTime value = (DateTime)prop.GetValue(item);
                                    if (value == (DateTime)criteria.GetValue())
                                        sortedList.Add(item);
                                    break;
                                }
                        }
                    } break;
            }

            sortedList.ApplySort(sortProperty, sortDirection);
            return sortedList;
        }

        /// <summary>
        /// Devuelve una lista ordenada a partir de los datos de la lista
        /// </summary>
        /// <param name="sortProperty"></param>
        /// <param name="sortDirection"></param>
        /// <returns></returns>
        public SortedBindingList<C> ToSortedList(string sortProperty,
                                                ListSortDirection sortDirection)
        {
            List<C> list = new List<C>();

            SortedBindingList<C> sortedList = new SortedBindingList<C>(list);

            if (Items.Count == 0) return sortedList;

            foreach (C item in Items)
            {
                sortedList.Add(item);
            }

            sortedList.ApplySort(sortProperty, sortDirection);
            return sortedList;
        }

        /// <summary>
        /// Devuelve una lista a partir de los datos de la lista actual
        /// </summary>
        /// <param name="criteria">Filtro</param>
        /// <returns>Lista</returns>
        public List<C> GetSortedListByCode(IComparer<C> comparer)
        {
            List<C> list = new List<C>();

            if (Items.Count == 0) return list;

            foreach (C item in Items)
                list.Add(item);

            list.Sort(comparer);
            return list;
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
        /// <returns>Código de la sesión</returns>
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

        #region Data Access

        protected virtual void DataPortal_Fetch(CriteriaEx criteria)
        {
            Fetch(criteria);
        }

        protected virtual void DataPortal_Fetch(string hql)
        {
            Fetch(hql);
        }

        // called to load data from db by criteria
        protected virtual void Fetch(CriteriaEx criteria) { }

        // called to load data from db by hql
        protected virtual void Fetch(string hql) { }

        #endregion

        #region Private

        protected override bool SupportsSearchingCore
        {
            get { return true; }
        }

        protected override int FindCore(PropertyDescriptor property, object key)
        {
            foreach (C item in Items)
            {
                foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
                    if (prop.Name == property.Name)
                    {
                        object value = prop.GetValue(item);
                        if (value.ToString() == key.ToString())
                            return IndexOf(item);
                    }
            }
            return -1;
        }

        #endregion

        #region SQL

        /// <summary>
        /// Construye el SELECT de la lista y lo ejecuta
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <param name="sesion"></param>
        /// <returns></returns>
        public static string SELECT(Type type, string schema)
        {
            string tabla = nHManager.Instance.Cfg.GetClassMapping(type).Table.Name;
            string query;

            if (schema == "COMMON")
            {
                query = "SELECT * " +
                            "FROM \"" + schema + "\".\"" + tabla + "\" ";
            }
            else
            {
                int esquema = Convert.ToInt32(schema);
                query = "SELECT * " +
                            "FROM \"" + esquema.ToString("0000") + "\".\"" + tabla + "\" ";
            }

            return query;
        }

        /// <summary>
        /// Construye el WHERE de la consulta SQL y devuelve la consulta completa
        /// </summary>
        /// <param name="criteria">CriteriaEx que tiene una lista de condiciones</param>
        /// <returns></returns>
        public static string WHERE(Type type, CriteriaEx criteria)
        {
            IEnumerable list = criteria.IterateExpressionEntries();
            string query = "WHERE 1=1 ";

            foreach (object cond in list)
            {
                string condicion = cond.ToString();
                int index = condicion.IndexOf(" ");
                string property = condicion.Substring(0, index);
                index = condicion.IndexOf(" ", index + 1);
                string value = condicion.Substring(index + 1);
                string columna = nHManager.Instance.GetTableField(type, property);

                if (condicion.Contains(" ilike "))
                    query += "AND \"" + columna + "\" ILIKE '" + value + "' ";

                if (condicion.Contains(" = "))
                    query += "AND \"" + columna + "\" = '" + value + "' ";

            }
            query += ";";
            return query;
        }
        #endregion

    }
}
