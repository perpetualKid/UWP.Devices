using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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

        protected internal async virtual Task InitializeDefaults()
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public static async Task<T> GetNamedInstance<T>(string controllerName, string targetComponentName) where T: ControllerBase
        {
            ControllerBase controller = ControllerHandler.GetByName(controllerName);
            if (null == controller)
            {
                controller = (T)Activator.CreateInstance(typeof(T), controllerName, targetComponentName);
                await ControllerHandler.RegisterController(controller).ConfigureAwait(false);
            }
            return controller as T;
        }
    }
}
