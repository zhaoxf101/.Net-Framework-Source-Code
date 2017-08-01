using System;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace System.Net.Http
{
	public class HttpResponseMessage : IDisposable
	{
		private const HttpStatusCode defaultStatusCode = HttpStatusCode.OK;

		private HttpStatusCode statusCode;

		private HttpResponseHeaders headers;

		private string reasonPhrase;

		private HttpRequestMessage requestMessage;

		private Version version;

		private HttpContent content;

		private bool disposed;

		public Version Version
		{
			get
			{
				return this.version;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.CheckDisposed();
				this.version = value;
			}
		}

		public HttpContent Content
		{
			get
			{
				return this.content;
			}
			set
			{
				this.CheckDisposed();
				if (Logging.On)
				{
					if (value == null)
					{
						Logging.PrintInfo(Logging.Http, this, SR.net_http_log_content_null);
					}
					else
					{
						Logging.Associate(Logging.Http, this, value);
					}
				}
				this.content = value;
			}
		}

		public HttpStatusCode StatusCode
		{
			get
			{
				return this.statusCode;
			}
			set
			{
				if (value < (HttpStatusCode)0 || value > (HttpStatusCode)999)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this.CheckDisposed();
				this.statusCode = value;
			}
		}

		public string ReasonPhrase
		{
			get
			{
				if (this.reasonPhrase != null)
				{
					return this.reasonPhrase;
				}
				return HttpStatusDescription.Get(this.StatusCode);
			}
			set
			{
				if (value != null && this.ContainsNewLineCharacter(value))
				{
					throw new FormatException(SR.net_http_reasonphrase_format_error);
				}
				this.CheckDisposed();
				this.reasonPhrase = value;
			}
		}

		public HttpResponseHeaders Headers
		{
			get
			{
				if (this.headers == null)
				{
					this.headers = new HttpResponseHeaders();
				}
				return this.headers;
			}
		}

		public HttpRequestMessage RequestMessage
		{
			get
			{
				return this.requestMessage;
			}
			set
			{
				this.CheckDisposed();
				if (Logging.On && value != null)
				{
					Logging.Associate(Logging.Http, this, value);
				}
				this.requestMessage = value;
			}
		}

		public bool IsSuccessStatusCode
		{
			get
			{
				return this.statusCode >= HttpStatusCode.OK && this.statusCode <= (HttpStatusCode)299;
			}
		}

		public HttpResponseMessage() : this(HttpStatusCode.OK)
		{
		}

		public HttpResponseMessage(HttpStatusCode statusCode)
		{
			if (Logging.On)
			{
				Logging.Enter(Logging.Http, this, ".ctor", string.Concat(new object[]
				{
					"StatusCode: ",
					(int)statusCode,
					", ReasonPhrase: '",
					this.reasonPhrase,
					"'"
				}));
			}
			if (statusCode < (HttpStatusCode)0 || statusCode > (HttpStatusCode)999)
			{
				throw new ArgumentOutOfRangeException("statusCode");
			}
			this.statusCode = statusCode;
			this.version = HttpUtilities.DefaultVersion;
			if (Logging.On)
			{
				Logging.Exit(Logging.Http, this, ".ctor", null);
			}
		}

		public HttpResponseMessage EnsureSuccessStatusCode()
		{
			if (!this.IsSuccessStatusCode)
			{
				if (this.content != null)
				{
					this.content.Dispose();
				}
				throw new HttpRequestException(string.Format(CultureInfo.InvariantCulture, SR.net_http_message_not_success_statuscode, new object[]
				{
					(int)this.statusCode,
					this.ReasonPhrase
				}));
			}
			return this;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("StatusCode: ");
			stringBuilder.Append((int)this.statusCode);
			stringBuilder.Append(", ReasonPhrase: '");
			stringBuilder.Append(this.ReasonPhrase ?? "<null>");
			stringBuilder.Append("', Version: ");
			stringBuilder.Append(this.version);
			stringBuilder.Append(", Content: ");
			stringBuilder.Append((this.content == null) ? "<null>" : this.content.GetType().FullName);
			stringBuilder.Append(", Headers:\r\n");
			stringBuilder.Append(HeaderUtilities.DumpHeaders(new HttpHeaders[]
			{
				this.headers,
				(this.content == null) ? null : this.content.Headers
			}));
			return stringBuilder.ToString();
		}

		private bool ContainsNewLineCharacter(string value)
		{
			for (int i = 0; i < value.Length; i++)
			{
				char c = value[i];
				if (c == '\r' || c == '\n')
				{
					return true;
				}
			}
			return false;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing && !this.disposed)
			{
				this.disposed = true;
				if (this.content != null)
				{
					this.content.Dispose();
				}
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void CheckDisposed()
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(base.GetType().FullName);
			}
		}
	}
}
