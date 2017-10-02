using System;

namespace CslaEx
{

    public abstract class FCriteria
    {
        public abstract string GetProperty();
        public abstract object GetValue();
        public abstract void SetOperation(Operation operation);
        public Operation Operation;

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
        Greater = 6
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
    {
        private string _property;
        private T _value;
        private Operation _operation;


        public string Property
        {
            get { return _property; }
        }

        public T Value
        {
            get { return _value; }
        }

        public new Operation Operation
        {
            get { return _operation; }
        }

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

        public override void SetOperation(Operation operation)
        {
            _operation = operation;
        }

        public override string GetProperty() { return Property; }
        public override object GetValue() { return Value; }
    }
}