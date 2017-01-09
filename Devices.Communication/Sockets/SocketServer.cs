using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Devices.Communication.Channels;
using Nito.AsyncEx;
using Windows.Networking.Sockets;

namespace Devices.Communication.Sockets
{
    public class SocketServer : SocketBase
    {
        private static Dictionary<int, SocketServer> activeSockets = new Dictionary<int, SocketServer>();

        private int port;
        private StreamSocketListener socketListener;
        private Dictionary<Guid, ChannelBase> activeSessions;
        private AsyncReaderWriterLock activeSessionsLock;

        #region static
        public static SocketServer Instance(int port)
        {
            if (!activeSockets.ContainsKey(port))
            {
                lock (activeSockets)
                {
                    SocketServer instance = new SocketServer(port);
                    activeSockets.Add(port, instance);
                }
            }
            return activeSockets[port];
        }

        public static async Task<SocketServer> RegisterChannelListener(int port, DataFormat dataFormat)
        {
            SocketServer instance = Instance(port);
            await instance.BindChannelAsync(dataFormat).ConfigureAwait(false);
            return instance;
        }
        #endregion

        #region Instance
        #region .ctor
        private SocketServer(int port)
        {
            this.port = port;
            this.activeSessions = new Dictionary<Guid, ChannelBase>();
            this.activeSessionsLock = new AsyncReaderWriterLock();
            cancellationTokenSource = new CancellationTokenSource();
        }
        #endregion

        #region public properties
        public int Port { get { return this.port; } }
        #endregion

        private async Task BindChannelAsync(DataFormat dataFormat)
        {
            if (socketListener != null)
                throw new InvalidOperationException("Only one Listner can be attached to this port.");
            try
            {
                this.ConnectionStatus = ConnectionStatus.Connecting;
                socketListener = new StreamSocketListener();
                socketListener.Control.NoDelay = true;
                socketListener.ConnectionReceived += async (streamSocketListener, streamSocketListenerConnectionReceivedEventArgs) =>
                {
                    using (IDisposable asyncLock = await activeSessionsLock.WriterLockAsync().ConfigureAwait(false))
                    {
                        ChannelBase channel = await ChannelFactory.BindChannelAsync(dataFormat, this, streamSocketListenerConnectionReceivedEventArgs.Socket).ConfigureAwait(false);
                        activeSessions.Add(channel.SessionId, channel);
                    }
                };
                await socketListener.BindServiceNameAsync(port.ToString()).AsTask().ConfigureAwait(false);
                this.ConnectionStatus = ConnectionStatus.Listening;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                this.ConnectionStatus = ConnectionStatus.Failed;
            }
        }


        public async Task StopListening()
        {
            if (socketListener != null)
            {
                CancelSocketTask();
                await socketListener.CancelIOAsync().AsTask().ConfigureAwait(false);
                socketListener.Dispose();
                socketListener = null;
                ConnectionStatus = ConnectionStatus.Disconnected;
            }
        }
        #endregion

        public override async Task Send(Guid sessionId, object data)
        {
            ChannelBase session;
            using (IDisposable asyncLock = await activeSessionsLock.ReaderLockAsync().ConfigureAwait(false))
            {

                if (activeSessions.TryGetValue(sessionId, out session))
                {
                    await session.Send(data).ConfigureAwait(false);
                }
            }
        }

        public override async Task Close()
        {
            await StopListening().ConfigureAwait(false);
        }

        public override async Task CloseSession(Guid sessionId)
        {
            ChannelBase session;
            using (IDisposable asyncLock = await activeSessionsLock.WriterLockAsync().ConfigureAwait(false))
            {

                if (activeSessions.TryGetValue(sessionId, out session))
                {
                    activeSessions.Remove(sessionId);
                    await session.Close().ConfigureAwait(false);
                }
            }
        }

    }
}
