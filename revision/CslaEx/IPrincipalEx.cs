using System;
using System.Collections.Generic;
using System.Security;

using Csla;

namespace CslaEx
{
   // [ComVisible(true)]
    public interface IPrincipalEx : System.Security.Principal.IPrincipal
    {
        // Resumen:
        //     Obtiene la identidad del principal actual.
        //
        // Devuelve:
        //     Objeto System.Security.Principal.IIdentity asociado al principal actual.
        new IIdentityEx Identity { get; }

        // Resumen:
        //     Determina si el principal actual pertenece a la función especificada.
        //
        // Parámetros:
        //   role:
        //     Nombre de la función cuya condición de pertenencia se va a comprobar.
        //
        // Devuelve:
        //     Es true si el principal actual es un miembro de la función especificada;
        //     en caso contrario, es false.
        //bool IsInRole(string role);

        bool CanReadObject(string elemento);
        bool CanWriteObject(string elemento);

    }
}
