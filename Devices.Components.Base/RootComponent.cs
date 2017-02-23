using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static Task<IList<string>> ListComponents()
        {
            return Task.FromResult<IList<string>>(ComponentHandler.registeredComponents.Keys.ToList());
        }

        public static async Task CloseChannel(CommunicationComponentBase channel, Guid sessionId)
        {
            await channel.CloseChannel(sessionId).ConfigureAwait(false);
        }

    }
}
