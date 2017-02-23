using System;
using System.Threading.Tasks;
using Devices.Communication;
using Devices.Communication.Channels;
using Devices.Communication.Sockets;
using Devices.Util.Extensions;
using Windows.Data.Json;

namespace Devices.Controllers.Base
{
    public class ConnectionHandler
    {

        private SocketClient socketClient;

        public event EventHandler<ConnectionStatusChangedEventArgs> OnConnectionStatusChanged;

        public ConnectionHandler()
        {
            this.socketClient = new SocketClient();
            socketClient.OnConnectionStatusChanged += SocketClient_OnConnectionStatusChanged;
        }

        private void SocketClient_OnConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            OnConnectionStatusChanged?.Invoke(this, e);
        }

        public ConnectionStatus ConnectionStatus { get { return socketClient.ConnectionStatus; } }

        public async Task<bool> Connect(string host, string port)
        {
            return await Connect(host, port, DataFormat.Json).ConfigureAwait(false);
        }

        public async Task<bool> Connect(string host, string port, DataFormat format)
        {
            if (socketClient.ConnectionStatus != ConnectionStatus.Connected)
            {
                ChannelBase channel = await socketClient.Connect(host, port, format).ConfigureAwait(false);
                if (channel?.ConnectionStatus == ConnectionStatus.Connected)
                    channel.OnMessageReceived += SocketClient_OnMessageReceived;
            }
            return socketClient.ConnectionStatus == ConnectionStatus.Connected;
        }

        public async Task Send(string sender, JsonObject data)
        {
            data.AddValue(nameof(FixedNames.Sender), sender);
            if (socketClient.ConnectionStatus == ConnectionStatus.Connected)
            {
                await socketClient.Send(Guid.Empty, data);
            }
        }

        private async void SocketClient_OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e is JsonMessageArgs)
            {
                JsonObject json = (e as JsonMessageArgs).Json;
                await ControllerHandler.HandleInput(json).ConfigureAwait(false);
            }
        }

    }
}
