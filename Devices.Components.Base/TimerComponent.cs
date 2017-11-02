using Devices.Util.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devices.Util.Parser;

namespace Devices.Components
{
    public class TimerComponent : ComponentBase
    {
        private CascadingWheelTimer<Tuple<ComponentActionDelegate, MessageContainer>> timerWheel;

        public TimerComponent() : base("Timer")
        {
            timerWheel = new CascadingWheelTimer<Tuple<ComponentActionDelegate, MessageContainer>>(20, 50);
            timerWheel.Start();
        }

        [Action("Register")]
        [ActionHelp("Adds a new timer task.")]
        [ActionParameter("Interval", ParameterType = typeof(int), Required = true)]
        [ActionParameter("Repetitions", ParameterType = typeof(int), Required = false)]
        [ActionParameter("TimerAction", ParameterType = typeof(string), Required = true)]
        private async Task RegisterTimerTask(MessageContainer data)
        {
            int param;
            TimerItem<Tuple<ComponentActionDelegate, MessageContainer>> timerItem = new TimerItem<Tuple<ComponentActionDelegate, MessageContainer>>();
            if (!int.TryParse(data.ResolveParameter("Interval", 0), out param))
                param = 1000;
            timerItem.Interval = TimeSpan.FromMilliseconds(param);
            if (!int.TryParse(data.ResolveParameter("Repetitions", 1), out param))
                param = 0;
            timerItem.Repetition = param;
            string timerAction = data.ResolveParameter("TimerAction", 2);
            string[] actionParams = timerAction.GetTokens().ToArray();
            MessageContainer timerActionData = new MessageContainer(data.SessionId, this, actionParams);

            ComponentActionDelegate actionDelegate = ComponentHandler.ResolveCommandHandler(timerActionData);
            if (null == actionDelegate || actionParams == null || actionParams.Length == 0)
            {
                data.AddValue("Register", "Could not parse task action. Please check the command provided.");
                await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
                return;
            }
            timerItem.Data = new Tuple<ComponentActionDelegate, MessageContainer>(actionDelegate, timerActionData); 
            timerItem.TimerAction += TimerAction;
            await timerWheel.Add(timerItem).ConfigureAwait(false);

            data.AddValue("Register", $"Task succcessfully registered with ID {timerItem.TimerItemId}");
            await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
        }

        [Action("List")]
        [Action("ListTimers")]
        [ActionHelp("Gets a lists of currently active timers.")]
        private async Task GetActiveTimers(MessageContainer data)
        {
            TimerItem<Tuple<ComponentActionDelegate, MessageContainer>>[] activeTimers = timerWheel.GetActiveTimerItems();
            foreach (TimerItem<Tuple<ComponentActionDelegate, MessageContainer>> item in activeTimers)
            {
                data.AddMultiPartValue("TimerItem", $"TimerItemId :: {item.TimerItemId.ToString()} Interval :: {item.Interval.ToString()} RemainingRepetitions :: {item.Repetition} ActionCommand :: {item.Data.Item2.GetJson().Stringify()}");
            }
            await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
        }

        [Action("Clear")]
        [ActionHelp("Removes all current timers.")]
        private async Task ClearActiveTimers(MessageContainer data)
        {
            await timerWheel.Clear().ConfigureAwait(false);
        }

        [Action("Remove")]
        [ActionHelp("Removes a specific timer action.")]
        [ActionParameter("TimerId", ParameterType = typeof(string), Required = true)]
        private async Task RemoveTimerItem(MessageContainer data)
        {
            if (!Guid.TryParse(data.ResolveParameter("TimerId", 0), out Guid param))
            {
                data.AddValue("Remove", "No valid timer id given");
                await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
                return;
            }
            await timerWheel.RemoveTimerItem(param).ConfigureAwait(false);
            data.AddValue("Remove", $"Task {param} succcessfully removed.");
            await ComponentHandler.HandleOutput(data).ConfigureAwait(false);
        }

        private async void TimerAction(TimerItem<Tuple<ComponentActionDelegate, MessageContainer>> item)
        {
            await ComponentHandler.ExecuteCommand(item.Data.Item1, item.Data.Item2.Clone()).ConfigureAwait(false);
        }

    }
}
