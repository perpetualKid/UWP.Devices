using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Controllable
{
    public abstract class ControllableComponent
    {
        protected string componentName;
        protected string root;
        protected ControllableComponent parent;

        private static Dictionary<string, ControllableComponent> globalComponents = new Dictionary<string, ControllableComponent>();
        private static List<CommunicationControllable> communicationComponents;

        #region static
        static ControllableComponent()
        {
            communicationComponents = new List<CommunicationControllable>();
        }

        public static async Task<ControllableComponent> RegisterComponent(ControllableComponent component)
        {
            globalComponents.Add(component.ResolveName(), component);
            await component.InitializeDefaults().ConfigureAwait(false);
            if (component is CommunicationControllable)
                communicationComponents.Add(component as CommunicationControllable);
            return component;
        }

        public static async Task<ControllableComponent> RegisterComponent(ControllableComponent component, ControllableComponent parent)
        {
            globalComponents.Add(component.ResolveName(), component);
            await component.InitializeDefaults().ConfigureAwait(false);
            if (component is CommunicationControllable)
                communicationComponents.Add(component as CommunicationControllable);
            return component;
        }

        public static ControllableComponent GetByName(string name)
        {
            name = name?.ToUpperInvariant();
            if (globalComponents.ContainsKey(name))
                return globalComponents[name];
            return null;
        }

        protected static async Task HandleInput(MessageContainer data)
        {
            string component = data.ResolveParameter(nameof(MessageContainer.FixedPropertyNames.Target), 0).ToUpperInvariant();
            if (string.IsNullOrEmpty(component))
                throw new ArgumentNullException("No target component specified.");
            if (globalComponents.ContainsKey(component))
            {
                try
                {
                    ControllableComponent processor = globalComponents[component] as ControllableComponent;
                    await processor.ProcessCommand(data);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(data.Target, ex.Message + "::" + ex.StackTrace);
                }
            }
            else //handle locally
            {
                if (component != "." && component != "ROOT")//assume no root component name given
                    data.PushParameters();  //and push all parameters back, as this is starting right with the action  

                string action = data.ResolveParameter("Action", 0).ToUpperInvariant();
                switch (action)
                {
                    case "HELP":
                        await ListHelp(data).ConfigureAwait(false);
                        break;
                    case "HELLO":
                        await ControllableHello(data).ConfigureAwait(false);
                        break;
                    case "LIST":
                        await ControllableListComponents(data).ConfigureAwait(false);
                        break;
                    case "ECHO":
                        await ControllableEcho(data).ConfigureAwait(false);
                        break;
                    case "DATETIME":
                        await ControllableDateTime(data).ConfigureAwait(false);
                        break;
                    case "BYE":
                    case "EXIT":
                    case "CLOSE":
                        await ControllableCloseChannel(data).ConfigureAwait(false);
                        break;
                    default:
                        Debug.WriteLine("{0} :: Nothing to do for '{1}'", component, data.Target);
                        break;
                }
            }
        }

        protected static async Task HandleOutput(MessageContainer data)
        {
            List<Task> sendTasks = new List<Task>();
            foreach (CommunicationControllable publisher in communicationComponents)
            {
                sendTasks.Add(publisher.Respond(data));
            }
            await Task.WhenAll(sendTasks).ConfigureAwait(false);
        }

        private static async Task ControllableEcho(MessageContainer data)
        {
            if (data.Parameters.Count == 0)
            {
                data.AddMultiPartValue("Echo", "Nothing to echo");
            }
            else
                foreach (string value in data.Parameters)
                {
                    data.AddMultiPartValue("Echo", value);
                }
            await HandleOutput(data).ConfigureAwait(false);
        }

        private static async Task ControllableDateTime(MessageContainer data)
        {
            data.AddValue("DateTime", DateTime.Now.ToString());
            await HandleOutput(data).ConfigureAwait(false);
        }

        private static async Task ControllableHello(MessageContainer data)
        {
            data.AddMultiPartValue("Hello", "HELLO. Great to see you here.");
            data.AddMultiPartValue("Hello", "Use 'HELP + CRLF' command to get help.");
            await HandleOutput(data).ConfigureAwait(false);
        }

        public static async Task ListHelp(MessageContainer data)
        {
            data.AddMultiPartValue("Help", "HELP : Shows this help screen.");
            data.AddMultiPartValue("Help", "LIST : Lists the available modules.");
            data.AddMultiPartValue("Help", "HELLO : Returns a simple greeting message. Useful to test communication channel.");
            data.AddMultiPartValue("Help", "ECHO : Echos any text following the ECHO command.");
            data.AddMultiPartValue("Help", "DATETIME : Gets the current System Date/Time on the Device.");
            data.AddMultiPartValue("Help", "EXIT|CLOSE : Closes the currently used channel.");
            await HandleOutput(data).ConfigureAwait(false);
        }

        private static async Task ControllableCloseChannel(MessageContainer data)
        {
            data.AddValue("Close", "BYE. Hope to see you soon again.");
            await HandleOutput(data).ConfigureAwait(false);
            if (data.Origin is CommunicationControllable)
                await CloseChannel(data.Origin as CommunicationControllable, data.SessionId).ConfigureAwait(false);
        }

        private static async Task ControllableListComponents(MessageContainer data)
        {
            data.AddValue("List", await ListComponents().ConfigureAwait(false));
            await HandleOutput(data).ConfigureAwait(false);
        }

        public static async Task CloseChannel(CommunicationControllable channel, Guid sessionId)
        {
            await channel.CloseChannel(sessionId).ConfigureAwait(false);
        }

        public static async Task<IList<string>> ListComponents()
        {
            return await Task.Run(() => globalComponents.Keys.ToList()).ConfigureAwait(false);
        }
        #endregion

        #region base instance
        public ControllableComponent(string componentName)
        {
            this.componentName = componentName;
        }

        public ControllableComponent(string componentName, ControllableComponent parent)
        {
            this.componentName = componentName;
            this.parent = parent;
        }


        protected async virtual Task InitializeDefaults()
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public string ComponentName { get { return this.componentName; } }

        protected abstract Task ProcessCommand(MessageContainer data);

        protected abstract Task ComponentHelp(MessageContainer data);
        #endregion

        #region helpers
        private string ResolveName()
        {
            StringBuilder builder = new StringBuilder();
            ControllableComponent component = this;
            while (component != null)
            {
                builder.Insert(0, component.componentName.ToUpperInvariant());
                builder.Insert(0, ".");
                component = component.parent;
            }
            return builder.Remove(0, 1).ToString();
        }
        #endregion


    }
}
