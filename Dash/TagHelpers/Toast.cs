using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public enum ToastType
    {
        Info,
        Success,
        Error
    }

    public class ToastTagHelper : BaseTagHelper
    {
        public ToastType Type { get; set; }
        public string Id { get; set; }
        public string Message { get; set; }
        public bool AllowClose { get; set; } = true;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (Id.IsEmpty())
            {
                Id = $"{Type.ToString().ToLower()}Toast";
            }

            var colDiv = new TagBuilder("div");
            colDiv.AddCssClass("columns");
            colDiv.AddCssClass("pl-2");
            colDiv.AddCssClass("pr-2");

            var colLeftDiv = new TagBuilder("div");
            colLeftDiv.AddCssClass("col-11");
            colLeftDiv.InnerHtml.Append(Message);

            var colRightDiv = new TagBuilder("div");
            colRightDiv.AddCssClass("col-1");
            var textDiv = new TagBuilder("div");
            textDiv.AddCssClass("text-right");
            if (AllowClose)
            {
                var iDiv = new TagBuilder("i");
                iDiv.AddCssClass("dash");
                iDiv.AddCssClass("dash-cancel");
                iDiv.AddCssClass("toast-dismiss");
                iDiv.Attributes.Add("data-toggle", "hide");
                iDiv.Attributes.Add("data-target", $"#{Id}");
                textDiv.InnerHtml.AppendHtml(iDiv);
            }
            colRightDiv.InnerHtml.AppendHtml(textDiv);

            colDiv.InnerHtml.AppendHtml(colLeftDiv);
            colDiv.InnerHtml.AppendHtml(colRightDiv);

            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = "div";
            output.AddClass("toast", HtmlEncoder.Default);
            output.AddClass($"toast-{Type.ToString().ToLower()}", HtmlEncoder.Default);
            output.Attributes.Add("id", Id);
            output.Content.AppendHtml(colDiv);
            base.Process(context, output);
        }
    }
}
