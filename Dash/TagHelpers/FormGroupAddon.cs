using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class FormGroupAddonTagHelper : BaseTagHelper
    {
        public FormGroupAddonTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
        }

        public string Id { get; set; }
        public bool IsChecked { get; set; }
        public string Label { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();

            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = "div";
            output.AddClass("input-group-addon", HtmlEncoder.Default);
            output.AddClass("input-group-custom", HtmlEncoder.Default);
            base.Process(context, output);
        }
    }
}
