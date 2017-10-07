using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Util.Timers
{
    internal class TimerSector<T>
    {
        public List<TimerItem<T>> Items { get; private set; } = new List<TimerItem<T>>();
    }
}
