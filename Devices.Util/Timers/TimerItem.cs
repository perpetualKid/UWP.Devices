using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Util.Timers
{
    public class TimerItem<T>
    {
        public TimeSpan Interval { get; set; }

        public int Repetition { get; set; }

        public TimerActionDelegate<T> TimerAction { get; set; }

        public T Data { get; set; }

        internal int ExecutionTick { get; set; }

    }
}
