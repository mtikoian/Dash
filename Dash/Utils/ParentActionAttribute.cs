﻿using System;

namespace Dash
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ParentActionAttribute : Attribute
    {
        public ParentActionAttribute(string action) => Action = action;

        public string Action { get; set; }
    }
}
