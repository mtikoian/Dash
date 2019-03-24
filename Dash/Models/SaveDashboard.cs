using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Dash.Models
{
    public class SaveDashboard : BaseModel, IValidatableObject
    {
        IHttpContextAccessor _HttpContextAccessor;

        public List<WidgetPosition> Widgets { get; set; }

        public void Update()
        {
            var myWidgets = DbContext.GetAll<Widget>(new { UserId = _HttpContextAccessor.HttpContext.User.UserId() });
            Widgets.ForEach(x => {
                myWidgets.Where(w => w.Id == x.SanitizedId()).FirstOrDefault()?.SavePosition(x.Width, x.Height, x.X, x.Y);
            });
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DbContext = (IDbContext)validationContext.GetService(typeof(IDbContext));
            _HttpContextAccessor = (IHttpContextAccessor)validationContext.GetService(typeof(IHttpContextAccessor));
            yield return null;
        }
    }
}
