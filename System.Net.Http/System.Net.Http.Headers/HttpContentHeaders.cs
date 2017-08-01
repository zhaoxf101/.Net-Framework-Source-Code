using System;
using System.Collections.Generic;

namespace System.Net.Http.Headers
{
	public sealed class HttpContentHeaders : HttpHeaders
	{
		private static readonly Dictionary<string, HttpHeaderParser> parserStore;

		private static readonly HashSet<string> invalidHeaders;

		private Func<long?> calculateLengthFunc;

		private bool contentLengthSet;

		private HttpHeaderValueCollection<string> allow;

		private HttpHeaderValueCollection<string> contentEncoding;

		private HttpHeaderValueCollection<string> contentLanguage;

		public ICollection<string> Allow
		{
			get
			{
				if (this.allow == null)
				{
					this.allow = new HttpHeaderValueCollection<string>("Allow", this, HeaderUtilities.TokenValidator);
				}
				return this.allow;
			}
		}

		public ContentDispositionHeaderValue ContentDisposition
		{
			get
			{
				return (ContentDispositionHeaderValue)base.GetParsedValues("Content-Disposition");
			}
			set
			{
				base.SetOrRemoveParsedValue("Content-Disposition", value);
			}
		}

		public ICollection<string> ContentEncoding
		{
			get
			{
				if (this.contentEncoding == null)
				{
					this.contentEncoding = new HttpHeaderValueCollection<string>("Content-Encoding", this, HeaderUtilities.TokenValidator);
				}
				return this.contentEncoding;
			}
		}

		public ICollection<string> ContentLanguage
		{
			get
			{
				if (this.contentLanguage == null)
				{
					this.contentLanguage = new HttpHeaderValueCollection<string>("Content-Language", this, HeaderUtilities.TokenValidator);
				}
				return this.contentLanguage;
			}
		}

		public long? ContentLength
		{
			get
			{
				object parsedValues = base.GetParsedValues("Content-Length");
				if (!this.contentLengthSet && parsedValues == null)
				{
					long? result = this.calculateLengthFunc();
					if (result.HasValue)
					{
						base.SetParsedValue("Content-Length", result.Value);
					}
					return result;
				}
				if (parsedValues == null)
				{
					return null;
				}
				return new long?((long)parsedValues);
			}
			set
			{
				base.SetOrRemoveParsedValue("Content-Length", value);
				this.contentLengthSet = true;
			}
		}

		public Uri ContentLocation
		{
			get
			{
				return (Uri)base.GetParsedValues("Content-Location");
			}
			set
			{
				base.SetOrRemoveParsedValue("Content-Location", value);
			}
		}

		public byte[] ContentMD5
		{
			get
			{
				return (byte[])base.GetParsedValues("Content-MD5");
			}
			set
			{
				base.SetOrRemoveParsedValue("Content-MD5", value);
			}
		}

		public ContentRangeHeaderValue ContentRange
		{
			get
			{
				return (ContentRangeHeaderValue)base.GetParsedValues("Content-Range");
			}
			set
			{
				base.SetOrRemoveParsedValue("Content-Range", value);
			}
		}

		public MediaTypeHeaderValue ContentType
		{
			get
			{
				return (MediaTypeHeaderValue)base.GetParsedValues("Content-Type");
			}
			set
			{
				base.SetOrRemoveParsedValue("Content-Type", value);
			}
		}

		public DateTimeOffset? Expires
		{
			get
			{
				return HeaderUtilities.GetDateTimeOffsetValue("Expires", this);
			}
			set
			{
				base.SetOrRemoveParsedValue("Expires", value);
			}
		}

		public DateTimeOffset? LastModified
		{
			get
			{
				return HeaderUtilities.GetDateTimeOffsetValue("Last-Modified", this);
			}
			set
			{
				base.SetOrRemoveParsedValue("Last-Modified", value);
			}
		}

		internal HttpContentHeaders(Func<long?> calculateLengthFunc)
		{
			this.calculateLengthFunc = calculateLengthFunc;
			base.SetConfiguration(HttpContentHeaders.parserStore, HttpContentHeaders.invalidHeaders);
		}

		static HttpContentHeaders()
		{
			HttpContentHeaders.parserStore = new Dictionary<string, HttpHeaderParser>(StringComparer.OrdinalIgnoreCase);
			HttpContentHeaders.parserStore.Add("Allow", GenericHeaderParser.TokenListParser);
			HttpContentHeaders.parserStore.Add("Content-Disposition", GenericHeaderParser.ContentDispositionParser);
			HttpContentHeaders.parserStore.Add("Content-Encoding", GenericHeaderParser.TokenListParser);
			HttpContentHeaders.parserStore.Add("Content-Language", GenericHeaderParser.TokenListParser);
			HttpContentHeaders.parserStore.Add("Content-Length", Int64NumberHeaderParser.Parser);
			HttpContentHeaders.parserStore.Add("Content-Location", UriHeaderParser.RelativeOrAbsoluteUriParser);
			HttpContentHeaders.parserStore.Add("Content-MD5", ByteArrayHeaderParser.Parser);
			HttpContentHeaders.parserStore.Add("Content-Range", GenericHeaderParser.ContentRangeParser);
			HttpContentHeaders.parserStore.Add("Content-Type", MediaTypeHeaderParser.SingleValueParser);
			HttpContentHeaders.parserStore.Add("Expires", DateHeaderParser.Parser);
			HttpContentHeaders.parserStore.Add("Last-Modified", DateHeaderParser.Parser);
			HttpContentHeaders.invalidHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HttpRequestHeaders.AddKnownHeaders(HttpContentHeaders.invalidHeaders);
			HttpResponseHeaders.AddKnownHeaders(HttpContentHeaders.invalidHeaders);
			HttpGeneralHeaders.AddKnownHeaders(HttpContentHeaders.invalidHeaders);
		}

		internal static void AddKnownHeaders(HashSet<string> headerSet)
		{
			headerSet.Add("Allow");
			headerSet.Add("Content-Disposition");
			headerSet.Add("Content-Encoding");
			headerSet.Add("Content-Language");
			headerSet.Add("Content-Length");
			headerSet.Add("Content-Location");
			headerSet.Add("Content-MD5");
			headerSet.Add("Content-Range");
			headerSet.Add("Content-Type");
			headerSet.Add("Expires");
			headerSet.Add("Last-Modified");
		}
	}
}
