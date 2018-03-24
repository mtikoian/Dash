using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    public class SaveFilters : BaseModel
    {
        [Required(ErrorMessageResourceType = typeof(I18n.Roles), ErrorMessageResourceName = "ErrorNameRequired")]
        public List<ReportFilter> Filters { get; set; }
    }
}
