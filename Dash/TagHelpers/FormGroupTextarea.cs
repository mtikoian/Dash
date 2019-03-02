using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class FormGroupTextareaTagHelper : FormBaseTagHelper
    {
        private IHtmlContent BuildInput()
        {
            var textarea = new TagBuilder("textarea");
            textarea.AddCssClass("form-input");
            textarea.Attributes.Add("id", FieldName);
            textarea.Attributes.Add("name", FieldName);
            textarea.Attributes.AddIf("required", "true", IsRequired == true || (!IsRequired.HasValue && For?.Metadata.IsRequired == true));
            if (For != null)
            {
                var maxLength = GetMaxLength(For.ModelExplorer.Metadata.ValidatorMetadata);
                textarea.Attributes.AddIf("maxlength", maxLength.ToString(), maxLength > 0);
                var minLength = GetMinLength(For.ModelExplorer.Metadata.ValidatorMetadata);
                textarea.Attributes.AddIf("minLength", minLength.ToString(), minLength > 0);
            }
            textarea.Attributes.AddIf("rows", Rows.ToString(), Rows > 0);
            textarea.InnerHtml.Append(For?.ModelExplorer.Model?.ToString());
            return textarea;
        }

        public FormGroupTextareaTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
        }

        public int Rows { get; set; } = 4;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();

            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = "div";
            output.AddClass("form-group", HtmlEncoder.Default);

            var div = new TagBuilder("div");
            div.AddCssClass("col-8");

            var inputGroup = new TagBuilder("div");
            inputGroup.AddCssClass("input-group");
            inputGroup.InnerHtml.AppendHtml(BuildInput());
            inputGroup.InnerHtml.AppendHtml(BuildHelp());
            div.InnerHtml.AppendHtml(inputGroup);

            output.Content.AppendHtml(BuildLabel());
            output.Content.AppendHtml(div);

            base.Process(context, output);
        }
    }
}
