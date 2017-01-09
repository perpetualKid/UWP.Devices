using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Devices.Communication.Sockets;
using Devices.Util.Parser;
using Windows.Storage.Streams;

namespace Devices.Communication.Channels
{
    public class StringTextChannel : ChannelBase
    {

        #region instance
        public StringTextChannel(SocketBase socket) : base(socket, DataFormat.Text)
        {
            streamAccess = new SemaphoreSlim(1);
            memoryStream = new MemoryStream();
            this.OnMessageReceived += socketObject.Instance_OnMessageReceived;
            this.ConnectionStatus = ConnectionStatus.Disconnected;
        }


        protected override async Task ParseStream()
        {
            using (StreamReader reader = new StreamReader(memoryStream, Encoding.UTF8, true, bufferSize, true))
            {
                StringBuilder builder = new StringBuilder();
                await streamAccess.WaitAsync().ConfigureAwait(false);

                memoryStream.Position = streamReadPosition;
                string[] message = reader.GetTokens().ToArray();
                if (message.Length > 0)
                    PublishMessageReceived(this, new StringMessageArgs(message));

                streamReadPosition = memoryStream.Position;
                streamAccess.Release();
            }
        }

        public override async Task Send(object data)
        {
            IList textData;
            if (data is string)
                textData = new List<string>() { data as string };
            else
                textData = data as IList;
            if (null == textData || textData.Count == 0)
            {
                throw new FormatException("Data is invalid or empty and cannot be send as text.");
            }
            using (DataWriter writer = new DataWriter(streamSocket.OutputStream))
            {
                foreach (var line in textData)
                {
                    bytesWritten += writer.WriteString(FormatSendData(line));
                    await writer.StoreAsync();
                }
                await writer.FlushAsync();
                writer.DetachBuffer();
                writer.DetachStream();
            }
        }

        private static string FormatSendData(object data)
        {
            StringBuilder result = new StringBuilder();
            result.Append(data?.ToString());
            if (result.Length != 0)
            {
                char last = result[result.Length - 1];
                if (last != '\0' && last != '\r' && last != '\n')
                    result.AppendLine();
            }
            return result.ToString();
        }
        #endregion
    }
}
