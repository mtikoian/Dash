using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class ToolbarTagHelper : BaseTagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var div = new TagBuilder("div");
            div.AddCssClass("text-right");
            div.InnerHtml.AppendHtml(output.GetChildContentAsync().Result);

            output.TagName = "div";
            output.AddClass("col-12", HtmlEncoder.Default);
            output.AddClass("pb-2", HtmlEncoder.Default);
            output.Content.AppendHtml(div);
            base.Process(context, output);
        }
    }
}
