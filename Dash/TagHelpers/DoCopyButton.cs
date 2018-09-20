using Dash.Resources;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.TagHelpers
{
    public class DoCopyButtonTagHelper : DoLinkTagHelper
    {
        public DoCopyButtonTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
            Title = Core.Copy;
            Class = "btn btn-info";
            InnerContent = new TagBuilder("i");
            InnerContent.AddCssClass("dash");
            InnerContent.AddCssClass("dash-clone");
        }
    }
}
