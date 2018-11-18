using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.TagHelpers;
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
            Contextualize();
            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = "li";
            output.AddClass("breadcrumb-item", HtmlEncoder.Default);
            if (IsActive)
            {
                output.Attributes.Add("data-pjax-title", Label);
                _HtmlHelper.ViewBag.Title = Label;
                var urlHelper = new UrlHelper(_HtmlHelper.ViewContext);
                output.Attributes.Add("data-pjax-url", urlHelper.Action(Action, Controller, RouteValues));
                output.Attributes.Add("data-pjax-method", "GET");
                output.Content.Append(Label);
            }
            else
            {
                output.Content.AppendHtml(_HtmlHelper.ActionLink(Label, Action, Controller, RouteValues));
            }
            base.Process(context, output);
        }
    }
}
