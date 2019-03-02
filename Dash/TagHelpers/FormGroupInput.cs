using System;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class FormGroupInputTagHelper : FormBaseTagHelper
    {
        private static readonly Type[] NumberTypes = { typeof(int), typeof(long), typeof(decimal), typeof(double), typeof(int?), typeof(long?), typeof(decimal?), typeof(double?) };

        private IHtmlContent BuildInput()
        {
            var input = new TagBuilder("input");
            input.AddCssClass("form-input");
            input.Attributes.Add("id", FieldName);
            input.Attributes.Add("name", FieldName);

            var name = FieldName.ToLower();
            var type = "text";
            if (name.EndsWith("password"))
            {
                type = "password";
            }
            else if (name.EndsWith("email"))
            {
                type = "email";
            }
            else if (name.EndsWith("date"))
            {
                Toggle = "datepicker";
            }
            else if (For != null && NumberTypes.Contains(For.ModelExplorer.ModelType))
            {
                type = "number";
            }
            input.Attributes.Add("type", type);
            input.Attributes.Add("value", type == "password" ? "" : For?.ModelExplorer.Model?.ToString());

            input.Attributes.AddIf("required", "true", (IsRequired.HasValue && IsRequired.Value) || (!IsRequired.HasValue && For?.Metadata.IsRequired == true));
            input.Attributes.AddIf("autofocus", "true", Autofocus);

            if (For != null)
            {
                var maxLength = GetMaxLength(For.ModelExplorer.Metadata.ValidatorMetadata);
                input.Attributes.AddIf("maxlength", maxLength.ToString(), maxLength > 0);
                var minLength = GetMinLength(For.ModelExplorer.Metadata.ValidatorMetadata);
                input.Attributes.AddIf("minLength", minLength.ToString(), minLength > 0);
            }
            input.Attributes.AddIf("data-toggle", Toggle, !Toggle.IsEmpty());
            input.Attributes.AddIf("data-url", Url, !Url.IsEmpty());
            input.Attributes.AddIf("data-params", Params, !Params.IsEmpty());
            input.Attributes.AddIf("data-preload", "true", Preload);
            input.Attributes.AddIf("data-input", "", Toggle == "datepicker");
            input.Attributes.AddIf("data-target", Target, !Target.IsEmpty());
            input.Attributes.AddIf("data-match", Match, Match != null);

            return input;
        }

        public FormGroupInputTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
        }

        public bool Autofocus { get; set; }
        public string Match { get; set; }
        public string Params { get; set; }
        public bool Preload { get; set; }
        public string Target { get; set; }
        public string Toggle { get; set; }
        public string Url { get; set; }

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
            if (Toggle == "datepicker")
            {
                var icon = new TagBuilder("i");
                icon.AddCssClass("dash");
                icon.AddCssClass("dash-calendar text-primary");

                var button = new TagBuilder("button");
                button.AddCssClass("btn btn-secondary input-group-btn");
                button.MergeAttribute("type", "button");
                button.MergeAttribute("role", "button");
                button.MergeAttribute("data-toggle", "");
                button.InnerHtml.AppendHtml(icon);

                var clearIcon = new TagBuilder("i");
                clearIcon.AddCssClass("dash");
                clearIcon.AddCssClass("dash-cancel text-primary");

                var clearButton = new TagBuilder("button");
                clearButton.AddCssClass("btn btn-secondary input-group-btn");
                clearButton.MergeAttribute("type", "button");
                clearButton.MergeAttribute("role", "button");
                clearButton.MergeAttribute("data-clear", "");
                clearButton.InnerHtml.AppendHtml(clearIcon);

                inputGroup.AddCssClass("flatpickr");
                inputGroup.InnerHtml.AppendHtml(button);
                inputGroup.InnerHtml.AppendHtml(clearButton);
            }
            inputGroup.InnerHtml.AppendHtml(BuildHelp());
            inputGroup.InnerHtml.AppendHtml(output.GetChildContentAsync().Result);
            div.InnerHtml.AppendHtml(inputGroup);

            output.Content.AppendHtml(BuildLabel());
            output.Content.AppendHtml(div);

            base.Process(context, output);
        }
    }
}
