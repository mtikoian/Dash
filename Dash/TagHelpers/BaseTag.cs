using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class BaseTagHelper : TagHelper
    {
        public bool NoRender { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (NoRender)
            {
                output.SuppressOutput();
            }
            base.Process(context, output);
        }
    }
}
