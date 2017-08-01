using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers
{
	public class NameValueHeaderValue : ICloneable
	{
		private static readonly Func<NameValueHeaderValue> defaultNameValueCreator = new Func<NameValueHeaderValue>(NameValueHeaderValue.CreateNameValue);

		private string name;

		private string value;

		public string Name
		{
			get
			{
				return this.name;
			}
		}

		public string Value
		{
			get
			{
				return this.value;
			}
			set
			{
				NameValueHeaderValue.CheckValueFormat(value);
				this.value = value;
			}
		}

		internal NameValueHeaderValue()
		{
		}

		public NameValueHeaderValue(string name) : this(name, null)
		{
		}

		public NameValueHeaderValue(string name, string value)
		{
			NameValueHeaderValue.CheckNameValueFormat(name, value);
			this.name = name;
			this.value = value;
		}

		protected NameValueHeaderValue(NameValueHeaderValue source)
		{
			this.name = source.name;
			this.value = source.value;
		}

		public override int GetHashCode()
		{
			int hashCode = this.name.ToLowerInvariant().GetHashCode();
			if (string.IsNullOrEmpty(this.value))
			{
				return hashCode;
			}
			if (this.value[0] == '"')
			{
				return hashCode ^ this.value.GetHashCode();
			}
			return hashCode ^ this.value.ToLowerInvariant().GetHashCode();
		}

		public override bool Equals(object obj)
		{
			NameValueHeaderValue nameValueHeaderValue = obj as NameValueHeaderValue;
			if (nameValueHeaderValue == null)
			{
				return false;
			}
			if (string.Compare(this.name, nameValueHeaderValue.name, StringComparison.OrdinalIgnoreCase) != 0)
			{
				return false;
			}
			if (string.IsNullOrEmpty(this.value))
			{
				return string.IsNullOrEmpty(nameValueHeaderValue.value);
			}
			if (this.value[0] == '"')
			{
				return string.CompareOrdinal(this.value, nameValueHeaderValue.value) == 0;
			}
			return string.Compare(this.value, nameValueHeaderValue.value, StringComparison.OrdinalIgnoreCase) == 0;
		}

		public static NameValueHeaderValue Parse(string input)
		{
			int num = 0;
			return (NameValueHeaderValue)GenericHeaderParser.SingleValueNameValueParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out NameValueHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (GenericHeaderParser.SingleValueNameValueParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (NameValueHeaderValue)obj;
				return true;
			}
			return false;
		}

		public override string ToString()
		{
			if (!string.IsNullOrEmpty(this.value))
			{
				return this.name + "=" + this.value;
			}
			return this.name;
		}

		internal static void ToString(ICollection<NameValueHeaderValue> values, char separator, bool leadingSeparator, StringBuilder destination)
		{
			if (values == null || values.Count == 0)
			{
				return;
			}
			foreach (NameValueHeaderValue current in values)
			{
				if (leadingSeparator || destination.Length > 0)
				{
					destination.Append(separator);
					destination.Append(' ');
				}
				destination.Append(current.ToString());
			}
		}

		internal static string ToString(ICollection<NameValueHeaderValue> values, char separator, bool leadingSeparator)
		{
			if (values == null || values.Count == 0)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder();
			NameValueHeaderValue.ToString(values, separator, leadingSeparator, stringBuilder);
			return stringBuilder.ToString();
		}

		internal static int GetHashCode(ICollection<NameValueHeaderValue> values)
		{
			if (values == null || values.Count == 0)
			{
				return 0;
			}
			int num = 0;
			foreach (NameValueHeaderValue current in values)
			{
				num ^= current.GetHashCode();
			}
			return num;
		}

		internal static int GetNameValueLength(string input, int startIndex, out NameValueHeaderValue parsedValue)
		{
			return NameValueHeaderValue.GetNameValueLength(input, startIndex, NameValueHeaderValue.defaultNameValueCreator, out parsedValue);
		}

		internal static int GetNameValueLength(string input, int startIndex, Func<NameValueHeaderValue> nameValueCreator, out NameValueHeaderValue parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
			{
				return 0;
			}
			int tokenLength = HttpRuleParser.GetTokenLength(input, startIndex);
			if (tokenLength == 0)
			{
				return 0;
			}
			string text = input.Substring(startIndex, tokenLength);
			int num = startIndex + tokenLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			if (num == input.Length || input[num] != '=')
			{
				parsedValue = nameValueCreator();
				parsedValue.name = text;
				num += HttpRuleParser.GetWhitespaceLength(input, num);
				return num - startIndex;
			}
			num++;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			int valueLength = NameValueHeaderValue.GetValueLength(input, num);
			if (valueLength == 0)
			{
				return 0;
			}
			parsedValue = nameValueCreator();
			parsedValue.name = text;
			parsedValue.value = input.Substring(num, valueLength);
			num += valueLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			return num - startIndex;
		}

		internal static int GetNameValueListLength(string input, int startIndex, char delimiter, ICollection<NameValueHeaderValue> nameValueCollection)
		{
			if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
			{
				return 0;
			}
			int num = startIndex + HttpRuleParser.GetWhitespaceLength(input, startIndex);
			while (true)
			{
				NameValueHeaderValue item = null;
				int nameValueLength = NameValueHeaderValue.GetNameValueLength(input, num, NameValueHeaderValue.defaultNameValueCreator, out item);
				if (nameValueLength == 0)
				{
					break;
				}
				nameValueCollection.Add(item);
				num += nameValueLength;
				num += HttpRuleParser.GetWhitespaceLength(input, num);
				if (num == input.Length || input[num] != delimiter)
				{
					goto IL_5B;
				}
				num++;
				num += HttpRuleParser.GetWhitespaceLength(input, num);
			}
			return 0;
			IL_5B:
			return num - startIndex;
		}

		internal static NameValueHeaderValue Find(ICollection<NameValueHeaderValue> values, string name)
		{
			if (values == null || values.Count == 0)
			{
				return null;
			}
			foreach (NameValueHeaderValue current in values)
			{
				if (string.Compare(current.Name, name, StringComparison.OrdinalIgnoreCase) == 0)
				{
					return current;
				}
			}
			return null;
		}

		internal static int GetValueLength(string input, int startIndex)
		{
			if (startIndex >= input.Length)
			{
				return 0;
			}
			int tokenLength = HttpRuleParser.GetTokenLength(input, startIndex);
			if (tokenLength == 0 && HttpRuleParser.GetQuotedStringLength(input, startIndex, out tokenLength) != HttpParseResult.Parsed)
			{
				return 0;
			}
			return tokenLength;
		}

		private static void CheckNameValueFormat(string name, string value)
		{
			HeaderUtilities.CheckValidToken(name, "name");
			NameValueHeaderValue.CheckValueFormat(value);
		}

		private static void CheckValueFormat(string value)
		{
			if (!string.IsNullOrEmpty(value) && NameValueHeaderValue.GetValueLength(value, 0) != value.Length)
			{
				throw new FormatException(string.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, new object[]
				{
					value
				}));
			}
		}

		private static NameValueHeaderValue CreateNameValue()
		{
			return new NameValueHeaderValue();
		}

		object ICloneable.Clone()
		{
			return new NameValueHeaderValue(this);
		}
	}
}
