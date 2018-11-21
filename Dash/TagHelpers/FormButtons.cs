using System.Text.Encodings.Web;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class FormButtonsTagHelper : BaseTagHelper
    {
        public FormButtonsTagHelper() : base()
        {
        }

        public string SubmitLabel { get; set; } = Core.Save;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = "div";
            output.AddClass("col-8", HtmlEncoder.Default);
            output.AddClass("col-ml-auto", HtmlEncoder.Default);
            output.AddClass("pt-5", HtmlEncoder.Default);
            output.AddClass("form-buttons", HtmlEncoder.Default);

            var input = new TagBuilder("input");
            input.AddCssClass("btn");
            input.AddCssClass("btn-primary");
            input.AddCssClass("mr-2");
            input.Attributes.Add("type", "submit");
            input.Attributes.Add("value", SubmitLabel);

            output.Content.AppendHtml(input);
            output.Content.AppendHtml(output.GetChildContentAsync().Result);

            base.Process(context, output);
        }
    }
}
