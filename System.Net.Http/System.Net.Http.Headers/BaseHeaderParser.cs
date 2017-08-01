using System;

namespace System.Net.Http.Headers
{
	internal abstract class BaseHeaderParser : HttpHeaderParser
	{
		protected BaseHeaderParser(bool supportsMultipleValues) : base(supportsMultipleValues)
		{
		}

		protected abstract int GetParsedValueLength(string value, int startIndex, object storeValue, out object parsedValue);

		public sealed override bool TryParseValue(string value, object storeValue, ref int index, out object parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(value) || index == value.Length)
			{
				return base.SupportsMultipleValues;
			}
			bool flag = false;
			int num = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(value, index, base.SupportsMultipleValues, out flag);
			if (flag && !base.SupportsMultipleValues)
			{
				return false;
			}
			if (num == value.Length)
			{
				if (base.SupportsMultipleValues)
				{
					index = num;
				}
				return base.SupportsMultipleValues;
			}
			object obj = null;
			int parsedValueLength = this.GetParsedValueLength(value, num, storeValue, out obj);
			if (parsedValueLength == 0)
			{
				return false;
			}
			num += parsedValueLength;
			num = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(value, num, base.SupportsMultipleValues, out flag);
			if ((flag && !base.SupportsMultipleValues) || (!flag && num < value.Length))
			{
				return false;
			}
			index = num;
			parsedValue = obj;
			return true;
		}
	}
}
