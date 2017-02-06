using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Components
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ActionHelpAttribute: Attribute
    {
        public string HelpText { get; set; }

        public ActionHelpAttribute(string helpText)
        {
            this.HelpText = helpText;
        }
    }
}
