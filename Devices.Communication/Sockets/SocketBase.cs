using System;
using System.Threading;
using System.Threading.Tasks;

namespace Devices.Communication.Sockets
{
    public abstract class SocketBase
    {
        protected object cancelLock = new Object();
        protected CancellationTokenSource cancellationTokenSource;
        protected ConnectionStatus connectionStatus;

        public event EventHandler<ConnectionStatusChangedEventArgs> OnConnectionStatusChanged;

        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;

        public void Instance_OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            OnMessageReceived?.Invoke(sender, e);
        }


        public void CancelSocketTask()
        {
            lock (cancelLock)
            {
                if ((cancellationTokenSource != null) && (!cancellationTokenSource.IsCancellationRequested))
                {
                    cancellationTokenSource.Cancel();
                    // Existing IO already has a local copy of the old cancellation token so this reset won't affect it 
                    ResetCancellationTokenSource();
                }
            }
        }

        public void ResetCancellationTokenSource()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Dispose();
            }
            // Create a new cancellation token source so that can cancel all the tokens again 
            cancellationTokenSource = new CancellationTokenSource();
            // Hook the cancellation callback (called whenever Task.cancel is called) 
            //cancellationTokenSource.Token.Register(() => NotifyCancelingTask()); 
        }

        public CancellationTokenSource CancellationTokenSource { get { return cancellationTokenSource; } }

        public ConnectionStatus ConnectionStatus
        {
            get { return connectionStatus; }
            internal set
            {
                if (value != connectionStatus)
                {
                    connectionStatus = value;
                    OnConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs { Status = value });
                }
            }
        }

        public abstract Task Send(Guid sessionId, object data);

        public abstract Task Close();

        public abstract Task CloseSession(Guid sessionId);
    }
}
