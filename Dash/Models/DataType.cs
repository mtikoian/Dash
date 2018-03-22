using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    public class DataType : BaseModel
    {
        public bool IsBinary { get; set; } = false;
        public bool IsBool { get; set; } = false;
        public bool IsCurrency { get; set; } = false;
        public bool IsDateTime { get; set; } = false;
        public bool IsDecimal { get; set; } = false;
        public bool IsInteger { get; set; } = false;
        public bool IsText { get; set; } = true;

        [Required, MaxLength(100)]
        public string Name { get; set; }
    }
}
