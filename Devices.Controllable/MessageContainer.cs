using System;
using System.Collections.Generic;
using System.Linq;
using Devices.Util;
using Devices.Util.Extensions;
using Windows.Data.Json;

namespace Devices.Controllable
{
    public class MessageContainer
    {
        public enum FixedPropertyNames
        {
            Target,
            Action,
            Parameters,
        }

        private JsonObject dataObject;
        private StringJsonCollection parameters; //shortcut reference to minimize lookups

        #region public properties
        public Guid SessionId { get; private set; }

        public string Target
        {
            get { return dataObject.GetNamedString(nameof(FixedPropertyNames.Target)); }
        }
        public string Action
        {
            get { return dataObject.GetNamedString(nameof(FixedPropertyNames.Action)); }
        }

        public ControllableComponent Origin { get; private set; }

        public StringJsonCollection Parameters { get { return parameters; } }

        public JsonObject JsonData { get { return this.dataObject; } }

        #endregion

        #region public methods
        public IJsonValue GetValueByName(string name)
        {
            return dataObject.GetNamedValue(name, JsonValue.CreateNullValue());
        }

        public void AddValue(string name, object value)
        {
            dataObject.AddValue(name, value);
        }

        public void AddMultiPartValue(string name, object value)
        {
            dataObject.AddMultiPartValue(name, value);
        }

        #endregion

        #region .ctor
        private MessageContainer(Guid sessionId, ControllableComponent origin)
        {
            this.SessionId = sessionId;
            this.Origin = origin;
        }

        public MessageContainer(Guid sessionId, ControllableComponent origin, string[] data) : this(sessionId, origin)
        {
            this.dataObject = new JsonObject();
            data = ResolveParameters(data);
            parameters = new StringJsonCollection(data);
            dataObject.Add(nameof(FixedPropertyNames.Parameters), parameters.JsonArray);
        }

        private string[] ResolveParameters(string[] data)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < data.Length; i++)
            {
                string item = data[i];
                string[] names = item.Split('=');
                if (names.Length == 2) //name-value pair
                {
                    dataObject.AddValue(names[0], names[1]);
                    result.Add(names[1]);
                }
                else
                {
                    if (i == 0)
                        dataObject.AddValue(nameof(FixedPropertyNames.Target), item);
                    else if (i == 1)
                        dataObject.AddValue(nameof(FixedPropertyNames.Action), item);
                    else
                    {
                        result.Add(item);
                    }
                }
            }
            return result.ToArray();
        }

        public MessageContainer(Guid sessionId, ControllableComponent origin, JsonObject data) : this(sessionId, origin)
        {
            this.dataObject = data;
            if (data.ContainsKey(nameof(FixedPropertyNames.Parameters)))
            {
                parameters = new StringJsonCollection(data.GetNamedArray(nameof(FixedPropertyNames.Parameters)));
            }
            else
            {
                parameters = new StringJsonCollection();
                dataObject.Add(nameof(FixedPropertyNames.Parameters), parameters.JsonArray);
            }
        }
        #endregion

        public JsonObject GetJson()
        {
            JsonObject result = this.dataObject;
            result.Remove(nameof(FixedPropertyNames.Parameters));
            return result;
        }

        public IList<string> GetText()
        {
            List<string> result = new List<string>();
            foreach (var item in this.dataObject)
            {
                if (Enum.GetNames(typeof(FixedPropertyNames)).Contains(item.Key))
                    continue;
                result.Add(item.Value.GetValueString());
            }
            return result;
        }

        public void PushParameters()
        {
            if (this.dataObject.ContainsKey(nameof(FixedPropertyNames.Action)))
            {
                IJsonValue value = dataObject.GetNamedValue(nameof(FixedPropertyNames.Action));
                parameters.Insert(0, value);
            }
            if (this.dataObject.ContainsKey(nameof(FixedPropertyNames.Target)))
            {
                IJsonValue value = dataObject.GetNamedValue(nameof(FixedPropertyNames.Target));
                dataObject.SetNamedValue(nameof(FixedPropertyNames.Action), value);
            }
        }

        /// <summary>
        /// resolve the parameter by name or index
        /// first look if the parameter is found by name in the json data object itself
        /// if not found by name, try the parameter array by index 
        /// </summary>
        /// <returns></returns>        
        public string ResolveParameter(string name, int index)
        {
            if (dataObject.ContainsKey(name))
                return dataObject.GetNamedValue(name).GetValueString();
            return parameters.GetAtAsString(index) ?? string.Empty;
        }

    }
}
