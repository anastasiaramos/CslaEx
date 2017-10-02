using System;
using System.Collections.Generic;
using System.Text;

namespace CslaEx
{
    /// <summary>
    /// Gestiona una lista de pares atributo-propiedad
    /// </summary>
    [Serializable()]
    public unsafe class AttributeMng
    {
        [Serializable()]
        public struct TAttribute
        {
            public void* atributo;
            public string propiedad;
        }

        List<TAttribute> _list = new List<TAttribute>();
        TAttribute _par = new TAttribute();

        public List<TAttribute> Lista { get { return _list; } }

        public void AddAtributo(void* atr, string prop)
        {
            _par.atributo = atr;
            _par.propiedad = prop;
            _list.Add(_par);
        }
    }
}
