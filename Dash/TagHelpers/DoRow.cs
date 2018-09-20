using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class DoRowTagHelper : BaseTagHelper
    {
        public string Id { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "script";
            output.Attributes.Add("id", Id);
            output.Attributes.Add("type", "text/html");
            var tr = new TagBuilder("tr");
            tr.InnerHtml.AppendHtml(output.GetChildContentAsync().Result);
            output.Content.AppendHtml(tr);
            base.Process(context, output);
        }
    }
}
