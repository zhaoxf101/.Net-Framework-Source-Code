using System;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers
{
	public class WarningHeaderValue : ICloneable
	{
		private int code;

		private string agent;

		private string text;

		private DateTimeOffset? date;

		public int Code
		{
			get
			{
				return this.code;
			}
		}

		public string Agent
		{
			get
			{
				return this.agent;
			}
		}

		public string Text
		{
			get
			{
				return this.text;
			}
		}

		public DateTimeOffset? Date
		{
			get
			{
				return this.date;
			}
		}

		public WarningHeaderValue(int code, string agent, string text)
		{
			WarningHeaderValue.CheckCode(code);
			WarningHeaderValue.CheckAgent(agent);
			HeaderUtilities.CheckValidQuotedString(text, "text");
			this.code = code;
			this.agent = agent;
			this.text = text;
		}

		public WarningHeaderValue(int code, string agent, string text, DateTimeOffset date)
		{
			WarningHeaderValue.CheckCode(code);
			WarningHeaderValue.CheckAgent(agent);
			HeaderUtilities.CheckValidQuotedString(text, "text");
			this.code = code;
			this.agent = agent;
			this.text = text;
			this.date = new DateTimeOffset?(date);
		}

		private WarningHeaderValue()
		{
		}

		private WarningHeaderValue(WarningHeaderValue source)
		{
			this.code = source.code;
			this.agent = source.agent;
			this.text = source.text;
			this.date = source.date;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(this.code.ToString("000", NumberFormatInfo.InvariantInfo));
			stringBuilder.Append(' ');
			stringBuilder.Append(this.agent);
			stringBuilder.Append(' ');
			stringBuilder.Append(this.text);
			if (this.date.HasValue)
			{
				stringBuilder.Append(" \"");
				stringBuilder.Append(HttpRuleParser.DateToString(this.date.Value));
				stringBuilder.Append('"');
			}
			return stringBuilder.ToString();
		}

		public override bool Equals(object obj)
		{
			WarningHeaderValue warningHeaderValue = obj as WarningHeaderValue;
			if (warningHeaderValue == null)
			{
				return false;
			}
			if (this.code != warningHeaderValue.code || string.Compare(this.agent, warningHeaderValue.agent, StringComparison.OrdinalIgnoreCase) != 0 || string.CompareOrdinal(this.text, warningHeaderValue.text) != 0)
			{
				return false;
			}
			if (this.date.HasValue)
			{
				return warningHeaderValue.date.HasValue && this.date.Value == warningHeaderValue.date.Value;
			}
			return !warningHeaderValue.date.HasValue;
		}

		public override int GetHashCode()
		{
			int num = this.code.GetHashCode() ^ this.agent.ToLowerInvariant().GetHashCode() ^ this.text.GetHashCode();
			if (this.date.HasValue)
			{
				num ^= this.date.Value.GetHashCode();
			}
			return num;
		}

		public static WarningHeaderValue Parse(string input)
		{
			int num = 0;
			return (WarningHeaderValue)GenericHeaderParser.SingleValueWarningParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out WarningHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (GenericHeaderParser.SingleValueWarningParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (WarningHeaderValue)obj;
				return true;
			}
			return false;
		}

		internal static int GetWarningLength(string input, int startIndex, out object parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
			{
				return 0;
			}
			int num = startIndex;
			int num2;
			if (!WarningHeaderValue.TryReadCode(input, ref num, out num2))
			{
				return 0;
			}
			string text;
			if (!WarningHeaderValue.TryReadAgent(input, num, ref num, out text))
			{
				return 0;
			}
			int num3 = 0;
			int startIndex2 = num;
			if (HttpRuleParser.GetQuotedStringLength(input, num, out num3) != HttpParseResult.Parsed)
			{
				return 0;
			}
			num += num3;
			DateTimeOffset? dateTimeOffset = null;
			if (!WarningHeaderValue.TryReadDate(input, ref num, out dateTimeOffset))
			{
				return 0;
			}
			parsedValue = new WarningHeaderValue
			{
				code = num2,
				agent = text,
				text = input.Substring(startIndex2, num3),
				date = dateTimeOffset
			};
			return num - startIndex;
		}

		private static bool TryReadAgent(string input, int startIndex, ref int current, out string agent)
		{
			agent = null;
			int hostLength = HttpRuleParser.GetHostLength(input, startIndex, true, out agent);
			if (hostLength == 0)
			{
				return false;
			}
			current += hostLength;
			int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
			current += whitespaceLength;
			return whitespaceLength != 0 && current != input.Length;
		}

		private static bool TryReadCode(string input, ref int current, out int code)
		{
			code = 0;
			int numberLength = HttpRuleParser.GetNumberLength(input, current, false);
			if (numberLength == 0 || numberLength > 3)
			{
				return false;
			}
			if (!HeaderUtilities.TryParseInt32(input.Substring(current, numberLength), out code))
			{
				return false;
			}
			current += numberLength;
			int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
			current += whitespaceLength;
			return whitespaceLength != 0 && current != input.Length;
		}

		private static bool TryReadDate(string input, ref int current, out DateTimeOffset? date)
		{
			date = null;
			int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
			current += whitespaceLength;
			if (current < input.Length && input[current] == '"')
			{
				if (whitespaceLength == 0)
				{
					return false;
				}
				current++;
				int num = current;
				while (current < input.Length && input[current] != '"')
				{
					current++;
				}
				if (current == input.Length || current == num)
				{
					return false;
				}
				DateTimeOffset value;
				if (!HttpRuleParser.TryStringToDate(input.Substring(num, current - num), out value))
				{
					return false;
				}
				date = new DateTimeOffset?(value);
				current++;
				current += HttpRuleParser.GetWhitespaceLength(input, current);
			}
			return true;
		}

		object ICloneable.Clone()
		{
			return new WarningHeaderValue(this);
		}

		private static void CheckCode(int code)
		{
			if (code < 0 || code > 999)
			{
				throw new ArgumentOutOfRangeException("code");
			}
		}

		private static void CheckAgent(string agent)
		{
			if (string.IsNullOrEmpty(agent))
			{
				throw new ArgumentException(SR.net_http_argument_empty_string, "agent");
			}
			string text = null;
			if (HttpRuleParser.GetHostLength(agent, 0, true, out text) != agent.Length)
			{
				throw new FormatException(string.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, new object[]
				{
					agent
				}));
			}
		}
	}
}
