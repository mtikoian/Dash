using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class TabActiveTagHelper : BaseTagHelper
    {
        public string Id { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "a";
            output.AddClass("active", HtmlEncoder.Default);
            output.Attributes.Add("role", "tab");
            output.Attributes.Add("data-toggle", DataToggles.Tab.ToHyphenCase());
            if (!Id.IsEmpty())
            {
                output.Attributes.Add("id", $"{Id}Tab");
                output.Attributes.Add("aria-aria-controls", $"{Id}Content");
            }
            base.Process(context, output);
        }
    }
}
