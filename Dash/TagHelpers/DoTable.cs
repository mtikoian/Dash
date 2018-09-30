using System.Text.Encodings.Web;
using Jil;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class DoTableTagHelper : BaseTagHelper
    {
        public string DisplayCurrencyFormat { get; set; }
        public string DisplayDateFormat { get; set; }
        public bool Editable { get; set; } = true;
        public string Id { get; set; }
        public bool LoadAll { get; set; } = true;
        public HttpVerbs RequestMethod { get; set; } = HttpVerbs.Post;
        public object RequestParams { get; set; }
        public bool Searchable { get; set; } = true;
        public HttpVerbs? StoreRequestMethod { get; set; }
        public string StoreUrl { get; set; }
        public string Template { get; set; }
        public string ResultUrl { get; set; }
        public string Width { get; set; }
        public bool CheckUpdateDate { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var table = new TagBuilder("div");
            table.Attributes.Add("id", Id);
            table.Attributes.Add("data-toggle", "dotable");
            table.Attributes.Add("data-template", Template);
            table.Attributes.Add("data-result-url", ResultUrl);
            table.Attributes.Add("data-editable", Editable.ToString());
            table.Attributes.Add("data-searchable", Searchable.ToString());
            table.Attributes.Add("data-load-all", LoadAll.ToString());
            table.Attributes.Add("data-request-method", RequestMethod.ToString());
            table.Attributes.Add("data-check-update-date", CheckUpdateDate.ToString());
            if (!StoreUrl.IsEmpty())
            {
                table.Attributes.Add("data-store-url", StoreUrl);
            }
            if (!Width.IsEmpty())
            {
                table.Attributes.Add("data-width", Width);
            }
            if (StoreRequestMethod.HasValue)
            {
                table.Attributes.Add("data-store-request-method", StoreRequestMethod.ToString());
            }
            if (!DisplayDateFormat.IsEmpty())
            {
                table.Attributes.Add("data-display-date-format", DisplayDateFormat);
            }
            if (!DisplayCurrencyFormat.IsEmpty())
            {
                table.Attributes.Add("data-display-currency-format", DisplayCurrencyFormat);
            }
            if (RequestParams != null)
            {
                table.Attributes.Add("json-request-params", JSON.SerializeDynamic(RequestParams, JilOutputFormatter.Options));
            }

            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = "div";
            output.AddClass("col-12", HtmlEncoder.Default);
            output.Content.AppendHtml(table);
            base.Process(context, output);
        }
    }
}
