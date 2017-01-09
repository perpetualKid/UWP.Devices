using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devices.Communication.Sockets;
using Windows.Networking.Sockets;

namespace Devices.Communication.Channels
{
    static class ChannelFactory
    {
        public static async Task<ChannelBase> BindChannelAsync(DataFormat dataFormat, SocketBase host, StreamSocket socketStream)
        {
            ChannelBase channel = null;
            switch (dataFormat)
            {
                case DataFormat.Text:
                    channel = new StringTextChannel(host);
                    channel.BindAsync(socketStream);
                    break;
                case DataFormat.Json:
                    channel = new JsonChannel(host);
                    channel.BindAsync(socketStream);
                    break;
                default:
                    await Task.CompletedTask.ConfigureAwait(false);
                    break;
            }
            return channel;
        }
    }
}
