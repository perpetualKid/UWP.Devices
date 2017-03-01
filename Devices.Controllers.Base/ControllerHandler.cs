using System;
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

        internal static event EventHandler OnConnectionUpdated;
        private static ConnectionHandler connection;

        static ControllerHandler()
        {
        }

        public static async Task<ControllerBase> RegisterController(ControllerBase controller)
        {
            List<Task> registerTasks = new List<Task>();
            registeredControllers.Add(controller.ControllerName.ToUpperInvariant(), controller);
            registerTasks.Add(controller.InitializeDefaults());
            registerTasks.Add(Task.Run(() => SetupActionHandler(controller)));
            await Task.WhenAll(registerTasks.ToArray()).ConfigureAwait(false);
            return controller;
        }

        public static Task UnregisterController(ControllerBase controller)
        {
            if (registeredControllers.ContainsKey(controller.ControllerName))
            {
                registeredControllers.Remove(controller.ControllerName);
            }
            if (catchAllActions.ContainsKey(controller.ControllerName))
                catchAllActions.Remove(controller.ControllerName);
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
            string action = (data.ContainsKey(nameof(FixedNames.Action)) ? data.GetNamedString(nameof(FixedNames.Action)) : string.Empty).ToUpperInvariant();
            if (registeredControllers.ContainsKey(sender))
            {
                ControllerBase targetController = registeredControllers[sender];
                if (targetController.actionHandlers.ContainsKey(action))
                    actions.Add(targetController.actionHandlers[action].Invoke(data));
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

                    foreach (TargetActionAttribute actionAttribute in targetAttributes)
                    {
                        if (actionAttribute.Actions == null || actionAttribute.Actions.Count() == 0) //catch all from certain target component
                        {
                            catchAllActions.Add(instance.controllerName.ToUpperInvariant(), actionHandler);
                        }
                        else //catch dedicated target/method(s)
                        { 
                            foreach (string action in actionAttribute.Actions)
                                instance.actionHandlers.Add($"{action.ToUpperInvariant()}", actionHandler);
                        }
                    }
                }
            }
        }

        #endregion

        #region ConnectionHandler
        internal static ConnectionHandler Connection
        {
            get { return connection; }
            set
            {
                connection = value;
                OnConnectionUpdated?.Invoke(value, new EventArgs());
            }
        }

        public static bool Connected
        {
            get { return Connection != null && Connection.ConnectionStatus == ConnectionStatus.Connected; }
        }

        public static ConnectionStatus ConnectionStatus { get { return (Connection == null ? ConnectionStatus.Disconnected : Connection.ConnectionStatus); } }

        public static async Task<bool> InitializeConnection(string host, string port)
        {
            return await InitializeConnection(host, port, DataFormat.Json).ConfigureAwait(false);
        }

        public static async Task<bool> InitializeConnection(string host, string port, DataFormat format)
        {
            Connection = new ConnectionHandler();
            Connection.OnConnectionStatusChanged += OnConnectionStatusChanged;
            return await Connection.Connect(host, port, format).ConfigureAwait(false);
        }

        public static event EventHandler<ConnectionStatusChangedEventArgs> OnConnectionStatusChanged;

        public static async Task Send(ControllerBase sender, JsonObject data)
        {
            await Connection.Send(sender, data).ConfigureAwait(false);
        }

        public static async Task Send(JsonObject data)
        {
            await Connection.Send(data).ConfigureAwait(false);
        }

        public static async Task Send(ControllerBase sender, string action)
        {
            await Connection.Send(sender, action).ConfigureAwait(false);
        }
        public static async Task Send(string sender, JsonObject data)
        {
            await Connection.Send(sender, data).ConfigureAwait(false);
        }

        #endregion

    }
}
