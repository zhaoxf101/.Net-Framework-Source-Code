using System;
using System.Net.Http.Headers;
using System.Text;

namespace System.Net.Http
{
	public class StringContent : ByteArrayContent
	{
		private const string defaultMediaType = "text/plain";

		public StringContent(string content) : this(content, null, null)
		{
		}

		public StringContent(string content, Encoding encoding) : this(content, encoding, null)
		{
		}

		public StringContent(string content, Encoding encoding, string mediaType) : base(StringContent.GetContentByteArray(content, encoding))
		{
			MediaTypeHeaderValue mediaTypeHeaderValue = new MediaTypeHeaderValue((mediaType == null) ? "text/plain" : mediaType);
			mediaTypeHeaderValue.CharSet = ((encoding == null) ? HttpContent.DefaultStringEncoding.WebName : encoding.WebName);
			base.Headers.ContentType = mediaTypeHeaderValue;
		}

		private static byte[] GetContentByteArray(string content, Encoding encoding)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}
			if (encoding == null)
			{
				encoding = HttpContent.DefaultStringEncoding;
			}
			return encoding.GetBytes(content);
		}
	}
}
