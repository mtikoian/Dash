using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Dash.I18n;
using Jil;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.Models
{
    public enum DatabaseTypes
    {
        SqlServer = 1,
        MySql = 2
    }

    public class Database : BaseModel, IValidatableObject
    {
        public Database()
        {
        }

        public Database(IDbContext dbContext)
        {
            DbContext = dbContext;
        }

        [BindNever, ValidateNever]
        public static IEnumerable<SelectListItem> DatabaseTypeSelectList
        {
            get
            {
                return typeof(DatabaseTypes).TranslatedSelect(new ResourceDictionary("Databases"), "LabelType_");
            }
        }

        [Display(Name = "AllowPaging", ResourceType = typeof(Databases))]
        public bool AllowPaging { get; set; }

        [Display(Name = "ConfirmPassword", ResourceType = typeof(Databases))]
        [StringLength(500, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        [Ignore]
        public string ConfirmPassword { get; set; }

        [Display(Name = "ConnectionString", ResourceType = typeof(Databases))]
        [StringLength(500, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string ConnectionString { get; set; }

        [Display(Name = "DatabaseName", ResourceType = typeof(Databases))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string DatabaseName { get; set; }

        [Display(Name = "Host", ResourceType = typeof(Databases))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Host { get; set; }

        [Display(Name = "IsEmptyPassword", ResourceType = typeof(Databases))]
        [Ignore]
        public bool IsEmptyPassword { get; set; } = false;

        [Ignore, JilDirective(true)]
        public bool IsSqlServer { get { return TypeId == (int)DatabaseTypes.SqlServer; } }

        [Display(Name = "Name", ResourceType = typeof(Databases))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Name { get; set; }

        [Display(Name = "Password", ResourceType = typeof(Databases))]
        [StringLength(500, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Password { get; set; }

        [Display(Name = "Port", ResourceType = typeof(Databases))]
        public int? Port { get; set; }

        [Display(Name = "Type", ResourceType = typeof(Databases))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int TypeId { get; set; }

        [Display(Name = "User", ResourceType = typeof(Databases))]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string User { get; set; }

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
                    var connBuilder = new SqlConnectionStringBuilder {
                        DataSource = Host + (Port.HasPositiveValue() ? "," + Port.ToString() : ""),
                        InitialCatalog = DatabaseName,
                        UserID = User,
                        Password = crypt.Decrypt(Password)
                    };
                    connectionString = connBuilder.ToString();
                }
                else
                {
                    var connBuilder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder {
                        Server = Host,
                        Database = DatabaseName,
                        UserID = User,
                        Password = crypt.Decrypt(Password)
                    };
                    if (Port.HasPositiveValue())
                    {
                        connBuilder.Port = (uint)Port.ToInt();
                    }
                    connectionString = connBuilder.ToString();
                }
            }

            var cnn = IsSqlServer ? SqlClientFactory.Instance.CreateConnection() : MySql.Data.MySqlClient.MySqlClientFactory.Instance.CreateConnection();
            cnn.ConnectionString = connectionString;
            cnn.Open();
            return cnn;
        }

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

        public IEnumerable<dynamic> Query(string sql, Dictionary<string, object> parameters = null)
        {
            using (var conn = GetOpenConnection())
            {
                var obj = conn.Query(sql, parameters);
                conn.Close();
                return obj;
            }
        }

        public IEnumerable<T> Query<T>(string sql)
        {
            using (var conn = GetOpenConnection())
            {
                var obj = conn.Query<T>(sql);
                conn.Close();
                return obj;
            }
        }

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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DbContext.GetAll<Database>(new { Name }).Any(x => x.Id != Id))
            {
                yield return new ValidationResult(Databases.ErrorDuplicateName, new[] { "Name" });
            }
            if (ConnectionString.IsEmpty())
            {
                if (User.IsEmpty())
                {
                    yield return new ValidationResult(Databases.ErrorUsernameRequired, new[] { "User" });
                }

                if (Password != ConfirmPassword)
                {
                    yield return new ValidationResult(Databases.ErrorPasswordMatching, new[] { "Password" });
                }
            }
        }
    }
}
