using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Globalization;

using NHibernate.Mapping;

namespace CslaEx
{
    public enum IFilterType
    {
        None = 0,
        Filter = 1,
        Search = 2,
        FilterBack = 3
    }

    public enum IFilterProperty
    {
        All = 0,
        ByParamenter = 1,
        ByList = 2
    }

    [Serializable()]
    public class FilterItem
    {
        public bool Active = true;
        public IFilterProperty FilterProperty = IFilterProperty.ByParamenter;
        public CslaEx.Operation Operation;
        public string Column;
        public string ColumnTitle;
        public Type EntityType { get; set; }
        public string Property;
        public Column TableColumn { get; set; }
        public object Value;        

        public string Text { get { return ColumnTitle.ToUpper() + " " + CslaEx.EnumText.GetString(Operation) + " '" + ValueToString + "'; "; } }
        public string ValueToString
        {
            get
            {
                if (Value != null)
                {
                    return ((Value is DateTime)) ? ((DateTime)Value).ToShortDateString() : Value.ToString();
                }
                else
                    return string.Empty;
            }
        }
    }

    [Serializable()]
    public class FilterList : List<FilterItem> 
    {
        public void NewFilter(object value,
                                                string propertyName,
                                                IFilterProperty filterProperty,
                                                Operation operation,
                                                Type entityType, String propLabel)
        { 
            this.Add(FilterMng.BuildFilterItem(value,
                                                  propertyName,
                                                  filterProperty,
                                                  operation,
                                                  entityType, propLabel));
        }
    }

    [Serializable()]
    public class OrderItem
    {
        public Type EntityType { get; set; }
        public string Property { get; set; }
        public Column TableColumn { get; set; }
        public ListSortDirection Direction { get; set; }
    }

    [Serializable()]
    public class OrderList : List<OrderItem>
    {
        public void NewOrder(   string propertyName,
                                ListSortDirection direction,
                                Type entityType)
        {
            this.Add(FilterMng.BuildOrderItem(   propertyName,
                                                  direction,
                                                  entityType));
        }

        public void EditOrder(int orderItemIndex, string propertyName,
                                ListSortDirection direction,
                                Type entityType)
        {
            this[orderItemIndex].Direction = direction;
            this[orderItemIndex].Property = propertyName;
            this[orderItemIndex].EntityType = entityType;
            try
            {
                if (propertyName == "Oid")
                   this[orderItemIndex].TableColumn = nHManager.Instance.GetTableIDColumn(entityType);
                else
                    this[orderItemIndex].TableColumn = nHManager.Instance.GetTableColumn(entityType, propertyName);
            }
            catch { }
        }
    }

    [Serializable()]
    public class FilterMng
    {
        public static FilterItem BuildFilterItem(object value, 
                                                    string propertyName, 
                                                    Type propertytype, 
                                                    IFilterProperty filterProperty, 
                                                    Operation operation)
        {
            FilterItem fItem = new FilterItem();
            fItem.Column = propertyName;
            fItem.Property = propertyName;
            fItem.FilterProperty = filterProperty;
            fItem.Operation = operation;

            if (propertytype == typeof(DateTime))
            {
                if (fItem.Operation == Operation.Contains) fItem.Operation = Operation.Equal;

                switch (fItem.Operation)
                {
                    case Operation.Equal:
                    case Operation.Distinct:
                        fItem.Value = ((DateTime)value).ToShortDateString() + " 0:00:00";
                        break;

                    case Operation.Greater:
                    case Operation.LessOrEqual:
                        fItem.Value = ((DateTime)value).ToShortDateString() + " 23:59:59";
                        break;

                    case Operation.Less:
                    case Operation.GreaterOrEqual:
                        fItem.Value = ((DateTime)value).ToShortDateString() + " 0:00:00";
                        break;
                }
            }
            else
                fItem.Value = value;

            return fItem;
        }

        public static FilterItem BuildFilterItem(object value,
                                                  string propertyName,
                                                  IFilterProperty filterProperty,
                                                  Operation operation,
                                                  Type entityType, String propLabel)
        {
            FilterItem fItem = new FilterItem();
            fItem.Column = propertyName;
            fItem.Property = propertyName;
            fItem.FilterProperty = filterProperty;
            fItem.Operation = operation;
            fItem.EntityType = entityType;
            try
            {
                fItem.TableColumn = (filterProperty == IFilterProperty.ByParamenter) ? nHManager.Instance.GetTableColumn(entityType, propertyName) : null;
            }
            catch { }
            fItem.ColumnTitle = propLabel;

            if (filterProperty == IFilterProperty.ByParamenter)
            {
                if ((fItem.TableColumn.Type is NHibernate.Type.DateTimeType) ||
                    (fItem.TableColumn.Type is NHibernate.Type.DateType))
                {
                    if (fItem.Operation == Operation.Contains) fItem.Operation = Operation.Equal;

                    switch (fItem.Operation)
                    {
                        case Operation.Equal:
                        case Operation.Distinct:
                            fItem.Value = ((DateTime)value).ToShortDateString() + " 0:00:00";
                            break;

                        case Operation.Greater:
                        case Operation.LessOrEqual:
                            fItem.Value = ((DateTime)value).ToShortDateString() + " 23:59:59";
                            break;

                        case Operation.Less:
                        case Operation.GreaterOrEqual:
                            fItem.Value = ((DateTime)value).ToShortDateString() + " 0:00:00";
                            break;
                    }
                }
                else
                    fItem.Value = value;
            }
            else
                fItem.Value = value;

            return fItem;
        }

        public static OrderItem BuildOrderItem(string propertyName,
                                                ListSortDirection direction,
                                                Type entityType)
        {
            OrderItem oItem = new OrderItem();
            oItem.Property = propertyName;
            oItem.Direction = direction;
            oItem.EntityType = entityType;
            try
            {
                if (propertyName == "Oid")
                    oItem.TableColumn = nHManager.Instance.GetTableIDColumn(entityType);
                else
                    oItem.TableColumn = nHManager.Instance.GetTableColumn(entityType, propertyName);
            }
            catch { }
           
            return oItem;
        }

        public static string GET_FILTERS_SQL(FilterList filters, string tableAlias, Dictionary<String, ForeignField> foreignFields = null)
        {
            return GET_FILTERS_SQL(filters, tableAlias, CultureInfo.CurrentCulture, foreignFields);
        }

        public static string GET_FILTERS_SQL(FilterList filters, string tableAlias, CultureInfo cultureInfo, Dictionary<String, ForeignField> foreignFields = null)
        {           
            string queryPattern = "(TRUE {0})";
            string stringPattern = @"{0}.""{1}"" {2} '{3}'";
            string intPattern = @"{0}.""{1}"" {2} {3}";
            string likePattern = @"{0}.""{1}"" {2} '%{3}%'";
            string likeIntPattern = @"TRIM(TO_CHAR({0}.""{1}"", '999999999999')) {2} '%{3}%'";
            string likeDatePattern = @"TO_CHAR({0}.""{1}"", '" + cultureInfo.DateTimeFormat.ShortDatePattern + "') {2} '%{3}%'";
            string StartPattern = @"{0}.""{1}"" {2} '{3}%'";
            string StartIntPattern = @"TRIM(TO_CHAR({0}.""{1}"", '999999999999')) {2} '{3}%'";
            string StartDatePattern = @"TO_CHAR({0}.""{1}"", '" + cultureInfo.DateTimeFormat.ShortDatePattern + "') {2} '{3}%'";
            string betweenPattern = @"{0}.""{1}"" {2} '{3}' AND '{4}'";
            string betweenIntPattern = @"{0}.""{1}"" {2} {3} AND {4}";
            string query = string.Empty;

            if (filters != null)
            {
                foreach (FilterItem item in filters)
                {
                    switch (item.FilterProperty)
                    {
                        case IFilterProperty.All:
                            {
                                query += @" 
									AND (FALSE ";

                                ICollection<Column> cols = nHManager.Instance.GetTableColumns(item.EntityType);
                                foreach (Column col in nHManager.Instance.GetTableColumns(item.EntityType))
                                {
                                    if (col.Type is NHibernate.Type.StringType)
                                    {
                                        query += " OR ";
                                        query += String.Format(likePattern, tableAlias, col.Name, GET_OPERATOR(item.Operation), item.ValueToString.ToLower());
                                    }
                                    else if ((col.Type is NHibernate.Type.DateTimeType) ||
                                            (col.Type is NHibernate.Type.DateType))
                                    {
                                        query += " OR ";
                                        query += String.Format(likeDatePattern, tableAlias, col.Name, GET_OPERATOR(item.Operation), item.ValueToString.ToLower());
                                    }
                                    else if ((col.Type is NHibernate.Type.Int32Type) ||
                                            (col.Type is NHibernate.Type.Int64Type))
                                    {
                                        query += " OR ";
                                        query += String.Format(likeIntPattern, tableAlias, col.Name, GET_OPERATOR(item.Operation), item.ValueToString.ToLower());
                                    }
                                }

                                if (foreignFields != null)
                                {
                                    NHibernate.Mapping.Column fcol;
                                    foreach (KeyValuePair<String, ForeignField> field in foreignFields)
                                    {
                                        fcol = field.Value.Column;
                                        if (fcol != null)
                                        {
                                            if (fcol.Type is NHibernate.Type.StringType)
                                            {
                                                query += " OR ";
                                                query += String.Format(likePattern, field.Value.TableAlias, fcol.Name, GET_OPERATOR(item.Operation), item.ValueToString.ToLower());
                                            }
                                            else if ((fcol.Type is NHibernate.Type.DateTimeType) ||
                                                    (fcol.Type is NHibernate.Type.DateType))
                                            {
                                                query += " OR ";
                                                query += String.Format(likeDatePattern, field.Value.TableAlias, fcol.Name, GET_OPERATOR(item.Operation), item.ValueToString.ToLower());
                                            }
                                            else if ((fcol.Type is NHibernate.Type.Int32Type) ||
                                                    (fcol.Type is NHibernate.Type.Int64Type))
                                            {
                                                query += " OR ";
                                                query += String.Format(likeIntPattern, field.Value.TableAlias, fcol.Name, GET_OPERATOR(item.Operation), item.ValueToString.ToLower());
                                            }
                                        }
                                    }
                                }
                                query += ")";
                            }
                            break;

                        case IFilterProperty.ByParamenter:
                            {
                                Column col = nHManager.Instance.GetTableColumn(item.EntityType, item.Property);
                                switch (item.Operation)
                                {                                    
                                    case Operation.Contains:

                                        query += " AND ";

                                        if (col.Type is NHibernate.Type.StringType)
                                        {
                                            query += String.Format(likePattern, tableAlias, col.Name, GET_OPERATOR(item.Operation), item.ValueToString.ToLower());
                                        }
                                        else if ((col.Type is NHibernate.Type.DateTimeType) ||
                                                (col.Type is NHibernate.Type.DateType))
                                        {
                                            query += String.Format(likeDatePattern, tableAlias, col.Name, GET_OPERATOR(item.Operation), item.ValueToString.ToLower());
                                        }
                                        else if ((col.Type is NHibernate.Type.Int32Type) ||
                                                (col.Type is NHibernate.Type.Int64Type))
                                        {
                                            query += String.Format(likeIntPattern, tableAlias, col.Name, GET_OPERATOR(item.Operation), item.ValueToString.ToLower());
                                        }

                                        break;

                                    case Operation.StartsWith:

                                        query += " AND ";

                                        if (col.Type is NHibernate.Type.StringType)
                                        {
                                            query += String.Format(StartPattern, tableAlias, col.Name, GET_OPERATOR(item.Operation), item.ValueToString.ToLower());
                                        }
                                        else if ((col.Type is NHibernate.Type.DateTimeType) ||
                                                (col.Type is NHibernate.Type.DateType))
                                        {
                                            query += String.Format(StartDatePattern, tableAlias, col.Name, GET_OPERATOR(item.Operation), item.ValueToString.ToLower());
                                        }
                                        else if ((col.Type is NHibernate.Type.Int32Type) ||
                                                (col.Type is NHibernate.Type.Int64Type))
                                        {
                                            query += String.Format(StartIntPattern, tableAlias, col.Name, GET_OPERATOR(item.Operation), item.ValueToString.ToLower());
                                        }

                                        break;

                                    case Operation.Between:

                                        query += " AND ";

                                        // TODO : Aggregate second item value to the filter
                                        // Necessary second item value in order to this work
                                        if ((item.TableColumn.Type is NHibernate.Type.DateTimeType) ||
                                            (item.TableColumn.Type is NHibernate.Type.DateType) ||
                                            (item.TableColumn.Type is NHibernate.Type.StringType))
                                        {
                                            query += String.Format(betweenPattern, tableAlias, item.TableColumn.Name, GET_OPERATOR(item.Operation), item.Value.ToString(), item.Value.ToString());
                                        }
                                        else
                                        {
                                            query += String.Format(betweenIntPattern, tableAlias, item.TableColumn.Name, GET_OPERATOR(item.Operation), item.Value, item.Value);
                                        }

                                        break;

                                    default:

                                        query += " AND ";

                                        if ((item.TableColumn.Type is NHibernate.Type.DateTimeType) ||
                                            (item.TableColumn.Type is NHibernate.Type.DateType) ||
                                            (item.TableColumn.Type is NHibernate.Type.StringType))
                                        {
                                            query += String.Format(stringPattern, tableAlias, item.TableColumn.Name, GET_OPERATOR(item.Operation), item.Value.ToString());
                                        }
                                        else
                                        {
                                            query += String.Format(intPattern, tableAlias, item.TableColumn.Name, GET_OPERATOR(item.Operation), item.Value);
                                        }

                                        break;

                                }
                            }
                            break;
                    }
                }
            }

            return String.Format(queryPattern, query);
        }

        public static string GET_ORDERS_SQL(OrderList orders, string tableAlias, Dictionary<String, ForeignField> foreignFields = null)
        {
            String pattern = " {0}.\"{1}\" {2}";
            String pattern2 = " \"{0}\" {1}";
            String query = @"
				ORDER BY";

            if ((orders == null) || (orders.Count == 0))
            {
                return query + String.Format(pattern, tableAlias, "OID", "ASC");
            }            

            foreach (OrderItem order in orders)
            {
                if (foreignFields != null && foreignFields.ContainsKey(order.Property))
                {
                    if (foreignFields[order.Property].Column == null)
                    {
                        query += String.Format(pattern2,
                                            foreignFields[order.Property].Property,
                                            ((order.Direction == ListSortDirection.Ascending) ? "ASC" : "DESC")
                                            );
                    }
                    else
                        query += String.Format(pattern,
                                            foreignFields[order.Property].TableAlias,
                                            foreignFields[order.Property].Column.Name,
                                            ((order.Direction == ListSortDirection.Ascending) ? "ASC" : "DESC")
                                            );
                }
                else
                {
                    query += String.Format(pattern,
                                            tableAlias,
                                            order.TableColumn.Name,
                                            ((order.Direction == ListSortDirection.Ascending) ? "ASC" : "DESC")
                                            );
                }

                query += ",";
            }

            return query.Substring(0, query.Length - 1);
        }

        public static string GET_OPERATOR(Operation operation)
        {
            switch (operation)
            {
                case Operation.Equal: return "=";
                case Operation.Contains: return "ILIKE";
                case Operation.StartsWith: return "ILIKE";
                case Operation.Less: return "<";
                case Operation.LessOrEqual: return "<=";
                case Operation.GreaterOrEqual: return ">=";
                case Operation.Greater: return ">";
                case Operation.Between: return "BETWEEN";
                case Operation.Distinct: return "!=";
                default: return "=";
            }
        }
    }

    public class ForeignField
    {
        public string Property { get; set; }
        public string TableAlias { get; set; }
        public NHibernate.Mapping.Column Column { get; set; }
    }
}
