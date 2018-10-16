using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Encodings.Web;
using Dash.Utils;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class FormGroupTagHelper : BaseTagHelper
    {
        private static readonly Type[] NumberTypes = { typeof(int), typeof(long), typeof(decimal), typeof(double), typeof(int?), typeof(long?), typeof(decimal?), typeof(double?) };

        public FormGroupTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
        }

        public IHtmlContent AddOn { get; set; }
        public bool Autofocus { get; set; }
        public ModelExpression For { get; set; }
        public Html.InputFieldType InputFieldType { get; set; }
        public string Params { get; set; }
        public bool Preload { get; set; }
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
            if (AddOn != null)
            {
                inputGroup.InnerHtml.AppendHtml(AddOn);
            }
            inputGroup.InnerHtml.AppendHtml(BuildHelp());
            div.InnerHtml.AppendHtml(inputGroup);

            output.Content.AppendHtml(BuildLabel());
            output.Content.AppendHtml(div);

            base.Process(context, output);
        }

        private static int GetMaxLength(IReadOnlyList<object> validatorMetadata)
        {
            for (var i = 0; i < validatorMetadata.Count; i++)
            {
                if (validatorMetadata[i] is StringLengthAttribute stringLengthAttribute && stringLengthAttribute.MaximumLength > 0)
                {
                    return stringLengthAttribute.MaximumLength;
                }

                if (validatorMetadata[i] is MaxLengthAttribute maxLengthAttribute && maxLengthAttribute.Length > 0)
                {
                    return maxLengthAttribute.Length;
                }
            }
            return 0;
        }

        private static int GetMinLength(IReadOnlyList<object> validatorMetadata)
        {
            for (var i = 0; i < validatorMetadata.Count; i++)
            {
                if (validatorMetadata[i] is StringLengthAttribute stringLengthAttribute && stringLengthAttribute.MinimumLength > 0)
                {
                    return stringLengthAttribute.MinimumLength;
                }

                if (validatorMetadata[i] is MinLengthAttribute minLengthAttribute && minLengthAttribute.Length > 0)
                {
                    return minLengthAttribute.Length;
                }
            }
            return 0;
        }

        private IHtmlContent BuildHelp()
        {
            if (_HtmlHelper.ViewContext.HttpContext?.WantsHelp() != true)
            {
                return HtmlString.Empty;
            }

            var key = $"{For.Metadata.ContainerType.Name}_{For.Metadata.PropertyName}";
            var resourceLib = new ResourceDictionary("ContextHelp");
            if (!resourceLib.ContainsKey($"{key}"))
            {
                return HtmlString.Empty;
            }

            var icon = new TagBuilder("i");
            icon.AddCssClass("dash");
            icon.AddCssClass("dash-help");

            var button = new TagBuilder("button");
            button.AddCssClass("btn btn-secondary");
            button.MergeAttribute("type", "button");
            button.MergeAttribute("role", "button");
            button.MergeAttribute("data-toggle", "context-help");
            button.MergeAttribute("data-message", resourceLib[$"{key}"].Replace("\"", "&quot;"));
            button.InnerHtml.AppendHtml(icon);

            var span = new TagBuilder("span");
            span.AddCssClass("input-group-custom");
            span.AddCssClass("input-group-addon");
            span.InnerHtml.AppendHtml(button);

            return span;
        }

        private IHtmlContent BuildInput()
        {
            var input = new TagBuilder("input");
            input.AddCssClass("form-input");
            input.Attributes.Add("id", For.Name);
            input.Attributes.Add("name", For.Name);
            input.Attributes.Add("value", For.ModelExplorer.Model?.ToString());

            var name = For.Name.ToLower();
            var type = "text";
            if (name.Contains("password"))
            {
                type = "password";
            }
            else if (name.Contains("email"))
            {
                type = "email";
            }
            else if (name.Contains("date"))
            {
                Toggle = "datepicker";
            }
            else if (NumberTypes.Contains(For.GetType()))
            {
                type = "number";
            }
            input.Attributes.Add("type", type);

            input.Attributes.AddIf("required", "true", For.Metadata.IsRequired);
            input.Attributes.AddIf("autofocus", "true", Autofocus);

            // @todo test min and max length thoroughly
            var maxLength = GetMaxLength(For.ModelExplorer.Metadata.ValidatorMetadata);
            input.Attributes.AddIf("maxlength", maxLength.ToString(), maxLength > 0);
            var minLength = GetMinLength(For.ModelExplorer.Metadata.ValidatorMetadata);
            input.Attributes.AddIf("minLength", minLength.ToString(), minLength > 0);
            input.Attributes.AddIf("data-toggle", Toggle, !Toggle.IsEmpty());
            input.Attributes.AddIf("data-url", Url, !Url.IsEmpty());
            input.Attributes.AddIf("data-params", Params, !Params.IsEmpty());
            input.Attributes.AddIf("data-preload", "true", Preload);

            return input;
        }

        private IHtmlContent BuildLabel()
        {
            var label = new TagBuilder("label");
            label.AddCssClass("form-label");
            label.AddCssClass("col-4");
            if (For.Metadata.IsRequired)
            {
                label.AddCssClass("required");
            }
            label.Attributes.Add("for", For.Name);
            label.InnerHtml.Append(For.Metadata.DisplayName);
            return label;
        }
    }
}
