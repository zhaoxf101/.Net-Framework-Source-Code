using System;
using System.Collections.Generic;

namespace System.Net.Http.Headers
{
	public sealed class HttpResponseHeaders : HttpHeaders
	{
		private static readonly Dictionary<string, HttpHeaderParser> parserStore;

		private static readonly HashSet<string> invalidHeaders;

		private HttpGeneralHeaders generalHeaders;

		private HttpHeaderValueCollection<string> acceptRanges;

		private HttpHeaderValueCollection<AuthenticationHeaderValue> wwwAuthenticate;

		private HttpHeaderValueCollection<AuthenticationHeaderValue> proxyAuthenticate;

		private HttpHeaderValueCollection<ProductInfoHeaderValue> server;

		private HttpHeaderValueCollection<string> vary;

		public HttpHeaderValueCollection<string> AcceptRanges
		{
			get
			{
				if (this.acceptRanges == null)
				{
					this.acceptRanges = new HttpHeaderValueCollection<string>("Accept-Ranges", this, HeaderUtilities.TokenValidator);
				}
				return this.acceptRanges;
			}
		}

		public TimeSpan? Age
		{
			get
			{
				return HeaderUtilities.GetTimeSpanValue("Age", this);
			}
			set
			{
				base.SetOrRemoveParsedValue("Age", value);
			}
		}

		public EntityTagHeaderValue ETag
		{
			get
			{
				return (EntityTagHeaderValue)base.GetParsedValues("ETag");
			}
			set
			{
				base.SetOrRemoveParsedValue("ETag", value);
			}
		}

		public Uri Location
		{
			get
			{
				return (Uri)base.GetParsedValues("Location");
			}
			set
			{
				base.SetOrRemoveParsedValue("Location", value);
			}
		}

		public HttpHeaderValueCollection<AuthenticationHeaderValue> ProxyAuthenticate
		{
			get
			{
				if (this.proxyAuthenticate == null)
				{
					this.proxyAuthenticate = new HttpHeaderValueCollection<AuthenticationHeaderValue>("Proxy-Authenticate", this);
				}
				return this.proxyAuthenticate;
			}
		}

		public RetryConditionHeaderValue RetryAfter
		{
			get
			{
				return (RetryConditionHeaderValue)base.GetParsedValues("Retry-After");
			}
			set
			{
				base.SetOrRemoveParsedValue("Retry-After", value);
			}
		}

		public HttpHeaderValueCollection<ProductInfoHeaderValue> Server
		{
			get
			{
				if (this.server == null)
				{
					this.server = new HttpHeaderValueCollection<ProductInfoHeaderValue>("Server", this);
				}
				return this.server;
			}
		}

		public HttpHeaderValueCollection<string> Vary
		{
			get
			{
				if (this.vary == null)
				{
					this.vary = new HttpHeaderValueCollection<string>("Vary", this, HeaderUtilities.TokenValidator);
				}
				return this.vary;
			}
		}

		public HttpHeaderValueCollection<AuthenticationHeaderValue> WwwAuthenticate
		{
			get
			{
				if (this.wwwAuthenticate == null)
				{
					this.wwwAuthenticate = new HttpHeaderValueCollection<AuthenticationHeaderValue>("WWW-Authenticate", this);
				}
				return this.wwwAuthenticate;
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

		internal HttpResponseHeaders()
		{
			this.generalHeaders = new HttpGeneralHeaders(this);
			base.SetConfiguration(HttpResponseHeaders.parserStore, HttpResponseHeaders.invalidHeaders);
		}

		static HttpResponseHeaders()
		{
			HttpResponseHeaders.parserStore = new Dictionary<string, HttpHeaderParser>(StringComparer.OrdinalIgnoreCase);
			HttpResponseHeaders.parserStore.Add("Accept-Ranges", GenericHeaderParser.TokenListParser);
			HttpResponseHeaders.parserStore.Add("Age", TimeSpanHeaderParser.Parser);
			HttpResponseHeaders.parserStore.Add("ETag", GenericHeaderParser.SingleValueEntityTagParser);
			HttpResponseHeaders.parserStore.Add("Location", UriHeaderParser.RelativeOrAbsoluteUriParser);
			HttpResponseHeaders.parserStore.Add("Proxy-Authenticate", GenericHeaderParser.MultipleValueAuthenticationParser);
			HttpResponseHeaders.parserStore.Add("Retry-After", GenericHeaderParser.RetryConditionParser);
			HttpResponseHeaders.parserStore.Add("Server", ProductInfoHeaderParser.MultipleValueParser);
			HttpResponseHeaders.parserStore.Add("Vary", GenericHeaderParser.TokenListParser);
			HttpResponseHeaders.parserStore.Add("WWW-Authenticate", GenericHeaderParser.MultipleValueAuthenticationParser);
			HttpGeneralHeaders.AddParsers(HttpResponseHeaders.parserStore);
			HttpResponseHeaders.invalidHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HttpContentHeaders.AddKnownHeaders(HttpResponseHeaders.invalidHeaders);
		}

		internal static void AddKnownHeaders(HashSet<string> headerSet)
		{
			headerSet.Add("Accept-Ranges");
			headerSet.Add("Age");
			headerSet.Add("ETag");
			headerSet.Add("Location");
			headerSet.Add("Proxy-Authenticate");
			headerSet.Add("Retry-After");
			headerSet.Add("Server");
			headerSet.Add("Vary");
			headerSet.Add("WWW-Authenticate");
		}

		internal override void AddHeaders(HttpHeaders sourceHeaders)
		{
			base.AddHeaders(sourceHeaders);
			HttpResponseHeaders httpResponseHeaders = sourceHeaders as HttpResponseHeaders;
			this.generalHeaders.AddSpecialsFrom(httpResponseHeaders.generalHeaders);
		}
	}
}
