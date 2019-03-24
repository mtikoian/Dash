using System;
using System.Linq;
using Dash.Resources;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class FormGroupInputTagHelper : FormBaseTagHelper
    {
        static readonly Type[] _NumberTypes = { typeof(int), typeof(long), typeof(decimal), typeof(double), typeof(int?), typeof(long?), typeof(decimal?), typeof(double?) };

        IHtmlContent BuildInput()
        {
            var input = new TagBuilder("input");
            input.AddCssClass("form-input");
            input.Attributes.Add("id", FieldName);
            input.Attributes.Add("name", FieldName);

            var name = FieldName.ToLower();
            var type = "text";
            if (name.EndsWith("password"))
                type = "password";
            else if (name.EndsWith("email"))
                type = "email";
            else if (name.EndsWith("date"))
                Toggle = DataToggles.Datepicker;
            else if (For != null && _NumberTypes.Contains(For.ModelExplorer.ModelType))
                type = "number";

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
            input.Attributes.AddIf("data-toggle", Toggle.ToHyphenCase(), Toggle.HasValue);
            input.Attributes.AddIf("data-url", Url, !Url.IsEmpty());
            input.Attributes.AddIf("data-params", Params, !Params.IsEmpty());
            input.Attributes.AddIf("data-preload", "true", Preload);
            input.Attributes.AddIf("data-input", "", Toggle == DataToggles.Datepicker);
            input.Attributes.AddIf("data-target", Target, !Target.IsEmpty());
            input.Attributes.AddIf("data-match", Match, Match != null);
            if (Toggle == DataToggles.Autocomplete)
                input.Attributes.Add("placeholder", Core.StartTyping);

            return input;
        }

        public FormGroupInputTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper) { }

        public bool Autofocus { get; set; }
        public string Match { get; set; }
        public string Params { get; set; }
        public bool Preload { get; set; }
        public string Target { get; set; }
        public DataToggles? Toggle { get; set; }
        public string Url { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();
            UseFormGroup(output);

            var div = BuildFormGroup();
            var inputGroup = BuildInputGroup();
            inputGroup.InnerHtml.AppendHtml(BuildInput());
            if (Toggle == DataToggles.Datepicker)
            {
                var icon = new TagBuilder("i");
                icon.AddCssClass("dash");
                icon.AddCssClass("dash-calendar text-primary");

                var button = new TagBuilder("button");
                button.AddCssClass("btn btn-secondary input-group-btn");
                button.MergeAttribute("type", "button");
                button.MergeAttribute("role", "button");
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
