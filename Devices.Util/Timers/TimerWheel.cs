using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Devices.Util.Timers
{
    internal class TimerWheel<T>
    {
        private SemaphoreSlim sectorHandling;

        private TimerSector<T>[] sectors;
        private int currentTick;
        private readonly int rank;
        private readonly int wheelSize;
        private readonly int wheelSpan;
        private readonly int sectorSpan;
        private TimerWheel<T> carryOverWheel;
        protected readonly int baseInterval = 1000; // Default tick interval of 1000ms

        protected Dictionary<Guid, TimerItem<T>> timerItems;

        public TimerWheel(int wheelSize, int baseInterval, int rank)
        {
            if (wheelSize < 2)
                throw new ArgumentOutOfRangeException(nameof(wheelSize));

            this.wheelSize = wheelSize;
            this.sectors = new TimerSector<T>[wheelSize];
            this.rank = rank;
            this.sectorHandling = new SemaphoreSlim(1);

            this.baseInterval = baseInterval;
            this.sectorSpan = (int)Math.Pow(wheelSize, rank) * baseInterval; ;
            this.wheelSpan = sectorSpan * wheelSize;
        }

        public TimerSector<T> this[int index]
        {
            get
            {
                if (null == sectors[index])
                    sectors[index] = new TimerSector<T>();
                return sectors[index];
            }
        }

        protected async Task<TimerSector<T>> TimerTick()
        {
            currentTick = ++currentTick % this.wheelSize;
            if (0 == currentTick)
                await CarryOver().ConfigureAwait(false);
            await sectorHandling.WaitAsync().ConfigureAwait(false);
            TimerSector<T> sector = sectors[currentTick];
            sectors[currentTick] = null;
            sectorHandling.Release();
            return sector;
        }

        private async Task AddItem(int distance, TimerItem<T> item)
        {
            await sectorHandling.WaitAsync().ConfigureAwait(false);
            this[(currentTick + distance) % wheelSize].Items.Add(item);
            sectorHandling.Release();
        }

        public async Task AddItem(TimerItem<T> item)
        {
            await this.AddItem(1, item);
            this.timerItems.Add(item.TimerItemId, item);
        }

        public async Task AddItem(DateTime startTime, TimerItem<T> item)
        {
            TimeSpan initialInterval = startTime.Subtract(DateTime.UtcNow);
            await this.AddInitialTime(initialInterval, item);
            this.timerItems.Add(item.TimerItemId, item);
        }

        public async Task AddItem(TimeSpan initialInterval, TimerItem<T> item)
        {
            await this.AddInitialTime(initialInterval, item);
            this.timerItems.Add(item.TimerItemId, item);
        }

        public async Task Clear()
        {
            await sectorHandling.WaitAsync().ConfigureAwait(false);
            ClearItems();
            sectorHandling.Release();
            timerItems.Clear();
        }

        public async Task<bool> RemoveTimerItem(Guid timerItemId)
        {
            if (timerItems.ContainsKey(timerItemId))
            {
                TimerItem<T> item = timerItems[timerItemId];
                timerItems.Remove(timerItemId);
                await sectorHandling.WaitAsync().ConfigureAwait(false);
                RemoveItem(item);
                sectorHandling.Release();
                return true;
            }
            return false;
        }

        private async Task CarryOver()
        {
            if (null != this.carryOverWheel)
            {
                TimerSector<T> sector = await carryOverWheel.TimerTick().ConfigureAwait(false);
                if (null != sector?.Items)
                    foreach (TimerItem<T> item in sector.Items)
                    {
                        int tick = item.ExecutionTick;
                        item.ExecutionTick %= sectorSpan;
                        await this.AddItem((tick / sectorSpan), item).ConfigureAwait(false);
                    }
            }
        }

        protected async Task AddOverflow(TimerItem<T> item)
        {
            //get largest wheel required
            if (this.wheelSpan < item.Interval.TotalMilliseconds)
            {
                if (null == this.carryOverWheel)
                {
                    this.carryOverWheel = new TimerWheel<T>(this.wheelSize, baseInterval, this.rank + 1);
                }
                item.ExecutionTick += this.currentTick * this.sectorSpan;
                await this.carryOverWheel.AddOverflow(item).ConfigureAwait(false);
            }
            else
            {
                int nextTickTime = (int)(item.Interval.TotalMilliseconds + item.ExecutionTick);
                item.ExecutionTick = nextTickTime % this.sectorSpan;
                //timerWheels.ForEach(x => { System.Diagnostics.Debug.Write($"{x.Magnitude}:{x.CurrentTick} "); });
                await this.AddItem((int)(nextTickTime / this.sectorSpan), item);
            }
        }

        protected async Task AddInitialTime(TimeSpan startInterval, TimerItem<T> item)
        {
            //get largest wheel required
            if (this.wheelSpan < startInterval.TotalMilliseconds)
            {
                if (null == this.carryOverWheel)
                {
                    this.carryOverWheel = new TimerWheel<T>(this.wheelSize, baseInterval, this.rank + 1);
                }
                item.ExecutionTick += this.currentTick * this.sectorSpan;
                await this.carryOverWheel.AddInitialTime(startInterval, item).ConfigureAwait(false);
            }
            else
            {
                int nextTickTime = (int)(startInterval.TotalMilliseconds + item.ExecutionTick);
                item.ExecutionTick = nextTickTime % this.sectorSpan;
                //timerWheels.ForEach(x => { System.Diagnostics.Debug.Write($"{x.Magnitude}:{x.CurrentTick} "); });
                await this.AddItem((int)(nextTickTime / this.sectorSpan), item);
            }
        }

        private void ClearItems()
        {
            if (this.carryOverWheel != null)
            {
                this.carryOverWheel.ClearItems();
                this.carryOverWheel = null;
            }
            foreach (TimerSector<T> sector in this.sectors)
            {
                sector?.Items?.Clear();
            }
        }
        private void RemoveItem(TimerItem<T> item)
        {
            if (null != carryOverWheel)
                carryOverWheel.RemoveItem(item);
            foreach (var sector in this.sectors)
            {
                sector?.Items?.Remove(item);
            }
        }

    }
}
