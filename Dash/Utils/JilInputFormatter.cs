using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Configuration;
using Dash.Models;
using Jil;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Serilog;

namespace Dash
{
    public class JilInputFormatter : TextInputFormatter
    {
        public static Options Options = new Options(false, true, false, DateTimeFormat.ISO8601, true, UnspecifiedDateTimeKindBehavior.IsLocal, SerializationNameFormat.Verbatim);

        public JilInputFormatter()
        {
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/jil"));
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
                    IModel model;
                    var json = streamReader.ReadToEnd();
                    if (json.StartsWith("{\"Model\":"))
                    {
                        // this is messy, but its the only way i could get it to correctly deserialize for an action with multiple params
                        // if i don't tack the `Model:` on from the front-end the input formatter doesn't run
                        var obj = JSON.Deserialize(json, typeof(ModelWrapper<>).MakeGenericType(context.ModelType), Options);
                        var propertyInfo = obj.GetType().GetProperty("Model");
                        model = propertyInfo.GetValue(obj) as IModel;
                    }
                    else
                    {
                        model = JSON.Deserialize(json, context.ModelType, Options) as IModel;
                    }
                    if (model != null)
                    {
                        if (new string[] { "POST", "PUT" }.Contains(context.HttpContext.Request.Method.ToUpper()))
                        {
                            model.SetForSave(true);
                        }
                        var dbContext = context.HttpContext.RequestServices.GetService(typeof(IDbContext)) as IDbContext;
                        if (dbContext != null)
                        {
                            model.SetDbContext(dbContext);
                        }
                        var appConfig = context.HttpContext.RequestServices.GetService(typeof(AppConfiguration)) as AppConfiguration;
                        if (appConfig != null)
                        {
                            model.SetAppConfig(appConfig);
                        }
                        model.SetRequestUserId(context.HttpContext.User.UserId());
                    }
                    return InputFormatterResult.SuccessAsync(model);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "JilInputFormatter Error");
                    return InputFormatterResult.FailureAsync();
                }
            }
        }

        protected override bool CanReadType(Type type)
        {
            if (typeof(IModel).IsAssignableFrom(type))
            {
                return base.CanReadType(type);
            }
            return false;
        }
    }
}
