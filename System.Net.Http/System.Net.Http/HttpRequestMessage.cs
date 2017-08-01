using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace System.Net.Http
{
	public class HttpRequestMessage : IDisposable
	{
		private const int messageAlreadySent = 1;

		private const int messageNotYetSent = 0;

		private int sendStatus;

		private HttpMethod method;

		private Uri requestUri;

		private HttpRequestHeaders headers;

		private Version version;

		private HttpContent content;

		private bool disposed;

		private IDictionary<string, object> properties;

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

		public HttpMethod Method
		{
			get
			{
				return this.method;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.CheckDisposed();
				this.method = value;
			}
		}

		public Uri RequestUri
		{
			get
			{
				return this.requestUri;
			}
			set
			{
				if (value != null && value.IsAbsoluteUri && !HttpUtilities.IsHttpUri(value))
				{
					throw new ArgumentException(SR.net_http_client_http_baseaddress_required, "value");
				}
				this.CheckDisposed();
				this.requestUri = value;
			}
		}

		public HttpRequestHeaders Headers
		{
			get
			{
				if (this.headers == null)
				{
					this.headers = new HttpRequestHeaders();
				}
				return this.headers;
			}
		}

		public IDictionary<string, object> Properties
		{
			get
			{
				if (this.properties == null)
				{
					this.properties = new Dictionary<string, object>();
				}
				return this.properties;
			}
		}

		public HttpRequestMessage() : this(HttpMethod.Get, null)
		{
		}

		public HttpRequestMessage(HttpMethod method, Uri requestUri)
		{
			if (Logging.On)
			{
				Logging.Enter(Logging.Http, this, ".ctor", string.Concat(new object[]
				{
					"Method: ",
					method,
					", Uri: '",
					requestUri,
					"'"
				}));
			}
			this.InitializeValues(method, requestUri);
			if (Logging.On)
			{
				Logging.Exit(Logging.Http, this, ".ctor", null);
			}
		}

		public HttpRequestMessage(HttpMethod method, string requestUri)
		{
			if (Logging.On)
			{
				Logging.Enter(Logging.Http, this, ".ctor", string.Concat(new object[]
				{
					"Method: ",
					method,
					", Uri: '",
					requestUri,
					"'"
				}));
			}
			if (string.IsNullOrEmpty(requestUri))
			{
				this.InitializeValues(method, null);
			}
			else
			{
				this.InitializeValues(method, new Uri(requestUri, UriKind.RelativeOrAbsolute));
			}
			if (Logging.On)
			{
				Logging.Exit(Logging.Http, this, ".ctor", null);
			}
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Method: ");
			stringBuilder.Append(this.method);
			stringBuilder.Append(", RequestUri: '");
			stringBuilder.Append((this.requestUri == null) ? "<null>" : this.requestUri.ToString());
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

		private void InitializeValues(HttpMethod method, Uri requestUri)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			if (requestUri != null && requestUri.IsAbsoluteUri && !HttpUtilities.IsHttpUri(requestUri))
			{
				throw new ArgumentException(SR.net_http_client_http_baseaddress_required, "requestUri");
			}
			this.method = method;
			this.requestUri = requestUri;
			this.version = HttpUtilities.DefaultVersion;
		}

		internal bool MarkAsSent()
		{
			return Interlocked.Exchange(ref this.sendStatus, 1) == 0;
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
