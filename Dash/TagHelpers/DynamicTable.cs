using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class DynamicTableTagHelper : BaseTagHelper
    {
        public Table Options { get; set; }
        public string Type { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var table = new TagBuilder("div");
            table.Attributes.Add("data-toggle", Type == "doT" ? "dotable" : "table");
            table.Attributes.Add("data-json", Options.ToJson);

            output.TagName = "div";
            output.AddClass("col-12", HtmlEncoder.Default);
            output.Content.AppendHtml(table);
            base.Process(context, output);
        }
    }
}
