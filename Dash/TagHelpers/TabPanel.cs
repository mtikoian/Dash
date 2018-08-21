using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class TabPanelTagHelper : BaseTagHelper
    {
        public string Id { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.AddClass("tab-pane", HtmlEncoder.Default);
            output.Attributes.Add("role", "tabpanel");
            if (!Id.IsEmpty())
            {
                output.Attributes.Add("id", $"{Id}Content");
                output.Attributes.Add("aria-labelledby", $"{Id}Tab");
            }
            base.Process(context, output);
        }
    }
}
