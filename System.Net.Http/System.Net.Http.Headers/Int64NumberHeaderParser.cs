using System;
using System.Globalization;

namespace System.Net.Http.Headers
{
	internal class Int64NumberHeaderParser : BaseHeaderParser
	{
		internal static readonly Int64NumberHeaderParser Parser = new Int64NumberHeaderParser();

		private Int64NumberHeaderParser() : base(false)
		{
		}

		public override string ToString(object value)
		{
			return ((long)value).ToString(NumberFormatInfo.InvariantInfo);
		}

		protected override int GetParsedValueLength(string value, int startIndex, object storeValue, out object parsedValue)
		{
			parsedValue = null;
			int numberLength = HttpRuleParser.GetNumberLength(value, startIndex, false);
			if (numberLength == 0 || numberLength > 19)
			{
				return 0;
			}
			long num = 0L;
			if (!HeaderUtilities.TryParseInt64(value.Substring(startIndex, numberLength), out num))
			{
				return 0;
			}
			parsedValue = num;
			return numberLength;
		}
	}
}
