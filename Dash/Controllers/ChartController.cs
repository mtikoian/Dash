using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Dash.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Dash.Controllers
{
    /// <summary>
    /// Handles CRUD for charts.
    /// </summary>
    [Authorize(Policy = "HasPermission")]
    public class ChartController : BaseController
    {
        public ChartController(IHttpContextAccessor httpContextAccessor, IDbContext dbContext, IMemoryCache cache, IAppConfiguration appConfig) : base(httpContextAccessor, dbContext, cache, appConfig)
        {
        }

        /// <summary>
        /// Show form to change chart type.
        /// </summary>
        /// <param name="id">ID of chart to display.</param>
        /// <returns>Form or error message.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult ChangeType(int id)
        {
            var model = DbContext.Get<Chart>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Charts.ErrorOwnerOnly);
            }
            return PartialView(model);
        }

        /// <summary>
        /// Change chart type.
        /// </summary>
        /// <param name="id">ID of chart to display.</param>
        /// <param name="chartTypeId">New chart type.</param>
        /// <returns>Success or error message.</returns>
        [HttpPost, ParentAction("ChangeType"), AjaxRequestOnly]
        public IActionResult ChangeTypeSave(int id, int chartTypeId)
        {
            var model = DbContext.Get<Chart>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Charts.ErrorOwnerOnly);
            }
            if (chartTypeId < 1)
            {
                return JsonError(Charts.ErrorTypeRequired);
            }

            model.ChartTypeId = chartTypeId;
            DbContext.Save(model, false);
            return Json(new {
                message = Charts.SuccessSavingChart,
                closeParent = true,
                parentTarget = true,
                dialogUrl = Url.Action("Details", new { @id = model.Id })
            });
        }

        /// <summary>
        /// Make a copy of a chart.
        /// </summary>
        /// <param name="model">CopyChart object</param>
        /// <returns>Redirects to new chart.</returns>
        [HttpGet]
        public IActionResult Copy(CopyChart model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Save();
            return Details(model.Id);
        }

        /// <summary>
        /// Create a new chart.
        /// </summary>
        /// <param name="prompt">Name value from prompt</param>
        /// <returns>Redirects to index.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return PartialView(new CreateChart());
        }

        /// <summary>
        /// Create a new chart.
        /// </summary>
        /// <param name="model">CreateChart object</param>
        /// <returns>Redirects to index.</returns>
        [HttpPost, ParentAction("Create"), AjaxRequestOnly]
        public IActionResult CreateChart(CreateChart model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }

            var newChart = Chart.Create(model, User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt());
            DbContext.Save(newChart);
            return Json(new { message = Charts.SuccessSavingChart, dialogUrl = Url.Action("Details", new { @id = newChart.Id }) });
        }

        /// <summary>
        /// Creates chart data for Charts.js to consume.
        /// </summary>
        /// <param name="id">Chart Id</param>
        /// <returns>Object with the queries run, execution time, retrieved data, and any errors.</returns>
        [HttpPost, AjaxRequestOnly]
        public IActionResult Data(int id)
        {
            var model = DbContext.Get<Chart>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt());
            if (!user.CanViewChart(model))
            {
                return JsonError(Charts.ErrorPermissionDenied);
            }
            if ((model.ChartRange?.Count ?? 0) == 0)
            {
                return JsonError(Charts.ErrorNoRanges);
            }

            var result = model.GetData(User.IsInRole("dataset.create"));
            return Json(result);
        }

        /// <summary>
        /// Delete a chart.
        /// </summary>
        /// <param name="id">ID of chart to delete</param>
        /// <param name="model">Chart object</param>
        /// <returns>Success or error message.</returns>
        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<Chart>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Charts.ErrorOwnerOnly);
            }

            DbContext.Delete(model);
            return JsonSuccess(Charts.SuccessDeletingChart);
        }

        /// <summary>
        /// Display a chart to view/edit.
        /// </summary>
        /// <param name="id">ID of chart to display.</param>
        /// <returns>Details view, or an error message.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Details(int id)
        {
            var model = DbContext.Get<Chart>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt());
            if (!user.CanViewChart(model))
            {
                return JsonError(Charts.ErrorPermissionDenied);
            }

            return PartialView("Details", model);
        }

        /// <summary>
        /// Return an object with translations and other data needed to view a chart.
        /// </summary>
        /// <param name="id">Chart ID</param>
        /// <param name="model">Chart object</param>
        /// <returns>Options object for viewing the chart, or an error message.</returns>
        [HttpGet, ParentAction("Details"), AjaxRequestOnly]
        public IActionResult DetailsOptions(int id)
        {
            var model = DbContext.Get<Chart>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt());
            if (!user.CanViewChart(model))
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

            return Json(new {
                dateIntervals = model.DateIntervalList.Prepend(new { Id = 0, Name = Charts.DateInterval }),
                aggregators = model.AggregatorList.Prepend(new { Id = 0, Name = Charts.Aggregator }),
                ranges = model.ChartRange,
                reports = DbContext.GetAll<Report>(new { UserId = user.Id })
                    .Select(x => new { id = x.Id, name = x.Name }).Prepend(new { id = 0, name = Charts.Report }),
                columns = columns.Select(x => new { reportId = x.Key, columns = x.Value }),
                filterTypes = new {
                    boolean = (int)FilterTypes.Boolean,
                    date = (int)FilterTypes.Date,
                    select = (int)FilterTypes.Select,
                    numeric = (int)FilterTypes.Numeric
                },
                allowEdit = model.IsOwner,
                wantsHelp = HttpContextAccessor.HttpContext.Session.GetString("ContextHelp").ToBool(),
                saveRangesUrl = Url.Action("SaveRanges", "Chart", new { model.Id })
            });
        }

        /// <summary>
        /// Export a chart to an image file.
        /// </summary>
        /// <param name="model">Export chart settings model</param>
        /// <param name="data">Base64 data for the image.</param>
        /// <param name="width">Width of the image.</param>
        /// <returns>Streams a png image.</returns>
        [HttpPost]
        public IActionResult Export(ExportChart model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }

            model.Stream();
            return null;
        }

        /// <summary>
        /// List all charts.
        /// </summary>
        /// <returns>Index view.</returns>
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

        /// <summary>
        /// Return the chart list for table to display.
        /// </summary>
        /// <returns>Array of chart objects.</returns>
        [HttpGet, ParentAction("Index"), AjaxRequestOnly]
        public IActionResult List()
        {
            return JsonRows(DbContext.GetAll<Chart>(new { UserId = User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt() }));
        }

        /// <summary>
        /// Rename a chart.
        /// </summary>
        /// <param name="id">ID of chart to display.</param>
        /// <param name="prompt">New chart name.</param>
        /// <returns>Success or error message.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Rename(int id, string prompt)
        {
            var model = DbContext.Get<Chart>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Charts.ErrorOwnerOnly);
            }
            if (prompt.IsEmpty())
            {
                return JsonError(Charts.ErrorNameRequired);
            }

            model.Name = prompt.Trim();
            DbContext.Save(model, false);
            return Json(new { message = Charts.NameSaved, content = prompt });
        }

        /// <summary>
        /// Save the ranges for a chart.
        /// </summary>
        /// <param name="id">Chart Id</param>
        /// <param name="groups">New ranges objects to save</param>
        /// <returns>Error message, or list of updated range objects.</returns>
        [HttpPost, AjaxRequestOnly]
        public IActionResult SaveRanges(int id, List<ChartRange> ranges = null)
        {
            var model = DbContext.Get<Chart>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Charts.ErrorOwnerOnly);
            }

            return Json(new { ranges = model.UpdateRanges(ranges) });
        }

        /// <summary>
        /// Display a chart to share with users/roles.
        /// </summary>
        /// <param name="id">ID of chart to display.</param>
        /// <returns>Share view, or error message.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Share(int id)
        {
            var model = DbContext.Get<Chart>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Charts.ErrorOwnerOnly);
            }
            return PartialView(model);
        }

        /// <summary>
        /// Save the chart shares.
        /// </summary>
        /// <param name="id">ID of chart to update.</param>
        /// <param name="chartShare">List of shares.</param>
        /// <returns>Success or error message.</returns>
        [HttpPut, ActionName("Share"), AjaxRequestOnly]
        public IActionResult ShareSave(int id, List<ChartShare> chartShare)
        {
            var model = DbContext.Get<Chart>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Charts.ErrorOwnerOnly);
            }

            chartShare?.ForEach(x => { x.ChartId = model.Id; DbContext.Save(x); });
            model.ChartShare?.Where(x => chartShare?.Any(s => s.Id == x.Id) != true).Each(x => DbContext.Delete(x));
            return JsonSuccess(Charts.SuccessSavingChart);
        }
    }
}