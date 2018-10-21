﻿using System;
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

        public FormGroupInputTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
        }

        public IHtmlContent AddOn { get; set; }
        public bool Autofocus { get; set; }
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

        private IHtmlContent BuildInput()
        {
            var input = new TagBuilder("input");
            input.AddCssClass("form-input");
            input.Attributes.Add("id", For.Name);
            input.Attributes.Add("name", For.Name);

            var name = For.Name.ToLower();
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
            else if (NumberTypes.Contains(For.ModelExplorer.ModelType))
            {
                type = "number";
            }
            input.Attributes.Add("type", type);
            input.Attributes.Add("value", type == "password" ? "" : For.ModelExplorer.Model?.ToString());

            input.Attributes.AddIf("required", "true", (IsRequired.HasValue && IsRequired.Value) || (!IsRequired.HasValue && For.Metadata.IsRequired));
            input.Attributes.AddIf("autofocus", "true", Autofocus);

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
    }
}
