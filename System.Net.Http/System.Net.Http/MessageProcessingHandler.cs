using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
	public abstract class MessageProcessingHandler : DelegatingHandler
	{
		protected MessageProcessingHandler()
		{
		}

		protected MessageProcessingHandler(HttpMessageHandler innerHandler) : base(innerHandler)
		{
		}

		protected abstract HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken);

		protected abstract HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken);

		protected internal sealed override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request", SR.net_http_handler_norequest);
			}
			TaskCompletionSource<HttpResponseMessage> tcs = new TaskCompletionSource<HttpResponseMessage>();
			try
			{
				HttpRequestMessage request2 = this.ProcessRequest(request, cancellationToken);
				Task<HttpResponseMessage> task2 = base.SendAsync(request2, cancellationToken);
				task2.ContinueWithStandard(delegate(Task<HttpResponseMessage> task)
				{
					if (task.IsFaulted)
					{
						tcs.TrySetException(task.Exception.GetBaseException());
						return;
					}
					if (task.IsCanceled)
					{
						tcs.TrySetCanceled();
						return;
					}
					if (task.Result == null)
					{
						tcs.TrySetException(new InvalidOperationException(SR.net_http_handler_noresponse));
						return;
					}
					try
					{
						HttpResponseMessage result = this.ProcessResponse(task.Result, cancellationToken);
						tcs.TrySetResult(result);
					}
					catch (OperationCanceledException e2)
					{
						MessageProcessingHandler.HandleCanceledOperations(cancellationToken, tcs, e2);
					}
					catch (Exception exception2)
					{
						tcs.TrySetException(exception2);
					}
				});
			}
			catch (OperationCanceledException e)
			{
				MessageProcessingHandler.HandleCanceledOperations(cancellationToken, tcs, e);
			}
			catch (Exception exception)
			{
				tcs.TrySetException(exception);
			}
			return tcs.Task;
		}

		private static void HandleCanceledOperations(CancellationToken cancellationToken, TaskCompletionSource<HttpResponseMessage> tcs, OperationCanceledException e)
		{
			if (cancellationToken.IsCancellationRequested && e.CancellationToken == cancellationToken)
			{
				tcs.TrySetCanceled();
				return;
			}
			tcs.TrySetException(e);
		}
	}
}
