using Dash.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dash.TagHelpers
{
    public class DoDeleteButtonTagHelper : DoLinkTagHelper
    {
        public DoDeleteButtonTagHelper(IHtmlHelper htmlHelper, IHttpContextAccessor httpContextAccessor) : base(htmlHelper, httpContextAccessor)
        {
            Title = Core.Delete;
            Method = "DELETE";
            Class = "btn btn-error";
            InnerContent = new TagBuilder("i");
            InnerContent.AddCssClass("dash");
            InnerContent.AddCssClass("dash-trash");
            Action = "Delete";
        }
    }
}
