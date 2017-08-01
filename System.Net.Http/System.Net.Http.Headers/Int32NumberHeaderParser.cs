using System;
using System.Globalization;

namespace System.Net.Http.Headers
{
	internal class Int32NumberHeaderParser : BaseHeaderParser
	{
		internal static readonly Int32NumberHeaderParser Parser = new Int32NumberHeaderParser();

		private Int32NumberHeaderParser() : base(false)
		{
		}

		public override string ToString(object value)
		{
			return ((int)value).ToString(NumberFormatInfo.InvariantInfo);
		}

		protected override int GetParsedValueLength(string value, int startIndex, object storeValue, out object parsedValue)
		{
			parsedValue = null;
			int numberLength = HttpRuleParser.GetNumberLength(value, startIndex, false);
			if (numberLength == 0 || numberLength > 10)
			{
				return 0;
			}
			int num = 0;
			if (!HeaderUtilities.TryParseInt32(value.Substring(startIndex, numberLength), out num))
			{
				return 0;
			}
			parsedValue = num;
			return numberLength;
		}
	}
}
