using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Controllers.Base
{
    public abstract class ControllerBase
    {
        protected internal string controllerName;
        protected internal ControllerBase parent;
        private string fqControllerName;

        internal Dictionary<string, ControllerActionDelegate> actionHandlers;

        public ControllerBase(string controllerName)
        {
            this.controllerName = controllerName;
            actionHandlers = new Dictionary<string, ControllerActionDelegate>();
        }

        public ControllerBase(string controllerName, ControllerBase parent) : this(controllerName)
        {
            this.parent = parent;
        }

        protected internal async virtual Task InitializeDefaults()
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public static async Task<T> GetNamedInstance<T>(string name) where T: ControllerBase
        {
            ControllerBase controller = ControllerHandler.GetByName(name);
            if (null == controller)
            {
                controller = (T)Activator.CreateInstance(typeof(T), name);
                await ControllerHandler.RegisterController(controller).ConfigureAwait(false);
            }
            return controller as T;
        }

        #region helpers
        internal string QualifiedName
        {
            get
            {
                if (string.IsNullOrEmpty(fqControllerName))
                {
                    StringBuilder builder = new StringBuilder();
                    ControllerBase controller = this;
                    while (controller != null)
                    {
                        builder.Insert(0, controller.controllerName.ToUpperInvariant());
                        builder.Insert(0, ".");
                        controller = controller.parent;
                    }
                    fqControllerName = builder.Remove(0, 1).ToString();
                }
                return fqControllerName;
            }
        }
        #endregion

    }
}
