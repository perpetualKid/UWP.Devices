using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace Devices.Communication
{
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public Guid SessionId;

        public ConnectionStatus Status;
    }

    public abstract class MessageReceivedEventArgs : EventArgs
    {
        public Guid SessionId;
    }

    public class DataReceivedEventArgs : EventArgs
    {

    }

    public class StringMessageArgs : MessageReceivedEventArgs
    {

        private string[] parameters;

        public StringMessageArgs(string[] lines)
        {
            this.parameters = lines;
        }

        public string[] Parameters { get { return parameters; } }
    }

    public class JsonMessageArgs : MessageReceivedEventArgs
    {

        private JsonObject json;

        public JsonMessageArgs(JsonObject json)
        {
            this.json = json;
        }

        public JsonObject Json { get { return json; } }
    }
}
