using System;

namespace System.Net.Http.Headers
{
	internal class MediaTypeHeaderParser : BaseHeaderParser
	{
		private bool supportsMultipleValues;

		private Func<MediaTypeHeaderValue> mediaTypeCreator;

		internal static readonly MediaTypeHeaderParser SingleValueParser = new MediaTypeHeaderParser(false, new Func<MediaTypeHeaderValue>(MediaTypeHeaderParser.CreateMediaType));

		internal static readonly MediaTypeHeaderParser SingleValueWithQualityParser = new MediaTypeHeaderParser(false, new Func<MediaTypeHeaderValue>(MediaTypeHeaderParser.CreateMediaTypeWithQuality));

		internal static readonly MediaTypeHeaderParser MultipleValuesParser = new MediaTypeHeaderParser(true, new Func<MediaTypeHeaderValue>(MediaTypeHeaderParser.CreateMediaTypeWithQuality));

		private MediaTypeHeaderParser(bool supportsMultipleValues, Func<MediaTypeHeaderValue> mediaTypeCreator) : base(supportsMultipleValues)
		{
			this.supportsMultipleValues = supportsMultipleValues;
			this.mediaTypeCreator = mediaTypeCreator;
		}

		protected override int GetParsedValueLength(string value, int startIndex, object storeValue, out object parsedValue)
		{
			MediaTypeHeaderValue mediaTypeHeaderValue = null;
			int mediaTypeLength = MediaTypeHeaderValue.GetMediaTypeLength(value, startIndex, this.mediaTypeCreator, out mediaTypeHeaderValue);
			parsedValue = mediaTypeHeaderValue;
			return mediaTypeLength;
		}

		private static MediaTypeHeaderValue CreateMediaType()
		{
			return new MediaTypeHeaderValue();
		}

		private static MediaTypeHeaderValue CreateMediaTypeWithQuality()
		{
			return new MediaTypeWithQualityHeaderValue();
		}
	}
}
