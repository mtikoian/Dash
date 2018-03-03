using Jil;
using Dash.I18n;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Dash.Models
{
    /// <summary>
    /// DataType is a single SQL type of data.
    /// </summary>
    public class DataType : BaseModel
    {
        /// <summary>
        /// Build a sorted list of all data types.
        /// </summary>
        /// <returns>List of objects with id and name.</returns>
        [JilDirective(true)]
        public static IEnumerable<object> DataTypes
        {
            get
            {
                return DbContext.GetAll<DataType>().OrderBy(d => d.Name).Select(x => new { Id = x.Id, Name = x.Name }).ToList().Prepend(new { Id = 0, Name = Datasets.ColumnDataType });
            }
        }

        public bool IsBinary { get; set; } = false;
        public bool IsBool { get; set; } = false;
        public bool IsCurrency { get; set; } = false;
        public bool IsDateTime { get; set; } = false;
        public bool IsDecimal { get; set; } = false;
        public bool IsInteger { get; set; } = false;
        public bool IsText { get; set; } = true;

        [Required, MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Get a datatype from the db. Results are cached to improve performance.
        /// </summary>
        /// <param name="id">DataType ID</param>
        /// <returns>Datatype object for the ID.</returns>
        public static DataType FromId(int id)
        {
            return Cached($"datatype:{id}", () => { return DbContext.Get<DataType>(id); });
        }
    }
}