﻿using System.Text;
using System.Threading.Tasks;
using Jil;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Dash
{
    public class JilOutputFormatter : TextOutputFormatter
    {
        // don't exclude nulls - it would reduce bandwidth, but objects in arrays that appear to be different will cause slower js performance.
        public static Options Options = new Options(false, false, false, DateTimeFormat.ISO8601, true, UnspecifiedDateTimeKindBehavior.IsLocal, SerializationNameFormat.CamelCase);

        public JilOutputFormatter()
        {
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding) => context.HttpContext.Response.WriteAsync(JSON.SerializeDynamic(context.Object, Options), selectedEncoding, context.HttpContext.RequestAborted);
    }
}
