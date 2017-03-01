using System;
using System.Text;
using System.Threading.Tasks;
using Devices.Controllers.Base;
using Windows.Data.Json;

namespace Devices.Controllers.Base
{
    public class DebugController: ControllerBase
    {
        public event EventHandler OnReceivedTextUpdated;

        public event EventHandler OnSentTextUpdated;

        private StringBuilder textSent;
        private StringBuilder textReceived;
        private ConnectionHandler connection;

        public DebugController(string controllerName, string targetComponentName): base(controllerName, targetComponentName)
        {
            textReceived = new StringBuilder();
            textSent = new StringBuilder();
            ControllerHandler.OnConnectionUpdated += ControllerHandler_OnConnectionUpdated;
            ControllerHandler_OnConnectionUpdated(this, new EventArgs());
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
            textSent.Insert(0, e.Stringify() + Environment.NewLine);
            OnSentTextUpdated?.Invoke(this, new EventArgs());
        }

        private void Connection_OnJsonDataReceived(object sender, JsonObject e)
        {
            textReceived.Insert(0, e.Stringify() + Environment.NewLine);
            OnReceivedTextUpdated?.Invoke(this, new EventArgs());
        }

        public string TextSent { get { return textSent.ToString(); } }

        public string TextReceived { get { return textReceived.ToString(); } }

        public void ClearSentBuffer()
        {
            textSent.Clear();
            OnSentTextUpdated?.Invoke(this, new EventArgs());
        }

        public void ClearReceivedBuffer()
        {
            textReceived.Clear();
            OnReceivedTextUpdated?.Invoke(this, new EventArgs());
        }

        public void ClearBuffer()
        {
            ClearReceivedBuffer();
            ClearSentBuffer();
        }

        [TargetAction()]
        protected Task DataReceived(JsonObject data)
        {
            return Task.CompletedTask;
        }
    }
}
