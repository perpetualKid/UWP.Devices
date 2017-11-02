using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Util.Timers
{
    public class CascadingWheelTimer<T>
    {
        private BaseTimerWheel<T> baseTimer;

        public CascadingWheelTimer(int wheelSize, int interval)
        {
            this.baseTimer = new BaseTimerWheel<T>(wheelSize, interval);
        }

        public async Task Add(TimerItem<T> item)
        {
            await baseTimer.AddItem(item);
        }

        public async Task Add(DateTime startTime, TimerItem<T> item)
        {
            await baseTimer.AddItem(startTime, item);
        }

        public void Start()
        {
            baseTimer.Start();
        }

        public void Stop()
        {
            baseTimer.Stop();
        }

        public TimerItem<T>[] GetActiveTimerItems()
        {
            return baseTimer.GetActiveTimers().ToArray();
        }

        public async Task<bool> RemoveTimerItem(Guid timerItemId)
        {
            bool result = await baseTimer.RemoveTimerItem(timerItemId).ConfigureAwait(false);
            return result;
        }

        public async Task Clear()
        {
            await baseTimer.Clear().ConfigureAwait(false);
        }
    }
}
