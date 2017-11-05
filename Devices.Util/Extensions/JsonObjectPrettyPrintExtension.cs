using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace Devices.Util.Extensions
{
    public static class JsonObjectPrettyPrintExtension
    {

        public static string PrettyPrint(this JsonObject jsonObject)
        {
            StringBuilder builder = new StringBuilder();
            PrettyPrintValue(builder, jsonObject, 0);
            builder.Remove(builder.Length - 3, 3);
            return builder.ToString();
        }

        private static void PrettyPrintValue(StringBuilder builder, IJsonValue jsonValue, int indent)
        {

            switch (jsonValue.ValueType)
            {
                case JsonValueType.String:
                    builder.Append($"\"{jsonValue.GetString()}\"");
                    break;
                case JsonValueType.Boolean:
                    builder.Append($"{jsonValue.GetBoolean()}");
                    break;
                case JsonValueType.Number:
                    builder.Append($"{jsonValue.GetNumber()}");
                    break;
                case JsonValueType.Null:
                    builder.Append($"null");
                    break;
                case JsonValueType.Object:
                    builder.Append("{\r\n");
                    indent++;
                    foreach(var item in jsonValue.GetObject()) //doesn't preserve order
                    {
                        Tabs(builder, indent);
                        builder.Append($"\"{item.Key}\": ");
                        PrettyPrintValue(builder, (item.Value), indent);
                    }
                    RemoveLastComma(builder);
                    indent--;
                    Tabs(builder, indent);
                    builder.Append("}");
                    break;
                case JsonValueType.Array:
                    builder.Append("[\r\n");
                    indent++;
                    foreach (IJsonValue value in jsonValue.GetArray())
                    {
                        Tabs(builder, indent);
                        PrettyPrintValue(builder, value, indent);
                    }
                    RemoveLastComma(builder);
                    indent--;
                    Tabs(builder, indent);
                    builder.Append("]");
                    break;
            }
            builder.Append(",\r\n");
        }

        private static void RemoveLastComma(StringBuilder builder)
        {
            builder.Remove(builder.Length - 3, 1);
        }

        private static void Tabs(StringBuilder builder, int count)
        {
            while (count-- > 0) builder.Append("\t");
        }
    }
}
