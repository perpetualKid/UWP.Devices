using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Components
{
    public abstract class StorageComponentBase: ComponentBase
    {
        public StorageComponentBase(string componentName) : base(componentName)
        {
        }

        protected abstract Task ConnectStorage(MessageContainer data);

        protected abstract Task DisconnectStorage(MessageContainer data);

        protected abstract Task ListContent(MessageContainer data);

    }
}
