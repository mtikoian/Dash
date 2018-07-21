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
    [Authorize(Policy = "HasPermission"), Pjax]
    public class ChartController : BaseController
    {
        public ChartController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet]
        public IActionResult ChangeType(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            if (!chart.IsOwner)
            {
                ViewBag.Error = Charts.ErrorOwnerOnly;
                return Details(id);
            }
            return View("ChangeType", chart);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ChangeType(ChangeType model)
        {
            if (model == null)
            {
                ViewBag.Error = Core.ErrorGeneric;
                return Index();
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View("ChangeType", model);
            }
            model.Update();
            ViewBag.Message = Charts.SuccessSavingChart;
            return Details(model.Id);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Copy(CopyChart model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return Index();
            }
            model.Save();
            ViewBag.Message = Charts.SuccessCopyingChart;
            return Index();
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = Charts.CreateChart;
            return View(new CreateChart(DbContext, User.UserId()));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(CreateChart model)
        {
            if (model == null)
            {
                ViewBag.Error = Core.ErrorGeneric;
                return Index();
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View("Create", model);
            }

            var newChart = Chart.Create(model, User.UserId());
            DbContext.Save(newChart, false);
            ViewBag.Message = Charts.SuccessSavingChart;
            return Index();
        }

        [HttpPost, AjaxRequestOnly]
        public IActionResult Data(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                return Error(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanViewChart(chart))
            {
                return Error(Charts.ErrorPermissionDenied);
            }
            if ((chart.ChartRange?.Count ?? 0) == 0)
            {
                return Error(Charts.ErrorNoRanges);
            }
            return Data(chart.GetData(User.IsInRole("dataset.create")));
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            if (!chart.IsOwner)
            {
                ViewBag.Error = Charts.ErrorOwnerOnly;
                return Index();
            }
            DbContext.Delete(chart);
            ViewBag.Message = Charts.SuccessDeletingChart;
            return Index();
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanViewChart(chart))
            {
                ViewBag.Error = Charts.ErrorPermissionDenied;
                return Index();
            }
            ViewBag.Title = chart.Name;
            return View("Details", chart);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult DetailsOptions(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                return Error(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanViewChart(chart))
            {
                return Error(Charts.ErrorPermissionDenied);
            }

            var columns = new Dictionary<int, List<RangeColumn>>();
            DbContext.GetAll<RangeColumn>(new { UserId = user.Id }).ToList().ForEach(x => {
                if (!columns.ContainsKey(x.ReportId))
                {
                    columns.Add(x.ReportId, new List<RangeColumn>());
                }
                columns[x.ReportId].Add(x);
            });

            return Data(new {
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
                return Error(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            return File(model.Stream(), model.ContentType, model.FormattedFileName);
        }

        [HttpGet]
        public IActionResult Index()
        {
            RouteData.Values.Remove("id");
            ViewBag.Title = Charts.ViewAll;
            return View("Index", new Table("tableCharts", Url.Action("List"), new List<TableColumn> {
                new TableColumn("name", Charts.Name, Table.EditLink($"{Url.Action("Details")}/{{id}}", User.IsInRole("chart.details"))),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Details")}/{{id}}"), User.IsInRole("chart.details"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", Charts.ConfirmDelete), User.IsInRole("chart.delete"))
                        .AddIf(Table.CopyButton($"{Url.Action("Copy")}/{{id}}", Charts.NewName), User.IsInRole("chart.copy"))
                )}
            ));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return Rows(DbContext.GetAll<Chart>(new { UserId = User.UserId() }));
        }

        [HttpPut]
        public IActionResult Rename(int id, string prompt)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            if (!chart.IsOwner)
            {
                ViewBag.Error = Charts.ErrorOwnerOnly;
                return Details(id);
            }
            if (prompt.IsEmpty())
            {
                ViewBag.Error = Charts.ErrorNameRequired;
                return Details(id);
            }

            chart.Name = prompt.Trim();
            DbContext.Save(chart, false);
            ViewBag.Message = Charts.NameSaved;
            return Details(id);
        }

        [HttpPost, AjaxRequestOnly]
        public IActionResult SaveRanges([FromBody] SaveRange model)
        {
            if (model == null)
            {
                return Error(Core.ErrorInvalidId);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            return Data(new { ranges = model.Update() });
        }

        [HttpGet]
        public IActionResult Share(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            if (!chart.IsOwner)
            {
                ViewBag.Error = Charts.ErrorOwnerOnly;
                return Details(id);
            }
            ViewBag.Title = Charts.ShareChart;
            return View("Share", chart);
        }

        [HttpPut]
        public IActionResult Share(SaveChartShare model)
        {
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View("Share", model.Chart);
            }
            model.Update();
            ViewBag.Message = Charts.SuccessSavingChart;
            return Details(model.Id);
        }
    }
}
