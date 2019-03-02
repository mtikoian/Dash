using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class BaseTagHelper : TagHelper
    {
        public IHtmlHelper _HtmlHelper;

        public BaseTagHelper()
        {
        }

        public BaseTagHelper(IHtmlHelper htmlHelper) => _HtmlHelper = htmlHelper;

        public bool NoRender { get; set; }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public void Contextualize() => (_HtmlHelper as IViewContextAware).Contextualize(ViewContext);

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
