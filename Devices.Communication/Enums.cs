using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Communication
{
    public enum DataFormat
    {
        Unknown,
        Text,
        Json,
        FrameSize,
    }

    public enum ConnectionStatus
    {
        Failed = -1,
        Disconnected = 0,
        Connecting = 1,
        Listening = 2,
        Connected = 3,
    }
}
