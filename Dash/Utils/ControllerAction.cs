﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Dash
{
    public class ControllerAction
    {
        private static List<Type> _CheckableType = new List<Type> { typeof(HttpPostAttribute), typeof(HttpPutAttribute), typeof(HttpDeleteAttribute) };
        private static string _Namespace = typeof(Controllers.BaseController).Namespace;

        public ControllerAction()
        {
        }

        public ControllerAction(string controller, string action, HttpVerbs method = HttpVerbs.Get)
        {
            Controller = controller;
            Action = action;
            Method = method;
        }

        public string Action { get; set; }
        public string Controller { get; set; }
        public HttpVerbs Method { get; set; }

        public List<string> EffectivePermissions()
        {
            var method = GetMethod();
            if (method != null)
            {
                var parentAttr = method.GetCustomAttributes(false).FirstOrDefault(x => x.GetType() == typeof(ParentActionAttribute));
                if (parentAttr != null)
                {
                    var parentAction = ((ParentActionAttribute)parentAttr).Action.ToLower().Trim().Split(',');
                    return parentAction.Select(x => $"{Controller.ToLower().Trim()}.{x.Trim()}").ToList();
                }
            }
            return new List<string> { $"{Controller.ToLower().Trim()}.{Action.ToLower().Trim()}" };
        }

        public Type GetControllerType()
        {
            return Assembly.GetExecutingAssembly().GetType($"{_Namespace}.{Controller}Controller", false, true);
        }

        public MethodInfo GetMethod()
        {
            var controllerType = GetControllerType();
            if (controllerType == null) return null;

            var requestType = RequestType();
            var methods = controllerType.GetMethods().Where(x => x.Name.ToLower() == Action.ToLower());
            if (requestType != null)
            {
                return methods.FirstOrDefault(x => x.GetCustomAttributes(false).Any(a => a.GetType() == requestType));
            }
            return methods.FirstOrDefault(x => !x.GetCustomAttributes(false).Any(a => _CheckableType.Contains(a.GetType())));
        }

        private Type RequestType()
        {
            if (Method == HttpVerbs.Post)
            {
                return typeof(HttpPostAttribute);
            }
            if (Method == HttpVerbs.Put)
            {
                return typeof(HttpPutAttribute);
            }
            if (Method == HttpVerbs.Delete)
            {
                return typeof(HttpDeleteAttribute);
            }
            return null;
        }
    }
}