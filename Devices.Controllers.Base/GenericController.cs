using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devices.Util.Extensions;
using Windows.Data.Json;

namespace Devices.Controllers.Base
{
    public class GenericController : ControllerBase
    {
        public event EventHandler<JsonObject> OnResponseReceived;

        public GenericController(string controllerName, string targetComponentName) : base(controllerName, targetComponentName)
        {
        }

        public async Task SendRequest(string action)
        {
            await Send(action).ConfigureAwait(false);
        }

        public async Task SendRequest(string action, string targetComponent)
        {
            await Send(action, targetComponent).ConfigureAwait(false);
        }

        public async Task SendRequest(JsonObject request, bool rawData = false)
        {
            if (rawData)
                await SendRaw(request).ConfigureAwait(false);
            else
                await Send(request).ConfigureAwait(false);
        }

        [TargetAction()]
        private Task HandleResponse(JsonObject data)
        {
            OnResponseReceived?.Invoke(this, data);
            return Task.CompletedTask;
        }

    }
}
