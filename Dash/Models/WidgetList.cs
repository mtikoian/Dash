using System.Collections.Generic;
using System.Linq;
using Jil;

namespace Dash.Models
{
    public class WidgetList : BaseModel
    {
        private IEnumerable<Widget> _Widgets { get; set; }

        public WidgetList(IDbContext dbContext, int userId)
        {
            DbContext = dbContext;
            RequestUserId = userId;
        }

        public string ToJson { get { return JSON.SerializeDynamic(Widgets, JilOutputFormatter.Options); } }

        public IEnumerable<Widget> Widgets
        {
            get
            {
                if (_Widgets == null && RequestUserId.HasPositiveValue())
                {
                    _Widgets = DbContext.GetAll<Widget>(new { UserId = RequestUserId })
                        .OrderBy(x => x.X < 0 ? int.MaxValue : x.X).ThenBy(x => x.Y < 0 ? int.MaxValue : x.Y);
                }
                return _Widgets;
            }
        }
    }
}