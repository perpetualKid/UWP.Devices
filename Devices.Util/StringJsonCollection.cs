using System;
using System.Collections;
using System.Collections.Generic;
using Devices.Util.Extensions;
using Windows.Data.Json;

namespace Devices.Util
{
    public class StringJsonCollection : IEnumerable<String>
    {
        private JsonArray array;

        #region .ctor
        public StringJsonCollection()
        {
            this.array = new JsonArray();
        }

        public StringJsonCollection(JsonArray array)
        {
            this.array = array;
        }

        public StringJsonCollection(IEnumerable<string> values) : this()
        {
            array = JsonExtensions.EvaluateJsonValue(values).GetArray();
        }
        #endregion

        public JsonArray JsonArray { get { return this.array; } }

        public IJsonValue this[int index]
        {
            get { return array[index]; }
            set { array[index] = value; }
        }

        public string GetAtAsString(int index)
        {
            return index < array.Count ? array[index]?.GetValueString() : null;
        }

        public void Add(string value)
        {
            array.Add(JsonExtensions.EvaluateJsonValue(value));
        }

        public void Add(IList<string> values)
        {
            foreach (string value in values)
                array.Add(JsonExtensions.EvaluateJsonValue(value));
        }

        public void Add(IJsonValue value)
        {
            array.Add(value);
        }

        public void Add(object value)
        {
            array.Add(JsonExtensions.EvaluateJsonValue(value));
        }

        public void Insert(int index, string value)
        {
            array.Insert(index, JsonExtensions.EvaluateJsonValue(value));
        }

        public void Insert(int index, IJsonValue value)
        {
            array.Insert(index, value);

        }

        public void Insert(int index, object value)
        {
            array.Insert(index, JsonExtensions.EvaluateJsonValue(value));
        }

        public int Count
        { get { return array.Count; } }

        public IEnumerator<string> GetEnumerator()
        {
            foreach (var item in array)
            {
                yield return item.GetValueString();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
