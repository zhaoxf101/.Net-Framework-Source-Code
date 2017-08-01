using System;
using System.Collections.Generic;

namespace System.Net.Http.Headers
{
	public sealed class HttpRequestHeaders : HttpHeaders
	{
		private static readonly Dictionary<string, HttpHeaderParser> parserStore;

		private static readonly HashSet<string> invalidHeaders;

		private HttpGeneralHeaders generalHeaders;

		private HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue> accept;

		private HttpHeaderValueCollection<NameValueWithParametersHeaderValue> expect;

		private bool expectContinueSet;

		private HttpHeaderValueCollection<EntityTagHeaderValue> ifMatch;

		private HttpHeaderValueCollection<EntityTagHeaderValue> ifNoneMatch;

		private HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue> te;

		private HttpHeaderValueCollection<ProductInfoHeaderValue> userAgent;

		private HttpHeaderValueCollection<StringWithQualityHeaderValue> acceptCharset;

		private HttpHeaderValueCollection<StringWithQualityHeaderValue> acceptEncoding;

		private HttpHeaderValueCollection<StringWithQualityHeaderValue> acceptLanguage;

		public HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue> Accept
		{
			get
			{
				if (this.accept == null)
				{
					this.accept = new HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue>("Accept", this);
				}
				return this.accept;
			}
		}

		public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptCharset
		{
			get
			{
				if (this.acceptCharset == null)
				{
					this.acceptCharset = new HttpHeaderValueCollection<StringWithQualityHeaderValue>("Accept-Charset", this);
				}
				return this.acceptCharset;
			}
		}

		public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptEncoding
		{
			get
			{
				if (this.acceptEncoding == null)
				{
					this.acceptEncoding = new HttpHeaderValueCollection<StringWithQualityHeaderValue>("Accept-Encoding", this);
				}
				return this.acceptEncoding;
			}
		}

		public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptLanguage
		{
			get
			{
				if (this.acceptLanguage == null)
				{
					this.acceptLanguage = new HttpHeaderValueCollection<StringWithQualityHeaderValue>("Accept-Language", this);
				}
				return this.acceptLanguage;
			}
		}

		public AuthenticationHeaderValue Authorization
		{
			get
			{
				return (AuthenticationHeaderValue)base.GetParsedValues("Authorization");
			}
			set
			{
				base.SetOrRemoveParsedValue("Authorization", value);
			}
		}

		public HttpHeaderValueCollection<NameValueWithParametersHeaderValue> Expect
		{
			get
			{
				return this.ExpectCore;
			}
		}

		public bool? ExpectContinue
		{
			get
			{
				if (this.ExpectCore.IsSpecialValueSet)
				{
					return new bool?(true);
				}
				if (this.expectContinueSet)
				{
					return new bool?(false);
				}
				return null;
			}
			set
			{
				if (value == true)
				{
					this.expectContinueSet = true;
					this.ExpectCore.SetSpecialValue();
					return;
				}
				this.expectContinueSet = value.HasValue;
				this.ExpectCore.RemoveSpecialValue();
			}
		}

		public string From
		{
			get
			{
				return (string)base.GetParsedValues("From");
			}
			set
			{
				if (value == string.Empty)
				{
					value = null;
				}
				if (value != null && !HeaderUtilities.IsValidEmailAddress(value))
				{
					throw new FormatException(SR.net_http_headers_invalid_from_header);
				}
				base.SetOrRemoveParsedValue("From", value);
			}
		}

		public string Host
		{
			get
			{
				return (string)base.GetParsedValues("Host");
			}
			set
			{
				if (value == string.Empty)
				{
					value = null;
				}
				string text = null;
				if (value != null && HttpRuleParser.GetHostLength(value, 0, false, out text) != value.Length)
				{
					throw new FormatException(SR.net_http_headers_invalid_host_header);
				}
				base.SetOrRemoveParsedValue("Host", value);
			}
		}

		public HttpHeaderValueCollection<EntityTagHeaderValue> IfMatch
		{
			get
			{
				if (this.ifMatch == null)
				{
					this.ifMatch = new HttpHeaderValueCollection<EntityTagHeaderValue>("If-Match", this);
				}
				return this.ifMatch;
			}
		}

		public DateTimeOffset? IfModifiedSince
		{
			get
			{
				return HeaderUtilities.GetDateTimeOffsetValue("If-Modified-Since", this);
			}
			set
			{
				base.SetOrRemoveParsedValue("If-Modified-Since", value);
			}
		}

		public HttpHeaderValueCollection<EntityTagHeaderValue> IfNoneMatch
		{
			get
			{
				if (this.ifNoneMatch == null)
				{
					this.ifNoneMatch = new HttpHeaderValueCollection<EntityTagHeaderValue>("If-None-Match", this);
				}
				return this.ifNoneMatch;
			}
		}

		public RangeConditionHeaderValue IfRange
		{
			get
			{
				return (RangeConditionHeaderValue)base.GetParsedValues("If-Range");
			}
			set
			{
				base.SetOrRemoveParsedValue("If-Range", value);
			}
		}

		public DateTimeOffset? IfUnmodifiedSince
		{
			get
			{
				return HeaderUtilities.GetDateTimeOffsetValue("If-Unmodified-Since", this);
			}
			set
			{
				base.SetOrRemoveParsedValue("If-Unmodified-Since", value);
			}
		}

		public int? MaxForwards
		{
			get
			{
				object parsedValues = base.GetParsedValues("Max-Forwards");
				if (parsedValues != null)
				{
					return new int?((int)parsedValues);
				}
				return null;
			}
			set
			{
				base.SetOrRemoveParsedValue("Max-Forwards", value);
			}
		}

		public AuthenticationHeaderValue ProxyAuthorization
		{
			get
			{
				return (AuthenticationHeaderValue)base.GetParsedValues("Proxy-Authorization");
			}
			set
			{
				base.SetOrRemoveParsedValue("Proxy-Authorization", value);
			}
		}

		public RangeHeaderValue Range
		{
			get
			{
				return (RangeHeaderValue)base.GetParsedValues("Range");
			}
			set
			{
				base.SetOrRemoveParsedValue("Range", value);
			}
		}

		public Uri Referrer
		{
			get
			{
				return (Uri)base.GetParsedValues("Referer");
			}
			set
			{
				base.SetOrRemoveParsedValue("Referer", value);
			}
		}

		public HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue> TE
		{
			get
			{
				if (this.te == null)
				{
					this.te = new HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue>("TE", this);
				}
				return this.te;
			}
		}

		public HttpHeaderValueCollection<ProductInfoHeaderValue> UserAgent
		{
			get
			{
				if (this.userAgent == null)
				{
					this.userAgent = new HttpHeaderValueCollection<ProductInfoHeaderValue>("User-Agent", this);
				}
				return this.userAgent;
			}
		}

		private HttpHeaderValueCollection<NameValueWithParametersHeaderValue> ExpectCore
		{
			get
			{
				if (this.expect == null)
				{
					this.expect = new HttpHeaderValueCollection<NameValueWithParametersHeaderValue>("Expect", this, HeaderUtilities.ExpectContinue);
				}
				return this.expect;
			}
		}

		public CacheControlHeaderValue CacheControl
		{
			get
			{
				return this.generalHeaders.CacheControl;
			}
			set
			{
				this.generalHeaders.CacheControl = value;
			}
		}

		public HttpHeaderValueCollection<string> Connection
		{
			get
			{
				return this.generalHeaders.Connection;
			}
		}

		public bool? ConnectionClose
		{
			get
			{
				return this.generalHeaders.ConnectionClose;
			}
			set
			{
				this.generalHeaders.ConnectionClose = value;
			}
		}

		public DateTimeOffset? Date
		{
			get
			{
				return this.generalHeaders.Date;
			}
			set
			{
				this.generalHeaders.Date = value;
			}
		}

		public HttpHeaderValueCollection<NameValueHeaderValue> Pragma
		{
			get
			{
				return this.generalHeaders.Pragma;
			}
		}

		public HttpHeaderValueCollection<string> Trailer
		{
			get
			{
				return this.generalHeaders.Trailer;
			}
		}

		public HttpHeaderValueCollection<TransferCodingHeaderValue> TransferEncoding
		{
			get
			{
				return this.generalHeaders.TransferEncoding;
			}
		}

		public bool? TransferEncodingChunked
		{
			get
			{
				return this.generalHeaders.TransferEncodingChunked;
			}
			set
			{
				this.generalHeaders.TransferEncodingChunked = value;
			}
		}

		public HttpHeaderValueCollection<ProductHeaderValue> Upgrade
		{
			get
			{
				return this.generalHeaders.Upgrade;
			}
		}

		public HttpHeaderValueCollection<ViaHeaderValue> Via
		{
			get
			{
				return this.generalHeaders.Via;
			}
		}

		public HttpHeaderValueCollection<WarningHeaderValue> Warning
		{
			get
			{
				return this.generalHeaders.Warning;
			}
		}

		internal HttpRequestHeaders()
		{
			this.generalHeaders = new HttpGeneralHeaders(this);
			base.SetConfiguration(HttpRequestHeaders.parserStore, HttpRequestHeaders.invalidHeaders);
		}

		static HttpRequestHeaders()
		{
			HttpRequestHeaders.parserStore = new Dictionary<string, HttpHeaderParser>(StringComparer.OrdinalIgnoreCase);
			HttpRequestHeaders.parserStore.Add("Accept", MediaTypeHeaderParser.MultipleValuesParser);
			HttpRequestHeaders.parserStore.Add("Accept-Charset", GenericHeaderParser.MultipleValueStringWithQualityParser);
			HttpRequestHeaders.parserStore.Add("Accept-Encoding", GenericHeaderParser.MultipleValueStringWithQualityParser);
			HttpRequestHeaders.parserStore.Add("Accept-Language", GenericHeaderParser.MultipleValueStringWithQualityParser);
			HttpRequestHeaders.parserStore.Add("Authorization", GenericHeaderParser.SingleValueAuthenticationParser);
			HttpRequestHeaders.parserStore.Add("Expect", GenericHeaderParser.MultipleValueNameValueWithParametersParser);
			HttpRequestHeaders.parserStore.Add("From", GenericHeaderParser.MailAddressParser);
			HttpRequestHeaders.parserStore.Add("Host", GenericHeaderParser.HostParser);
			HttpRequestHeaders.parserStore.Add("If-Match", GenericHeaderParser.MultipleValueEntityTagParser);
			HttpRequestHeaders.parserStore.Add("If-Modified-Since", DateHeaderParser.Parser);
			HttpRequestHeaders.parserStore.Add("If-None-Match", GenericHeaderParser.MultipleValueEntityTagParser);
			HttpRequestHeaders.parserStore.Add("If-Range", GenericHeaderParser.RangeConditionParser);
			HttpRequestHeaders.parserStore.Add("If-Unmodified-Since", DateHeaderParser.Parser);
			HttpRequestHeaders.parserStore.Add("Max-Forwards", Int32NumberHeaderParser.Parser);
			HttpRequestHeaders.parserStore.Add("Proxy-Authorization", GenericHeaderParser.SingleValueAuthenticationParser);
			HttpRequestHeaders.parserStore.Add("Range", GenericHeaderParser.RangeParser);
			HttpRequestHeaders.parserStore.Add("Referer", UriHeaderParser.RelativeOrAbsoluteUriParser);
			HttpRequestHeaders.parserStore.Add("TE", TransferCodingHeaderParser.MultipleValueWithQualityParser);
			HttpRequestHeaders.parserStore.Add("User-Agent", ProductInfoHeaderParser.MultipleValueParser);
			HttpGeneralHeaders.AddParsers(HttpRequestHeaders.parserStore);
			HttpRequestHeaders.invalidHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HttpContentHeaders.AddKnownHeaders(HttpRequestHeaders.invalidHeaders);
		}

		internal static void AddKnownHeaders(HashSet<string> headerSet)
		{
			headerSet.Add("Accept");
			headerSet.Add("Accept-Charset");
			headerSet.Add("Accept-Encoding");
			headerSet.Add("Accept-Language");
			headerSet.Add("Authorization");
			headerSet.Add("Expect");
			headerSet.Add("From");
			headerSet.Add("Host");
			headerSet.Add("If-Match");
			headerSet.Add("If-Modified-Since");
			headerSet.Add("If-None-Match");
			headerSet.Add("If-Range");
			headerSet.Add("If-Unmodified-Since");
			headerSet.Add("Max-Forwards");
			headerSet.Add("Proxy-Authorization");
			headerSet.Add("Range");
			headerSet.Add("Referer");
			headerSet.Add("TE");
			headerSet.Add("User-Agent");
		}

		internal override void AddHeaders(HttpHeaders sourceHeaders)
		{
			base.AddHeaders(sourceHeaders);
			HttpRequestHeaders httpRequestHeaders = sourceHeaders as HttpRequestHeaders;
			this.generalHeaders.AddSpecialsFrom(httpRequestHeaders.generalHeaders);
			if (!this.ExpectContinue.HasValue)
			{
				this.ExpectContinue = httpRequestHeaders.ExpectContinue;
			}
		}
	}
}
