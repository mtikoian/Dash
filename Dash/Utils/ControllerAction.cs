using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Dash
{
    public class ControllerAction
    {
        static readonly string _Namespace = typeof(Controllers.BaseController).Namespace;
        static List<Type> _CheckableType = new List<Type> { typeof(HttpPostAttribute), typeof(HttpPutAttribute), typeof(HttpDeleteAttribute) };
        IMemoryCache _Cache;

        Type GetControllerType() => Assembly.GetExecutingAssembly().GetType($"{_Namespace}.{Controller}Controller", false, true);

        MethodInfo GetMethod()
        {
            var controllerType = GetControllerType();
            if (controllerType == null)
                return null;

            var requestType = RequestType();
            var methods = controllerType.GetMethods().Where(x => x.Name.ToLower() == Action.ToLower());
            if (requestType != null)
                return methods.FirstOrDefault(x => x.GetCustomAttributes(false).Any(a => a.GetType() == requestType));
            return methods.FirstOrDefault(x => !x.GetCustomAttributes(false).Any(a => _CheckableType.Contains(a.GetType())));
        }

        Type RequestType()
        {
            if (Method == HttpVerbs.Post)
                return typeof(HttpPostAttribute);
            if (Method == HttpVerbs.Put)
                return typeof(HttpPutAttribute);
            if (Method == HttpVerbs.Delete)
                return typeof(HttpDeleteAttribute);
            return null;
        }

        public ControllerAction() { }

        public ControllerAction(IMemoryCache cache) => _Cache = cache;

        public ControllerAction(string controller, string action, HttpVerbs method = HttpVerbs.Get)
        {
            Controller = controller;
            Action = action;
            Method = method;
        }

        public string Action { get; set; }
        public string Controller { get; set; }
        public HttpVerbs Method { get; set; }

        public List<string> EffectivePermissions() => _Cache.Cached($"effectivePermissions_{Controller}_{Action}_{Method}", () => {
            var method = GetMethod();
            if (method != null)
            {
                var parentAttr = method.GetCustomAttributes(false).FirstOrDefault(x => x.GetType() == typeof(ParentActionAttribute));
                if (parentAttr != null)
                    return ((ParentActionAttribute)parentAttr).Action.ToLower().Trim().Split(',').Select(x => $"{Controller.ToLower().Trim()}.{x.Trim()}").ToList();
            }
            return new List<string> { $"{Controller.ToLower().Trim()}.{Action.ToLower().Trim()}" };
        });
    }
}
