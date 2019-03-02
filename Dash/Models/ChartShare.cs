using System.ComponentModel.DataAnnotations;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Dash.Models
{
    public class ChartShare : Share
    {
        private Chart _Chart;

        public ChartShare()
        {
        }

        public ChartShare(IDbContext dbContext) => DbContext = dbContext;

        public ChartShare(IDbContext dbContext, int chartId)
        {
            DbContext = dbContext;
            ChartId = chartId;
        }

        [BindNever, ValidateNever]
        public Chart Chart => _Chart ?? (_Chart = DbContext.Get<Chart>(ChartId));

        [Required(ErrorMessageResourceType = typeof(Core), ErrorMessageResourceName = "ErrorRequired")]
        public int ChartId { get; set; }

        [DbIgnore, BindNever, ValidateNever]
        public string ChartName => Chart?.Name;
    }
}
