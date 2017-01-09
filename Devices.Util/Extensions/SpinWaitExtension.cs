using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Devices.Util.Extensions
{
    public static class SpinWaitExtension
    {
        public static void SpinFor(TimeSpan delay)
        {
            SpinFor(delay.Ticks);
        }

        public static void SpinFor(long ticks)
        {
            Stopwatch watch = Stopwatch.StartNew();
            SpinWait.SpinUntil(() => watch.ElapsedTicks > ticks);
        }

    }
}
