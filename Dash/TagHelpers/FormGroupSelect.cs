using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using Jil;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class FormGroupSelectTagHelper : FormBaseTagHelper
    {
        private static readonly Type[] NumberTypes = { typeof(int), typeof(long), typeof(decimal), typeof(double), typeof(int?), typeof(long?), typeof(decimal?), typeof(double?) };
        private Dictionary<string, string> OptionsDictionary;
        private List<string> SelectedValues;

        private IHtmlContent BuildInput()
        {
            var input = new TagBuilder(Multiple ? "input" : "select");
            input.AddCssClass("form-input");
            input.Attributes.Add("id", FieldName);
            input.Attributes.Add("name", Multiple ? $"{FieldName}Autocomplete" : FieldName);
            input.Attributes.AddIf("required", "true", IsRequired == true || (!IsRequired.HasValue && For?.Metadata.IsRequired == true));
            input.Attributes.AddIf("autofocus", "true", Autofocus);
            input.Attributes.AddIf("data-toggle", Toggle, !Toggle.IsEmpty());
            input.Attributes.AddIf("data-url", Url, !Url.IsEmpty());
            input.Attributes.AddIf("data-params", Params, !Params.IsEmpty());
            input.Attributes.AddIf("data-target", Target, !Target.IsEmpty());
            input.Attributes.AddIf("data-match", Match, Match != null);
            input.Attributes.AddIf("disabled", "true", Disabled == true);

            SelectedValues = new List<string>();
            if (Multiple)
            {
                try
                {
                    SelectedValues = JSON.Deserialize<List<string>>(For?.ModelExplorer.Model?.ToString());
                }
                catch { }
                input.Attributes.Add("data-preload", "true");
                input.Attributes.Add("data-chip-input-name", $"{FieldName}List");
                input.Attributes.Add("data-options", JSON.Serialize(Options.Select(x => $"{x.Text} ({x.Value})"), JilOutputFormatter.Options));
            }
            else
            {
                input.AddCssClass("form-select");
                SelectedValues.Add(For?.ModelExplorer.Model?.ToString());
                input.InnerHtml.AppendHtml(new TagBuilder("option"));
                Options.ToList().ForEach(x => {
                    var opt = new TagBuilder("option");
                    opt.Attributes.Add("value", x.Value);
                    opt.Attributes.AddIf("selected", "true", SelectedValues.Contains(x.Value));
                    opt.InnerHtml.Append(x.Text);
                    input.InnerHtml.AppendHtml(opt);
                });
            }

            return input;
        }

        public FormGroupSelectTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
        }

        public bool Autofocus { get; set; }
        public string Match { get; set; }
        public bool Multiple { get; set; }
        public IEnumerable<SelectListItem> Options { get; set; }
        public string Params { get; set; }
        public string Target { get; set; }
        public string Toggle { get; set; }
        public string Url { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();

            Toggle = Multiple ? "autocomplete" : Toggle;
            if (Options != null)
            {
                Options = Options.GroupBy(x => x.Value).Select(x => x.First());
                OptionsDictionary = Options.ToDictionary(x => x.Value, x => x.Text);
            }

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

            if (Multiple)
            {
                var chipGroupDiv = new TagBuilder("div");
                chipGroupDiv.AddCssClass("input-group-chips");
                var optionsDictionary =
                SelectedValues.Each(x => {
                    var chipDiv = new TagBuilder("span");
                    chipDiv.AddCssClass("chip");
                    chipDiv.InnerHtml.Append(OptionsDictionary?.ContainsKey(x) == true ? $"{OptionsDictionary[x]} ({x})" : "");

                    var chipBtn = new TagBuilder("a");
                    chipBtn.AddCssClass("btn");
                    chipBtn.AddCssClass("btn-clear");
                    chipBtn.Attributes.Add("aria-label", "close");
                    chipBtn.Attributes.Add("role", "button");
                    chipDiv.InnerHtml.AppendHtml(chipBtn);

                    var chipInput = new TagBuilder("input");
                    chipInput.Attributes.Add("type", "hidden");
                    chipInput.Attributes.Add("name", $"{FieldName}List[]");
                    chipInput.Attributes.Add("value", x);
                    chipDiv.InnerHtml.AppendHtml(chipInput);

                    chipGroupDiv.InnerHtml.AppendHtml(chipDiv);
                });
                div.InnerHtml.AppendHtml(chipGroupDiv);
            }

            base.Process(context, output);
        }
    }
}
