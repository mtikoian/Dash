using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class DoColumnTagHelper : BaseTagHelper
    {
        public string Field { get; set; }
        public string Label { get; set; }
        public bool Sortable { get; set; } = true;
        public string Type { get; set; } = "string";
        public string Width { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "td";
            if (!Width.IsEmpty())
            {
                output.Attributes.Add("data-width", Width);
            }
            output.Attributes.Add("data-sortable", Sortable.ToString());
            output.Attributes.Add("data-type", Type);
            output.Attributes.Add("data-label", Label);
            output.Attributes.Add("data-field", Field);
            base.Process(context, output);
        }
    }
}
