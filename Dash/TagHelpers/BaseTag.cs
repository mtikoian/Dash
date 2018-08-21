using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class BaseTagHelper : TagHelper
    {
        public IHtmlHelper HtmlHelper;

        public BaseTagHelper()
        {
        }

        public BaseTagHelper(IHtmlHelper htmlHelper)
        {
            HtmlHelper = htmlHelper;
        }

        public bool NoRender { get; set; }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (NoRender)
            {
                output.SuppressOutput();
            }
            base.Process(context, output);
        }
    }
}
