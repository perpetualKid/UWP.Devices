using System;

namespace Devices.Controllers.Base
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TargetActionAttribute: Attribute
    {
        public string Target { get; }

        public string[] Actions;

        public TargetActionAttribute(string target, params string[] actions)
        {
            this.Target = target;
            this.Actions = actions;
        }

    }
}
