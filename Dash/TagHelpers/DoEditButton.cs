using Dash.Resources;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.TagHelpers
{
    public class DoEditButtonTagHelper : DoLinkTagHelper
    {
        public DoEditButtonTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
            Title = Core.Edit;
            Class = "btn btn-warning";
            InnerContent = new TagBuilder("i");
            InnerContent.AddCssClass("dash");
            InnerContent.AddCssClass("dash-edit");
        }
    }
}
