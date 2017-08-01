using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
	public class HttpMessageInvoker : IDisposable
	{
		private volatile bool disposed;

		private bool disposeHandler;

		private HttpMessageHandler handler;

		public HttpMessageInvoker(HttpMessageHandler handler) : this(handler, true)
		{
		}

		public HttpMessageInvoker(HttpMessageHandler handler, bool disposeHandler)
		{
			if (Logging.On)
			{
				Logging.Enter(Logging.Http, this, ".ctor", handler);
			}
			if (handler == null)
			{
				throw new ArgumentNullException("handler");
			}
			if (Logging.On)
			{
				Logging.Associate(Logging.Http, this, handler);
			}
			this.handler = handler;
			this.disposeHandler = disposeHandler;
			if (Logging.On)
			{
				Logging.Exit(Logging.Http, this, ".ctor", null);
			}
		}

		public virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			this.CheckDisposed();
			if (Logging.On)
			{
				Logging.Enter(Logging.Http, this, "SendAsync", Logging.GetObjectLogHash(request) + ": " + request);
			}
			Task<HttpResponseMessage> task = this.handler.SendAsync(request, cancellationToken);
			if (Logging.On)
			{
				Logging.Exit(Logging.Http, this, "SendAsync", task);
			}
			return task;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing && !this.disposed)
			{
				this.disposed = true;
				if (this.disposeHandler)
				{
					this.handler.Dispose();
				}
			}
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
