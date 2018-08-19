using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class TabPanelTagHelper : BaseTagHelper
    {
        public string Id { get; set; }
        public string LabelId { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.AddClass("tab-pane", HtmlEncoder.Default);
            output.Attributes.Add("role", "tabpanel");
            output.Attributes.Add("id", Id);
            output.Attributes.Add("aria-labelledby", LabelId);
            base.Process(context, output);
        }
    }
}
