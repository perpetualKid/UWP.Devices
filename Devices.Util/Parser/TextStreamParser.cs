using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Devices.Util.Parser
{
    internal enum TextTokenType
    {
        Separator,
        SingleQuote,
        DoubleQoute,
        Value
    }

    internal struct TextToken
    {
        public TextToken(TextTokenType type, string value)
        {
            Value = value;
            Type = type;
        }

        public string Value { get; private set; }
        public TextTokenType Type { get; private set; }
    }

    internal class StreamTokenizer : IEnumerable<TextToken>
    {
        private TextReader reader;

        public StreamTokenizer(TextReader reader)
        {
            this.reader = reader;
        }

        public IEnumerator<TextToken> GetEnumerator()
        {
            string line;
            StringBuilder value = new StringBuilder();

            while ((line = reader.ReadLine()) != null)
            {
                foreach (char c in line)
                {
                    switch (c)
                    {
                        case '\'':
                        case '"':
                            if (value.Length > 0)
                            {
                                yield return new TextToken(TextTokenType.Value, value.ToString());
                                value.Length = 0;
                            }
                            yield return new TextToken(c == '"' ? TextTokenType.DoubleQoute : TextTokenType.SingleQuote, c.ToString());
                            break;
                        case ',':
                        case ':':
                        case ' ':
                            if (value.Length > 0)
                            {
                                yield return new TextToken(TextTokenType.Value, value.ToString());
                                value.Length = 0;
                            }
                            yield return new TextToken(TextTokenType.Separator, c.ToString());
                            break;
                        default:
                            value.Append(c);
                            break;
                    }
                }
                if (value.Length > 0)
                {
                    yield return new TextToken(TextTokenType.Value, value.ToString());
                    value.Length = 0;
                }
                yield return new TextToken(TextTokenType.Separator, string.Empty);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }


    public static class TextParserExtensions
    {
        public static IEnumerable<string> GetTokens(this StreamReader reader)
        {
            StreamTokenizer tokenizer = new StreamTokenizer(reader);
            return new TextStreamParser(tokenizer);
        }

        public static IEnumerable<string> GetTokens(this string value)
        {
            StreamTokenizer tokenizer = new StreamTokenizer(new StringReader(value));
            return new TextStreamParser(tokenizer);
        }
    }

    public class TextStreamParser : IEnumerable<String>
    {
        private StreamTokenizer tokenizer;

        internal TextStreamParser(StreamTokenizer tokenizer)
        {
            this.tokenizer = tokenizer;
        }

        public IEnumerator<string> GetEnumerator()
        {
            bool insideQuote = false;
            TextTokenType quoteType = TextTokenType.Separator;
            StringBuilder result = new StringBuilder();

            foreach (TextToken token in tokenizer)
            {
                switch (token.Type)
                {
                    case TextTokenType.Separator:
                        if (insideQuote)
                        {
                            result.Append(token.Value);
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(token.Value) || result.Length > 0)
                                yield return result.ToString();
                            result.Length = 0;
                        }
                        break;
                    case TextTokenType.SingleQuote:
                    case TextTokenType.DoubleQoute:
                        if (!insideQuote)
                        {
                            quoteType = token.Type;
                            insideQuote = true;
                            if (result.Length > 0)
                                yield return result.ToString();
                            result.Length = 0;

                        }
                        else if (token.Type == quoteType)
                        {
                            insideQuote = false;
                            quoteType = TextTokenType.Separator;
                        }
                        else
                        {
                            result.Append(token.Value);
                        }
                        break;
                    case TextTokenType.Value:
                        result.Append(token.Value);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown token type: " + token.Type);
                }
            }
            if (result.Length > 0)
            {
                yield return result.ToString();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
