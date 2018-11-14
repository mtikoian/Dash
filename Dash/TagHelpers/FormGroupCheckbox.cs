using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class FormGroupCheckboxTagHelper : FormBaseTagHelper
    {
        public FormGroupCheckboxTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();
            IsRequired = false;

            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = "div";
            output.AddClass("form-group", HtmlEncoder.Default);

            var div = new TagBuilder("div");
            div.AddCssClass("col-8");

            var inputGroup = new TagBuilder("div");
            inputGroup.AddCssClass("input-group");
            inputGroup.InnerHtml.AppendHtml(BuildCheckbox());
            inputGroup.InnerHtml.AppendHtml(BuildHelp());
            div.InnerHtml.AppendHtml(inputGroup);

            output.Content.AppendHtml(BuildLabel());
            output.Content.AppendHtml(div);

            base.Process(context, output);
        }

        private IHtmlContent BuildCheckbox()
        {
            var label = new TagBuilder("label");
            label.AddCssClass("form-checkbox");
            label.Attributes.Add("for", FieldName);

            var input = new TagBuilder("input");
            input.AddCssClass("form-input");
            input.Attributes.Add("id", FieldName);
            input.Attributes.Add("name", FieldName);
            input.Attributes.Add("type", "checkbox");
            input.Attributes.Add("value", "true");
            input.Attributes.AddIf("checked", "true", For?.ModelExplorer.Model?.ToString().ToBool() == true);

            var icon = new TagBuilder("i");
            icon.AddCssClass("form-icon");

            label.InnerHtml.AppendHtml(input);
            label.InnerHtml.AppendHtml(icon);
            label.InnerHtml.Append(FieldTitle);

            return label;
        }
    }
}
