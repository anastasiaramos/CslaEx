using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;

namespace Csla
{

  /// <summary>
  /// Provides a sorted view into an existing IList<T>.
  /// </summary>
  /// <typeparam name="T">
  /// Type of child object contained by
  /// the original list or collection.
  /// </typeparam>
	public class SortedBindingListEx<T> : Csla.SortedBindingList<T>
	{
		public SortedBindingListEx()
			: base(null) { }

		public SortedBindingListEx(IList<T> list)
			: base(list) { }
	}
}