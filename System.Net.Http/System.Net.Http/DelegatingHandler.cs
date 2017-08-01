using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
	public abstract class DelegatingHandler : HttpMessageHandler
	{
		private HttpMessageHandler innerHandler;

		private volatile bool operationStarted;

		private volatile bool disposed;

		public HttpMessageHandler InnerHandler
		{
			get
			{
				return this.innerHandler;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.CheckDisposedOrStarted();
				if (Logging.On)
				{
					Logging.Associate(Logging.Http, this, value);
				}
				this.innerHandler = value;
			}
		}

		protected DelegatingHandler()
		{
		}

		protected DelegatingHandler(HttpMessageHandler innerHandler)
		{
			this.InnerHandler = innerHandler;
		}

		protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request", SR.net_http_handler_norequest);
			}
			this.SetOperationStarted();
			return this.innerHandler.SendAsync(request, cancellationToken);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !this.disposed)
			{
				this.disposed = true;
				if (this.innerHandler != null)
				{
					this.innerHandler.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		private void CheckDisposed()
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(base.GetType().FullName);
			}
		}

		private void CheckDisposedOrStarted()
		{
			this.CheckDisposed();
			if (this.operationStarted)
			{
				throw new InvalidOperationException(SR.net_http_operation_started);
			}
		}

		private void SetOperationStarted()
		{
			this.CheckDisposed();
			if (this.innerHandler == null)
			{
				throw new InvalidOperationException(SR.net_http_handler_not_assigned);
			}
			if (!this.operationStarted)
			{
				this.operationStarted = true;
			}
		}
	}
}
