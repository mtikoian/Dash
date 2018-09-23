using Dash.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.TagHelpers
{
    public class DoEditButtonTagHelper : DoLinkTagHelper
    {
        public DoEditButtonTagHelper(IHtmlHelper htmlHelper, IHttpContextAccessor httpContextAccessor) : base(htmlHelper, httpContextAccessor)
        {
            Title = Core.Edit;
            Class = "btn btn-warning";
            InnerContent = new TagBuilder("i");
            InnerContent.AddCssClass("dash");
            InnerContent.AddCssClass("dash-edit");
            Action = "Edit";
        }
    }
}
