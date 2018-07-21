﻿using System.Collections.Generic;
using System.Linq;
using Jil;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Dash.Models
{
    public class WidgetList : BaseModel
    {
        private IActionContextAccessor _ActionContextAccessor;
        private IEnumerable<WidgetView> _Widgets;

        public WidgetList(IDbContext dbContext, IActionContextAccessor actionContextAccessor, int userId)
        {
            DbContext = dbContext;
            _ActionContextAccessor = actionContextAccessor;
            RequestUserId = userId;
        }

        public string ToJson { get { return JSON.SerializeDynamic(Widgets, JilOutputFormatter.Options); } }

        public IEnumerable<WidgetView> Widgets
        {
            get
            {
                if (_Widgets == null && RequestUserId.HasPositiveValue())
                {
                    _Widgets = DbContext.Query<WidgetView>("WidgetGet", new { UserId = RequestUserId })
                        .Each(x => {
                            x.ActionContextAccessor = _ActionContextAccessor;
                            x.DbContext = DbContext;
                        })
                        .OrderBy(x => x.X < 0 ? int.MaxValue : x.X).ThenBy(x => x.Y < 0 ? int.MaxValue : x.Y);
                }
                return _Widgets;
            }
        }
    }
}
