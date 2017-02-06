using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Components
{
    public static class ComponentHandler
    {
        internal static Dictionary<string, ComponentBase> registeredComponents = new Dictionary<string, ComponentBase>();
        private static List<CommunicationComponentBase> communicationComponents = new List<CommunicationComponentBase>();

        static ComponentHandler()
        {
            RootComponent root = new RootComponent();
            registeredComponents.Add(root.ResolveName(), root);
            SetupCommandHandler(root);
        }

        public static async Task<ComponentBase> RegisterComponent(ComponentBase component)
        {
            List<Task> registerTasks = new List<Task>();
            registeredComponents.Add(component.ResolveName(), component);
            registerTasks.Add(component.InitializeDefaults());
            registerTasks.Add(Task.Run(()=> SetupCommandHandler(component)));
            if (component is CommunicationComponentBase)
                communicationComponents.Add(component as CommunicationComponentBase);
            await Task.WhenAll(registerTasks.ToArray()).ConfigureAwait(false);
            return component;
        }

        public static ComponentBase GetByName(string name)
        {
            name = name?.ToUpperInvariant();
            if (registeredComponents.ContainsKey(name))
                return registeredComponents[name];
            return null;
        }

        #region commmand processing
        public static async Task HandleInput(MessageContainer data)
        {
            string component = data.ResolveParameter(nameof(MessageContainer.FixedPropertyNames.Target), 0);
            if (string.IsNullOrEmpty(component))
                throw new ArgumentNullException("No target component specified.");
            ComponentBase instance = GetByName(component);

            if (null == instance)
            {
                data.PushParameters();  //push all parameters back, as this is starting right with the action  
                component = "Root";
                instance = GetByName(component);
            }

            if (null != instance)
            {
                string action = data.ResolveParameter(nameof(MessageContainer.FixedPropertyNames.Action), 1).ToUpperInvariant();
                ComponentActionDelegate actionHandler;
                if (instance.commandHandlers.TryGetValue(action, out actionHandler))
                    try
                    {
                        await actionHandler?.Invoke(data);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(data.Target, ex.Message + "::" + ex.StackTrace);
                    }
                else
                {
                    Debug.WriteLine($"{component} :: No handler found do for '{action}'");
                }
            }
            else
            {
                Debug.WriteLine($"{component} :: Component not found ");
            }
        }

        public static async Task HandleOutput(MessageContainer data)
        {
            List<Task> sendTasks = new List<Task>();
            foreach (CommunicationComponentBase publisher in communicationComponents)
            {
                sendTasks.Add(publisher.Respond(data));
            }
            await Task.WhenAll(sendTasks).ConfigureAwait(false);
        }

        #endregion

        private static void SetupCommandHandler(ComponentBase instance)
        {
            foreach (MethodInfo method in instance.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                IEnumerable<ActionAttribute> actionAttributes = method.GetCustomAttributes<ActionAttribute>(true);
                if (actionAttributes.Count() > 0)
                {
                    ComponentActionDelegate actionHandler = BuildActionHandlerDelegate(method, instance);

                    foreach (ActionAttribute actionAttribute in actionAttributes)
                    {
                        instance.commandHandlers.Add(actionAttribute.Action.ToUpperInvariant(), actionHandler);
                    }
                }
            }
        }

        private static ComponentActionDelegate BuildActionHandlerDelegate(MethodInfo methodInfo, ComponentBase instance)
        {
            return (ComponentActionDelegate)methodInfo.CreateDelegate(typeof(ComponentActionDelegate), instance);
        }
    }
}
