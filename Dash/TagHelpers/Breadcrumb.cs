using Dash.Resources;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class BreadcrumbTagHelper : BaseTagHelper
    {
        public BreadcrumbTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper) { }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();

            var ul = new TagBuilder("ul");
            ul.AddCssClass("breadcrumb");
            var li = new TagBuilder("li");
            li.AddCssClass("breadcrumb-item");
            li.InnerHtml.AppendHtml(HtmlHelper.ActionLink(Core.Dashboard, "Index", "Dashboard"));
            ul.InnerHtml.AppendHtml(li);
            ul.InnerHtml.AppendHtml(output.GetChildContentAsync().Result);

            var divider = new TagBuilder("div");
            divider.AddCssClass("divider");

            output.TagName = "div";
            output.Content.AppendHtml(ul);
            output.Content.AppendHtml(divider);
            base.Process(context, output);
        }
    }
}
