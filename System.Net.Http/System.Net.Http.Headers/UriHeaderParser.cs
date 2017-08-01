using System;
using System.Text;

namespace System.Net.Http.Headers
{
	internal class UriHeaderParser : HttpHeaderParser
	{
		private UriKind uriKind;

		internal static readonly UriHeaderParser RelativeOrAbsoluteUriParser = new UriHeaderParser(UriKind.RelativeOrAbsolute);

		private UriHeaderParser(UriKind uriKind) : base(false)
		{
			this.uriKind = uriKind;
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
			Uri uri;
			if (!Uri.TryCreate(text, this.uriKind, out uri))
			{
				text = UriHeaderParser.DecodeUtf8FromString(text);
				if (!Uri.TryCreate(text, this.uriKind, out uri))
				{
					return false;
				}
			}
			index = value.Length;
			parsedValue = uri;
			return true;
		}

		internal static string DecodeUtf8FromString(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				return input;
			}
			bool flag = false;
			for (int i = 0; i < input.Length; i++)
			{
				if (input[i] > 'ÿ')
				{
					return input;
				}
				if (input[i] > '\u007f')
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				byte[] array = new byte[input.Length];
				for (int j = 0; j < input.Length; j++)
				{
					if (input[j] > 'ÿ')
					{
						return input;
					}
					array[j] = (byte)input[j];
				}
				try
				{
					Encoding encoding = Encoding.GetEncoding("utf-8", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
					return encoding.GetString(array);
				}
				catch (ArgumentException)
				{
				}
				return input;
			}
			return input;
		}

		public override string ToString(object value)
		{
			Uri uri = (Uri)value;
			if (uri.IsAbsoluteUri)
			{
				return uri.AbsoluteUri;
			}
			return uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
		}
	}
}
