using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;
using System;

namespace Dash
{
    // Courtesy https://stackoverflow.com/questions/39276939/how-to-inject-dependencies-into-models-in-asp-net-core/41059267#41059267
    public class DiModelBinder : ComplexTypeModelBinder
    {
        public DiModelBinder(IDictionary<ModelMetadata, IModelBinder> propertyBinders, ILoggerFactory loggerFactory) : base(propertyBinders, loggerFactory)
        {
        }

        protected override object CreateModel(ModelBindingContext bindingContext)
        {
            var services = bindingContext.HttpContext.RequestServices;
            var modelType = bindingContext.ModelType;
            var ctors = modelType.GetConstructors().OrderByDescending(x => x.GetParameters().Length);
            foreach (var ctor in ctors)
            {
                var paramTypes = ctor.GetParameters().Select(p => p.ParameterType).ToList();
                var parameters = paramTypes.Select(p => services.GetService(p)).ToArray();
                if (parameters.All(p => p != null))
                {
                    var model = ctor.Invoke(parameters);
                    // @todo now that this is being injected correctly, review and remove places where it was being set manually
                    var userId = bindingContext.HttpContext.User.UserId();
                    if (userId > 0)
                    {
                        var prop = modelType.GetProperties().FirstOrDefault( x=> x.Name == "RequestUserId");
                        if (prop != null)
                        {
                            prop.SetValue(model, userId);
                        }
                    }

                    return model;
                }
            }

            return null;
        }
    }

    public class DiModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null) { throw new ArgumentNullException(nameof(context)); }

            if (context.Metadata.IsComplexType && !context.Metadata.IsCollectionType)
            {
                var propertyBinders = context.Metadata.Properties.ToDictionary(property => property, context.CreateBinder);
                return new DiModelBinder(propertyBinders, (ILoggerFactory)context.Services.GetService(typeof(ILoggerFactory)));
            }

            return null;
        }
    }
}
