using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class AuthorizedMenuItemTagHelper : BaseTagHelper
    {
        private IHttpContextAccessor _HttpContextAccessor;

        public AuthorizedMenuItemTagHelper(IHtmlHelper htmlHelper, IHttpContextAccessor httpContextAccessor) : base(htmlHelper)
        {
            _HttpContextAccessor = httpContextAccessor;
        }

        public string Action { get; set; }
        public string Controller { get; set; }
        public bool ForceReload { get; set; } = false;
        public bool? HasAccess { get; set; }
        public DashIcons Icon { get; set; }
        public bool IsPjax { get; set; } = true;
        public string Title { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();
            var hasAccess = HasAccess ?? _HttpContextAccessor.HttpContext.User.HasAccess(Controller, Action, HttpVerbs.Get);

            output.TagMode = TagMode.StartTagAndEndTag;
            if (!hasAccess)
            {
                NoRender = true;
                base.Process(context, output);
                return;
            }

            output.TagName = "li";

            var a = new TagBuilder("a");
            var urlHelper = new UrlHelper(_HtmlHelper.ViewContext);
            a.Attributes.Add("href", urlHelper.Action(Action, Controller));
            a.Attributes.Add("data-method", "GET");
            a.Attributes.Add("title", Title);
            a.Attributes.AddIf("data-reload", "true", ForceReload);
            if (!IsPjax)
            {
                a.AddCssClass("pjax-no-follow");
            }

            var i = new TagBuilder("i");
            i.AddCssClass("dash");
            i.AddCssClass("dash-lg");
            i.AddCssClass($"dash-{Icon.ToCssClass()}");
            a.InnerHtml.AppendHtml(i);

            var span = new TagBuilder("span");
            span.InnerHtml.Append(Title);
            a.InnerHtml.AppendHtml(span);

            output.Content.AppendHtml(a);
            output.Content.AppendHtml(output.GetChildContentAsync().Result);

            base.Process(context, output);
        }
    }
}
