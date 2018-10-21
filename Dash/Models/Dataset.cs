using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using Dash.Resources;
using Dash.Utils;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.Models
{
    public enum DatasetTypes
    {
        Table = 1,
        Proc = 2
    }

    public class Dataset : BaseModel, IValidatableObject
    {
        private List<Role> _AllRoles;
        private Database _Database;
        private List<DatasetColumn> _DatasetColumn;
        private List<DatasetJoin> _DatasetJoin;
        private List<DatasetRole> _DatasetRole;

        public Dataset()
        {
        }

        public Dataset(IDbContext dbContext)
        {
            DbContext = dbContext;
        }

        [Display(Name = "Conditions", ResourceType = typeof(Datasets))]
        [StringLength(250, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Conditions { get; set; }

        [Display(Name = "CurrencyFormat", ResourceType = typeof(Datasets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(50, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string CurrencyFormat { get; set; } = "{s:$} {[t:,][d:.][p:2]}";

        [BindNever, ValidateNever]
        public Database Database
        {
            get { return _Database ?? (_Database = DbContext.Get<Database>(DatabaseId)); }
            set { _Database = value; }
        }

        [DbIgnore]
        public string DatabaseHost { get; set; }

        [Display(Name = "Database", ResourceType = typeof(Datasets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DatabaseId { get; set; }

        [DbIgnore]
        public string DatabaseName { get; set; }

        [BindNever, ValidateNever]
        public List<DatasetColumn> DatasetColumn
        {
            get { return _DatasetColumn ?? (_DatasetColumn = DbContext?.GetAll<DatasetColumn>(new { DatasetId = Id }).ToList()); }
            set { _DatasetColumn = value; }
        }

        [BindNever, ValidateNever]
        public List<DatasetJoin> DatasetJoin
        {
            get { return _DatasetJoin ?? (_DatasetJoin = DbContext?.GetAll<DatasetJoin>(new { DatasetId = Id }).ToList()); }
            set { _DatasetJoin = value; }
        }

        [BindNever, ValidateNever]
        public List<DatasetRole> DatasetRole
        {
            get { return _DatasetRole ?? (_DatasetRole = DbContext.GetAll<DatasetRole>(new { DatasetId = Id }).ToList()); }
            set { _DatasetRole = value; }
        }

        [Display(Name = "DateFormat", ResourceType = typeof(Datasets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(50, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string DateFormat { get; set; } = "Y-m-d H:i:S";

        [DbIgnore]
        public List<string> DefaultCurrencyFormats
        {
            get { return new List<string> { "{s:$} {[t:,][d:.][p:2]}", "{s:£}{[t:,][d:.][p:2]}", "{[t:.][d:,][p:2]} {s:€}" }; }
        }

        [DbIgnore]
        public List<string> DefaultDateFormats
        {
            get { return new List<string> { "Y-m-d H:i:S", "Y-m-d", "n/j/Y H:i:S", "n/j/y" }; }
        }

        [DbIgnore]
        public bool IsProc { get { return TypeId == (int)DatasetTypes.Proc; } }

        [Display(Name = "Name", ResourceType = typeof(Datasets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Name { get; set; }

        [Display(Name = "PrimarySource", ResourceType = typeof(Datasets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string PrimarySource { get; set; }

        [Display(Name = "Roles", ResourceType = typeof(Datasets))]
        [DbIgnore]
        public List<int> RoleIds { get; set; }

        [Display(Name = "Type", ResourceType = typeof(Datasets))]
        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int TypeId { get; set; }

        [DbIgnore]
        public IEnumerable<SelectListItem> TypeList
        {
            get { return typeof(DatasetTypes).TranslatedSelect(new ResourceDictionary("Datasets"), "LabelType_"); }
        }

        public List<object> AvailableColumns(string[] tableNames = null)
        {
            var columns = new List<object>();
            if (Id == 0)
            {
                return columns;
            }

            if (Database?.TestConnection(out var error) == true)
            {
                var tableList = tableNames != null ? tableNames.Distinct().ToList() : TableList();
                tableList.Each(table => {
                    Database.GetTableSchema(table).Rows.OfType<DataRow>().Each(row => {
                        columns.Add($"{row.ToTableName(Database.IsSqlServer)}.{row.ToColumnName(Database.IsSqlServer, false)}");
                    });
                });
            }
            return columns;
        }

        public Dataset Copy(string name = null)
        {
            var newDataset = this.Clone();
            newDataset.Id = 0;
            newDataset.Name = name ?? Datasets.LabelCopyOf.ToString().Replace("{0}", Name);
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

        public List<Role> GetAllRoles()
        {
            return _AllRoles ?? (_AllRoles = DbContext.GetAll<Role>().OrderBy(r => r.Name).ToList());
        }

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

        public bool ImportSchema(int userId, out string error)
        {
            error = "";
            if (Database?.TestConnection(out var testError) != true)
            {
                error = Datasets.ImportErrorDatabaseRequired;
                return false;
            }
            if (IsProc)
            {
                error = Datasets.ImportErrorNoProcs;
                return false;
            }

            var sources = TableList();
            if (!sources.Any())
            {
                error = Datasets.ImportErrorPrimarySourceRequired;
                return false;
            }

            var existingColumns = new Dictionary<string, DatasetColumn>();
            var dataTypes = DbContext.GetAll<DataType>().ToDictionary(t => t.Name, t => t);
            DatasetColumn?.Each(x => existingColumns.Add(x.ColumnName.ToLower(), x));
            var totalColumns = 0;
            sources.Each(source => {
                Database.GetTableSchema(source).Rows.OfType<DataRow>().Each(row => {
                    totalColumns++;
                    var columnName = row.ToColumnName(Database.IsSqlServer);
                    if (!existingColumns.ContainsKey(columnName.ToLower()))
                    {
                        var newColumn = new DatasetColumn {
                            DatasetId = Id,
                            DataTypeId = 0,
                            FilterTypeId = 0,
                            Title = row["COLUMN_NAME"].ToString(),
                            ColumnName = columnName,
                            RequestUserId = userId
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

                            DbContext.Save(newColumn);
                            existingColumns.Add(columnName.ToLower(), newColumn);
                        }
                    }
                });
            });

            if (totalColumns == 0)
            {
                error = Datasets.ImportErrorNoColumnsRead;
                return false;
            }
            return true;
        }

        public bool IsUniqueName(string name, int id)
        {
            return !DbContext.GetAll<Dataset>(new { Name = name }).Any(x => x.Id != id);
        }

        public void Save(bool lazySave = true, bool rolesOnly = false)
        {
            var keyedDatasetRoles = DbContext.GetAll<DatasetRole>(new { DatasetId = Id }).ToDictionary(x => x.RoleId, x => x);
            DatasetRole = RoleIds?.Where(x => x > 0).Select(id => keyedDatasetRoles.ContainsKey(id) ? keyedDatasetRoles[id] : new DatasetRole { DatasetId = Id, RoleId = id }).ToList()
                ?? new List<DatasetRole>();
            DatasetRole.Each(x => x.RequestUserId = RequestUserId);

            DbContext.WithTransaction(() => {
                DbContext.Save(this);
                if (lazySave)
                {
                    DbContext.SaveMany(this, DatasetColumn);
                    DbContext.SaveMany(this, DatasetJoin);
                    DbContext.SaveMany(this, DatasetRole);
                }
                if (rolesOnly)
                {
                    DbContext.SaveMany(this, DatasetRole);
                }
            });
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!IsUniqueName(Name, Id))
            {
                yield return new ValidationResult(Datasets.ErrorDuplicateName, new[] { "Name" });
            }
        }

        private List<string> TableList()
        {
            var tableList = new List<string>();
            if (!PrimarySource.IsEmpty())
            {
                tableList.Add(PrimarySource);
            }
            // add any joined tables as groups
            tableList.AddRange(DatasetJoin?.Select(x => x.TableName).Distinct().Where(x => !tableList.Contains(x)));
            return tableList;
        }
    }

    public class LookupItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
    }
}
