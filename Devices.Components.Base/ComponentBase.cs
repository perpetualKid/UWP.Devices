using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Devices.Components
{
    public abstract class ComponentBase
    {
        private const string constHelp = "Help";
        protected internal string componentName;
        protected internal ComponentBase parent;

        internal List<Tuple<string, string, string>> componentHelp;
        internal Dictionary<string, ComponentActionDelegate> commandHandlers;

        public ComponentBase(string componentName)
        {
            this.componentName = componentName;
            commandHandlers = new Dictionary<string, ComponentActionDelegate>();
        }

        public ComponentBase(string componentName, ComponentBase parent) : this(componentName)
        {
            this.parent = parent;
        }

        protected internal async virtual Task InitializeDefaults()
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [Action("Help")]
        [Action("?")]
        [ActionHelp("Shows this help screen.")]
        protected async Task ComponentHelp(MessageContainer data)
        {
            if (null == componentHelp)
            {
                componentHelp = new List<Tuple<string, string, string>>();
                data.AddValue(constHelp, "Preparing help text.");
                Task sendWaitMessage = ComponentHandler.HandleOutput(data);
                BuildHelpCache();
                await sendWaitMessage.ConfigureAwait(false);
                data.ClearValue(constHelp);
            }
            foreach (Tuple<string, string, string> helpText in this.componentHelp)
            {
                data.AddMultiPartValue(constHelp, $"{componentName} {helpText.Item1} {helpText.Item2}: {helpText.Item3}");
            }
            await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
        }

        public string ComponentName { get { return this.componentName; } }

        #region helpers
        internal string ResolveName()
        {
            StringBuilder builder = new StringBuilder();
            ComponentBase component = this;
            while (component != null)
            {
                builder.Insert(0, component.componentName.ToUpperInvariant());
                builder.Insert(0, ".");
                component = component.parent;
            }
            return builder.Remove(0, 1).ToString();
        }

        private void BuildHelpCache()
        {
            foreach (MethodInfo method in this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                ActionHelpAttribute helpAttribute = method.GetCustomAttribute<ActionHelpAttribute>(true);
                if (helpAttribute != null)
                {
                    IEnumerable<ActionAttribute> actionAttributes = method.GetCustomAttributes<ActionAttribute>(true);
                    StringBuilder actionBuilder = new StringBuilder();
                    foreach(ActionAttribute actionAttribute in actionAttributes)
                    {
                        actionBuilder.Append("|");
                        actionBuilder.Append(actionAttribute.Action);
                    }
                    if (actionBuilder.Length > 0)
                        actionBuilder.Remove(0, 1);
                    StringBuilder parameterBuilder = new StringBuilder();
                    IEnumerable<ActionParameterAttribute> paramAttributes = method.GetCustomAttributes<ActionParameterAttribute>(true);
                    foreach(ActionParameterAttribute paramAttribute in paramAttributes)
                    {
                        parameterBuilder.Append(paramAttribute.Required? $"<{paramAttribute.ParameterName}>" : $"[<{paramAttribute.ParameterName}>]");
                        parameterBuilder.Append(" ");
                    }
                    componentHelp.Add(new Tuple<string, string, string>(actionBuilder.ToString(), parameterBuilder.ToString(), helpAttribute.HelpText));
                }
            }
        }

        #endregion

    }
}
