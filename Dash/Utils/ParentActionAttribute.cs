using System;

namespace Dash.Utils
{
    /// <summary>
    /// Specify action that permission should inherit from.
    /// </summary>
    public class ParentActionAttribute : Attribute
    {
        public ParentActionAttribute(string actionName)
        {
            ActionName = actionName;
        }

        public string ActionName { get; set; }
    }
}