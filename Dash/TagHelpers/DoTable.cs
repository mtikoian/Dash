using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class DoTableTagHelper : BaseTagHelper
    {
        public string Id { get; set; }
        public string Template { get; set; }
        public string Url { get; set; }
        public bool Searchable { get; set; } = true;
        public bool LoadAll { get; set; } = true;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var table = new TagBuilder("div");
            table.Attributes.Add("id", Id);
            table.Attributes.Add("data-toggle", "dotable");
            table.Attributes.Add("data-template", Template);
            table.Attributes.Add("data-url", Url);
            table.Attributes.Add("data-searchable", Searchable.ToString());
            table.Attributes.Add("data-load-all", LoadAll.ToString());

            output.TagName = "div";
            output.AddClass("col-12", HtmlEncoder.Default);
            output.Content.AppendHtml(table);
            base.Process(context, output);
        }
    }
}
