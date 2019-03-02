using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dash.Utils;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class FormBaseTagHelper : BaseTagHelper
    {
        public FormBaseTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
        }

        public bool? Disabled { get; set; }
        public string FieldName => For == null ? Name : For.Name;
        public string FieldTitle => For == null ? Title : For.Metadata.DisplayName;
        public ModelExpression For { get; set; }
        public string HelpText { get; set; }
        public bool? IsRequired { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }

        public static int GetMaxLength(IReadOnlyList<object> validatorMetadata)
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

        public static int GetMinLength(IReadOnlyList<object> validatorMetadata)
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

        public IHtmlContent BuildHelp()
        {
            if (_HtmlHelper.ViewContext.HttpContext?.WantsHelp() != true)
            {
                return HtmlString.Empty;
            }

            if (HelpText.IsEmpty() && For != null)
            {
                var key = $"{For.Metadata.ContainerType.Name}_{For.Metadata.PropertyName}";
                var resourceLib = new ResourceDictionary("ContextHelp");
                if (resourceLib.ContainsKey($"{key}"))
                {
                    HelpText = resourceLib[$"{key}"];
                }
            }
            if (HelpText.IsEmpty())
            {
                return HtmlString.Empty;
            }

            var icon = new TagBuilder("i");
            icon.AddCssClass("dash");
            icon.AddCssClass("dash-help");

            var button = new TagBuilder("button");
            button.AddCssClass("btn btn-secondary input-group-btn");
            button.MergeAttribute("type", "button");
            button.MergeAttribute("role", "button");
            button.MergeAttribute("data-toggle", "context-help");
            button.MergeAttribute("data-message", HelpText.Replace("\"", "&quot;"));
            button.InnerHtml.AppendHtml(icon);

            var span = new TagBuilder("span");
            span.AddCssClass("input-group-custom");
            span.AddCssClass("input-group-addon");
            span.InnerHtml.AppendHtml(button);

            return span;
        }

        public IHtmlContent BuildLabel()
        {
            var label = new TagBuilder("label");
            label.AddCssClass("form-label");
            label.AddCssClass("col-4");
            if ((IsRequired.HasValue && IsRequired.Value) || (!IsRequired.HasValue && For?.Metadata.IsRequired == true))
            {
                label.AddCssClass("required");
            }
            label.Attributes.Add("for", FieldName);
            label.InnerHtml.Append(FieldTitle);
            return label;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output) => base.Process(context, output);
    }
}
