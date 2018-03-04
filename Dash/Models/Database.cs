using Dapper;
using Jil;
using Dash.I18n;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.Models
{
    /// <summary>
    /// Database types.
    /// </summary>
    public enum DatabaseTypes
    {
        SqlServer = 1,
        MySql = 2
    }

    /// <summary>
    /// Database is a single SQL server database.
    /// </summary>
    public class Database : BaseModel, IValidatableObject
    {
        /// <summary>
        /// Get the translated dropdown list of database types.
        /// </summary>
        public static IEnumerable<SelectListItem> DatabaseTypeSelectList
        {
            get {
                return typeof(DatabaseTypes).TranslatedSelect(new ResourceDictionary("Databases"), "LabelType_");
            }
        }

        [Display(Name = "AllowPaging", ResourceType = typeof(I18n.Databases))]
        [JilDirective(true)]
        public bool AllowPaging { get; set; }

        [Display(Name = "ConfirmPassword", ResourceType = typeof(I18n.Databases))]
        [StringLength(500, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        [Ignore, JilDirective(true)]
        public string ConfirmPassword { get; set; }

        [Display(Name = "ConnectionString", ResourceType = typeof(I18n.Databases))]
        [StringLength(500, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        [JilDirective(true)]
        public string ConnectionString { get; set; }

        [Display(Name = "DatabaseName", ResourceType = typeof(I18n.Databases))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string DatabaseName { get; set; }

        [Display(Name = "Host", ResourceType = typeof(I18n.Databases))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Host { get; set; }

        [Display(Name = "IsEmptyPassword", ResourceType = typeof(I18n.Databases))]
        [Ignore, JilDirective(true)]
        public bool IsEmptyPassword { get; set; } = false;

        [Ignore, JilDirective(true)]
        public bool IsSqlServer { get { return TypeId == (int)DatabaseTypes.SqlServer; } }

        [Display(Name = "Name", ResourceType = typeof(I18n.Databases))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Name { get; set; }

        [Display(Name = "Password", ResourceType = typeof(I18n.Databases))]
        [StringLength(500, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        [JilDirective(true)]
        public string Password { get; set; }

        [Display(Name = "Port", ResourceType = typeof(I18n.Databases))]
        [StringLength(50, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Port { get; set; }

        [Display(Name = "Type", ResourceType = typeof(I18n.Databases))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [JilDirective(true)]
        public int TypeId { get; set; }

        [Display(Name = "User", ResourceType = typeof(I18n.Databases))]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string User { get; set; }

        /// <summary>
        /// Build a connection object for this database.
        /// </summary>
        /// <returns>Returns a SQL connection.</returns>
        public DbConnection GetOpenConnection()
        {
            var crypt = new Crypt(AppConfig);
            string connectionString = null;
            if (!ConnectionString.IsEmpty())
            {
                connectionString = crypt.Decrypt(ConnectionString);
            }
            else
            {
                if (IsSqlServer)
                {
                    var connBuilder = new SqlConnectionStringBuilder();
                    connBuilder.DataSource = Host + (Port.IsEmpty() ? "" : "," + Port);
                    connBuilder.InitialCatalog = DatabaseName;
                    connBuilder.UserID = User;
                    connBuilder.Password = crypt.Decrypt(Password);
                    connectionString = connBuilder.ToString();
                }
                else
                {
                    var connBuilder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder();
                    connBuilder.Server = Host;
                    if (!Port.IsEmpty())
                    {
                        connBuilder.Port = (uint)Port.ToInt();
                    }
                    connBuilder.Database = DatabaseName;
                    connBuilder.UserID = User;
                    connBuilder.Password = crypt.Decrypt(Password);
                    connectionString = connBuilder.ToString();
                }
            }

            var cnn = IsSqlServer ? SqlClientFactory.Instance.CreateConnection() : MySql.Data.MySqlClient.MySqlClientFactory.Instance.CreateConnection();
            cnn.ConnectionString = connectionString;
            cnn.Open();
            return cnn;
        }

        /// <summary>
        /// Get the tables/procs from this database.
        /// </summary>
        /// <param name="includeEmpty">Include an empty entry at start of list.</param>
        /// <returns>True if successful, else false.</returns>
        public IEnumerable<string> GetSourceList(bool includeEmpty = false, bool isProc = false)
        {
            using (var conn = GetOpenConnection())
            {
                var res = new List<string>();
                if (includeEmpty)
                {
                    res.Add("");
                }
                conn.GetSchema(isProc ? "Procedures" : "Tables").Rows.OfType<DataRow>().Each(row => {
                    if (isProc)
                    {
                        res.Add(row["ROUTINE_NAME"].ToString());
                    }
                    else if (row["TABLE_NAME"].ToString().IndexOf("conflict_") == -1)
                    {
                        // filter out tables that start with 'conflict_' - they are for replication
                        res.Add(row.ToTableName(IsSqlServer));
                    }
                });
                conn.Close();
                return res.OrderBy(x => x);
            }
        }

        /// <summary>
        /// Get the schema for a table from this database.
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <returns>List of fields, else false.</returns>
        public DataTable GetTableSchema(string tableName)
        {
            using (var conn = GetOpenConnection())
            {
                var parts = tableName.Split('.');
                if (parts.Any())
                {
                    parts = parts.Select(p => IsSqlServer ? p.Replace("[", "").Replace("]", "") : p.Replace("`", "")).ToArray();
                }
                var columnRestrictions = new string[4];
                columnRestrictions[1] = parts.Length > 1 ? parts[0] : null;
                columnRestrictions[2] = parts.Length > 1 ? parts[1] : parts[0];
                var tableSchema = conn.GetSchema("Columns", columnRestrictions);
                conn.Close();
                return tableSchema;
            }
        }

        /// <summary>
        /// Query the data source.
        /// </summary>
        /// <param name="sql">SQL statement to execute</param>
        /// <returns>IEnumerable results.</returns>
        public IEnumerable<dynamic> Query(string sql, Dictionary<string, object> parameters = null)
        {
            using (var conn = GetOpenConnection())
            {
                var obj = conn.Query(sql, parameters);
                conn.Close();
                return obj;
            }
        }

        /// <summary>
        /// Query the data source and map to a type.
        /// </summary>
        /// <typeparam name="T">Type to return results as</typeparam>
        /// <param name="sql">SQL statement to execute</param>
        /// <returns>IEnumerable results.</returns>
        public IEnumerable<T> Query<T>(string sql)
        {
            using (var conn = GetOpenConnection())
            {
                var obj = conn.Query<T>(sql);
                conn.Close();
                return obj;
            }
        }

        /// <summary>
        /// Save a database, including encrypting the password if provided.
        /// </summary>
        /// <returns>True if successful, else false.</returns>
        public bool Save()
        {
            var crypt = new Crypt(AppConfig);
            // Set password from db or encrypt new one
            if (Id > 0 && Password.IsEmpty() && !IsEmptyPassword)
            {
                var myDatabase = DbContext.Get<Database>(Id);
                if (myDatabase != null)
                {
                    Password = Password.IsEmpty() ? myDatabase.Password : crypt.Encrypt(Password);
                }
            }
            else
            {
                Password = Password.IsEmpty() ? null : crypt.Encrypt(Password);
            }
            ConnectionString = ConnectionString.IsEmpty() ? null : crypt.Encrypt(ConnectionString);

            DbContext.Save(this);
            return true;
        }

        /// <summary>
        /// Test the data source connection.
        /// </summary>
        /// <param name="errorMessage">Pass an error message back.</param>
        /// <returns>True on success, else false.</returns>
        public bool TestConnection(out string errorMessage)
        {
            try
            {
                using (var conn = GetOpenConnection())
                {
                    conn.Query("SELECT 1");
                    conn.Close();
                    errorMessage = "";
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Validate database object. Check that name is unique.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DbContext.GetAll<Database>(new { Name = Name }).Any(x => x.Id != Id))
            {
                yield return new ValidationResult(I18n.Databases.ErrorDuplicateName, new[] { "Name" });
            }
            if (ConnectionString.IsEmpty())
            {
                if (User.IsEmpty())
                {
                    yield return new ValidationResult(I18n.Databases.ErrorUsernameRequired, new[] { "User" });
                }

                if (Password != ConfirmPassword)
                {
                    yield return new ValidationResult(I18n.Databases.ErrorPasswordMatching, new[] { "Password" });
                }
            }
        }
    }
}