using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class DoLinkTagHelper : BaseTagHelper
    {
        public DoLinkTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
        }

        public string Action { get; set; }
        public string Class { get; set; } = "btn btn-link";
        public string Confirm { get; set; }
        public string Controller { get; set; }
        public bool HasAccess { get; set; } = true;
        public string IdProperty { get; set; } = "id";
        public string TextProperty { get; set; } = "name";
        public string Method { get; set; } = "GET";
        public string Prompt { get; set; }
        public bool RenderWithoutAccess { get; set; } = false;
        public string Title { get; set; }
        internal TagBuilder InnerContent { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Content.Clear();
            if (!HasAccess)
            {
                if (RenderWithoutAccess)
                {
                    output.TagName = "span";
                    if (InnerContent != null)
                    {
                        output.Content.AppendHtml(InnerContent);
                    }
                    else
                    {
                        output.Content.Append($"{{{{=x.{TextProperty}}}}}");
                    }
                    base.Process(context, output);
                }
                else
                {
                    output.SuppressOutput();
                }
                return;
            }

        (HtmlHelper as IViewContextAware).Contextualize(ViewContext);

            output.TagName = "a";
            var urlHelper = new UrlHelper(HtmlHelper.ViewContext);
            output.Attributes.Add("href", $"{urlHelper.Action(Action, Controller)}/{{{{=x.{IdProperty}}}}}");
            output.Attributes.Add("title", Title);
            output.Attributes.Add("data-method", Method);
            output.Attributes.Add("class", Class);
            if (!Confirm.IsEmpty())
            {
                output.Attributes.Add("data-confirm", Confirm);
            }
            if (!Prompt.IsEmpty())
            {
                output.Attributes.Add("data-prompt", Prompt);
            }

            if (InnerContent != null)
            {
                output.Content.AppendHtml(InnerContent);
            }
            else
            {
                output.Content.Append($"{{{{=x.{TextProperty}}}}}");
            }

            base.Process(context, output);
        }
    }
}
