using System;
using System.Text;
using System.Threading.Tasks;
using Devices.Controllers.Base;
using Windows.Data.Json;

namespace Devices.Controllers.Base
{
    public class DebugController: ControllerBase
    {
        public event EventHandler<string> OnDataReceived;

        private StringBuilder textBuffer;
        private ConnectionHandler connection;

        public DebugController(string name): base(name, string.Empty)
        {
            textBuffer = new StringBuilder();
            ControllerHandler.OnConnectionUpdated += ControllerHandler_OnConnectionUpdated;
        }

        private void ControllerHandler_OnConnectionUpdated(object sender, EventArgs e)
        {
            if (connection != null)
            {
                connection.OnJsonDataReceived -= Connection_OnJsonDataReceived;
                connection.OnJsonDataSend -= Connection_OnJsonDataSend;
            }
            if (ControllerHandler.Connection != null)
            {
                ControllerHandler.Connection.OnJsonDataReceived += Connection_OnJsonDataReceived;
                ControllerHandler.Connection.OnJsonDataSend += Connection_OnJsonDataSend;
            }
            connection = ControllerHandler.Connection;

        }

        private void Connection_OnJsonDataSend(object sender, JsonObject e)
        {
        }

        private void Connection_OnJsonDataReceived(object sender, JsonObject e)
        {
        }

        public string Textbuffer
        {
            get { return textBuffer.ToString(); }
        }

        [TargetAction()]
        protected Task DataReceived(JsonObject data)
        {
            textBuffer.Insert(0, data.Stringify() + Environment.NewLine);
            OnDataReceived?.Invoke(this, data.Stringify());
            return Task.CompletedTask;
        }
    }
}
