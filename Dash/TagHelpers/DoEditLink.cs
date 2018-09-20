using Dash.Resources;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.TagHelpers
{
    public class DoEditLinkTagHelper : DoLinkTagHelper
    {
        public DoEditLinkTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
            Title = Core.Edit;
            RenderWithoutAccess = true;
        }
    }
}
