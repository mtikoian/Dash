using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class DoLinkTagHelper : BaseTagHelper
    {
        private IHttpContextAccessor _HttpContextAccessor;

        public DoLinkTagHelper(IHtmlHelper htmlHelper, IHttpContextAccessor httpContextAccessor) : base(htmlHelper)
        {
            _HttpContextAccessor = httpContextAccessor;
        }

        public string Action { get; set; }
        public string Class { get; set; } = "btn btn-link";
        public string Confirm { get; set; }
        public string Controller { get; set; }
        public bool? HasAccess { get; set; }
        public string IdProperty { get; set; } = "id";
        public string Method { get; set; } = "GET";
        public string ParentIdProperty { get; set; }
        public string Prompt { get; set; }
        public bool RenderWithoutAccess { get; set; } = false;
        public string TextProperty { get; set; }
        public string Title { get; set; }
        internal TagBuilder InnerContent { get; set; }

        public HttpVerbs GetMethod()
        {
            if (Method.ToUpper() == "POST")
            {
                return HttpVerbs.Post;
            }
            if (Method.ToUpper() == "PUT")
            {
                return HttpVerbs.Put;
            }
            if (Method.ToUpper() == "DELETE")
            {
                return HttpVerbs.Delete;
            }
            return HttpVerbs.Get;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();
            var hasAccess = HasAccess ?? _HttpContextAccessor.HttpContext.User.HasAccess(Controller, Action, GetMethod());

            output.TagMode = TagMode.StartTagAndEndTag;
            if (!hasAccess)
            {
                if (RenderWithoutAccess)
                {
                    output.TagName = "span";
                    if (InnerContent != null)
                    {
                        output.Content.AppendHtml(InnerContent);
                    }
                    else if (!TextProperty.IsEmpty())
                    {
                        output.Content.AppendHtml($"{{{{=x.{TextProperty} || ''}}}}");
                    }
                }
                else
                {
                    NoRender = true;
                }
                base.Process(context, output);
                return;
            }

            output.TagName = "a";
            var urlHelper = new UrlHelper(_HtmlHelper.ViewContext);
            // ParentIdProperty
            var url = $"{urlHelper.Action(Action, Controller)}/";
            if (!ParentIdProperty.IsEmpty())
            {
                url += $"{{{{=x.{ParentIdProperty}}}}}/";
            }
            if (!IdProperty.IsEmpty())
            {
                url += $"{{{{=x.{IdProperty}}}}}";
            }
            output.Attributes.Add("href", url);
            output.Attributes.Add("title", Title);
            output.Attributes.Add("data-method", Method);
            output.Attributes.Add("class", Class);
            output.Attributes.AddIf("data-confirm", Confirm, !Confirm.IsEmpty());
            output.Attributes.AddIf("data-prompt", Prompt, !Prompt.IsEmpty());

            if (InnerContent != null)
            {
                output.Content.AppendHtml(InnerContent);
            }
            else if (!TextProperty.IsEmpty())
            {
                output.Content.AppendHtml($"{{{{=x.{TextProperty} || ''}}}}");
            }

            base.Process(context, output);
        }
    }
}
