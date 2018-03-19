using Jil;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class JilOutputFormatter : TextOutputFormatter
    {
        public static Options Options = new Options(false, true, false, DateTimeFormat.ISO8601, true, UnspecifiedDateTimeKindBehavior.IsLocal, SerializationNameFormat.CamelCase);

        public JilOutputFormatter()
        {
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/jil"));
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            return context.HttpContext.Response.WriteAsync(JSON.SerializeDynamic(context.Object, Options), selectedEncoding, context.HttpContext.RequestAborted);
        }
    }
}