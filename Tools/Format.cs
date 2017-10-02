using System;
using System.Data;
using System.Text;
using System.Globalization;

namespace CslaEx
{
    /// <summary>
    /// Clase para formato y simplificación de acceso a valores de objetos
    /// </summary>
    public static class Format
    {
        #region IDataReader

        public static class DataReader
        {
            /// <summary>
            /// Extrae el valor de una columna tipo STRING de un IDataReader
            /// </summary>
            /// <param name="source">DataReader origen</param>
            /// <param name="column_name">Columna del DataReader</param>
            /// <returns>Valor o string.Empty si está vacío</returns>
            public static string GetString(IDataReader source, string column_name)
            {
#if TRACE
                try { return DBNull.Value.Equals(source[column_name]) ? string.Empty : source[column_name].ToString(); }
                catch (IndexOutOfRangeException) { throw new Exception(String.Format(Resources.Messages.FIELD_NOT_FOUND, column_name)); }
#else
                try { return DBNull.Value.Equals(source[column_name]) ? string.Empty : source[column_name].ToString(); }
                catch (IndexOutOfRangeException) { return string.Empty;  }
#endif
            }

            /// <summary>
            /// Extrae el valor de una columna tipo INT32 de un IDataReader
            /// </summary>
            /// <param name="source">DataReader origen</param>
            /// <param name="column_name">Columna del DataReader</param>
            /// <returns>Valor o 0 si está vacío </returns>
            public static int GetInt32(IDataReader source, string column_name)
            {
#if TRACE
                try { return DBNull.Value.Equals(source[column_name]) ? 0 : Convert.ToInt32(source[column_name]); }
                catch (IndexOutOfRangeException) { throw new Exception(String.Format(Resources.Messages.FIELD_NOT_FOUND, column_name)); }
#else
                try { return DBNull.Value.Equals(source[column_name]) ? 0 : Convert.ToInt32(source[column_name]); }
                catch (IndexOutOfRangeException) { return 0; }
#endif
            }

            /// <summary>
            /// Extrae el valor de una columna tipo INT64 de un IDataReader
            /// </summary>
            /// <param name="source">DataReader origen</param>
            /// <param name="column_name">Columna del DataReader</param>
            /// <returns>Valor o 0 si está vacío </returns>
            public static long GetInt64(IDataReader source, string column_name)
            {
#if TRACE
                try { return DBNull.Value.Equals(source[column_name]) ? 0 : Convert.ToInt64(source[column_name]); }
                catch (IndexOutOfRangeException) { throw new Exception(String.Format(Resources.Messages.FIELD_NOT_FOUND, column_name)); }
#else
                try { return DBNull.Value.Equals(source[column_name]) ? 0 : Convert.ToInt64(source[column_name]); }
                catch (IndexOutOfRangeException) { return 0; }
#endif
            }

            /// <summary>
            /// Extrae el valor de una columna tipo DECIMAL de un IDataReader
            /// </summary>
            /// <param name="source">DataReader origen</param>
            /// <param name="column_name">Columna del DataReader</param>
            /// <returns>Valor o 0 si está vacío </returns>
            public static Decimal GetDecimal(IDataReader source, string column_name)
            {
#if TRACE
                try { return DBNull.Value.Equals(source[column_name]) ? 0 : Convert.ToDecimal(source[column_name]); }
                catch (IndexOutOfRangeException) { throw new Exception(String.Format(Resources.Messages.FIELD_NOT_FOUND, column_name)); }
#else
                try { return DBNull.Value.Equals(source[column_name]) ? 0 : Convert.ToDecimal(source[column_name]); }
                catch (IndexOutOfRangeException) { return 0; }
#endif
            }

            /// <summary>
            /// Extrae el valor de una columna tipo DOUBLE de un IDataReader
            /// </summary>
            /// <param name="source">DataReader origen</param>
            /// <param name="column_name">Columna del DataReader</param>
            /// <returns>Valor o 0 si está vacío </returns>
            public static Double GetDouble(IDataReader source, string column_name)
            {
#if TRACE
                try { return DBNull.Value.Equals(source[column_name]) ? 0 : Convert.ToDouble(source[column_name]); }
                catch (IndexOutOfRangeException) { throw new Exception(String.Format(Resources.Messages.FIELD_NOT_FOUND, column_name)); }
#else
                try { return DBNull.Value.Equals(source[column_name]) ? 0 : Convert.ToDouble(source[column_name]); }
                catch (IndexOutOfRangeException) { return 0; }
#endif
            }

            /// <summary>
            /// Extrae el valor de una columna tipo BOOL de un IDataReader
            /// </summary>
            /// <param name="source">DataReader origen</param>
            /// <param name="column_name">Columna del DataReader</param>
            /// <returns>Valor o FALSE si está vacío</returns>
            public static bool GetBool(IDataReader source, string column_name)
            {
#if TRACE
                try { return DBNull.Value.Equals(source[column_name]) ? false : Convert.ToBoolean(source[column_name]); }
                catch (IndexOutOfRangeException) { throw new Exception(String.Format(Resources.Messages.FIELD_NOT_FOUND, column_name)); }
#else
                try { return DBNull.Value.Equals(source[column_name]) ? false : Convert.ToBoolean(source[column_name]); }
                catch (IndexOutOfRangeException) { return false; }
#endif
            }

            /// <summary>
            /// Extrae el valor de una columna tipo DATETIME de un IDataReader
            /// </summary>
            /// <param name="source">DataReader origen</param>
            /// <param name="column_name">Columna del DataReader</param>
            /// <returns>Valor o MinValue si está vacío</returns>
            public static DateTime GetDateTime(IDataReader source, string column_name)
            {
#if TRACE
                try { return DBNull.Value.Equals(source[column_name]) ? DateTime.MinValue : Convert.ToDateTime(source[column_name]); }
                catch (IndexOutOfRangeException) { throw new Exception(String.Format(Resources.Messages.FIELD_NOT_FOUND, column_name)); }
#else
                try { return DBNull.Value.Equals(source[column_name]) ? DateTime.MinValue : Convert.ToDateTime(source[column_name]); }
                catch (IndexOutOfRangeException) { return DateTime.MinValue; }
#endif
            }
            
        }

        #endregion

        #region String

        public static string ReplaceAccents(string inputString)
        {
            string normalizedString = inputString.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < normalizedString.Length; i++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(normalizedString[i]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(normalizedString[i]);
                }
            }
            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }

        #endregion
    }
}



    



