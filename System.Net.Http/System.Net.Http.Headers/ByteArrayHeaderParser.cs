using System;
using System.Globalization;

namespace System.Net.Http.Headers
{
	internal class ByteArrayHeaderParser : HttpHeaderParser
	{
		internal static readonly ByteArrayHeaderParser Parser = new ByteArrayHeaderParser();

		private ByteArrayHeaderParser() : base(false)
		{
		}

		public override string ToString(object value)
		{
			return Convert.ToBase64String((byte[])value);
		}

		public override bool TryParseValue(string value, object storeValue, ref int index, out object parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(value) || index == value.Length)
			{
				return false;
			}
			string text = value;
			if (index > 0)
			{
				text = value.Substring(index);
			}
			try
			{
				parsedValue = Convert.FromBase64String(text);
				index = value.Length;
				return true;
			}
			catch (FormatException ex)
			{
				if (Logging.On)
				{
					Logging.PrintError(Logging.Http, string.Format(CultureInfo.InvariantCulture, SR.net_http_parser_invalid_base64_string, new object[]
					{
						text,
						ex.Message
					}));
				}
			}
			return false;
		}
	}
}
