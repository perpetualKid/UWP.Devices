using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Util.Collections
{
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool suppressNotification;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!suppressNotification)
                base.OnCollectionChanged(e);
        }

        public void AddRange(IEnumerable<T> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            SuppressNotification = true;

            foreach (T item in list)
            {
                Add(item);
            }
            SuppressNotification = false;
        }

        public bool SuppressNotification
        {   get { return this.suppressNotification; }
            set
            {
                this.suppressNotification = value;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
    }
}
