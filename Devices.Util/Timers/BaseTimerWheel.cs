using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Devices.Util.Timers
{
    internal class BaseTimerWheel<T> : TimerWheel<T>
    {
        private Timer timer;
        private DateTime nextTickTime;

        #region .ctor
        public BaseTimerWheel(int wheelSize, int interval) :
            base(wheelSize, interval, 0)
        {
            if (interval < 20)
                throw new ArgumentOutOfRangeException(nameof(interval));
            this.timer = new Timer(TimerEvent, null, Timeout.Infinite, Timeout.Infinite);
        }
        #endregion

        private async void TimerEvent(object status)
        {
            TimerSector<T> sector = await base.TimerTick().ConfigureAwait(false);

            if (null != sector)
            {
                foreach (TimerItem<T> item in sector.Items)
                {
                    item.Repetition -= 1;
                    if (item.Repetition > 0)
                    {
                        item.ExecutionTick = 0;
                        await this.AddOverflow(item).ConfigureAwait(false);
                    }
                }
                sector.Items.ForEach(item => item.TimerAction.Invoke(item));
            }

            nextTickTime = nextTickTime.AddMilliseconds(this.baseInterval);
            TimeSpan nextInterval = nextTickTime.Subtract(DateTime.UtcNow);
            if (nextInterval.TotalMilliseconds < 0)
                nextInterval = TimeSpan.FromMilliseconds(1);
            timer.Change(nextInterval, Timeout.InfiniteTimeSpan);
        }

        // Starts raising the WheelTick event.
        public virtual void Start()
        {
            nextTickTime = DateTime.UtcNow.AddMilliseconds(baseInterval);
            timer.Change(baseInterval, Timeout.Infinite);
        }

        // Stops raising the WheelTick event.
        public virtual void Stop()
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        #region IDisposable
        // Releasing unmanaged resources
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Destructor. Automatically calls Dispose.
        ~BaseTimerWheel()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        // Dispose the TimerWheel.
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (timer != null)
                {
                    timer.Dispose();
                    timer = null;
                }
            }
        }
        #endregion
    }
}
