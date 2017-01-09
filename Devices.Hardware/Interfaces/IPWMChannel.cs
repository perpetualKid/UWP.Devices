using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Hardware.Interfaces
{
    public interface IPWMChannel
    {
        void SetPulse(ushort pulseLength);

        void SetDutyCycle(double dutyCycle);

        void Release();

        int Resolution { get; }

        int Frequency { get; set; }
    }
}
