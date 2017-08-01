using System;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers
{
	public class ContentRangeHeaderValue : ICloneable
	{
		private string unit;

		private long? from;

		private long? to;

		private long? length;

		public string Unit
		{
			get
			{
				return this.unit;
			}
			set
			{
				HeaderUtilities.CheckValidToken(value, "value");
				this.unit = value;
			}
		}

		public long? From
		{
			get
			{
				return this.from;
			}
		}

		public long? To
		{
			get
			{
				return this.to;
			}
		}

		public long? Length
		{
			get
			{
				return this.length;
			}
		}

		public bool HasLength
		{
			get
			{
				return this.length.HasValue;
			}
		}

		public bool HasRange
		{
			get
			{
				return this.from.HasValue;
			}
		}

		public ContentRangeHeaderValue(long from, long to, long length)
		{
			if (length < 0L)
			{
				throw new ArgumentOutOfRangeException("length");
			}
			if (to < 0L || to > length)
			{
				throw new ArgumentOutOfRangeException("to");
			}
			if (from < 0L || from > to)
			{
				throw new ArgumentOutOfRangeException("from");
			}
			this.from = new long?(from);
			this.to = new long?(to);
			this.length = new long?(length);
			this.unit = "bytes";
		}

		public ContentRangeHeaderValue(long length)
		{
			if (length < 0L)
			{
				throw new ArgumentOutOfRangeException("length");
			}
			this.length = new long?(length);
			this.unit = "bytes";
		}

		public ContentRangeHeaderValue(long from, long to)
		{
			if (to < 0L)
			{
				throw new ArgumentOutOfRangeException("to");
			}
			if (from < 0L || from > to)
			{
				throw new ArgumentOutOfRangeException("from");
			}
			this.from = new long?(from);
			this.to = new long?(to);
			this.unit = "bytes";
		}

		private ContentRangeHeaderValue()
		{
		}

		private ContentRangeHeaderValue(ContentRangeHeaderValue source)
		{
			this.from = source.from;
			this.to = source.to;
			this.length = source.length;
			this.unit = source.unit;
		}

		public override bool Equals(object obj)
		{
			ContentRangeHeaderValue contentRangeHeaderValue = obj as ContentRangeHeaderValue;
			return contentRangeHeaderValue != null && (this.from == contentRangeHeaderValue.from && this.to == contentRangeHeaderValue.to && this.length == contentRangeHeaderValue.length) && string.Compare(this.unit, contentRangeHeaderValue.unit, StringComparison.OrdinalIgnoreCase) == 0;
		}

		public override int GetHashCode()
		{
			int num = this.unit.ToLowerInvariant().GetHashCode();
			if (this.HasRange)
			{
				num = (num ^ this.from.GetHashCode() ^ this.to.GetHashCode());
			}
			if (this.HasLength)
			{
				num ^= this.length.GetHashCode();
			}
			return num;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(this.unit);
			stringBuilder.Append(' ');
			if (this.HasRange)
			{
				stringBuilder.Append(this.from.Value.ToString(NumberFormatInfo.InvariantInfo));
				stringBuilder.Append('-');
				stringBuilder.Append(this.to.Value.ToString(NumberFormatInfo.InvariantInfo));
			}
			else
			{
				stringBuilder.Append('*');
			}
			stringBuilder.Append('/');
			if (this.HasLength)
			{
				stringBuilder.Append(this.length.Value.ToString(NumberFormatInfo.InvariantInfo));
			}
			else
			{
				stringBuilder.Append('*');
			}
			return stringBuilder.ToString();
		}

		public static ContentRangeHeaderValue Parse(string input)
		{
			int num = 0;
			return (ContentRangeHeaderValue)GenericHeaderParser.ContentRangeParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out ContentRangeHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (GenericHeaderParser.ContentRangeParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (ContentRangeHeaderValue)obj;
				return true;
			}
			return false;
		}

		internal static int GetContentRangeLength(string input, int startIndex, out object parsedValue)
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
			int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, num);
			if (whitespaceLength == 0)
			{
				return 0;
			}
			num += whitespaceLength;
			if (num == input.Length)
			{
				return 0;
			}
			int fromStartIndex = num;
			int fromLength = 0;
			int toStartIndex = 0;
			int toLength = 0;
			if (!ContentRangeHeaderValue.TryGetRangeLength(input, ref num, out fromLength, out toStartIndex, out toLength))
			{
				return 0;
			}
			if (num == input.Length || input[num] != '/')
			{
				return 0;
			}
			num++;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			if (num == input.Length)
			{
				return 0;
			}
			int lengthStartIndex = num;
			int lengthLength = 0;
			if (!ContentRangeHeaderValue.TryGetLengthLength(input, ref num, out lengthLength))
			{
				return 0;
			}
			if (!ContentRangeHeaderValue.TryCreateContentRange(input, text, fromStartIndex, fromLength, toStartIndex, toLength, lengthStartIndex, lengthLength, out parsedValue))
			{
				return 0;
			}
			return num - startIndex;
		}

		private static bool TryGetLengthLength(string input, ref int current, out int lengthLength)
		{
			lengthLength = 0;
			if (input[current] == '*')
			{
				current++;
			}
			else
			{
				lengthLength = HttpRuleParser.GetNumberLength(input, current, false);
				if (lengthLength == 0 || lengthLength > 19)
				{
					return false;
				}
				current += lengthLength;
			}
			current += HttpRuleParser.GetWhitespaceLength(input, current);
			return true;
		}

		private static bool TryGetRangeLength(string input, ref int current, out int fromLength, out int toStartIndex, out int toLength)
		{
			fromLength = 0;
			toStartIndex = 0;
			toLength = 0;
			if (input[current] == '*')
			{
				current++;
			}
			else
			{
				fromLength = HttpRuleParser.GetNumberLength(input, current, false);
				if (fromLength == 0 || fromLength > 19)
				{
					return false;
				}
				current += fromLength;
				current += HttpRuleParser.GetWhitespaceLength(input, current);
				if (current == input.Length || input[current] != '-')
				{
					return false;
				}
				current++;
				current += HttpRuleParser.GetWhitespaceLength(input, current);
				if (current == input.Length)
				{
					return false;
				}
				toStartIndex = current;
				toLength = HttpRuleParser.GetNumberLength(input, current, false);
				if (toLength == 0 || toLength > 19)
				{
					return false;
				}
				current += toLength;
			}
			current += HttpRuleParser.GetWhitespaceLength(input, current);
			return true;
		}

		private static bool TryCreateContentRange(string input, string unit, int fromStartIndex, int fromLength, int toStartIndex, int toLength, int lengthStartIndex, int lengthLength, out object parsedValue)
		{
			parsedValue = null;
			long num = 0L;
			if (fromLength > 0 && !HeaderUtilities.TryParseInt64(input.Substring(fromStartIndex, fromLength), out num))
			{
				return false;
			}
			long num2 = 0L;
			if (toLength > 0 && !HeaderUtilities.TryParseInt64(input.Substring(toStartIndex, toLength), out num2))
			{
				return false;
			}
			if (fromLength > 0 && toLength > 0 && num > num2)
			{
				return false;
			}
			long num3 = 0L;
			if (lengthLength > 0 && !HeaderUtilities.TryParseInt64(input.Substring(lengthStartIndex, lengthLength), out num3))
			{
				return false;
			}
			if (toLength > 0 && lengthLength > 0 && num2 >= num3)
			{
				return false;
			}
			ContentRangeHeaderValue contentRangeHeaderValue = new ContentRangeHeaderValue();
			contentRangeHeaderValue.unit = unit;
			if (fromLength > 0)
			{
				contentRangeHeaderValue.from = new long?(num);
				contentRangeHeaderValue.to = new long?(num2);
			}
			if (lengthLength > 0)
			{
				contentRangeHeaderValue.length = new long?(num3);
			}
			parsedValue = contentRangeHeaderValue;
			return true;
		}

		object ICloneable.Clone()
		{
			return new ContentRangeHeaderValue(this);
		}
	}
}
