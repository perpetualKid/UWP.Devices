using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Devices.Communication;
using Windows.Data.Json;

namespace Devices.Controllers.Base
{
    public static class ControllerHandler
    {
        internal static Dictionary<string, ControllerBase> registeredControllers = new Dictionary<string, ControllerBase>();
        private static Dictionary<string, ControllerActionDelegate> catchAllActions = new Dictionary<string, ControllerActionDelegate>();

        static ControllerHandler()
        {
        }

        public static async Task<ControllerBase> RegisterController(ControllerBase controller)
        {
            List<Task> registerTasks = new List<Task>();
            registeredControllers.Add(controller.QualifiedName, controller);
            registerTasks.Add(controller.InitializeDefaults());
            registerTasks.Add(Task.Run(() => SetupActionHandler(controller)));
            await Task.WhenAll(registerTasks.ToArray()).ConfigureAwait(false);
            return controller;
        }

        public static Task UnregisterController(ControllerBase controller)
        {
            if (registeredControllers.ContainsKey(controller.QualifiedName))
            {
                registeredControllers.Remove(controller.QualifiedName);
            }
            if (catchAllActions.ContainsKey(controller.QualifiedName))
                catchAllActions.Remove(controller.QualifiedName);
            return Task.CompletedTask;
        }

        public static ControllerBase GetByName(string name)
        {
            name = name?.ToUpperInvariant();
            if (registeredControllers.ContainsKey(name))
                return registeredControllers[name];
            return null;
        }

        internal static async Task HandleInput(JsonObject data)
        {
            List<Task> actions = new List<Task>();
            foreach (ControllerActionDelegate actionHandler in catchAllActions.Values)
                actions.Add(actionHandler.Invoke(data));

            string sender = (data.ContainsKey(nameof(FixedNames.Sender)) ? data.GetNamedString(nameof(FixedNames.Sender)) : string.Empty).ToUpperInvariant();
            string target = (data.ContainsKey(nameof(FixedNames.Target)) ? data.GetNamedString(nameof(FixedNames.Target)) : string.Empty).ToUpperInvariant();
            string action = (data.ContainsKey(nameof(FixedNames.Action)) ? data.GetNamedString(nameof(FixedNames.Action)) : string.Empty).ToUpperInvariant();
            if (registeredControllers.ContainsKey(sender))
            {
                ControllerBase targetController = registeredControllers[sender];
                if (targetController.actionHandlers.ContainsKey(target))
                    actions.Add(targetController.actionHandlers[target].Invoke(data));
                target += "." + action;
                if (targetController.actionHandlers.ContainsKey(target))
                    actions.Add(targetController.actionHandlers[target].Invoke(data));
                target += "." + action;
            }
            await Task.WhenAll(actions).ConfigureAwait(false);
        }

        #region delegate setup
        private static void SetupActionHandler(ControllerBase instance)
        {
            foreach (MethodInfo method in instance.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                IEnumerable<TargetActionAttribute> targetAttributes = method.GetCustomAttributes<TargetActionAttribute>(true);
                if (targetAttributes.Count() > 0)
                {
                    ControllerActionDelegate actionHandler = (ControllerActionDelegate)method.CreateDelegate(typeof(ControllerActionDelegate), instance);

                    foreach (TargetActionAttribute targetAttribute in targetAttributes)
                    {
                        if (string.IsNullOrWhiteSpace(targetAttribute.Target))  //catch all
                        {
                            catchAllActions.Add(instance.QualifiedName, actionHandler);
                        }
                        else if (targetAttribute.Actions.Count() == 0) //catch all from certain target component
                        {
                            instance.actionHandlers.Add(targetAttribute.Target.ToUpperInvariant(), actionHandler);
                        }
                        else //catch dedicated target/method(s)
                        { 
                            foreach (string action in targetAttribute.Actions)
                                instance.actionHandlers.Add($"{targetAttribute.Target.ToUpperInvariant()}.{action.ToUpperInvariant()}", actionHandler);
                        }
                    }
                }
            }
        }

        #endregion

        #region ConnectionHandler
        public static ConnectionHandler Connection { get; set; }

        public static bool Connected
        {
            get { return Connection != null && Connection.ConnectionStatus == ConnectionStatus.Connected; }
        }

        public static async Task<bool> InitializeConnection(string host, string port)
        {
            return await InitializeConnection(host, port, DataFormat.Json).ConfigureAwait(false);
        }

        public static async Task<bool> InitializeConnection(string host, string port, DataFormat format)
        {
            Connection = new ConnectionHandler();
            return await Connection.Connect(host, port, format).ConfigureAwait(false);
        }
        #endregion

    }
}
