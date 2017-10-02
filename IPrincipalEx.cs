using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;

using Csla;

namespace CslaEx
{
    public interface IPrincipalEx : IPrincipal
    {
        bool CanReadObject(long elemento);
        bool CanCreateObject(long elemento);
        bool CanModifyObject(long elemento);
        bool CanRemoveObject(long elemento);
    }
}
