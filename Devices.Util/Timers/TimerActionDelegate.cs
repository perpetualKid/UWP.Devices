using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Util.Timers
{
    public delegate void TimerActionDelegate<T>(TimerItem<T> data);
}
