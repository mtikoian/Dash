using System.Collections.Generic;
using System.Linq;
using Dash.Resources;
using Jil;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class FormGroupTagListTagHelper : FormBaseTagHelper
    {
        Dictionary<string, string> _OptionsDictionary;

        IHtmlContent BuildInput()
        {
            var input = new TagBuilder("input");
            input.AddCssClass("form-input");
            input.Attributes.Add("id", FieldName);
            input.Attributes.Add("placeholder", Core.StartTyping);
            input.Attributes.Add("name", $"{FieldName}Autocomplete");
            input.Attributes.Add("data-toggle", DataToggles.TagList.ToHyphenCase());
            input.Attributes.AddIf("autofocus", "true", Autofocus);
            input.Attributes.AddIf("disabled", "true", Disabled == true);

            if (SelectedValues == null)
            {
                SelectedValues = new List<string>();
                try
                {
                    SelectedValues = JSON.Deserialize<List<string>>(For?.ModelExplorer.Model?.ToString());
                }
                catch { }
            }
            input.Attributes.Add("data-chip-input-name", $"{FieldName}List");
            input.Attributes.Add("data-options", JSON.Serialize(Options.Select(x => $"{x.Text} ({x.Value})"), JilOutputFormatter.Options));

            return input;
        }

        public FormGroupTagListTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper) { }

        public bool Autofocus { get; set; }
        public IEnumerable<SelectListItem> Options { get; set; }

        public List<string> SelectedValues { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();
            UseFormGroup(output);

            if (Options != null)
            {
                Options = Options.Where(x => !x.Value.IsEmpty()).GroupBy(x => x.Value).Select(x => x.First());
                _OptionsDictionary = Options.ToDictionary(x => x.Value, x => x.Text);
            }

            var div = BuildFormGroup();
            var inputGroup = BuildInputGroup();
            inputGroup.InnerHtml.AppendHtml(BuildInput());
            inputGroup.InnerHtml.AppendHtml(BuildHelp());
            div.InnerHtml.AppendHtml(inputGroup);

            output.Content.AppendHtml(BuildLabel());
            output.Content.AppendHtml(div);

            var chipGroupDiv = new TagBuilder("div");
            chipGroupDiv.AddCssClass("input-group-chips");
            var optionsDictionary =
            SelectedValues.Where(x => _OptionsDictionary?.ContainsKey(x) == true).OrderBy(x => _OptionsDictionary[x]).Each(x => {
                var chipDiv = new TagBuilder("span");
                chipDiv.AddCssClass("chip");
                chipDiv.InnerHtml.Append($"{_OptionsDictionary[x].Trim()} ({x.Trim()})".Trim());

                var chipBtn = new TagBuilder("button");
                chipBtn.AddCssClass("btn");
                chipBtn.AddCssClass("btn-clear");
                chipBtn.Attributes.Add("aria-label", "close");
                chipBtn.Attributes.Add("role", "button");
                chipDiv.InnerHtml.AppendHtml(chipBtn);

                var chipInput = new TagBuilder("input");
                chipInput.Attributes.Add("type", "hidden");
                chipInput.Attributes.Add("name", $"{FieldName}List[]");
                chipInput.Attributes.Add("value", x.Trim());
                chipDiv.InnerHtml.AppendHtml(chipInput);

                chipGroupDiv.InnerHtml.AppendHtml(chipDiv);
            });
            div.InnerHtml.AppendHtml(chipGroupDiv);

            base.Process(context, output);
        }
    }
}
