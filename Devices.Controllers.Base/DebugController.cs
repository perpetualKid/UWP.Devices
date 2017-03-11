using System;
using System.Text;
using System.Threading.Tasks;
using Devices.Controllers.Base;
using Windows.Data.Json;

namespace Devices.Controllers.Base
{
    public class DebugController: ControllerBase
    {
        public event EventHandler OnDataReceived;
        public event EventHandler OnDataSent;

        private StringBuilder dataSent;
        private StringBuilder dataReceived;
        private ConnectionHandler connection;
        private bool enabled;

        private static DebugController instance;

        public DebugController(string controllerName, string targetComponentName): base(controllerName, targetComponentName)
        {
            dataReceived = new StringBuilder();
            dataSent = new StringBuilder();
            ControllerHandler.OnConnectionUpdated += ControllerHandler_OnConnectionUpdated;
            ControllerHandler_OnConnectionUpdated(this, new EventArgs());
        }

        public static DebugController Instance
        {
            get
            {
                if (null == instance)
                {
                    instance = new DebugController("DebugController", string.Empty);
                    ControllerHandler.RegisterController(instance).ConfigureAwait(false);
                }
                return instance;
            }
        }

        public bool Enabled
        {
            get { return this.enabled; }
            set
            {
                this.enabled = value;
                SubscribeDebugMessages();
            }
        }

        private void ControllerHandler_OnConnectionUpdated(object sender, EventArgs e)
        {
            connection = ControllerHandler.Connection;
            SubscribeDebugMessages();
        }

        private void SubscribeDebugMessages()
        {
            if (connection != null)
            {
                if (enabled)
                {
                    connection.OnJsonDataReceived += Connection_OnJsonDataReceived;
                    connection.OnJsonDataSend += Connection_OnJsonDataSend;
                }
                else
                {
                    connection.OnJsonDataReceived -= Connection_OnJsonDataReceived;
                    connection.OnJsonDataSend -= Connection_OnJsonDataSend;
                }
            }
        }

        private void Connection_OnJsonDataSend(object sender, JsonObject e)
        {
            dataSent.Insert(0, e.Stringify() + Environment.NewLine);
            OnDataSent?.Invoke(this, new EventArgs());
        }

        private void Connection_OnJsonDataReceived(object sender, JsonObject e)
        {
            dataReceived.Insert(0, e.Stringify() + Environment.NewLine);
            OnDataReceived?.Invoke(this, new EventArgs());
        }

        public string DataSent { get { return dataSent.ToString(); } }

        public string DataReceived { get { return dataReceived.ToString(); } }

        public void ClearSentBuffer()
        {
            dataSent.Clear();
            OnDataSent?.Invoke(this, new EventArgs());
        }

        public void ClearReceivedBuffer()
        {
            dataReceived.Clear();
            OnDataReceived?.Invoke(this, new EventArgs());
        }

        public void ClearBuffer()
        {
            ClearReceivedBuffer();
            ClearSentBuffer();
        }

    }
}
