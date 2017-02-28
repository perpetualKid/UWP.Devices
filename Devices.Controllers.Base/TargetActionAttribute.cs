using System;

namespace Devices.Controllers.Base
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TargetActionAttribute: Attribute
    {
        public string[] Actions;

        public TargetActionAttribute(params string[] actions)
        {
            this.Actions = actions;
        }

    }
}
