using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Components
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ActionAttribute : Attribute
    {
        public string Action { get; }

        public ActionAttribute(string action)
        {
            this.Action = action;
        }
    }
}
