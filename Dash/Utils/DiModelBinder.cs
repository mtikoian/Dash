﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;

namespace Dash
{
    // Courtesy https://stackoverflow.com/questions/39276939/how-to-inject-dependencies-into-models-in-asp-net-core/41059267#41059267
    public class DiModelBinder : ComplexTypeModelBinder
    {
        protected override object CreateModel(ModelBindingContext bindingContext)
        {
            var services = bindingContext.HttpContext.RequestServices;
            var modelType = bindingContext.ModelType;
            var ctors = modelType.GetConstructors().OrderByDescending(x => x.GetParameters().Length);
            foreach (var ctor in ctors)
            {
                var parameters = ctor.GetParameters().Select(p => p.ParameterType).ToList().Select(p => services.GetService(p)).ToArray();
                if (parameters.All(p => p != null))
                {
                    var model = ctor.Invoke(parameters);
                    var userId = bindingContext.HttpContext.User.UserId();
                    if (userId > 0)
                    {
                        var prop = modelType.GetProperties().FirstOrDefault(x => x.Name == "RequestUserId");
                        if (prop != null)
                            prop.SetValue(model, userId);
                    }

                    return model;
                }
            }

            return null;
        }

        public DiModelBinder(IDictionary<ModelMetadata, IModelBinder> propertyBinders, ILoggerFactory loggerFactory) : base(propertyBinders, loggerFactory) { }
    }

    public class DiModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.Metadata.IsComplexType && !context.Metadata.IsCollectionType)
                return new DiModelBinder(context.Metadata.Properties.ToDictionary(property => property, context.CreateBinder), (ILoggerFactory)context.Services.GetService(typeof(ILoggerFactory)));
            return null;
        }
    }
}
