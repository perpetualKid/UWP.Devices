using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devices.Communication;
using Devices.Communication.Sockets;
using Devices.Controllable;

namespace Devices.Components.Common.Communication
{
    public class SocketListener : CommunicationControllable
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
                await HandleInput(new MessageContainer(e.SessionId, this, (e as StringMessageArgs).Parameters));
            else if (e is JsonMessageArgs)
                await HandleInput(new MessageContainer(e.SessionId, this, (e as JsonMessageArgs).Json));
        }

        protected override async Task ComponentHelp(MessageContainer data)
        {
            data.AddMultiPartValue("Help", "LISTENER HELP : Shows this help screen.");
            data.AddMultiPartValue("Help", "LISTENER DATAFORMAT : Returns the data format this channel uses.");
            data.AddMultiPartValue("Help", "LISTENER PORT : Returns the port number for this channel.");
            await HandleOutput(data).ConfigureAwait(false);
        }


        protected override async Task ProcessCommand(MessageContainer data)
        {
            switch (data.ResolveParameter(nameof(MessageContainer.FixedPropertyNames.Action), 1).ToUpperInvariant())
            {
                case "HELP":
                    await ComponentHelp(data).ConfigureAwait(false);
                    break;
                case "FORMAT":
                case "DATAFORMAT":
                    await ListenerGetDataFormat(data).ConfigureAwait(false);
                    break;
                case "PORT":
                    await ListenerGetPort(data).ConfigureAwait(false);
                    break;
            }
        }

        private async Task ListenerGetDataFormat(MessageContainer data)
        {
            data.AddValue("DataFormat", this.dataFormat.ToString());
            await HandleOutput(data).ConfigureAwait(false);
        }

        private async Task ListenerGetPort(MessageContainer data)
        {
            data.AddValue("Port", this.port);
            await HandleOutput(data).ConfigureAwait(false);
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
