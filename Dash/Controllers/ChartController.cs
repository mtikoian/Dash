using System.Collections.Generic;
using System.Linq;
using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission")]
    public class ChartController : BaseController
    {
        public ChartController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult ChangeType(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!chart.IsOwner)
            {
                return JsonError(Charts.ErrorOwnerOnly);
            }
            return PartialView(chart);
        }

        [HttpPost, AjaxRequestOnly, ValidateAntiForgeryToken]
        public IActionResult ChangeType([FromBody] ChangeType model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Update();
            return JsonSuccess();
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Copy(CopyChart model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Save();
            return JsonSuccess();
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return PartialView(new CreateChart(DbContext, User.UserId()));
        }

        [HttpPost, AjaxRequestOnly, ValidateAntiForgeryToken]
        public IActionResult Create([FromBody] CreateChart model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }

            var newChart = Chart.Create(model, User.UserId());
            DbContext.Save(newChart, false);
            return JsonData(new { message = Charts.SuccessSavingChart, dialogUrl = Url.Action("Details", new { @id = newChart.Id }) });
        }

        [HttpPost, AjaxRequestOnly]
        public IActionResult Data(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanViewChart(chart))
            {
                return JsonError(Charts.ErrorPermissionDenied);
            }
            if ((chart.ChartRange?.Count ?? 0) == 0)
            {
                return JsonError(Charts.ErrorNoRanges);
            }
            return JsonData(chart.GetData(User.IsInRole("dataset.create")));
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!chart.IsOwner)
            {
                return JsonError(Charts.ErrorOwnerOnly);
            }
            DbContext.Delete(chart);
            return JsonSuccess(Charts.SuccessDeletingChart);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Details(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanViewChart(chart))
            {
                return JsonError(Charts.ErrorPermissionDenied);
            }
            return PartialView("Details", chart);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult DetailsOptions(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanViewChart(chart))
            {
                return JsonError(Charts.ErrorPermissionDenied);
            }

            var columns = new Dictionary<int, List<RangeColumn>>();
            DbContext.GetAll<RangeColumn>(new { UserId = user.Id }).ToList().ForEach(x => {
                if (!columns.ContainsKey(x.ReportId))
                {
                    columns.Add(x.ReportId, new List<RangeColumn>());
                }
                columns[x.ReportId].Add(x);
            });

            return JsonData(new {
                chartId = chart.Id,
                dateIntervals = chart.DateIntervalList.Prepend(new { Id = 0, Name = Charts.DateInterval }),
                aggregators = chart.AggregatorList.Prepend(new { Id = 0, Name = Charts.Aggregator }),
                ranges = chart.ChartRange,
                reports = DbContext.GetAll<Report>(new { UserId = user.Id })
                    .Select(x => new { id = x.Id, name = x.Name }).Prepend(new { id = 0, name = Charts.Report }),
                columns = columns.Select(x => new { reportId = x.Key, columns = x.Value }),
                filterTypes = new {
                    boolean = (int)FilterTypes.Boolean,
                    date = (int)FilterTypes.Date,
                    select = (int)FilterTypes.Select,
                    numeric = (int)FilterTypes.Numeric
                },
                allowEdit = chart.IsOwner,
                wantsHelp = HttpContext.Session.GetString("ContextHelp").ToBool(),
                saveRangesUrl = Url.Action("SaveRanges", "Chart", new { chart.Id })
            });
        }

        [HttpPost]
        public IActionResult Export(ExportChart model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            return File(model.Stream(), model.ContentType, model.FormattedFileName);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Index()
        {
            return PartialView(new Table("tableCharts", Url.Action("List"), new List<TableColumn>() {
                new TableColumn("name", Charts.Name, Table.EditLink($"{Url.Action("Details")}/{{id}}", "Chart", "Details", User.IsInRole("chart.details"))),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink> {
                    Table.EditButton($"{Url.Action("Details")}/{{id}}", "Chart", "Details", User.IsInRole("chart.details")),
                    Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", "Chart", Charts.ConfirmDelete, User.IsInRole("chart.delete")),
                    Table.CopyButton($"{Url.Action("Copy")}/{{id}}", "Chart", Charts.NewName, User.IsInRole("chart.copy"))
                })
            }));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return JsonRows(DbContext.GetAll<Chart>(new { UserId = User.UserId() }));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Rename(int id, string prompt)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!chart.IsOwner)
            {
                return JsonError(Charts.ErrorOwnerOnly);
            }
            if (prompt.IsEmpty())
            {
                return JsonError(Charts.ErrorNameRequired);
            }

            chart.Name = prompt.Trim();
            DbContext.Save(chart, false);
            return JsonData(new { message = Charts.NameSaved, content = prompt });
        }

        [HttpPost, AjaxRequestOnly]
        public IActionResult SaveRanges([FromBody] SaveRange model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            return JsonData(new { ranges = model.Update() });
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Share(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!chart.IsOwner)
            {
                return JsonError(Charts.ErrorOwnerOnly);
            }
            return PartialView(chart);
        }

        [HttpPut, AjaxRequestOnly]
        public IActionResult Share([FromBody] SaveChartShare model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Update();
            return JsonSuccess(Charts.SuccessSavingChart);
        }
    }
}
