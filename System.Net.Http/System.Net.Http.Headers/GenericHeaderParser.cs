using System;
using System.Collections;

namespace System.Net.Http.Headers
{
	internal sealed class GenericHeaderParser : BaseHeaderParser
	{
		private delegate int GetParsedValueLengthDelegate(string value, int startIndex, out object parsedValue);

		internal static readonly HttpHeaderParser HostParser = new GenericHeaderParser(false, new GenericHeaderParser.GetParsedValueLengthDelegate(GenericHeaderParser.ParseHost), StringComparer.OrdinalIgnoreCase);

		internal static readonly HttpHeaderParser TokenListParser = new GenericHeaderParser(true, new GenericHeaderParser.GetParsedValueLengthDelegate(GenericHeaderParser.ParseTokenList), StringComparer.OrdinalIgnoreCase);

		internal static readonly HttpHeaderParser SingleValueNameValueWithParametersParser = new GenericHeaderParser(false, new GenericHeaderParser.GetParsedValueLengthDelegate(NameValueWithParametersHeaderValue.GetNameValueWithParametersLength));

		internal static readonly HttpHeaderParser MultipleValueNameValueWithParametersParser = new GenericHeaderParser(true, new GenericHeaderParser.GetParsedValueLengthDelegate(NameValueWithParametersHeaderValue.GetNameValueWithParametersLength));

		internal static readonly HttpHeaderParser SingleValueNameValueParser = new GenericHeaderParser(false, new GenericHeaderParser.GetParsedValueLengthDelegate(GenericHeaderParser.ParseNameValue));

		internal static readonly HttpHeaderParser MultipleValueNameValueParser = new GenericHeaderParser(true, new GenericHeaderParser.GetParsedValueLengthDelegate(GenericHeaderParser.ParseNameValue));

		internal static readonly HttpHeaderParser MailAddressParser = new GenericHeaderParser(false, new GenericHeaderParser.GetParsedValueLengthDelegate(GenericHeaderParser.ParseMailAddress));

		internal static readonly HttpHeaderParser SingleValueProductParser = new GenericHeaderParser(false, new GenericHeaderParser.GetParsedValueLengthDelegate(GenericHeaderParser.ParseProduct));

		internal static readonly HttpHeaderParser MultipleValueProductParser = new GenericHeaderParser(true, new GenericHeaderParser.GetParsedValueLengthDelegate(GenericHeaderParser.ParseProduct));

		internal static readonly HttpHeaderParser RangeConditionParser = new GenericHeaderParser(false, new GenericHeaderParser.GetParsedValueLengthDelegate(RangeConditionHeaderValue.GetRangeConditionLength));

		internal static readonly HttpHeaderParser SingleValueAuthenticationParser = new GenericHeaderParser(false, new GenericHeaderParser.GetParsedValueLengthDelegate(AuthenticationHeaderValue.GetAuthenticationLength));

		internal static readonly HttpHeaderParser MultipleValueAuthenticationParser = new GenericHeaderParser(true, new GenericHeaderParser.GetParsedValueLengthDelegate(AuthenticationHeaderValue.GetAuthenticationLength));

		internal static readonly HttpHeaderParser RangeParser = new GenericHeaderParser(false, new GenericHeaderParser.GetParsedValueLengthDelegate(RangeHeaderValue.GetRangeLength));

		internal static readonly HttpHeaderParser RetryConditionParser = new GenericHeaderParser(false, new GenericHeaderParser.GetParsedValueLengthDelegate(RetryConditionHeaderValue.GetRetryConditionLength));

		internal static readonly HttpHeaderParser ContentRangeParser = new GenericHeaderParser(false, new GenericHeaderParser.GetParsedValueLengthDelegate(ContentRangeHeaderValue.GetContentRangeLength));

		internal static readonly HttpHeaderParser ContentDispositionParser = new GenericHeaderParser(false, new GenericHeaderParser.GetParsedValueLengthDelegate(ContentDispositionHeaderValue.GetDispositionTypeLength));

		internal static readonly HttpHeaderParser SingleValueStringWithQualityParser = new GenericHeaderParser(false, new GenericHeaderParser.GetParsedValueLengthDelegate(StringWithQualityHeaderValue.GetStringWithQualityLength));

		internal static readonly HttpHeaderParser MultipleValueStringWithQualityParser = new GenericHeaderParser(true, new GenericHeaderParser.GetParsedValueLengthDelegate(StringWithQualityHeaderValue.GetStringWithQualityLength));

		internal static readonly HttpHeaderParser SingleValueEntityTagParser = new GenericHeaderParser(false, new GenericHeaderParser.GetParsedValueLengthDelegate(GenericHeaderParser.ParseSingleEntityTag));

		internal static readonly HttpHeaderParser MultipleValueEntityTagParser = new GenericHeaderParser(true, new GenericHeaderParser.GetParsedValueLengthDelegate(GenericHeaderParser.ParseMultipleEntityTags));

		internal static readonly HttpHeaderParser SingleValueViaParser = new GenericHeaderParser(false, new GenericHeaderParser.GetParsedValueLengthDelegate(ViaHeaderValue.GetViaLength));

		internal static readonly HttpHeaderParser MultipleValueViaParser = new GenericHeaderParser(true, new GenericHeaderParser.GetParsedValueLengthDelegate(ViaHeaderValue.GetViaLength));

		internal static readonly HttpHeaderParser SingleValueWarningParser = new GenericHeaderParser(false, new GenericHeaderParser.GetParsedValueLengthDelegate(WarningHeaderValue.GetWarningLength));

		internal static readonly HttpHeaderParser MultipleValueWarningParser = new GenericHeaderParser(true, new GenericHeaderParser.GetParsedValueLengthDelegate(WarningHeaderValue.GetWarningLength));

		private GenericHeaderParser.GetParsedValueLengthDelegate getParsedValueLength;

		private IEqualityComparer comparer;

		public override IEqualityComparer Comparer
		{
			get
			{
				return this.comparer;
			}
		}

		private GenericHeaderParser(bool supportsMultipleValues, GenericHeaderParser.GetParsedValueLengthDelegate getParsedValueLength) : this(supportsMultipleValues, getParsedValueLength, null)
		{
		}

		private GenericHeaderParser(bool supportsMultipleValues, GenericHeaderParser.GetParsedValueLengthDelegate getParsedValueLength, IEqualityComparer comparer) : base(supportsMultipleValues)
		{
			this.getParsedValueLength = getParsedValueLength;
			this.comparer = comparer;
		}

		protected override int GetParsedValueLength(string value, int startIndex, object storeValue, out object parsedValue)
		{
			return this.getParsedValueLength(value, startIndex, out parsedValue);
		}

		private static int ParseNameValue(string value, int startIndex, out object parsedValue)
		{
			NameValueHeaderValue nameValueHeaderValue = null;
			int nameValueLength = NameValueHeaderValue.GetNameValueLength(value, startIndex, out nameValueHeaderValue);
			parsedValue = nameValueHeaderValue;
			return nameValueLength;
		}

		private static int ParseProduct(string value, int startIndex, out object parsedValue)
		{
			ProductHeaderValue productHeaderValue = null;
			int productLength = ProductHeaderValue.GetProductLength(value, startIndex, out productHeaderValue);
			parsedValue = productHeaderValue;
			return productLength;
		}

		private static int ParseSingleEntityTag(string value, int startIndex, out object parsedValue)
		{
			EntityTagHeaderValue entityTagHeaderValue = null;
			parsedValue = null;
			int entityTagLength = EntityTagHeaderValue.GetEntityTagLength(value, startIndex, out entityTagHeaderValue);
			if (entityTagHeaderValue == EntityTagHeaderValue.Any)
			{
				return 0;
			}
			parsedValue = entityTagHeaderValue;
			return entityTagLength;
		}

		private static int ParseMultipleEntityTags(string value, int startIndex, out object parsedValue)
		{
			EntityTagHeaderValue entityTagHeaderValue = null;
			int entityTagLength = EntityTagHeaderValue.GetEntityTagLength(value, startIndex, out entityTagHeaderValue);
			parsedValue = entityTagHeaderValue;
			return entityTagLength;
		}

		private static int ParseMailAddress(string value, int startIndex, out object parsedValue)
		{
			parsedValue = null;
			if (HttpRuleParser.ContainsInvalidNewLine(value, startIndex))
			{
				return 0;
			}
			string text = value.Substring(startIndex);
			if (!HeaderUtilities.IsValidEmailAddress(text))
			{
				return 0;
			}
			parsedValue = text;
			return text.Length;
		}

		private static int ParseHost(string value, int startIndex, out object parsedValue)
		{
			string text = null;
			int hostLength = HttpRuleParser.GetHostLength(value, startIndex, false, out text);
			parsedValue = text;
			return hostLength;
		}

		private static int ParseTokenList(string value, int startIndex, out object parsedValue)
		{
			int tokenLength = HttpRuleParser.GetTokenLength(value, startIndex);
			parsedValue = value.Substring(startIndex, tokenLength);
			return tokenLength;
		}
	}
}
