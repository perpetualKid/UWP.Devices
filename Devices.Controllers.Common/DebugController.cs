using System;
using System.Text;
using System.Threading.Tasks;
using Devices.Controllers.Base;
using Windows.Data.Json;

namespace Devices.Controllers.Common
{
    public class DebugController: ControllerBase
    {
        public event EventHandler<string> OnDataReceived;

        private StringBuilder textBuffer;

        public DebugController(string name): base(name)
        {
            textBuffer = new StringBuilder();
        }

        public string Textbuffer
        {
            get { return textBuffer.ToString(); }
        }

        [TargetAction("")]
        protected Task DataReceived(JsonObject data)
        {
            textBuffer.Insert(0, data.Stringify() + Environment.NewLine);
            OnDataReceived?.Invoke(this, data.Stringify());
            return Task.CompletedTask;
        }
    }
}
