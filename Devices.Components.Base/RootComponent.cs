using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace Devices.Components
{
    public class RootComponent: ComponentBase
    {
        internal RootComponent(): base("Root")
        {
        }

        [Action("List")]
        [ActionHelp("Lists the available modules.")]
        private async Task ListComponents(MessageContainer data)
        {
            data.AddValue("List", await ListComponents().ConfigureAwait(false));
            await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
        }

        [Action("Hello")]
        [ActionHelp("Returns a simple greeting message. Useful to test communication channel.")]
        private async Task SendHello(MessageContainer data)
        {
            data.AddMultiPartValue("Hello", "Hello. Great to see you here.");
            data.AddMultiPartValue("Hello", "Use 'Help + CRLF or Enter' command to get help.");
            await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
        }

        [Action("DateTime")]
        [ActionHelp("Gets the current System Date and Time on the Device.")]
        private async Task SendDateTime(MessageContainer data)
        {
            data.AddValue("DateTime", DateTime.Now.ToString());
            await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
        }

        [Action("Echo")]
        [ActionParameter("EchoText", ParameterType = typeof(string))]
        [ActionHelp("Echos any text following the ECHO command.")]
        private async Task SendEcho(MessageContainer data)
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
            await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
        }

        [Action("Exit")]
        [Action("Bye")]
        [Action("Close")]
        [ActionHelp("Closes the currently used channel.")]
        private async Task CloseChannel(MessageContainer data)
        {
            data.AddValue("Close", "BYE. Hope to see you soon again.");
            await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
            if (data.Origin is CommunicationComponentBase)
                await CloseChannel(data.Origin as CommunicationComponentBase, data.SessionId).ConfigureAwait(false);
        }

        [Action("Shutdown")]
        [ActionParameter("Restart", ParameterType = typeof(bool), Required = false)]
        [ActionParameter("Timeout", ParameterType = typeof(int), Required = false)]
        [ActionHelp("Shutting down the system. Once shut down, need to manually restart the system")]
        private async Task Shutdown(MessageContainer data)
        {
            bool.TryParse(data.ResolveParameter("Restart", 0), out bool restart);
            if (!int.TryParse(data.ResolveParameter("Timeout", 1), out int timeout))
                timeout = 10;
            if (restart)
                data.AddValue("Restart", $"Restarting the system in {timeout}sec. We will be back online in a moment.");
            else
                data.AddValue("Shutdown", $"Shutting down the system in {timeout}sec. Hope to be back soon again.");

            await Shutdown(restart, timeout);

            await ComponentHandler.HandleOutput(data).ConfigureAwait(false);

            if (data.Origin is CommunicationComponentBase)
                await CloseChannel(data.Origin as CommunicationComponentBase, data.SessionId).ConfigureAwait(false);
        }

        public static Task<IList<string>> ListComponents()
        {
            return Task.FromResult<IList<string>>(ComponentHandler.registeredComponents.Keys.ToList());
        }

        public static async Task CloseChannel(CommunicationComponentBase channel, Guid sessionId)
        {
            await channel.CloseChannel(sessionId).ConfigureAwait(false);
        }

        public static Task Shutdown(bool restart, int timeout)
        {
            ShutdownManager.BeginShutdown(restart ? ShutdownKind.Restart : ShutdownKind.Shutdown, TimeSpan.FromSeconds(timeout));
            return Task.CompletedTask;
        }

    }
}
