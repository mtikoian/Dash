using Jil;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class JilInputFormatter : TextInputFormatter
    {
        public static Options Options = new Options(false, true, false, DateTimeFormat.ISO8601, true, UnspecifiedDateTimeKindBehavior.IsLocal, SerializationNameFormat.CamelCase);
        private static readonly Encoding DefaultEncoding = Encoding.UTF8;

        public JilInputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
            SupportedEncodings.Add(DefaultEncoding);
        }

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            var request = context.HttpContext.Request;
            using (var streamReader = context.ReaderFactory(request.Body, encoding))
            {
                try
                {
                    var model = JSON.Deserialize(streamReader, context.ModelType, Options);
                    return InputFormatterResult.SuccessAsync(model);
                }
                catch (Exception)
                {
                    return InputFormatterResult.FailureAsync();
                }
            }
        }
    }
}