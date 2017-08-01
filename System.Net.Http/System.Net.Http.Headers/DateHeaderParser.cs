using System;

namespace System.Net.Http.Headers
{
	internal class DateHeaderParser : HttpHeaderParser
	{
		internal static readonly DateHeaderParser Parser = new DateHeaderParser();

		private DateHeaderParser() : base(false)
		{
		}

		public override string ToString(object value)
		{
			return HttpRuleParser.DateToString((DateTimeOffset)value);
		}

		public override bool TryParseValue(string value, object storeValue, ref int index, out object parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(value) || index == value.Length)
			{
				return false;
			}
			string input = value;
			if (index > 0)
			{
				input = value.Substring(index);
			}
			DateTimeOffset dateTimeOffset;
			if (!HttpRuleParser.TryStringToDate(input, out dateTimeOffset))
			{
				return false;
			}
			index = value.Length;
			parsedValue = dateTimeOffset;
			return true;
		}
	}
}
