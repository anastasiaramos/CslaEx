using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

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
        #region Attributes & Properties

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

		#endregion

		#region Factory Methods

		#endregion

		#region Data Access Methods

		// called to copy objects data from list
        protected void Fetch(IList<C> lista)
        {
			if (lista == null) return;

            this.RaiseListChangedEvents = false;

            IsReadOnly = false;

            foreach (C item in lista)
                this.AddItem(item);

            IsReadOnly = true;

            this.RaiseListChangedEvents = true;
        }

        #endregion

        #region Business Methods

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
            if (pos != -1) return Items[pos];

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
        /// Función que determina si existe un objeto con el mismo Oid en la lista
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual bool Contains(long oid)
        {
            return GetItem(oid) != null;
        }

        public virtual void Change(long oid, C item, bool add_if_no_exists)
        {
            IsReadOnly = false;
            
            int index = this.IndexOf(this.GetItem(oid));
            if (index >= 0) 
                this[index] = item;
            else if (add_if_no_exists)
                this.AddItem(item);

            IsReadOnly = true;
        }  

        public virtual void RemoveItem(long oid)
        {
            IsReadOnly = false;
            this.Remove(this.GetItem(oid));
            this.HashList.Remove(oid);
			HashList.Remove(oid);
            IsReadOnly = true;
        }

        /// <summary>
        /// Devuelve una lista ordenada y filtrada
        /// </summary>
        /// <param name="criteria">Filtro</param>
        /// <param name="sortProperty">Campo de ordenación</param>
        /// <param name="sortDirection">Sentido de ordenación</param>
        /// <returns>Lista ordenada</returns>
        public SortedBindingList<C> GetSortedList()
        {
            return new SortedBindingList<C>(this);
        }

        /// <summary>
        /// Devuelve una lista de todos los elementos
        /// </summary>
        /// <returns>Lista de elementos</returns>
        protected static T RetrieveList(Type type, string schema, CriteriaEx criteria)
        {
            criteria.Query = SELECT(type, schema);
            criteria.Query = criteria.Query.Remove(criteria.Query.Length - 1);
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
            SortedBindingList<C> sortedList = GetSortedList(list);
            sortedList.ApplySort(sortProperty, sortDirection);
            return sortedList;
        }

        /// <summary>
        /// Devuelve una lista ordenada y filtrada
        /// </summary>
        /// <param name="criteria">Filtro</param>
        /// <param name="sortProperty">Campo de ordenación</param>
        /// <param name="sortDirection">Sentido de ordenación</param>
        /// <returns>Lista ordenada</returns>
        public static SortedBindingList<C> GetSortedList(T list) 
        {
            return new SortedBindingList<C>(list);
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

			PropertyDescriptor property = null;

			if (criteria.GetProperty() != null)
				property = TypeDescriptor.GetProperties(Items[0]).Find(criteria.GetProperty(), false);
			else
				property = null;

			IEnumerable<C> results;

			switch (criteria.Operation)
			{
				case Operation.Equal:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where Equals(item, property.Name, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where Equals(item, criteria)
								select item;
						}
					} break;

				case Operation.Distinct:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where Distinct(item, property.Name, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where Distinct(item, criteria)
								select item;
						}
					} break;

				case Operation.StartsWith:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where StartsWith(item, property.Name, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where StartsWith(item, criteria)
								select item;
						}
					} break;

				case Operation.Less:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where Less(item, property.Name, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where Less(item, criteria)
								select item;
						}
					} break;

				case Operation.LessOrEqual:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where LessOrEqual(item, property.Name, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where LessOrEqual(item, criteria)
								select item;
						}
					} break;

				case Operation.Greater:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where Greater(item, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where Greater(item, criteria)
								select item;
						}
					} break;

				case Operation.GreaterOrEqual:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where GreaterOrEqual(item, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where GreaterOrEqual(item, criteria)
								select item;
						}
					} break;

				case Operation.Contains:
				default:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where Contains(item, property.Name, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where Contains(item, criteria)
								select item;
						}
					} break;
			}

			foreach (var item in results)
				list.Add(item);

			/*switch (criteria.Operation)
			{
				case Operation.Contains:
					{
						foreach (C item in Items)
						{
							foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
							{
								if (property != null)
								{
									if (prop.Name == property.Name)
									{
										object value = prop.GetValue(item);
										if (value == null) continue;
										if (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
											list.Add(item);
										break;
									}
								}
								else
								{
									object value = prop.GetValue(item);
									if (value == null) continue;
									if (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
									{
										list.Add(item);
										break;
									}
								}
							}						
						}
					} break;

				case Operation.Equal:
					{
						foreach (C item in Items)
						{
							foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
							{
								if (property != null)
								{
									if (prop.Name == property.Name)
									{
										object value = prop.GetValue(item);
										if (value == null) continue;
										if (value.ToString().ToLower().Equals(criteria.GetValue().ToString().ToLower()))
											list.Add(item);
										break;
									}
								}
								else
								{
									object value = prop.GetValue(item);
									if (value == null) continue;
									if (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
									{
										list.Add(item);
										break;
									}
								}
							}
						}
					} break;

				case Operation.Distinct:
					{
						foreach (C item in Items)
						{
							foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
							{
								if (property != null)
								{
									if (prop.Name == property.Name)
									{
										object value = prop.GetValue(item);
										if (value == null) continue;
										if (!value.ToString().ToLower().Equals(criteria.GetValue().ToString().ToLower()))
											list.Add(item);
										break;
									}
								}
								else
								{
									object value = prop.GetValue(item);
									if (value == null) continue;
									if (!value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
									{
										list.Add(item);
										break;
									}
								}
							}
						}
					} break;

				case Operation.StartsWith:
					{
						foreach (C item in Items)
						{
							foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
							{
								if (property != null)
								{
									if (prop.Name == property.Name)
									{
										object value = prop.GetValue(item);
										if (value == null) continue;
										if (value.ToString().ToLower().StartsWith(criteria.GetValue().ToString().ToLower()))
											list.Add(item);
										break;
									}
								}
								else
								{
									object value = prop.GetValue(item);
									if (value == null) continue;
									if (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
									{
										list.Add(item);
										break;
									}
								}
							}
						}
					} break;

				case Operation.Less:
					{
						foreach (C item in Items)
						{
							foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
							{
								if (property != null)
								{
									if (prop.Name == property.Name)
									{
										object value = prop.GetValue(item);
										if (value == null) continue;
										if (criteria.Less(value))
											list.Add(item);
										break;
									}
								}
								else
								{
									object value = prop.GetValue(item);
									if (value == null) continue;
									if (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
									{
										list.Add(item);
										break;
									}
								}
							}
						}
					} break;

				case Operation.LessOrEqual:
					{
						foreach (C item in Items)
						{
							foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
							{
								if (property != null)
								{
									if (prop.Name == property.Name)
									{
										object value = prop.GetValue(item);
										if (value == null) continue;
										if (criteria.LessOrEqual(value))
											list.Add(item);
										break;
									}
								}
								else
								{
									object value = prop.GetValue(item);
									if (value == null) continue;
									if (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
									{
										list.Add(item);
										break;
									}
								}
							}
						}
					} break;

				case Operation.Greater:
					{
						foreach (C item in Items)
						{
							foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
							{
								if (property != null)
								{
									if (prop.Name == property.Name)
									{
										object value = prop.GetValue(item);
										if (value == null) continue;
										if (criteria.Greater(value))
											list.Add(item);
										break;
									}
								}
								else
								{
									object value = prop.GetValue(item);
									if (value == null) continue;
									if (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
									{
										list.Add(item);
										break;
									}
								}
							}
						}
					} break;

				case Operation.GreaterOrEqual:
					{
						foreach (C item in Items)
						{
							foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
							{
								if (property != null)
								{
									if (prop.Name == property.Name)
									{
										object value = prop.GetValue(item);
										if (value == null) continue;
										if (criteria.GreaterOrEqual(value))
											list.Add(item);
										break;
									}
								}
								else
								{
									object value = prop.GetValue(item);
									if (value == null) continue;
									if (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
									{
										list.Add(item);
										break;
									}
								}
							}
						}
					} break;

				default:
					{
						foreach (C item in Items)
						{
							foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(item))
							{
								if (property != null)
								{
									if (prop.Name == property.Name)
									{
										object value = prop.GetValue(item);
										if (value == null) continue;
										if (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
											list.Add(item);
										break;
									}
								}
								else
								{
									object value = prop.GetValue(item);
									if (value == null) continue;
									if (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()))
									{
										list.Add(item);
										break;
									}
								}
							}
						}
					} break;
			}*/

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

			IEnumerable<C> results;

			switch (criteria.Operation)
			{
				case Operation.Less:
					{
						results =
							from item in Items.AsEnumerable()
							where Less(item, property.Name, criteria)
							select item;
					} break;

				case Operation.LessOrEqual:
					{
						results =
							from item in Items.AsEnumerable()
							where LessOrEqual(item, property.Name, criteria)
							select item;
					} break;

				case Operation.Distinct:
					{
						results =
							from item in Items.AsEnumerable()
							where Distinct(item, property.Name, criteria)
							select item;
					} break;

				case Operation.GreaterOrEqual:
					{
						results =
							from item in Items.AsEnumerable()
							where GreaterOrEqual(item, property.Name, criteria)
							select item;
					} break;

				case Operation.Greater:
					{
						results =
							from item in Items.AsEnumerable()
							where Greater(item, property.Name, criteria)
							select item;
					} break;

				case Operation.Equal:
				default:
					{
						results =
							from item in Items.AsEnumerable()
							where Equals(item, property.Name, criteria)
							select item;
					} break;
			}

			foreach (var item in results)
				list.Add(item);

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
        /// Devuelve una lista ordenada a partir de los datos de otra lista
        /// </summary>
        /// <param name="criteria">Criterio para DateTime</param>
        /// <param name="sortProperty">Campo de ordenación</param>
        /// <param name="sortDirection">Sentido de ordenación</param>
        /// <returns>Lista ordenada</returns>
        public static SortedBindingList<C> GetSortedList(List<C> list, string sortProperty, ListSortDirection sortDirection)
        {
            SortedBindingList<C> sortedList = new SortedBindingList<C>(list);
            sortedList.ApplySort(sortProperty, sortDirection);
            return sortedList;
        }

        /// <summary>
        /// Devuelve una lista ordenada y filtrada a partir de los datos de la lista
        /// actual
        /// </summary>
        /// <param name="criteria">Filtro</param>
        /// <param name="sortProperty">Campo de ordenación</param>
        /// <param name="sortDirection">Sentido de ordenación</param>
        /// <returns>Lista ordenada</returns>
        public SortedBindingList<C> GetSortedSubList(FCriteria criteria)
        {
            List<C> list = GetSubList(criteria);
            SortedBindingList<C> sortedList = new SortedBindingList<C>(list);
            
            return sortedList;
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
			SortedBindingList<C> sortedList = GetSortedSubList(criteria);
			sortedList.ApplySort(sortProperty, sortDirection);

			return sortedList;
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
														ListSortDirection sortDirection,
														List<string> properties_list)
		{
			SortedBindingList<C> sortedList = GetSortedSubList(criteria, properties_list);
			sortedList.ApplySort(sortProperty, sortDirection);

			return sortedList;
		}		
	
		/// <summary>
		/// Devuelve una lista a partir de los datos de la lista actual
		/// </summary>
		/// <param name="criteria">Filtro (Insensitive)</param>
		/// <returns>Lista</returns>
		public virtual SortedBindingList<C> GetSortedSubList(FCriteria criteria, List<string> properties_list)
		{
			List<C> list = new List<C>();
			SortedBindingList<C> sortedList = new SortedBindingList<C>(list);
			
			if (Items.Count == 0) return sortedList;

			PropertyDescriptor property = null;

			if (criteria.GetProperty() != null)
				property = TypeDescriptor.GetProperties(Items[0]).Find(criteria.GetProperty(), false);
			else
				property = null;

			IEnumerable<C> results;

			switch (criteria.Operation)
			{
				case Operation.Equal:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where Equals(item, property.Name, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where Equals(item, properties_list, criteria)
								select item;
						}	
					} break;

				case Operation.Distinct:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where Distinct(item, property.Name, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where Distinct(item, properties_list, criteria)
								select item;
						}	
					} break;

				case Operation.StartsWith:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where StartsWith(item, property.Name, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where StartsWith(item, properties_list, criteria)
								select item;
						}	
					} break;

				case Operation.Less:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where Less(item, property.Name, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where Less(item, properties_list, criteria)
								select item;
						}	
					} break;

				case Operation.LessOrEqual:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where LessOrEqual(item, property.Name, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where LessOrEqual(item, properties_list, criteria)
								select item;
						}	
					} break;

				case Operation.Greater:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where Greater(item, property.Name, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where Greater(item, properties_list, criteria)
								select item;
						}	
					} break;

				case Operation.GreaterOrEqual:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where GreaterOrEqual(item, property.Name, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where GreaterOrEqual(item, properties_list, criteria)
								select item;
						}	
					} break;

				case Operation.Contains:
				default:
					{
						if (property != null)
						{
							results =
								from item in Items.AsEnumerable()
								where Contains(item, property.Name, criteria)
								select item;
						}
						else
						{
							results =
								from item in Items.AsEnumerable()
								where Contains(item, properties_list, criteria)
								select item;
						}						
					} break;
			}

			foreach (var item in results)
				sortedList.Add(item);

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
			List<C> list = GetSubList(criteria);

            SortedBindingList<C> sortedList = new SortedBindingList<C>(list);

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

		#region Comparers

		protected virtual bool Contains(C item, FCriteria criteria)
		{
			IEnumerable<C> results;			

			results =
				from PropertyDescriptor prop in TypeDescriptor.GetProperties(item)
				where Contains(item, prop.Name, criteria)
				select item;

			return results.Count() > 0;
		}
		protected virtual bool Contains(C item, string propertyName, FCriteria criteria)
		{
            if (propertyName == string.Empty) return false;

			Type type = typeof(C);

			System.Reflection.PropertyInfo prop = type.GetProperty(propertyName);
            object value = prop.GetValue(item, null);

            if (prop.PropertyType == typeof(DateTime) && criteria.GetValue().GetType() != typeof(DateTime))
                value = ((DateTime)value).ToShortDateString() + " 0:00:00";

			if (value == null) return false;

			return (value.ToString().ToLower().Contains(criteria.GetValue().ToString().ToLower()));
		}
		protected virtual bool Contains(C item, List<string> propertyList, FCriteria criteria)
		{
			IEnumerable<C> results;

			results =
				from propName in propertyList
				where Contains(item, propName, criteria)
				select item;

			return results.Count() > 0;
		}

		private bool Distinct(C item, FCriteria criteria)
		{
			IEnumerable<C> results;

			results =
				from PropertyDescriptor prop in TypeDescriptor.GetProperties(item)
				where Distinct(item, prop.Name, criteria)
				select item;

			return results.Count() > 0;
		}
		private bool Distinct(C item, string propertyName, FCriteria criteria)
		{
			Type type = typeof(C);

			System.Reflection.PropertyInfo prop = type.GetProperty(propertyName);

            object value = prop.GetValue(item, null);

			if (prop.PropertyType == typeof(DateTime))
				value = Convert.ToDateTime(((DateTime)value).ToShortDateString() + " 0:00:00");

 			return (!value.ToString().ToLower().Equals(criteria.GetValue().ToString().ToLower()));
		}

		private bool Distinct(C item, List<string> propertyList, FCriteria criteria)
		{
			IEnumerable<C> results;

			results =
				from propName in propertyList.AsEnumerable()
				where Distinct(item, propName, criteria)
				select item;

			return results.Count() > 0;
		}
		private bool Distinct(C item, string propertyName, DCriteria criteria)
		{
			Type type = typeof(C);

			System.Reflection.PropertyInfo prop = type.GetProperty(propertyName);

			if (prop.PropertyType != typeof(DateTime)) return false;

			DateTime value = (DateTime)prop.GetValue(item, null);

			return (value != (DateTime)criteria.GetValue());
		}

		private bool Equals(C item, FCriteria criteria)
		{
			IEnumerable<C> results;

			results =
				from PropertyDescriptor prop in TypeDescriptor.GetProperties(item)
				where Equals(item, prop.Name, criteria)
				select item;

			return results.Count() > 0;
		}
		private bool Equals(C item, string propertyName, FCriteria criteria)
		{
			Type type = typeof(C);

			System.Reflection.PropertyInfo prop = type.GetProperty(propertyName);

            object value = prop.GetValue(item, null);

			if (prop.PropertyType == typeof(DateTime))
				value = Convert.ToDateTime(((DateTime)value).ToShortDateString() + " 0:00:00");

			return (value.ToString().ToLower().Equals(criteria.GetValue().ToString().ToLower()));
		}

		private bool Equals(C item, List<string> propertyList, FCriteria criteria)
		{
			IEnumerable<C> results;

			results =
				from propName in propertyList.AsEnumerable()
				where Equals(item, propName, criteria)
				select item;

			return results.Count() > 0;
		}
		private bool Equals(C item, string propertyName, DCriteria criteria)
		{
			Type type = typeof(C);

			System.Reflection.PropertyInfo prop = type.GetProperty(propertyName);

			if (prop.PropertyType != typeof(DateTime)) return false;

			DateTime value = (DateTime)prop.GetValue(item, null);

			return (value == (DateTime)criteria.GetValue());
		}

		private bool Greater(C item, FCriteria criteria)
		{
			IEnumerable<C> results;

			results =
				from PropertyDescriptor prop in TypeDescriptor.GetProperties(item)
				where Greater(item, prop.Name, criteria)
				select item;

			return results.Count() > 0;
		}
		private bool Greater(C item, string propertyName, FCriteria criteria)
		{
			Type type = typeof(C);

			System.Reflection.PropertyInfo prop = type.GetProperty(propertyName);

            object value = prop.GetValue(item, null);

			if (prop.PropertyType == typeof(DateTime))
				value = Convert.ToDateTime(((DateTime)value).ToShortDateString() + " 0:00:00");


			return (criteria.Greater(value));
		}
		private bool Greater(C item, List<string> propertyList, FCriteria criteria)
		{
			IEnumerable<C> results;

			results =
				from propName in propertyList.AsEnumerable()
				where Greater(item, propName, criteria)
				select item;

			return results.Count() > 0;
		}
		private bool Greater(C item, string propertyName, DCriteria criteria)
		{
			Type type = typeof(C);

			System.Reflection.PropertyInfo prop = type.GetProperty(propertyName);

			if (prop.PropertyType != typeof(DateTime)) return false;

			DateTime value = (DateTime)prop.GetValue(item, null);

			return (value > (DateTime)criteria.GetValue());
		}

		private bool GreaterOrEqual(C item, FCriteria criteria)
		{
			IEnumerable<C> results;

			results =
				from PropertyDescriptor prop in TypeDescriptor.GetProperties(item)
				where GreaterOrEqual(item, prop.Name, criteria)
				select item;

			return results.Count() > 0;
		}
		private bool GreaterOrEqual(C item, string propertyName, FCriteria criteria)
		{
			Type type = typeof(C);

			System.Reflection.PropertyInfo prop = type.GetProperty(propertyName);

            object value = prop.GetValue(item, null);

			if (prop.PropertyType == typeof(DateTime))
				value = Convert.ToDateTime(((DateTime)value).ToShortDateString() + " 0:00:00");


			return (criteria.GreaterOrEqual(value));
		}
		private bool GreaterOrEqual(C item, List<string> propertyList, FCriteria criteria)
		{
			IEnumerable<C> results;

			results =
				from propName in propertyList.AsEnumerable()
				where GreaterOrEqual(item, propName, criteria)
				select item;

			return results.Count() > 0;
		}
		private bool GreaterOrEqual(C item, string propertyName, DCriteria criteria)
		{
			Type type = typeof(C);

			System.Reflection.PropertyInfo prop = type.GetProperty(propertyName);

			if (prop.PropertyType != typeof(DateTime)) return false;

			DateTime value = (DateTime)prop.GetValue(item, null);

			return (value >= (DateTime)criteria.GetValue());
		}

		private bool Less(C item, FCriteria criteria)
		{
			IEnumerable<C> results;

			results =
				from PropertyDescriptor prop in TypeDescriptor.GetProperties(item)
				where Less(item, prop.Name, criteria)
				select item;

			return results.Count() > 0;
		}
		private bool Less(C item, string propertyName, FCriteria criteria)
		{
			Type type = typeof(C);

			System.Reflection.PropertyInfo prop = type.GetProperty(propertyName);

            object value = prop.GetValue(item, null);

			if (prop.PropertyType == typeof(DateTime))
				value = Convert.ToDateTime(((DateTime)value).ToShortDateString() + " 23:59:59");


			return (criteria.LessOrEqual(value));
		}
		private bool Less(C item, List<string> propertyList, FCriteria criteria)
		{
			IEnumerable<C> results;

			results =
				from propName in propertyList.AsEnumerable()
				where Less(item, propName, criteria)
				select item;

			return results.Count() > 0;
		}
		private bool Less(C item, string propertyName, DCriteria criteria)
		{
			Type type = typeof(C);

			System.Reflection.PropertyInfo prop = type.GetProperty(propertyName);

			if (prop.PropertyType != typeof(DateTime)) return false;

			DateTime value = (DateTime)prop.GetValue(item, null);

			return (value < (DateTime)criteria.GetValue());
		}

		private bool LessOrEqual(C item, FCriteria criteria)
		{
			IEnumerable<C> results;

			results =
				from PropertyDescriptor prop in TypeDescriptor.GetProperties(item)
				where LessOrEqual(item, prop.Name, criteria)
				select item;

			return results.Count() > 0;
		}
		private bool LessOrEqual(C item, string propertyName, FCriteria criteria)
		{
			Type type = typeof(C);

			System.Reflection.PropertyInfo prop = type.GetProperty(propertyName);

            object value = prop.GetValue(item, null);

			if (prop.PropertyType == typeof(DateTime))
				value = Convert.ToDateTime(((DateTime)value).ToShortDateString() + " 23:59:59");


			return (criteria.LessOrEqual(value));
		}
		private bool LessOrEqual(C item, List<string> propertyList, FCriteria criteria)
		{
			IEnumerable<C> results;

			results =
				from propName in propertyList.AsEnumerable()
				where LessOrEqual(item, propName, criteria)
				select item;

			return results.Count() > 0;
		}
		private bool LessOrEqual(C item, string propertyName, DCriteria criteria)
		{
			Type type = typeof(C);

			System.Reflection.PropertyInfo prop = type.GetProperty(propertyName);

			if (prop.PropertyType != typeof(DateTime)) return false;

			DateTime value = (DateTime)prop.GetValue(item, null);

			return (value <= (DateTime)criteria.GetValue());
		}

		private bool StartsWith(C item, FCriteria criteria)
		{
			IEnumerable<C> results;

			PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(item);

			results =
				from PropertyDescriptor prop in properties
				where StartsWith(item, prop.Name, criteria)
				select item;

			return results.Count() > 0;
		}
		private bool StartsWith(C item, string propertyName, FCriteria criteria)
		{
			Type type = typeof(C);

			System.Reflection.PropertyInfo prop = type.GetProperty(propertyName);

			object value = prop.GetValue(item, null);
			if (value == null) return false;

			if (prop.PropertyType == typeof(DateTime))
				value = Convert.ToDateTime(((DateTime)value).ToShortDateString() + " 0:00:00");

			return (value.ToString().ToLower().StartsWith(criteria.GetValue().ToString().ToLower()));
		}
		private bool StartsWith(C item, List<string> propertyList, FCriteria criteria)
		{
			IEnumerable<C> results;

			results =
				from propName in propertyList.AsEnumerable()
				where StartsWith(item, propName, criteria)
				select item;

			return results.Count() > 0;
		}

		#endregion

        #region NHibernate Default Interface

        public virtual ITransaction BeginTransaction() { return BeginTransaction(SessionCode); }

        public virtual ISession Session() { return Session(SessionCode); }

        public virtual ITransaction Transaction() { return Transaction(_sessCode); }

        public virtual void CloseSession() { CloseSession(_sessCode); }

        #endregion

		#region NHibernate By Code Interface

		/// <summary>
        /// Abre una nueva sesión 
        /// </summary>
        /// <returns>Código de la sesión</returns>
        public static int OpenSession() { return nHManager.Instance.OpenSession(); }

        /// <summary>
        /// Inicia una transacción para la sessión actual
        /// </summary>
        /// <returns></returns>
        public static ITransaction BeginTransaction(int sessionCode) { return nHManager.Instance.BeginTransaction(sessionCode); }

        /// <summary>
        /// Devuelve la sesión correspondiente a este objeto
        /// </summary>
        /// <returns></returns>
        public static ISession Session(int sessionCode) { return nHManager.Instance.GetSession(sessionCode); }

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

        #region Data Access

        protected virtual void DataPortal_Fetch(CriteriaEx criteria) { Fetch(criteria); }

        protected virtual void DataPortal_Fetch(string hql) { Fetch(hql); }

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
        public static string SELECT(Type type)
        {
            return nHManager.Instance.SELECT(type, null, false, null, null, null);
        }

        /// <summary>
        /// Construye el SELECT de la lista y lo ejecuta
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <param name="sesion"></param>
        /// <returns></returns>
        public static string SELECT(Type type, string schema)
        {
            return nHManager.Instance.SELECT(type, schema, false, null, null, null);
        }

        /// <summary>
        /// Construye el SELECT de la lista y lo ejecuta
        /// </summary>
        /// <param name="type"></param>
        /// <param name="schema"></param>
        /// <param name="sesion"></param>
        /// <returns></returns>
        public static string SELECT(Type type, string filter_field, object filter_value)
        {
            return nHManager.Instance.SELECT(type, null, false, filter_field, filter_value, null);
        }

        /// <summary>
        /// Construye el SELECT de la lista y lo ejecuta
        /// </summary>
        /// <param name="type"></param>
        /// <param name="filter_field"></param>
        /// <param name="filter_value"></param>
        /// <param name="order_field"></param>
        /// <returns></returns>
        public static string SELECT(Type type, string filter_field, object filter_value, string order_field)
        {
            return nHManager.Instance.SELECT(type, null, false, filter_field, filter_value, order_field);
        }

        public static string SELECT_BY_FIELD(Type type, string parent_field, object field_value)
        {
            string tabla = nHManager.Instance.GetSQLTable(type);
            string columna = nHManager.Instance.GetTableField(type, parent_field);
            string query;

            query = "SELECT * " +
                    " FROM " + tabla +
                    " WHERE \"" + columna + "\" = " + field_value.ToString() + ";";

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

    [Serializable()]
    public abstract class ReadOnlyListBaseEx<T, C, A> : ReadOnlyListBaseEx<T, C>
        where T : ReadOnlyListBaseEx<T, C>
        where C : ReadOnlyBaseEx<C>
        where A : BusinessBaseEx<A>
    {
        public static T GetList(int pageIndex, int pageSize, out int totalUserCount, FilterList filters = null, bool childs = false)
        {
            PagingInfo pagingInfo = new PagingInfo { CurrentPage = pageIndex, ItemsPerPage = pageSize };
            T list = GetList(pagingInfo, filters, childs);
            totalUserCount = pagingInfo.TotalItems;
            return list;
        }
        public static T GetList(PagingInfo pagingInfo, FilterList filters = null, bool childs = false)
        {
            Type type = typeof(BusinessBaseEx<A>);

            CriteriaEx criteria = (CriteriaEx)type.InvokeMember("GetCriteria"
                                                                ,BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public
                                                                ,null, null, new object[1] { OpenSession() });

            criteria.Childs = childs;
            criteria.PagingInfo = pagingInfo;
			criteria.Filters = criteria.Filters;

            type = typeof(A);

            criteria.Query = (string)type.InvokeMember("SELECT"
                                                                , BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public
                                                                , null, null, new object[2] { criteria, false });

            T list = DataPortal.Fetch<T>(criteria);

            CloseSession(criteria.SessionCode);

            return list;
        }
		public static T GetList(CriteriaEx criteria, bool childs = false)
		{
			Type type = typeof(BusinessBaseEx<A>);

			CriteriaEx criteriaex = (CriteriaEx)type.InvokeMember("GetCriteria"
																, BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public
																, null, null, new object[1] { OpenSession() });

			criteriaex.Childs = childs;
			criteriaex.PagingInfo = criteria.PagingInfo;
			criteriaex.Filters = criteria.Filters;
            criteriaex.Orders = criteria.Orders;

			type = typeof(A);

			criteriaex.Query = (string)type.InvokeMember("SELECT"
																, BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public
																, null, null, new object[2] { criteriaex , false });

			T list = DataPortal.Fetch<T>(criteriaex);

			CloseSession(criteriaex.SessionCode);

			return list;
		}
    }
}
