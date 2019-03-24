using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class FormHelpTagHelper : FormBaseTagHelper
    {
        public FormHelpTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper) { }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();

            output.TagName = null;
            output.TagMode = TagMode.StartTagAndEndTag;
            output.PostContent.AppendHtml(BuildHelp());

            base.Process(context, output);
        }
    }
}
