using System;
using System.Globalization;

namespace System.Net.Http.Headers
{
	public class RetryConditionHeaderValue : ICloneable
	{
		private DateTimeOffset? date;

		private TimeSpan? delta;

		public DateTimeOffset? Date
		{
			get
			{
				return this.date;
			}
		}

		public TimeSpan? Delta
		{
			get
			{
				return this.delta;
			}
		}

		public RetryConditionHeaderValue(DateTimeOffset date)
		{
			this.date = new DateTimeOffset?(date);
		}

		public RetryConditionHeaderValue(TimeSpan delta)
		{
			if (delta.TotalSeconds > 2147483647.0)
			{
				throw new ArgumentOutOfRangeException("delta");
			}
			this.delta = new TimeSpan?(delta);
		}

		private RetryConditionHeaderValue(RetryConditionHeaderValue source)
		{
			this.delta = source.delta;
			this.date = source.date;
		}

		private RetryConditionHeaderValue()
		{
		}

		public override string ToString()
		{
			if (this.delta.HasValue)
			{
				return ((int)this.delta.Value.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
			}
			return HttpRuleParser.DateToString(this.date.Value);
		}

		public override bool Equals(object obj)
		{
			RetryConditionHeaderValue retryConditionHeaderValue = obj as RetryConditionHeaderValue;
			if (retryConditionHeaderValue == null)
			{
				return false;
			}
			if (this.delta.HasValue)
			{
				return retryConditionHeaderValue.delta.HasValue && this.delta.Value == retryConditionHeaderValue.delta.Value;
			}
			return retryConditionHeaderValue.date.HasValue && this.date.Value == retryConditionHeaderValue.date.Value;
		}

		public override int GetHashCode()
		{
			if (!this.delta.HasValue)
			{
				return this.date.Value.GetHashCode();
			}
			return this.delta.Value.GetHashCode();
		}

		public static RetryConditionHeaderValue Parse(string input)
		{
			int num = 0;
			return (RetryConditionHeaderValue)GenericHeaderParser.RetryConditionParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out RetryConditionHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (GenericHeaderParser.RetryConditionParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (RetryConditionHeaderValue)obj;
				return true;
			}
			return false;
		}

		internal static int GetRetryConditionLength(string input, int startIndex, out object parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
			{
				return 0;
			}
			DateTimeOffset minValue = DateTimeOffset.MinValue;
			int num = -1;
			char c = input[startIndex];
			int num2;
			if (c >= '0' && c <= '9')
			{
				int numberLength = HttpRuleParser.GetNumberLength(input, startIndex, false);
				if (numberLength == 0 || numberLength > 10)
				{
					return 0;
				}
				num2 = startIndex + numberLength;
				num2 += HttpRuleParser.GetWhitespaceLength(input, num2);
				if (num2 != input.Length)
				{
					return 0;
				}
				if (!HeaderUtilities.TryParseInt32(input.Substring(startIndex, numberLength), out num))
				{
					return 0;
				}
			}
			else
			{
				if (!HttpRuleParser.TryStringToDate(input.Substring(startIndex), out minValue))
				{
					return 0;
				}
				num2 = input.Length;
			}
			RetryConditionHeaderValue retryConditionHeaderValue = new RetryConditionHeaderValue();
			if (num == -1)
			{
				retryConditionHeaderValue.date = new DateTimeOffset?(minValue);
			}
			else
			{
				retryConditionHeaderValue.delta = new TimeSpan?(new TimeSpan(0, 0, num));
			}
			parsedValue = retryConditionHeaderValue;
			return num2 - startIndex;
		}

		object ICloneable.Clone()
		{
			return new RetryConditionHeaderValue(this);
		}
	}
}
