using System;
using System.Text;
using System.Threading.Tasks;
using Devices.Controllers.Base;
using Windows.Data.Json;
using Devices.Util.Extensions;

namespace Devices.Controllers.Base
{
    public class DebugHandler
    {
        public event EventHandler OnDataReceived;
        public event EventHandler OnDataSent;

        private StringBuilder dataSent;
        private StringBuilder dataReceived;
        private ConnectionHandler connection;
        private bool enabled;

        //http://www.laserbrain.se/2015/11/async-singleton-initialization/
        private static readonly Lazy<DebugHandler> instance = new Lazy<DebugHandler>(CreateDebugHandler);

        private DebugHandler()
        {
            dataReceived = new StringBuilder();
            dataSent = new StringBuilder();
            ControllerHandler.OnConnectionUpdated += ControllerHandler_OnConnectionUpdated;
            ControllerHandler_OnConnectionUpdated(this, new EventArgs());
        }

        private static DebugHandler CreateDebugHandler()
        {
            return new DebugHandler();
        }

        public static DebugHandler Instance
        {
            get { return instance.Value; }
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
//            dataSent.Insert(0, e.Stringify() + Environment.NewLine);
            dataSent.Insert(0, e.PrettyPrint() + Environment.NewLine);
            OnDataSent?.Invoke(this, new EventArgs());
        }

        private void Connection_OnJsonDataReceived(object sender, JsonObject e)
        {
//            dataReceived.Insert(0, e.Stringify() + Environment.NewLine);
            dataReceived.Insert(0, e.PrettyPrint() + Environment.NewLine);
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
