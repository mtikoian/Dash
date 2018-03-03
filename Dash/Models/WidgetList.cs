using Jil;
using System.Collections.Generic;
using System.Linq;

namespace Dash.Models
{
    public class WidgetList : BaseModel
    {
        private IEnumerable<Widget> _Widgets { get; set; }

        public string ToJson { get { return JSON.SerializeDynamic(Widgets, JilOutputFormatter.Options); } }

        public IEnumerable<Widget> Widgets
        {
            get
            {
                if (_Widgets == null && (Authorization.User?.Id ?? 0) > 0)
                {
                    _Widgets = DbContext.GetAll<Widget>(new { UserId = Authorization.User.Id })
                        .OrderBy(x => x.X < 0 ? int.MaxValue : x.X).ThenBy(x => x.Y < 0 ? int.MaxValue : x.Y);
                }
                return _Widgets;
            }
        }
    }
}