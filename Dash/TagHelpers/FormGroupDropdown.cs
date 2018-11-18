using System.Collections.Generic;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class FormGroupDropdownTagHelper : BaseTagHelper
    {
        public FormGroupDropdownTagHelper(IHtmlHelper htmlHelper) : base(htmlHelper)
        {
        }

        public string Label { get; set; }
        public string Id { get; set; }
        public bool IsChecked { get; set; }
        public IEnumerable<DropdownListItem> Items { get; set; }
        public string Name { get; set; }
        public string TargetId { get; set; }
        public string Value { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();

            var button = new TagBuilder("button");
            button.AddCssClass("btn btn-secondary dropdown-toggle");
            button.Attributes["type"] = "button";
            button.Attributes["data-toggle"] = "dropdown";
            button.InnerHtml.AppendHtml(output.GetChildContentAsync().Result);

            var ul = new TagBuilder("ul");
            ul.AddCssClass("menu");
            Items.Each(x => {
                var itemTag = new TagBuilder("li");
                itemTag.AddCssClass("menu-item c-hand");
                itemTag.Attributes["data-toggle"] = "input-replace";
                itemTag.Attributes["data-target"] = $"#{TargetId}";
                itemTag.Attributes["data-value"] = x.Label;
                itemTag.InnerHtml.AppendHtml(x.Label);
                ul.InnerHtml.AppendHtml(itemTag);
            });

            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = "div";
            output.AddClass("dropdown", HtmlEncoder.Default);
            output.AddClass("dropdown-right", HtmlEncoder.Default);
            output.Content.AppendHtml(button);
            output.Content.AppendHtml(ul);

            base.Process(context, output);
        }
    }

    public class DropdownListItem
    {
        public string Action { get; set; }
        public string Controller { get; set; }
        public string Icon { get; set; }
        public string Label { get; set; }
        public object RouteValues { get; set; }
    }
}
