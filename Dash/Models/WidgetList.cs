using Jil;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Dash.Models
{
    public class WidgetList : BaseModel
    {
        private IHttpContextAccessor HttpContextAccessor;

        public WidgetList(IHttpContextAccessor httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
        }

        public string ToJson { get { return JSON.SerializeDynamic(Widgets, JilOutputFormatter.Options); } }
        public IEnumerable<Widget> Widgets
        {
            get
            {
                var userId = HttpContextAccessor.HttpContext.User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt();
                if (_Widgets == null && userId > 0)
                {
                    _Widgets = DbContext.GetAll<Widget>(new { UserId = userId })
                        .OrderBy(x => x.X < 0 ? int.MaxValue : x.X).ThenBy(x => x.Y < 0 ? int.MaxValue : x.Y);
                }
                return _Widgets;
            }
        }
        private IEnumerable<Widget> _Widgets { get; set; }
    }
}