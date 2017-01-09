using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Devices.Communication.Channels;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace Devices.Communication.Sockets
{
    public class SocketClient : SocketBase
    {
        private HostName hostName;
        private StreamSocket streamSocket;
        private ChannelBase channel;


        public SocketClient()
        {
            cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task<ChannelBase> Connect(string remoteServer, string remotePort, DataFormat dataFormat)
        {
            try
            {
                ConnectionStatus = ConnectionStatus.Connecting;
                hostName = new HostName(remoteServer);
                streamSocket = new StreamSocket();
                streamSocket.Control.NoDelay = true;
                await streamSocket.ConnectAsync(hostName, remotePort).AsTask().ConfigureAwait(false);
                ConnectionStatus = ConnectionStatus.Connected;

                channel = await ChannelFactory.BindChannelAsync(dataFormat, this, streamSocket).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                ConnectionStatus = ConnectionStatus.Failed;
                Debug.WriteLine(string.Format("Error receiving data: {0}", exception.Message));
            }
            return channel;
        }

        public async Task Disconnect()
        {

            await Task.Run(() => CancelSocketTask()).ConfigureAwait(false); ;
            ConnectionStatus = ConnectionStatus.Disconnected;
        }

        public override async Task Send(Guid sessionId, object data)
        {
            await channel.Send(data).ConfigureAwait(false);
        }

        public override async Task Close()
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public override async Task CloseSession(Guid sessionId)
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
