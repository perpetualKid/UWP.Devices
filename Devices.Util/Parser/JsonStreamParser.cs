using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace Devices.Util.Parser
{
    internal enum JsonTokenType
    {
        StartArray,
        StartObject,
        Quote,
        Value,
        EndArray,
        EndObject,
    }

    internal struct JsonToken
    {
        public JsonToken(JsonTokenType type, string value)
        {
            Value = value;
            Type = type;
        }

        public string Value { get; private set; }

        public JsonTokenType Type { get; private set; }
    }

    internal class JsonStreamTokenizer : IEnumerable<JsonToken>
    {
        private TextReader reader;

        public JsonStreamTokenizer(TextReader reader)
        {
            this.reader = reader;
        }

        public IEnumerator<JsonToken> GetEnumerator()
        {
            StringBuilder value = new StringBuilder();
            int c;
            while ((c = reader.Read()) >= 0)
            {
                switch (c)
                {
                    case '"':
                        if (value.Length > 0)
                        {
                            yield return new JsonToken(JsonTokenType.Value, value.ToString());
                            value.Length = 0;
                        }
                        yield return new JsonToken(JsonTokenType.Quote, ((char)c).ToString());
                        break;
                    case '{':
                        if (value.Length > 0)
                        {
                            yield return new JsonToken(JsonTokenType.Value, value.ToString());
                            value.Length = 0;
                        }
                        yield return new JsonToken(JsonTokenType.StartObject, ((char)c).ToString());
                        break;
                    case '}':
                        if (value.Length > 0)
                        {
                            yield return new JsonToken(JsonTokenType.Value, value.ToString());
                            value.Length = 0;
                        }
                        yield return new JsonToken(JsonTokenType.EndObject, ((char)c).ToString());
                        break;
                    case '[':
                        if (value.Length > 0)
                        {
                            yield return new JsonToken(JsonTokenType.Value, value.ToString());
                            value.Length = 0;
                        }
                        yield return new JsonToken(JsonTokenType.StartArray, ((char)c).ToString());
                        break;
                    case ']':
                        if (value.Length > 0)
                        {
                            yield return new JsonToken(JsonTokenType.Value, value.ToString());
                            value.Length = 0;
                        }
                        yield return new JsonToken(JsonTokenType.EndArray, ((char)c).ToString());
                        break;
                    default:
                        value.Append((char)c);
                        break;
                }
            }
            if (value.Length > 0)
            {
                yield return new JsonToken(JsonTokenType.Value, value.ToString());
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class JsonStreamParser : IEnumerable<JsonObject>
    {
        private JsonStreamTokenizer tokenizer;
        private long position;

        public JsonStreamParser(string value, long position = 0) :
            this(new StringReader(value), position)
        {
        }

        public JsonStreamParser(TextReader reader, long position = 0)
        {
            this.tokenizer = new JsonStreamTokenizer(reader);
            this.position = position;
        }

        public IEnumerator<JsonObject> GetEnumerator()
        {
            Stack<JsonTokenType> tokenStack = new Stack<JsonTokenType>();
            bool insideQuote = false;
            StringBuilder result = new StringBuilder();

            foreach (JsonToken token in tokenizer)
            {
                result.Append(token.Value);
                switch (token.Type)
                {
                    case JsonTokenType.Quote:
                        insideQuote = !insideQuote;
                        break;
                    case JsonTokenType.Value:
                        break;
                    case JsonTokenType.StartObject:
                        tokenStack.Push(JsonTokenType.StartObject);
                        break;
                    case JsonTokenType.StartArray:
                        tokenStack.Push(JsonTokenType.StartArray);
                        break;
                    case JsonTokenType.EndObject:
                        if (tokenStack.Pop() != JsonTokenType.StartObject)
                            throw new InvalidDataException("Error in stream data, matching element not found.");
                        if ((tokenStack.Count == 0) || (tokenStack.Count == 1 && tokenStack.Peek() == JsonTokenType.StartArray))
                        {
                            position += result.Length;
                            if (result.Length > 0)
                                yield return JsonObject.Parse(result.ToString());
                            result.Length = 0;
                        }
                        break;
                    case JsonTokenType.EndArray:
                        if (tokenStack.Pop() != JsonTokenType.StartArray)
                            throw new InvalidDataException("Error in stream data, matching element not found.");
                        if (tokenStack.Count == 0)
                        {
                            position += result.Length;
                            if (result.Length > 0)
                                yield return JsonObject.Parse(result.ToString());
                            result.Length = 0;
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Unknown token type: " + token.Type);
                }
            }
        }

        public long ReadPosition
        {
            get { return position; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
