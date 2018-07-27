using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    public enum JoinTypes
    {
        Inner = 1,
        Left = 2,
        Right = 3
    }

    public class DatasetJoin : BaseModel
    {
        public DatasetJoin()
        {
        }

        public DatasetJoin(IDbContext dbContext, int datasetid)
        {
            DbContext = dbContext;
            DatasetId = datasetid;
        }


        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int DatasetId { get; set; }

        [Required]
        public int JoinOrder { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        public int JoinTypeId { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(500, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string Keys { get; set; }

        [Required(ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorRequired")]
        [StringLength(100, ErrorMessageResourceType = typeof(I18n.Core), ErrorMessageResourceName = "ErrorMaxLength")]
        public string TableName { get; set; }
    }
}
