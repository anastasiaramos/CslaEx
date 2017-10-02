using System;
using System.Collections;
using System.Data;
using System.Reflection;

namespace CslaEx
{
    [Serializable()]
	public class PagingInfo
	{
		#region Properties

		public int TotalItems { get; set; }
		public int ItemsPerPage { get; set; }
		public int CurrentPage { get; set; }
		public int TotalPages { get { return (int)Math.Ceiling((decimal)TotalItems / ItemsPerPage); } }

		#endregion
    }
}
