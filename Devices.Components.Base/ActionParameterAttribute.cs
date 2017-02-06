using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Components
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ActionParameterAttribute: Attribute
    {
        public string ParameterName { get; private set; }

        public bool Required { get; set; } = true;

        public Type ParameterType { get; set; }

        public ActionParameterAttribute(string name)
        {
            this.ParameterName = name;
        }
    }
}
