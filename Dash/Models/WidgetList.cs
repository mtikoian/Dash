using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Dash.Models
{
    public class WidgetList : BaseModel
    {
        IActionContextAccessor _ActionContextAccessor;
        IEnumerable<WidgetView> _Widgets;

        public WidgetList(IDbContext dbContext, IActionContextAccessor actionContextAccessor, int userId)
        {
            DbContext = dbContext;
            _ActionContextAccessor = actionContextAccessor;
            RequestUserId = userId;
        }

        public IEnumerable<WidgetView> Widgets
        {
            get
            {
                if (_Widgets == null && RequestUserId.HasPositiveValue())
                    _Widgets = DbContext.Query<WidgetView>("WidgetGet", new { UserId = RequestUserId })
                        .Each(x => {
                            x.ActionContextAccessor = _ActionContextAccessor;
                            x.DbContext = DbContext;
                        })
                        .OrderBy(x => x.X < 0 ? int.MaxValue : x.X).ThenBy(x => x.Y < 0 ? int.MaxValue : x.Y);
                return _Widgets;
            }
        }
    }
}
