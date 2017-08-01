using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
	public abstract class HttpMessageHandler : IDisposable
	{
		protected HttpMessageHandler()
		{
			if (Logging.On)
			{
				Logging.Enter(Logging.Http, this, ".ctor", null);
			}
			if (Logging.On)
			{
				Logging.Exit(Logging.Http, this, ".ctor", null);
			}
		}

		protected internal abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);

		protected virtual void Dispose(bool disposing)
		{
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
