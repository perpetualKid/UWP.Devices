using System;
using System.Threading.Tasks;
using Devices.Communication;
using Devices.Communication.Sockets;

namespace Devices.Components.Common.Communication
{
    public class SocketListener : CommunicationComponentBase
    {
        private int port;
        private DataFormat dataFormat = DataFormat.Text;
        private SocketBase instance;

        public SocketListener(int port) : base("TCP." + port.ToString())
        {
            this.port = port;
        }

        public SocketListener(int port, DataFormat dataFormat) : this(port)
        {
            this.dataFormat = dataFormat;
        }

        protected override async Task InitializeDefaults()
        {
            this.instance = await SocketServer.RegisterChannelListener(port, dataFormat);
            instance.OnMessageReceived += Server_OnMessageReceived;
        }

        public override async Task CloseChannel(Guid sessionId)
        {
            await instance.CloseSession(sessionId).ConfigureAwait(false);
        }

        private async void Server_OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e is StringMessageArgs)
            {
                await ComponentHandler.HandleInput(new MessageContainer(e.SessionId, this, (e as StringMessageArgs).Parameters));
            }
            else if (e is JsonMessageArgs)
            {
                await ComponentHandler.HandleInput(new MessageContainer(e.SessionId, this, (e as JsonMessageArgs).Json));
            }
        }

        [Action("DataFormat")]
        [Action("Format")]
        [ActionHelp("Returns the data format this channel uses.")]
        private async Task ListenerGetDataFormat(MessageContainer data)
        {
            data.AddValue("DataFormat", this.dataFormat.ToString());
            await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
        }

        [Action("Port")]
        [ActionHelp("Returns the port number for this channel.")]
        private async Task ListenerGetPort(MessageContainer data)
        {
            data.AddValue("Port", this.port);
            await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
        }

        public override async Task Respond(MessageContainer data)
        {
            switch (dataFormat)
            {
                case DataFormat.Text:
                    await instance.Send(data.SessionId, data.GetText());
                    break;
                case DataFormat.Json:
                    await instance.Send(data.SessionId, data.GetJson());
                    break;

            }
        }
    }
}
