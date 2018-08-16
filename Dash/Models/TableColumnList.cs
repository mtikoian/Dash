using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class TableColumnList : BaseModel, IValidatableObject
    {
        private Database _Database;

        public List<string> Tables { get; set; }

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DatabaseId { get; set; }

        [BindNever, ValidateNever]
        public Database Database { get { return _Database ?? (_Database = DbContext.Get<Database>(DatabaseId)); } }

        public decimal ReportWidth { get; set; }

        public List<string> GetList()
        {
            var list = new List<string>();
            Tables.ForEach(x => {
                var schema = Database.GetTableSchema(x);
                if (schema.Rows.Count > 0)
                {
                    foreach (System.Data.DataRow row in schema.Rows)
                    {
                        list.Add(row.ToColumnName(Database.IsSqlServer));
                    }
                }
            });
            return list.Distinct().OrderBy(x => x).ToList();
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            if (Database == null)
            {
                yield return new ValidationResult(Core.ErrorInvalidId);
            }
        }
    }
}
