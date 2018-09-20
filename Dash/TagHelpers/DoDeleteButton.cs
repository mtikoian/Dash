using Dash.Resources;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.TagHelpers
{
    public class DoDeleteButtonTagHelper : DoLinkTagHelper
    {
        public DoDeleteButtonTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
            Title = Core.Delete;
            Method = "DELETE";
            Class = "btn btn-error";
            InnerContent = new TagBuilder("i");
            InnerContent.AddCssClass("dash");
            InnerContent.AddCssClass("dash-trash");
        }
    }
}
