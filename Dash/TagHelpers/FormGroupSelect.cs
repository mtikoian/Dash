﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class FormGroupSelectTagHelper : FormBaseTagHelper
    {
        IHtmlContent BuildInput()
        {
            var input = new TagBuilder("select");
            input.AddCssClass("form-input");
            input.AddCssClass("form-select");
            input.Attributes.Add("id", FieldName);
            input.Attributes.Add("name", FieldName);
            input.Attributes.AddIf("required", "true", IsRequired == true || (!IsRequired.HasValue && For?.Metadata.IsRequired == true));
            input.Attributes.AddIf("autofocus", "true", Autofocus);
            input.Attributes.AddIf("data-toggle", Toggle.ToHyphenCase(), Toggle.HasValue);
            input.Attributes.AddIf("data-url", Url, !Url.IsEmpty());
            input.Attributes.AddIf("data-params", Params, !Params.IsEmpty());
            input.Attributes.AddIf("data-target", Target, !Target.IsEmpty());
            input.Attributes.AddIf("data-match", Match, Match != null);
            input.Attributes.AddIf("disabled", "true", Disabled == true);

            var selectedValue = For?.ModelExplorer.Model?.ToString();
            input.InnerHtml.AppendHtml(new TagBuilder("option"));
            Options.ToList().ForEach(x => {
                var opt = new TagBuilder("option");
                opt.Attributes.Add("value", x.Value.Trim());
                opt.Attributes.AddIf("selected", "true", selectedValue == x.Value);
                opt.InnerHtml.Append(x.Text.Trim());
                input.InnerHtml.AppendHtml(opt);
            });

            return input;
        }

        public FormGroupSelectTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper) { }

        public bool Autofocus { get; set; }
        public string Match { get; set; }
        public IEnumerable<SelectListItem> Options { get; set; }
        public string Params { get; set; }
        public string Target { get; set; }
        public DataToggles? Toggle { get; set; }
        public string Url { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();
            UseFormGroup(output);

            if (Options != null)
                Options = Options.Where(x => !x.Value.IsEmpty()).GroupBy(x => x.Value).Select(x => x.First());

            var div = BuildFormGroup();
            var inputGroup = BuildInputGroup();
            inputGroup.InnerHtml.AppendHtml(BuildInput());
            inputGroup.InnerHtml.AppendHtml(BuildHelp());
            div.InnerHtml.AppendHtml(inputGroup);

            output.Content.AppendHtml(BuildLabel());
            output.Content.AppendHtml(div);

            base.Process(context, output);
        }
    }
}
