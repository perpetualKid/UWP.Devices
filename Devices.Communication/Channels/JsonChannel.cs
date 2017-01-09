using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Devices.Communication.Sockets;
using Devices.Util.Parser;
using Windows.Data.Json;
using Windows.Storage.Streams;

namespace Devices.Communication.Channels
{
    public class JsonChannel : ChannelBase
    {
        public JsonChannel(SocketBase socket) : base(socket, DataFormat.Json)
        {
            streamAccess = new SemaphoreSlim(1);
            memoryStream = new MemoryStream();
            this.OnMessageReceived += socketObject.Instance_OnMessageReceived;
            this.ConnectionStatus = ConnectionStatus.Disconnected;
        }


        public override async Task Send(object data)
        {
            JsonObject jsonData = data as JsonObject;
            if (null == jsonData)
            {
                throw new FormatException("Data is invalid or empty and cannot be send as json.");
            }
            using (DataWriter writer = new DataWriter(streamSocket.OutputStream))
            {
                bytesWritten += writer.WriteString(jsonData.ToString());
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachBuffer();
                writer.DetachStream();
            }
        }

        protected override async Task ParseStream()
        {
            using (StreamReader reader = new StreamReader(memoryStream, Encoding.UTF8, true, bufferSize, true))
            {
                StringBuilder builder = new StringBuilder();
                await streamAccess.WaitAsync().ConfigureAwait(false);

                JsonStreamParser parser = new JsonStreamParser(reader, streamReadPosition);

                memoryStream.Position = streamReadPosition;
                foreach (JsonObject item in parser)
                {
                    PublishMessageReceived(this, new JsonMessageArgs(item));
                }

                streamReadPosition = parser.ReadPosition;
                streamAccess.Release();
            }
        }

    }
}
