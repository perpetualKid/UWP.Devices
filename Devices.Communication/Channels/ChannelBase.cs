using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Devices.Communication.Sockets;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Devices.Util.Parser;


namespace Devices.Communication.Channels
{
    public abstract class ChannelBase
    {
        protected SocketBase socketObject;
        protected StreamSocket streamSocket;
        protected CancellationTokenSource cancellationTokenSource;

        protected uint bytesRead;
        protected uint bytesWritten;
        private readonly DataFormat dataFormat;
        private readonly Guid sessionId;

        private ConnectionStatus connectionStatus;

        protected const int bufferSize = 512;
        private DataReaderLoadOperation loadOperation;
        protected SemaphoreSlim streamAccess;

        protected MemoryStream memoryStream;
        protected long streamReadPosition;
        protected long streamWritePosition;

        #region public events
        public event EventHandler<ConnectionStatusChangedEventArgs> OnConnectionStatusChanged;

        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;
        #endregion

        #region base
        public ChannelBase(SocketBase socket, DataFormat format)
        {
            this.socketObject = socket;
            this.dataFormat = format;
            this.sessionId = Guid.NewGuid();
        }

        internal virtual async void BindAsync(StreamSocket socketStream)
        {
            this.ConnectionStatus = ConnectionStatus.Connecting;
            this.streamSocket = socketStream;
            try
            {
                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(socketObject.CancellationTokenSource.Token);
                using (DataReader dataReader = new DataReader(socketStream.InputStream))
                {
                    CancellationToken cancellationToken = cancellationTokenSource.Token;
                    //setup
                    cancellationToken.ThrowIfCancellationRequested();
                    dataReader.InputStreamOptions = InputStreamOptions.Partial;
                    this.ConnectionStatus = ConnectionStatus.Connected;

                    //Send a Hello message across
                    await Parse("Hello" + Environment.NewLine).ConfigureAwait(false);

                    loadOperation = dataReader.LoadAsync(bufferSize);
                    uint bytesAvailable = await loadOperation.AsTask(cancellationToken).ConfigureAwait(false);
                    while (bytesAvailable > 0 && loadOperation.Status == Windows.Foundation.AsyncStatus.Completed)
                    {
                        await streamAccess.WaitAsync().ConfigureAwait(false);
                        if (streamWritePosition == streamReadPosition)
                        {
                            streamReadPosition = 0;
                            streamWritePosition = 0;
                            memoryStream.SetLength(0);
                        }
                        memoryStream.Position = streamWritePosition;
                        byte[] buffer = dataReader.ReadBuffer(bytesAvailable).ToArray();
                        await memoryStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                        streamWritePosition = memoryStream.Position;
                        streamAccess.Release();

                        await ParseStream().ConfigureAwait(false);
                        bytesRead += bytesAvailable;
                        loadOperation = dataReader.LoadAsync(bufferSize);
                        bytesAvailable = await loadOperation.AsTask(cancellationToken).ConfigureAwait(false);
                    }
                    dataReader.DetachBuffer();
                    dataReader.DetachStream();
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception exception)
            {
                socketObject.ConnectionStatus = ConnectionStatus.Failed;
                Debug.WriteLine(string.Format("Error receiving data: {0}", exception.Message));
            }
            await socketObject.CloseSession(this.sessionId).ConfigureAwait(false);
            this.ConnectionStatus = ConnectionStatus.Disconnected;
            this.OnMessageReceived -= socketObject.Instance_OnMessageReceived;
        }

        private async Task Parse(string data)
        {
            string[] message = data.GetTokens().ToArray();
            if (message.Length > 0)
                PublishMessageReceived(this, new StringMessageArgs(message));
            await Task.CompletedTask.ConfigureAwait(false);
        }


        protected abstract Task ParseStream();

        public abstract Task Send(object data);

        public virtual async Task Close()
        {
            if (null != loadOperation)
            {
                await Task.Run(() =>
                {
                    cancellationTokenSource.Cancel();
                    loadOperation.Cancel();
                    loadOperation.Close();
                }
                ).ConfigureAwait(false);
            }
        }
        #endregion

        #region public properties
        public StreamSocket StreamSocket { get { return this.streamSocket; } set { this.streamSocket = value; } }

        public DataFormat DataFormat { get { return this.dataFormat; } }

        public Guid SessionId { get { return this.sessionId; } }

        public uint BytesWritten { get { return bytesWritten; } }

        public uint BytesRead { get { return bytesRead; } }

        protected virtual void PublishMessageReceived(ChannelBase sender, MessageReceivedEventArgs eventArgs)
        {
            eventArgs.SessionId = this.sessionId;
            OnMessageReceived?.Invoke(sender, eventArgs);
        }

        public ConnectionStatus ConnectionStatus
        {
            get { return connectionStatus; }
            internal set
            {
                if (value != connectionStatus)
                {
                    connectionStatus = value;
                    OnConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs { SessionId = this.sessionId, Status = value });
                }
            }
        }

        #endregion
    }
}
