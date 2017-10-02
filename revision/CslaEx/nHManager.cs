using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

using NHibernate;
using NHibernate.Impl;
using NHibernate.Cfg;
using NHibernate.Expression;
using NHibernate.Transform;
using NHibernate.Mapping;

using CslaEx.Resources;

namespace CslaEx
{
    [Serializable()]
    public class nHManager
    {

        #region Business Methods
        
        [NonSerialized()]
        private Configuration _cfg = null;
		[NonSerialized()]
        private ISessionFactory _sFactory = null;
		[NonSerialized()]
        private List<ISession> _sessions = new List<ISession>();
        [NonSerialized()]
        private string _server = null;

        public Configuration Cfg
        {
            get { return _cfg; }
            set { _cfg = value; }
        }

        public bool UseDirectSQL
        {
            get { return (Cfg != null) ? Convert.ToBoolean(Cfg.Properties["use_direct_sql"]) : false; }
        }

        public string DefaultUser
        {
            get { return (Cfg != null) ? Cfg.Properties["default_user"].ToString() : string.Empty; }
            set 
            {
                Cfg.Properties["default_user"] = value;
            }
        }
        
        public ISessionFactory SFactory
        {
            get { return _sFactory; }
            set { _sFactory = value; }
        }

		public List<ISession> Sessions
		{
			get { return _sessions; }
        }

        public string Server
        {
            get { return _server; }
            set { _server = value; }
        }

        /// <summary>
        /// Abre una sesión
        /// </summary>
        /// <returns>Código de session</returns>
		/// 
        public int OpenSession()
        {
			int pos = 0;

			ISession sess = SFactory.OpenSession();
			
			// Buscamos la primera posicion vacía
			foreach (ISession session in Sessions)
			{
				if (session == null)
				{
					Sessions.RemoveAt(pos);
					break;
				}
				pos++;
			}

			Sessions.Insert(pos, sess);
			return pos;
        }

        /// <summary>
        /// Devuelve la sesión asociada a un objeto
        /// </summary>
        /// <param name="type">Tipo del objeto</param>
		/// <param name="oid">ID del objeto</param>
        /// <returns></returns>
        public ISession GetSession(Type type, long oid)
        {
			int pos = -1;
			foreach (ISession sess in Sessions)
			{
				pos++;
				if (sess.Get(type, oid) != null)
					return sess;
			}

			return null;
        }


        /// <summary>
        /// Devuelve el nombre del campo de la tabla asociado a la propiedad
        /// </summary>
        /// <param name="type">Tipo del objeto del que se quiere obtener la propiedad</param>
        /// <param name="property"></param>
        /// <returns></returns>
        public string GetTableField(Type type, string property)
        {
            System.Collections.ICollection cols;

            cols = Cfg.GetClassMapping(type).PropertyCollection;
            
            foreach (Property prop in cols)
            {
                if (prop.Name == property)
                {
                    Column col = (Column)(((IList)(prop.ColumnCollection))[0]);
                    return col.Text;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Devuelve el nombre del campo identificador en la tabla
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string GetTableID(Type type)
        {
            PersistentClass pclass;

            //Obtencion de la información de mapeo
            pclass = Cfg.GetClassMapping(type);

            Column columna = (Column)((IList)(pclass.Identifier.ColumnCollection))[0];
            return (columna!=null) ? columna.Text : string.Empty;
            
        }



		#region By Code Interface

		/// <summary>
		/// Devuelve una sesión
		/// </summary>
		/// <param name="type">Tipo del objeto</param>
		/// <returns></returns>
		public ISession GetSession(int sessionCode)
		{
			return (sessionCode < Sessions.Count) ? Sessions[sessionCode] : null;
		}

		/// <summary>
		/// Devuelve un criterio asociado a la sesion perteneciente a type.
		/// </summary>
		/// <param name="type">Tipo del objeto</param>
		/// <param name="oid">Oid del objeto</param>
		/// <returns></returns>
		public CriteriaEx GetCriteria(Type type, int sessionCode)
		{
			ISession sess = GetSession(sessionCode);
			return (sess == null) ? null : new CriteriaEx(type, (SessionImpl)sess, sessionCode);
		}

		public ITransaction BeginTransaction(int sessionCode)
		{
			ISession sess = GetSession(sessionCode);
			return (sess != null) ? sess.BeginTransaction() : null;
		}

		public void CloseSession(int sessionCode)
		{
            if (Sessions.Count == 0) return;
            if ((Sessions.Count <= sessionCode) || (Sessions[sessionCode] == null))
            {
                throw new Exception(Messages.CLOSE_SESSION_ERROR + System.Environment.NewLine +
                                    Messages.INVALID_SESSION_NUMBER);
            }

			Sessions[sessionCode].Close();
			//Si es la última la eliminamos
			//si no la ponemos a null para reaprovecharla luego
            if (sessionCode == Sessions.Count - 1)
                Sessions.RemoveAt(sessionCode);
            else
                Sessions[sessionCode] = null;
		}

		/// <summary>
		/// Devuelve la transaccion asociada a una session
		/// </summary>
		/// <param name="sess">Sesión</param>
		/// <returns></returns>
		public ITransaction GetTransaction(int codeSession)
		{
			ISession sess = GetSession(codeSession);
			return (sess != null) ? sess.Transaction : null;
		}

		#endregion

		/// <summary>
		/// Devuelve un criterio asociado a la sesion
		/// </summary>
		/// <param name="sess">Sesión desde la que obtener el criterio</param>
		/// <returns></returns>
		public CriteriaEx GetCriteria(ISession sess, Type type)
		{
			return (sess == null) ? null : new CriteriaEx(type, (SessionImpl)sess, 0);
		}

		/// <summary>
		/// Devuelve un criterio asociado a la sesion perteneciente a type y oid.
		/// </summary>
		/// <param name="type">Tipo del objeto</param>
		/// <param name="oid">Oid del objeto</param>
		/// <returns></returns>
        public CriteriaEx GetCriteria(Type type, long oid)
        {
			ISession sess = GetSession(type, oid);
            return (sess == null) ? null : new CriteriaEx(type, (SessionImpl)sess, 0);
        }

		public ITransaction BeginTransaction(ISession sess)
		{
			return (sess != null) ? sess.BeginTransaction() : null;
		}

		/// <summary>
		/// Devuelve la transaccion asociada a una session
		/// </summary>
		/// <param name="sess">Sesión</param>
		/// <returns></returns>
		public ITransaction GetTransaction(ISession sess)
		{
			return (sess != null) ? sess.Transaction : null;
		}

		/// <summary>
		/// Devuelve la transaccion asociada a un objeto
		/// </summary>
		/// <param name="type">Tipo del objeto</param>
		/// <param name="oid">ID del objeto</param>
		/// <returns></returns>
		public ITransaction GetTransaction(Type type, long oid)
		{
			foreach (ISession sess in Sessions)
			{
				if (sess.Get(type, oid) != null)
					return sess.Transaction;
			}

			return null;
		}

        public IList HQLSelect(string query)
        {
			ISession sess = SFactory.OpenSession();
			ITransaction trans = sess.BeginTransaction();
			IList results;

			try
			{
				results = sess.CreateQuery(query).List();
				trans.Commit();
			}
			catch (Exception ex)
			{
				if (trans != null) trans.Rollback();
				throw new Exception(ex.Message);
			}
			finally
			{
				sess.Close();
			}

			return results;
		}

		public void HQLExecute(string query, Type type)
		{
			ISession sess = SFactory.OpenSession();
			ITransaction trans = sess.BeginTransaction();

			try
			{
				IQuery nHQ = sess.CreateSQLQuery(query).AddEntity(type);
				nHQ.UniqueResult();
				trans.Commit();
			}
			catch (Exception ex)
			{
				if (trans != null) trans.Rollback();
				throw new Exception(ex.Message);
			}
			finally
			{
				sess.Close();
			}
		}

		/*public IList SQLNativeSelect(string query, Type type)
		{
			ISession sess = SFactory.OpenSession();
			ITransaction trans = sess.BeginTransaction();
			IList results = null;

			try
			{
				IQuery nHQ = sess.CreateSQLQuery(query)
								.SetResultTransformer(Transformers.AliasToBean(type));

				results = nHQ.List();

				trans.Commit();
			}
			catch (Exception ex)
			{
				if (trans != null) trans.Rollback();
				throw new Exception(ex.Message);
			}
			finally
			{
				sess.Close();
			}

			return results;
		}*/

		/// <summary>
		/// Ejecuta una consulta SQL nativa mediante una nueva session
		/// </summary>
		/// <param name="query"></param>
		public IDataReader SQLNativeSelect(string query)
		{
			ISession sess = null;
			ITransaction trans = null;
            //System.Object values;

			try
			{
				sess = SFactory.OpenSession();
				trans = sess.BeginTransaction();
				IDbCommand command = sess.Connection.CreateCommand();

				command.CommandText = query;
				IDataReader list = command.ExecuteReader();

				trans.Commit();

                //IDataRecord rec;

                //rec.GetValues(values);

				return list;
			}
			catch (Exception ex)
			{
				if (trans != null) trans.Rollback();
				throw new Exception(ex.Message);
			}
			finally
			{
				sess.Close();
			}
		}

		/// <summary>
		/// Ejecuta una consulta SQL nativa mediante una session abierta
		/// </summary>
		/// <param name="query"></param>
		/// <param name="sess"></param>
		public IDataReader SQLNativeSelect(string query, ISession sess)
		{
			try
			{
				IDbCommand command = sess.Connection.CreateCommand();

				command.CommandText = query;
				return command.ExecuteReader();
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}

		/// <summary>
		/// Ejecuta una instruccion SQL nativa mediante una nueva session
		/// </summary>
		/// <param name="query"></param>
		public void SQLNativeExecute(string query)
		{
			ISession sess = null;
			ITransaction trans = null;

			try
			{
				sess = SFactory.OpenSession();
				trans = sess.BeginTransaction();
				IDbCommand command = sess.Connection.CreateCommand();

				command.CommandText = query;
				command.ExecuteNonQuery();

				trans.Commit();
			}
			catch (Exception ex)
			{
				if (trans != null) trans.Rollback();
				throw new Exception(ex.Message);
			}
			finally
			{
				sess.Close();
			}
		}

		/// <summary>
		/// Ejecuta una instruccion SQL nativa mediante una session abierta
		/// </summary>
		/// <param name="query"></param>
		/// <param name="sess"></param>
		public void SQLNativeExecute(string query, ISession sess)
		{
			try
			{
				IDbCommand command = sess.Connection.CreateCommand();

				command.CommandText = query;
				command.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}

        #endregion

        #region Factory Methods
		
        public static nHManager _main;

        public static nHManager Instance
        {
          get { return (_main != null) ? _main : new nHManager();}
        }

        private nHManager()
        {
            _main = this;
        }

		public void Configure(string nHConfigFile)
		{
		    _cfg = new Configuration();
			_cfg.Configure(nHConfigFile);
            _sFactory = _cfg.BuildSessionFactory();

            string key = string.Empty;
            foreach (DictionaryEntry item in _cfg.Properties)
            {
                key = item.Key.ToString();
                if (key == "hibernate.connection.connection_string") 
                {
                    int pos = 0;
                    _server = item.Value.ToString();
                    pos = _server.IndexOf("Server=");
                    pos += 7;
                    _server = _server.Substring(pos);
                    pos = _server.IndexOf(";");
                    _server = _server.Substring(0, pos);

                }
            }

        }

        /// <summary>
        /// Función que cambia el usuario por defecto que se carga al ejecutar la aplicación
        /// </summary>
        /// <param name="nHConfigFile"></param>
        public void ConfigureDefaultUser(string nHConfigFile)
        {
            string propiedad = "<property name=\"default_user\">";
            string fin_propiedad = "</property>";
            string texto = System.IO.File.ReadAllText(nHConfigFile);

            int pos = texto.IndexOf(propiedad);
            string inicio = texto.Substring(0, pos + propiedad.Length);
            texto = texto.Substring(inicio.Length);
            pos = texto.IndexOf(fin_propiedad);
            string fin = texto.Substring(pos);
            texto = inicio + DefaultUser + fin;

            System.IO.File.WriteAllText(nHConfigFile, texto);         
        }
	
		#endregion

		#region Format Methods

		public string FormatQuery(string query)
		{
			return query;
		}

		#endregion

	}

}