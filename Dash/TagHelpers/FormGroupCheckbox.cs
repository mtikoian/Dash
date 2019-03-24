using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class FormGroupCheckboxTagHelper : FormBaseTagHelper
    {
        IHtmlContent BuildCheckbox()
        {
            var label = new TagBuilder("label");
            label.AddCssClass("form-checkbox");
            label.Attributes.Add("for", FieldName);
            label.Attributes.AddIf("disabled", "true", Disabled == true);

            var input = new TagBuilder("input");
            input.AddCssClass("form-input");
            input.Attributes.Add("id", FieldName);
            input.Attributes.Add("name", FieldName);
            input.Attributes.Add("type", "checkbox");
            input.Attributes.Add("value", "true");
            input.Attributes.AddIf("checked", "true", For?.ModelExplorer.Model?.ToString().ToBool() == true);
            input.Attributes.AddIf("disabled", "true", Disabled == true);

            var icon = new TagBuilder("i");
            icon.AddCssClass("form-icon");

            label.InnerHtml.AppendHtml(input);
            label.InnerHtml.AppendHtml(icon);
            label.InnerHtml.Append(FieldTitle);

            return label;
        }

        public FormGroupCheckboxTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper) { }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();
            UseFormGroup(output);
            IsRequired = false;

            var div = BuildFormGroup();
            var inputGroup = BuildInputGroup();
            inputGroup.InnerHtml.AppendHtml(BuildCheckbox());
            inputGroup.InnerHtml.AppendHtml(BuildHelp());
            div.InnerHtml.AppendHtml(inputGroup);

            output.Content.AppendHtml(BuildLabel());
            output.Content.AppendHtml(div);

            base.Process(context, output);
        }
    }
}
