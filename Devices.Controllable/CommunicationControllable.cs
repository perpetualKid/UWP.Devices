using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Controllable
{
    public abstract class CommunicationControllable : ControllableComponent
    {
        public CommunicationControllable(string componentName) : base(componentName)
        {
        }

        public abstract Task Respond(MessageContainer data);

        public abstract Task CloseChannel(Guid sessionId);
    }
}
