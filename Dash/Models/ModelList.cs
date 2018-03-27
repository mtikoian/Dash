using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dash.Models
{
    public class ModelList<T> : BaseModel
    {
        [Required(ErrorMessageResourceType = typeof(I18n.Roles), ErrorMessageResourceName = "ErrorNameRequired")]
        public List<T> List { get; set; }
    }
}
