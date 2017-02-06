using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Components
{
    public abstract class CommunicationComponentBase: ComponentBase
    {
        public CommunicationComponentBase(string componentName) : base(componentName)
        {
        }

        public abstract Task Respond(MessageContainer data);

        public abstract Task CloseChannel(Guid sessionId);

    }
}
