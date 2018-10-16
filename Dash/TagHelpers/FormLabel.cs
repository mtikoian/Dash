using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class FormLabelTagHelper : BaseTagHelper
    {
        public FormLabelTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
        }

        public ModelExpression For { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();

            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = "label";
            output.AddClass("form-label", HtmlEncoder.Default);
            output.AddClass("col-4", HtmlEncoder.Default);
            output.Attributes.Add("for", For.Name);
            if (For.Metadata.IsRequired)
            {
                output.AddClass("required", HtmlEncoder.Default);
            }
            output.Content.Append(For.Metadata.DisplayName);

            base.Process(context, output);
        }
    }
}
