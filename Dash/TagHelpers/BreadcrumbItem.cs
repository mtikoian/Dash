using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class BreadcrumbItemTagHelper : BaseTagHelper
    {
        public BreadcrumbItemTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
        }

        public string Action { get; set; }
        public string Controller { get; set; }
        public bool IsActive { get; set; } = false;
        public string Label { get; set; }
        public object RouteValues { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            (HtmlHelper as IViewContextAware).Contextualize(ViewContext);
            output.TagName = "li";
            output.AddClass("breadcrumb-item", HtmlEncoder.Default);
            output.Content.AppendHtml(Html.AuthorizedActionLink(HtmlHelper, Label, Action, Controller, RouteValues, returnEmpty: false, hasAccess: !IsActive));
            base.Process(context, output);
        }
    }
}
