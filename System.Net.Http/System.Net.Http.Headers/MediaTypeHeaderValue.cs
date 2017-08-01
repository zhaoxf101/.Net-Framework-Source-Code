using System;
using System.Collections.Generic;
using System.Globalization;

namespace System.Net.Http.Headers
{
	public class MediaTypeHeaderValue : ICloneable
	{
		private const string charSet = "charset";

		private ICollection<NameValueHeaderValue> parameters;

		private string mediaType;

		public string CharSet
		{
			get
			{
				NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(this.parameters, "charset");
				if (nameValueHeaderValue != null)
				{
					return nameValueHeaderValue.Value;
				}
				return null;
			}
			set
			{
				NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(this.parameters, "charset");
				if (string.IsNullOrEmpty(value))
				{
					if (nameValueHeaderValue != null)
					{
						this.parameters.Remove(nameValueHeaderValue);
						return;
					}
				}
				else
				{
					if (nameValueHeaderValue != null)
					{
						nameValueHeaderValue.Value = value;
						return;
					}
					this.Parameters.Add(new NameValueHeaderValue("charset", value));
				}
			}
		}

		public ICollection<NameValueHeaderValue> Parameters
		{
			get
			{
				if (this.parameters == null)
				{
					this.parameters = new ObjectCollection<NameValueHeaderValue>();
				}
				return this.parameters;
			}
		}

		public string MediaType
		{
			get
			{
				return this.mediaType;
			}
			set
			{
				MediaTypeHeaderValue.CheckMediaTypeFormat(value, "value");
				this.mediaType = value;
			}
		}

		internal MediaTypeHeaderValue()
		{
		}

		protected MediaTypeHeaderValue(MediaTypeHeaderValue source)
		{
			this.mediaType = source.mediaType;
			if (source.parameters != null)
			{
				foreach (NameValueHeaderValue current in source.parameters)
				{
					this.Parameters.Add((NameValueHeaderValue)((ICloneable)current).Clone());
				}
			}
		}

		public MediaTypeHeaderValue(string mediaType)
		{
			MediaTypeHeaderValue.CheckMediaTypeFormat(mediaType, "mediaType");
			this.mediaType = mediaType;
		}

		public override string ToString()
		{
			return this.mediaType + NameValueHeaderValue.ToString(this.parameters, ';', true);
		}

		public override bool Equals(object obj)
		{
			MediaTypeHeaderValue mediaTypeHeaderValue = obj as MediaTypeHeaderValue;
			return mediaTypeHeaderValue != null && string.Compare(this.mediaType, mediaTypeHeaderValue.mediaType, StringComparison.OrdinalIgnoreCase) == 0 && HeaderUtilities.AreEqualCollections<NameValueHeaderValue>(this.parameters, mediaTypeHeaderValue.parameters);
		}

		public override int GetHashCode()
		{
			return this.mediaType.ToLowerInvariant().GetHashCode() ^ NameValueHeaderValue.GetHashCode(this.parameters);
		}

		public static MediaTypeHeaderValue Parse(string input)
		{
			int num = 0;
			return (MediaTypeHeaderValue)MediaTypeHeaderParser.SingleValueParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out MediaTypeHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (MediaTypeHeaderParser.SingleValueParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (MediaTypeHeaderValue)obj;
				return true;
			}
			return false;
		}

		internal static int GetMediaTypeLength(string input, int startIndex, Func<MediaTypeHeaderValue> mediaTypeCreator, out MediaTypeHeaderValue parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
			{
				return 0;
			}
			string text = null;
			int mediaTypeExpressionLength = MediaTypeHeaderValue.GetMediaTypeExpressionLength(input, startIndex, out text);
			if (mediaTypeExpressionLength == 0)
			{
				return 0;
			}
			int num = startIndex + mediaTypeExpressionLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			MediaTypeHeaderValue mediaTypeHeaderValue;
			if (num >= input.Length || input[num] != ';')
			{
				mediaTypeHeaderValue = mediaTypeCreator();
				mediaTypeHeaderValue.mediaType = text;
				parsedValue = mediaTypeHeaderValue;
				return num - startIndex;
			}
			mediaTypeHeaderValue = mediaTypeCreator();
			mediaTypeHeaderValue.mediaType = text;
			num++;
			int nameValueListLength = NameValueHeaderValue.GetNameValueListLength(input, num, ';', mediaTypeHeaderValue.Parameters);
			if (nameValueListLength == 0)
			{
				return 0;
			}
			parsedValue = mediaTypeHeaderValue;
			return num + nameValueListLength - startIndex;
		}

		private static int GetMediaTypeExpressionLength(string input, int startIndex, out string mediaType)
		{
			mediaType = null;
			int tokenLength = HttpRuleParser.GetTokenLength(input, startIndex);
			if (tokenLength == 0)
			{
				return 0;
			}
			int num = startIndex + tokenLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			if (num >= input.Length || input[num] != '/')
			{
				return 0;
			}
			num++;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			int tokenLength2 = HttpRuleParser.GetTokenLength(input, num);
			if (tokenLength2 == 0)
			{
				return 0;
			}
			int num2 = num + tokenLength2 - startIndex;
			if (tokenLength + tokenLength2 + 1 == num2)
			{
				mediaType = input.Substring(startIndex, num2);
			}
			else
			{
				mediaType = input.Substring(startIndex, tokenLength) + "/" + input.Substring(num, tokenLength2);
			}
			return num2;
		}

		private static void CheckMediaTypeFormat(string mediaType, string parameterName)
		{
			if (string.IsNullOrEmpty(mediaType))
			{
				throw new ArgumentException(SR.net_http_argument_empty_string, parameterName);
			}
			string text;
			int mediaTypeExpressionLength = MediaTypeHeaderValue.GetMediaTypeExpressionLength(mediaType, 0, out text);
			if (mediaTypeExpressionLength == 0 || text.Length != mediaType.Length)
			{
				throw new FormatException(string.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, new object[]
				{
					mediaType
				}));
			}
		}

		object ICloneable.Clone()
		{
			return new MediaTypeHeaderValue(this);
		}
	}
}
