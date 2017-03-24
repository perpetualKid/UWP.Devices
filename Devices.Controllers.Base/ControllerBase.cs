using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Devices.Util.Extensions;
using Windows.Data.Json;

namespace Devices.Controllers.Base
{
    public abstract class ControllerBase
    {
        protected internal string controllerName;
        protected internal string targetComponentName;

        internal Dictionary<string, ControllerActionDelegate> actionHandlers;

        public ControllerBase(string controllerName, string targetComponentName)
        {
            this.controllerName = controllerName;
            this.targetComponentName = targetComponentName;
            actionHandlers = new Dictionary<string, ControllerActionDelegate>();
        }

        public string ControllerName { get { return this.controllerName; } }

        public string TargetComponentName { get { return this.targetComponentName; } }

        protected internal virtual async Task InitializeDefaults()
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public static async Task<T> GetNamedInstance<T>(string controllerName, string targetComponentName) where T : ControllerBase
        {
            ControllerBase controller = ControllerHandler.GetByName(controllerName);
            if (null == controller)
            {
                controller = (T)Activator.CreateInstance(typeof(T), controllerName, targetComponentName);
                await ControllerHandler.RegisterController(controller).ConfigureAwait(false);
            }
            return controller as T;
        }

        protected async Task Send(string action)
        {
            JsonObject data = new JsonObject();
            data.AddValue(nameof(FixedNames.Action), action);
            data.AddValue(nameof(FixedNames.Sender), this.controllerName);
            data.AddValue(nameof(FixedNames.Target), this.targetComponentName);
            await SendRaw(data).ConfigureAwait(false);
        }

        protected async Task Send(string action, string targetComponent)
        {
            JsonObject data = new JsonObject();
            data.AddValue(nameof(FixedNames.Action), action);
            data.AddValue(nameof(FixedNames.Sender), this.controllerName);
            data.AddValue(nameof(FixedNames.Target), targetComponent);
            await SendRaw(data).ConfigureAwait(false);
        }

        protected async Task Send(JsonObject data)
        {
            data.AddValue(nameof(FixedNames.Sender), this.controllerName);
            data.AddValue(nameof(FixedNames.Target), this.targetComponentName);
            await SendRaw(data).ConfigureAwait(false);
        }

        protected async Task SendRaw(JsonObject data)
        {
            if (ControllerHandler.Connected)
            {
                await ControllerHandler.Connection.Send(data).ConfigureAwait(false);
            }
        }

    }
}
