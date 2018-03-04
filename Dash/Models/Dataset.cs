using Jil;
using Dash.I18n;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.Models
{
    public enum DatasetTypes
    {
        Table = 1,
        Proc = 2
    }

    /// <summary>
    /// Dataset defines a combination of tables and their columns that reports can be created from.
    /// </summary>
    [HasMany(typeof(DatasetColumn))]
    [HasMany(typeof(DatasetJoin))]
    [HasMany(typeof(DatasetRole))]
    public class Dataset : BaseModel, IValidatableObject
    {
        private List<Role> _AllRoles;
        private Database _Database;
        private List<DatasetColumn> _DatasetColumn;
        private List<DatasetJoin> _DatasetJoin;
        private List<DatasetRole> _DatasetRole;

        [Display(Name = "Conditions", ResourceType = typeof(I18n.Datasets))]
        [StringLength(250, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        [JilDirective(true)]
        public string Conditions { get; set; }

        [Display(Name = "CurrencyFormat", ResourceType = typeof(I18n.Datasets))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(50, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        [JilDirective(true)]
        public string CurrencyFormat { get; set; } = "{s:$} {[t:,][d:.][p:2]}";

        /// <summary>
        /// Hold the data source for this dataset.
        /// </summary>
        [JilDirective(true)]
        public Database Database
        {
            get { return _Database ?? (_Database = DbContext.Get<Database>(DatabaseId)); }
            set { _Database = value; }
        }

        [Ignore]
        public string DatabaseHost { get; set; }

        [Display(Name = "Database", ResourceType = typeof(I18n.Datasets))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DatabaseId { get; set; }

        [Ignore]
        public string DatabaseName { get; set; }

        /// <summary>
        /// Hold all of the columns for this dataset.
        /// </summary>
        public List<DatasetColumn> DatasetColumn
        {
            get { return _DatasetColumn ?? (_DatasetColumn = ForSave ? null : DbContext.GetAll<DatasetColumn>(new { DatasetId = Id }).ToList()); }
            set { _DatasetColumn = value; }
        }

        /// <summary>
        /// Hold all of the joins for this dataset.
        /// </summary>
        public List<DatasetJoin> DatasetJoin
        {
            get { return _DatasetJoin ?? (_DatasetJoin = ForSave ? null : DbContext.GetAll<DatasetJoin>(new { DatasetId = Id }).ToList()); }
            set { _DatasetJoin = value; }
        }

        /// <summary>
        /// Hold all of the roles for this dataset.
        /// </summary>
        public List<DatasetRole> DatasetRole
        {
            get { return _DatasetRole ?? (_DatasetRole = ForSave ? null : DbContext.GetAll<DatasetRole>(new { DatasetId = Id }).ToList()); }
            set { _DatasetRole = value; }
        }

        [Display(Name = "DateFormat", ResourceType = typeof(I18n.Datasets))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(50, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        [JilDirective(true)]
        public string DateFormat { get; set; } = "YYYY-MM-DD HH:mm:ss";

        [Ignore, JilDirective(true)]
        public List<string> DefaultCurrencyFormats
        {
            get
            {
                return new List<string> { "{s:$} {[t:,][d:.][p:2]}", "{s:£}{[t:,][d:.][p:2]}", "{[t:.][d:,][p:2]} {s:€}" };
            }
        }

        [Ignore, JilDirective(true)]
        public List<string> DefaultDateFormats
        {
            get
            {
                return new List<string> { "YYYY-MM-DD HH:mm:ss", "YYYY-MM-DD", "MM/DD/YYYY HH:mm:ss", "MM/DD/YYYY" };
            }
        }

        [Display(Name = "DefaultGroupBy", ResourceType = typeof(I18n.Datasets))]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        [JilDirective(true)]
        public string DefaultGroupBy { get; set; }

        [Display(Name = "Description", ResourceType = typeof(I18n.Datasets))]
        [StringLength(500, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Description { get; set; }

        [Ignore]
        public bool ForSave { get; set; } = false;

        [Ignore, JilDirective(true)]
        public bool IsProc { get { return TypeId == (int)DatasetTypes.Proc; } }

        [Display(Name = "Name", ResourceType = typeof(I18n.Datasets))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Name { get; set; }

        [Display(Name = "PrimarySource", ResourceType = typeof(I18n.Datasets))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string PrimarySource { get; set; }

        [Ignore]
        public List<int> RoleIds { get; set; }

        [Display(Name = "Type", ResourceType = typeof(I18n.Datasets))]
        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [JilDirective(true)]
        public int TypeId { get; set; }

        [Ignore, JilDirective(true)]
        public IEnumerable<SelectListItem> TypeList
        {
            get
            {
                return typeof(DatasetTypes).TranslatedSelect(new ResourceDictionary("Datasets"), "LabelType_");
            }
        }

        /// <summary>
        /// Get all datasets a user can access.
        /// </summary>
        /// <returns>Returns a dictionary of <RoleId, DatasetRole>.</returns>
        public IEnumerable<Dataset> GetDatasetsForUser()
        {
            return DbContext.GetAll<Dataset>(new { UserId = Authorization.User.Id });
        }

        /// <summary>
        /// Import columns for the tables in the dataset.
        /// </summary>
        /// <returns>Returns the status message to display.</returns>
        public List<DatasetColumn> ImportSchema(int databaseId, List<string> sources)
        {
            var database = DbContext.Get<Database>(databaseId);
            var existingColumns = new Dictionary<string, DatasetColumn>();
            if (sources.Count > 0 && database != null && database.TestConnection(out var error))
            {
                // first build a dictionary of all datatypes
                var dataTypes = DbContext.GetAll<DataType>().ToDictionary(t => t.Name, t => t);
                sources.Each(source => {
                    database.GetTableSchema(source).Rows.OfType<DataRow>().Each(row => {
                        var columnName = row.ToColumnName(database.IsSqlServer);
                        if (!existingColumns.ContainsKey(columnName.ToLower()))
                        {
                            var newColumn = new DatasetColumn {
                                DatasetId = 0,
                                DataTypeId = 0,
                                FilterTypeId = 0,
                                Title = row["COLUMN_NAME"].ToString(),
                                ColumnName = columnName
                            };

                            var dataType = row["DATA_TYPE"].ToString();
                            if (dataTypes.ContainsKey(dataType))
                            {
                                // set the correct datatype
                                newColumn.DataTypeId = dataTypes[dataType].Id;

                                // try to set the correct filtertype
                                if ((dataTypes[dataType].IsInteger || dataTypes[dataType].IsDecimal))
                                {
                                    newColumn.FilterTypeId = (int)FilterTypes.Numeric;
                                }
                                else if (dataTypes[dataType].IsBool)
                                {
                                    newColumn.FilterTypeId = (int)FilterTypes.Boolean;
                                }
                                else if (dataTypes[dataType].IsDateTime)
                                {
                                    newColumn.FilterTypeId = (int)FilterTypes.Date;
                                }
                                else
                                {
                                    newColumn.FilterTypeId = (int)FilterTypes.Text;
                                }

                                existingColumns.Add(columnName.ToLower(), newColumn);
                            }
                        }
                    });
                });
            }
            return existingColumns.Values.ToList();
        }

        /// <summary>
        /// Make sure no other datasets have the same name.
        /// </summary>
        /// <param name="name">Name to check for.</param>
        /// <param name="id">ID of current dataset.</param>
        /// <returns>True if name is unique, else false.</returns>
        public bool IsUniqueName(string name, int id)
        {
            return !DbContext.GetAll<Dataset>(new { Name = name }).Any(x => x.Id != id);
        }

        /// <summary>
        /// Serialize all of the columns available in all of the tables for this dataset.
        /// </summary>
        public List<object> AvailableColumns(string[] tableNames = null)
        {
            var columns = new List<object>();
            if (Id == 0)
            {
                return columns;
            }

            // start building a list of columns. first add the primary table as a group
            var tableList = new List<string>();
            if (tableNames != null)
            {
                tableList = tableNames.Distinct().ToList();
            }
            else
            {
                if (!PrimarySource.IsEmpty())
                {
                    tableList.Add(PrimarySource);
                }
                // add any joined tables as groups
                tableList.AddRange(DatasetJoin?.Select(x => x.TableName).Distinct().Where(x => !tableList.Contains(x)));
            }

            if (Database?.TestConnection(out var error) == true)
            {
                tableList.Each(table => {
                    Database.GetTableSchema(table).Rows.OfType<DataRow>().Each(row => {
                        columns.Add(new {
                            table = row.ToTableName(Database.IsSqlServer),
                            column = row.ToColumnName(Database.IsSqlServer, false)
                        });
                    });
                });
            }
            return columns;
        }

        /// <summary>
        /// Copy a dataset.
        /// </summary>
        /// <param name="name">New dataset name.</param>
        public Dataset Copy(string name = null)
        {
            var newDataset = this.Clone();
            newDataset.Id = 0;
            newDataset.Name = name ?? Datasets.LabelCopyOf.ToString().Replace("{0}", this.Name);
            newDataset.DatasetRole = (DatasetRole ?? DbContext.GetAll<DatasetRole>(new { DatasetId = Id }))?.Select(x => new DatasetRole() { RoleId = x.RoleId }).ToList();
            newDataset.DatasetJoin = (DatasetJoin ?? DbContext.GetAll<DatasetJoin>(new { DatasetId = Id }))?.Select(x => {
                var newJoin = x.Clone();
                newJoin.Id = 0;
                newJoin.DatasetId = 0;
                return newJoin;
            }).ToList();
            newDataset.DatasetColumn = (DatasetColumn ?? DbContext.GetAll<DatasetColumn>(new { DatasetId = Id }))?.Select(x => {
                var newColumn = x.Clone();
                newColumn.Id = 0;
                newColumn.DatasetId = 0;
                return newColumn;
            }).ToList();
            return newDataset;
        }

        /// <summary>
        /// Get a list of all of the roles.
        /// </summary>
        public List<Role> GetAllRoles()
        {
            return _AllRoles ?? (_AllRoles = DbContext.GetAll<Role>().OrderBy(r => r.Name).ToList());
        }

        /// <summary>
        /// Get all the select lookups for the dataset.
        /// </summary>
        /// <param name="prependEmpty">Prepend an empty item if true.</param>
        /// <returns>Returns a dictionary of dictionaries of items.</returns>
        public Dictionary<int, Dictionary<string, LookupItem>> GetSelectFilters(bool prependEmpty = false)
        {
            var selectColumns = new Dictionary<int, Dictionary<string, LookupItem>>();
            if (Database != null && DatasetColumn?.Count > 0)
            {
                DatasetColumn.Where(x => x.IsSelect && !x.FilterQuery.IsEmpty()).ToList().ForEach(x => {
                    try
                    {
                        selectColumns.Add(x.Id, Database.Query<LookupItem>(x.FilterQuery)
                            .Prepend(new LookupItem { Value = "", Text = Reports.FilterCriteria }, prependEmpty)
                            .ToDictionary(y => y.Value, y => y));
                    }
                    catch { }
                });
            }

            return selectColumns;
        }

        /// <summary>
        /// Save a dataset.
        /// </summary>
        /// <returns>True if successful, else false.</returns>
        public bool Save()
        {
            // set the new roles
            if (RoleIds?.Any() == true)
            {
                // make a list of all dataset roles
                var keyedDatasetRoles = DbContext.GetAll<DatasetRole>(new { DatasetId = Id }).ToDictionary(x => x.RoleId, x => x);
                DatasetRole = RoleIds?.Where(x => x > 0).Select(id => keyedDatasetRoles.ContainsKey(id) ? keyedDatasetRoles[id] : new DatasetRole { DatasetId = Id, RoleId = id }).ToList()
                    ?? new List<DatasetRole>();
            }
            ForSave = true;
            DbContext.Save(this, forceSaveNulls: true);
            return true;
        }

        /// <summary>
        /// Validate dataset object. Check that name is unique.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!IsUniqueName(Name, Id))
            {
                yield return new ValidationResult(I18n.Datasets.ErrorDuplicateName, new[] { "Name" });
            }
        }
    }

    /// <summary>
    /// Select list item holder for JSON serialization.
    /// </summary>
    public class LookupItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
    }
}