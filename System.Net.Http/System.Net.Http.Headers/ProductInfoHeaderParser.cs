using System;

namespace System.Net.Http.Headers
{
	internal class ProductInfoHeaderParser : HttpHeaderParser
	{
		private const string separator = " ";

		internal static readonly ProductInfoHeaderParser SingleValueParser = new ProductInfoHeaderParser(false);

		internal static readonly ProductInfoHeaderParser MultipleValueParser = new ProductInfoHeaderParser(true);

		private ProductInfoHeaderParser(bool supportsMultipleValues) : base(supportsMultipleValues, " ")
		{
		}

		public override bool TryParseValue(string value, object storeValue, ref int index, out object parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(value) || index == value.Length)
			{
				return false;
			}
			int num = index + HttpRuleParser.GetWhitespaceLength(value, index);
			if (num == value.Length)
			{
				return false;
			}
			ProductInfoHeaderValue productInfoHeaderValue = null;
			int productInfoLength = ProductInfoHeaderValue.GetProductInfoLength(value, num, out productInfoHeaderValue);
			if (productInfoLength == 0)
			{
				return false;
			}
			num += productInfoLength;
			if (num < value.Length)
			{
				char c = value[num - 1];
				if (c != ' ' && c != '\t')
				{
					return false;
				}
			}
			index = num;
			parsedValue = productInfoHeaderValue;
			return true;
		}
	}
}
