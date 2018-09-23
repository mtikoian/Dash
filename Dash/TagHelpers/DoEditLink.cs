using Dash.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.TagHelpers
{
    public class DoEditLinkTagHelper : DoLinkTagHelper
    {
        public DoEditLinkTagHelper(IHtmlHelper htmlHelper, IHttpContextAccessor httpContextAccessor) : base(htmlHelper, httpContextAccessor)
        {
            Title = Core.Edit;
            Action = "Edit";
            RenderWithoutAccess = true;
        }
    }
}
