using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class DoColumnTagHelper : BaseTagHelper
    {
        public string Field { get; set; }
        public string Label { get; set; }
        public bool Sortable { get; set; } = true;
        public string TextProperty { get; set; }
        public TableDataType Type { get; set; } = TableDataType.String;
        public string Width { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = "td";
            if (!Width.IsEmpty())
            {
                output.Attributes.Add("data-width", Width);
            }
            output.Attributes.Add("data-sortable", Sortable.ToString());
            output.Attributes.Add("data-type", Type);
            output.Attributes.Add("data-label", Label);
            output.Attributes.Add("data-field", Field);
            if (!TextProperty.IsEmpty())
            {
                output.Content.AppendHtml($"{{{{=x.{TextProperty} || ''}}}}");
            }
            base.Process(context, output);
        }
    }
}
