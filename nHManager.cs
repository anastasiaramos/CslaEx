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
            get { return (Cfg != null) ? (Cfg.Properties["default_user"] != null ? Cfg.Properties["default_user"].ToString() : string.Empty) : string.Empty; }
            set 
            {
                Cfg.Properties["default_user"] = value;
            }
        }

        public string Host { get { return (Cfg != null) ? GetConnectionParam("Server") : string.Empty; } }
        public string User { get { return (Cfg != null) ? GetConnectionParam("User Id") : string.Empty; } }
        public string Password { get { return (Cfg != null) ? GetConnectionParam("Password") : string.Empty; } }
        public string Database { get { return (Cfg != null) ? GetConnectionParam("Database") : string.Empty; } }

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

        public string ConnectionString
        {
            get { return nHManager.Instance.Cfg.GetProperty("connection.connection_string"); }
            set 
            { 
                nHManager.Instance.Cfg.SetProperty("connection.connection_string", value);
                nHManager.Instance.Cfg.SetProperty("hibernate.connection.connection_string", value); 
            }
        }

        public string GetConnectionParam(string param)
        {
            try
            {
                string con = nHManager.Instance.Cfg.GetProperty("connection.connection_string");
                String[] conParams = con.Split(new Char[] { ';' });

                for (int i = 0; i < conParams.Length; i++)
                    if (conParams[i].Contains(param))
                        return conParams[i].Substring(conParams[i].IndexOf("=") + 1);
            }
            catch { }

            throw new Exception(String.Format(Messages.NOT_FOUND_CONNECTION_PARAM, param));
        }

		public void SetUser(string user)
		{
			string con = ConnectionString;

			int pass_pos = con.IndexOf("User Id=");
			int pass_length = pass_pos + con.Substring(pass_pos).IndexOf(";");

			string pass = con.Substring(0, pass_pos);
			pass += "User Id=" + user;
			pass += con.Substring(pass_length);

			ConnectionString = pass;
		}

		public void SetServer(string server)
		{
			string con = ConnectionString;

			int pass_pos = con.IndexOf("Server=");
			int pass_length = pass_pos + con.Substring(pass_pos).IndexOf(";");

			string pass = con.Substring(0, pass_pos);
			pass += "Server=" + server;
			pass += con.Substring(pass_length);

			ConnectionString = pass;
		}

		public void SetDBName(string db_name)
		{
			string con = ConnectionString;

			int pass_pos = con.IndexOf("Database=");
			int pass_length = pass_pos + con.Substring(pass_pos).IndexOf(";");

			string pass = con.Substring(0, pass_pos);
			pass += "Database=" + db_name;
			pass += con.Substring(pass_length);

			ConnectionString = pass;
		}

        public void SetDBPassword(string db_pass)
        {
            string con = ConnectionString;

            int pass_pos = con.IndexOf("Password=");
            int pass_length = pass_pos + con.Substring(pass_pos).IndexOf(";");

            string pass = con.Substring(0, pass_pos);
            pass += "Password=" + db_pass;
            pass += con.Substring(pass_length);

            ConnectionString = pass;
        }

        /// <summary>
        /// Devuelve el nombre de la tabla asociada a un tipo
        /// </summary>
        /// <param name="type">Tipo del objeto</param>
        /// <returns>Lo devuelve de la forma "schema"."tabla"</returns>
        public string GetSQLTable(Type type)
        {
            try
            {
                string schema = nHManager.Instance.Cfg.GetClassMapping(type).Table.Schema;
                schema = schema.Replace("`", "\"");
                return schema + ".\"" + nHManager.Instance.Cfg.GetClassMapping(type).Table.Name + "\"";
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("No se ha podido mapear el objeto {0}. Revise el fichero de configuración de nHibernate", type.Name), ex.InnerException);
            }
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

           throw new Exception(String.Format(Messages.COLUMN_NOT_FOUND, property, type.Name));
        }

        public Column GetTableColumn(Type type, string property)
        {
            System.Collections.ICollection cols;

            cols = Cfg.GetClassMapping(type).PropertyCollection;

            foreach (Property prop in cols)
            {
                if (prop.Name == property)
                {
                    return (Column)(((IList)(prop.ColumnCollection))[0]);
                }
            }

            throw new Exception(String.Format(Messages.COLUMN_NOT_FOUND, property, type.Name));
        }

        public ICollection<Column> GetTableColumns(Type type)
        {
            System.Collections.ICollection props;
            List<Column> cols = new List<Column>();

            props = Cfg.GetClassMapping(type).PropertyCollection;

            foreach (Property prop in props)
            {
               cols.Add((Column)(((IList)(prop.ColumnCollection))[0]));
            }

            return cols;
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

        public Column GetTableIDColumn(Type type)
        {
            PersistentClass pclass;

            //Obtencion de la información de mapeo
            pclass = Cfg.GetClassMapping(type);

            return (Column)((IList)(pclass.Identifier.ColumnCollection))[0];
           
        }
        #endregion

        #region By Code Interface

        /// <summary>
		/// Devuelve una sesión
		/// </summary>
		/// <param name="type">Tipo del objeto</param>
		/// <returns></returns>
		public ISession GetSession(int sessionCode)
		{
            if (sessionCode < Sessions.Count)
                return Sessions[sessionCode];
            else
                throw new CslaSessionException(typeof(int), sessionCode);
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
            try
            {
                ISession sess = GetSession(sessionCode);
                return (sess != null) ? sess.BeginTransaction() : null;
            }
            catch
            {
                return null;
            }
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
            {
                Sessions.RemoveAt(sessionCode);
                for (int i = Sessions.Count - 1; i >= 0; i--)
                {
                    if (Sessions[i] == null)
                        Sessions.RemoveAt(i);
                    else
                        break;
                }
            }
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
            try
            {
                ISession sess = GetSession(codeSession);
                return (sess != null) ? sess.Transaction : null;
            }
            catch
            {
                return null;
            }			
		}

		#endregion

        #region Criteria, Session & Transaction

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

        #endregion

        #region Commands

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
			
				throw ex;
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
                throw new NHibernate.ADOException(ex.Message, ex);
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
				throw ex;
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
			catch (Exception ex) { throw ex; } 
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
            Configure(nHConfigFile, string.Empty, string.Empty);
        }
        public void Configure(string nHConfigFile, string db_pwd, string db_name)
        {
			Configure(nHConfigFile, string.Empty, string.Empty, string.Empty);
        }
		public void Configure(string nHConfigFile, string db_pwd, string db_name, string server)
		{
			Configure(nHConfigFile, string.Empty, string.Empty, string.Empty, db_name);
		}
		public void Configure(string nHConfigFile, string db_pwd, string db_user, string db_name, string server)
		{
			try
			{
				_cfg = new Configuration();
				_cfg.Configure(nHConfigFile);

				if (db_user != string.Empty)
					SetUser(db_user);

				if (db_pwd != string.Empty)
					SetDBPassword(db_pwd);

				if (db_name != string.Empty)
					SetDBName(db_name);

				if (server != string.Empty)
					SetServer(server);

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
			catch (Exception ex) { throw ex; } 
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

        #region SQL

        /// <summary>
        /// Construye un LOCK para el esquema dado
        /// </summary>
        /// <param name="schema">Esquema de la base de datos</param>
        /// <returns>Consulta</returns>
        public string LOCK(Type type, string schema)
        {
            string tabla = string.Empty;

            if (schema == null)
                tabla = GetSQLTable(type);
            else
            {
                schema = "\"" + ((schema == "COMMON") ? schema : Convert.ToInt32(schema).ToString("0000")) + "\"";
                tabla = schema + ".\"" + Cfg.GetClassMapping(type).Table.Name + "\"";
            }

            //return String.Empty;

            switch (Cfg.GetProperty("dialect"))
            {
                // En PostgreSQL el LOCK se hace en el SELECT para hacerlo a nivel de registro
                // y no de tabla
                case "NHibernate.Dialect.PostgreSQLDialect":
                    return string.Empty;
                
                default:
                    return "LOCK TABLE " + tabla + " IN ROW EXCLUSIVE MODE NOWAIT;";
            }
        }

        /// <summary>
        /// Construye el SELECT de la lista
        /// </summary>
        /// <returns>Consulta SQL</returns>
        /// <remarks>Obtiene el esquema del fichero de configuración de nHibernate</remarks>
        public string SELECT(Type type)
        {
            return SELECT(type, null, true, null, null, null);
        }

        /// <summary>
        /// Construye el SELECT de la lista
        /// </summary>
        /// <param name="type">Tipo del objeto para obtener la tabla</param>
        /// <param name="schema">Esquema de datos</param>
        /// <param name="lock_regs">Bloqueo de registros</param>
        /// <param name="filter_field">Campo de filtrado</param>
        /// <param name="field_value">Valor</param>
        /// <param name="order_field">Campo de ordenación</param>
        /// <returns></returns>
        public string SELECT(Type type,
                             string schema,
                             bool lock_regs,
                             string filter_field,
                             object field_value,
                             string order_field)
        {

            string tabla = string.Empty;
            string columna = string.Empty;

            if (schema == null)
                tabla = GetSQLTable(type);
            else
            {
                schema = "\"" + ((schema == "COMMON") ? schema : Convert.ToInt32(schema).ToString("0000")) + "\"";
                tabla = schema + ".\"" + Cfg.GetClassMapping(type).Table.Name + "\"";
            }

            string query = "SELECT * " +
                            " FROM " + tabla;

            if (filter_field != null)
            {
                columna = (filter_field == "Oid") ? "OID" : GetTableField(type, filter_field);
                query += " WHERE \"" + columna + "\" = " + field_value.ToString();
            }

            if (order_field != null)
            {
                columna = (order_field == "Oid") ? "OID" : GetTableField(type, order_field);
                query += " ORDER BY \"" + columna + "\"";
            }

            query += (lock_regs) ? " FOR UPDATE NOWAIT;" : ";";

            return query;
        }


        #endregion

	}

}