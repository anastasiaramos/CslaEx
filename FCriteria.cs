using System;

namespace CslaEx
{

    public abstract class FCriteria
    {
        public abstract string GetProperty();
        public abstract object GetValue();
        public abstract void SetOperation(Operation operation);
        public abstract Operation Operation { get; }

        #region Operations

        public abstract bool Less(object value);
        public abstract bool LessOrEqual(object value);
        public abstract bool Equal(object value);
        public abstract bool GreaterOrEqual(object value);
        public abstract bool Greater(object value);
        public abstract bool Between(object value);

        #endregion
    }

    /// <summary>
    /// Operaciones posibles con string y con DateTime.
    /// Las operaciones Less, LessOrEqual, GreaterOrEqual y Greater
    /// solo se aplican a DateTime.
    /// </summary>
    public enum Operation
    {
        StartsWith = 0,
        Equal = 1,
        Contains = 2,
        Less = 3,
        LessOrEqual = 4,
        GreaterOrEqual = 5,
        Greater = 6,
        Between = 7,
		Distinct = 8
    }

    public class EnumText
    {
        public static string GetString(Operation type, long item) { return GetString((Operation)item); }
        public static string GetString(Operation item) { return GetString(item.GetType().Name.ToUpper() + "_" + item.ToString().ToUpper()); }
        public static string GetOperator(Operation item) { return GetString(item.GetType().Name.ToUpper() + "_OP_" + item.ToString().ToUpper()); }

        private static string GetString(string label) { return Resources.Enums.ResourceManager.GetString(label); }
    }


    /// <summary>
    /// Criterio especifico para el tipo DateTime. Hereda de la clase FCriteria
    /// y se crea una nueva clase para que al llamar a GetSubList y GetSortedList
    /// se usen correctamente los operadores >=, <=, >, <, y == con DateTime, ya
    /// que no se pueden comparar correctamente fechas si se pasan a string.
    /// </summary>
    public class DCriteria : FCriteria<DateTime>
    {
        public DCriteria(string prop, DateTime val, Operation op)
            : base(prop, val, op)
        { }

        public DCriteria(string prop, DateTime val1, DateTime val2, Operation op)
            : base(prop, val1, val2, op)
        { }

        public DCriteria(string prop, DateTime val)
            : base(prop, val)
        { }
    }

    /// <summary>
    /// Criterio de filtrado para una lista
    /// </summary>
    /// <typeparam name="T">Tipo de la propiedad</typeparam>
    [Serializable()]
    public class FCriteria<T> : FCriteria
        where T: IComparable
    {
        #region Attributes & Properties

        private string _property;
        private T _value;
        private T _value2;
        private Operation _operation;

        public string Property { get { return _property; } }
        public T Value { get { return _value; } }
        public T Value2 { get { return _value2; } }
        public override Operation Operation { get { return _operation; } }

        #endregion

        #region Factory Methods

        public FCriteria(string property, T value)
        {
            _property = property;
            _value = value;
            SetOperation(CslaEx.Operation.Contains);
        }

        public FCriteria(string property, T value, Operation operation)
        {
            _property = property;
            _value = value;
            _operation = operation;
        }

        public FCriteria(string property, T value1, T value2, Operation operation)
        {
            _property = property;
            _value = value1;
            _value2 = value2;
            _operation = operation;
        }

        #endregion

        public override void SetOperation(Operation operation)
        {
            _operation = operation;
        }

        public override string GetProperty() { return Property; }
        public override object GetValue() { return Value; }

        #region Operations

        public override bool Less(object value) { return (_value.CompareTo(value) > 0); }
        public override bool LessOrEqual(object value) { return ((_value.CompareTo(value) > 0) || (_value.CompareTo(value) == 0)); }
        public override bool Equal(object value) { return (_value.CompareTo(value) == 0); }
        public override bool GreaterOrEqual(object value) { return ((_value.CompareTo(value) < 0) || (_value.CompareTo(value) == 0)); }
        public override bool Greater(object value) { return (_value.CompareTo(value) < 0); }
        public override bool Between(object value) 
        {
            return ((_value.CompareTo(value) < 0) || (_value.CompareTo(value) == 0)) &&
                   ((_value2.CompareTo(value) > 0) || (_value2.CompareTo(value) == 0)); 
        }

        #endregion
    }
}