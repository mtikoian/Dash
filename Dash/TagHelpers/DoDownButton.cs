using Dash.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.TagHelpers
{
    public class DoDownButtonTagHelper : DoLinkTagHelper
    {
        public DoDownButtonTagHelper(IHtmlHelper htmlHelper, IHttpContextAccessor httpContextAccessor) : base(htmlHelper, httpContextAccessor)
        {
            Title = Core.MoveDown;
            Class = "btn btn-info";
            InnerContent = new TagBuilder("i");
            InnerContent.AddCssClass("dash");
            InnerContent.AddCssClass("dash-down");
            Action = "MoveDown";
        }
    }
}
