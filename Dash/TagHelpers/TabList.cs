using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class TabListTagHelper : BaseTagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "ul";
            output.AddClass("tab", HtmlEncoder.Default);
            output.AddClass("tab-block", HtmlEncoder.Default);
            output.Attributes.Add("role", "tablist");
            base.Process(context, output);
        }
    }
}
