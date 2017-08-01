using System;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
	public class HttpClient : HttpMessageInvoker
	{
		private const HttpCompletionOption defaultCompletionOption = HttpCompletionOption.ResponseContentRead;

		private static readonly TimeSpan defaultTimeout = TimeSpan.FromSeconds(100.0);

		private static readonly TimeSpan maxTimeout = TimeSpan.FromMilliseconds(2147483647.0);

		private static readonly TimeSpan infiniteTimeout = TimeSpan.FromMilliseconds(-1.0);

		private volatile bool operationStarted;

		private volatile bool disposed;

		private CancellationTokenSource pendingRequestsCts;

		private HttpRequestHeaders defaultRequestHeaders;

		private Uri baseAddress;

		private TimeSpan timeout;

		private long maxResponseContentBufferSize;

		private TimerThread.Queue timerQueue;

		private static readonly TimerThread.Callback timeoutCallback = new TimerThread.Callback(HttpClient.TimeoutCallback);

		private TimerThread.Queue TimerQueue
		{
			get
			{
				if (this.timerQueue == null)
				{
					this.timerQueue = TimerThread.GetOrCreateQueue((int)this.timeout.TotalMilliseconds);
				}
				return this.timerQueue;
			}
		}

		public HttpRequestHeaders DefaultRequestHeaders
		{
			get
			{
				if (this.defaultRequestHeaders == null)
				{
					this.defaultRequestHeaders = new HttpRequestHeaders();
				}
				return this.defaultRequestHeaders;
			}
		}

		public Uri BaseAddress
		{
			get
			{
				return this.baseAddress;
			}
			set
			{
				HttpClient.CheckBaseAddress(value, "value");
				this.CheckDisposedOrStarted();
				if (Logging.On)
				{
					Logging.PrintInfo(Logging.Http, this, "BaseAddress: '" + this.baseAddress + "'");
				}
				this.baseAddress = value;
			}
		}

		public TimeSpan Timeout
		{
			get
			{
				return this.timeout;
			}
			set
			{
				if (value != HttpClient.infiniteTimeout && (value <= TimeSpan.Zero || value > HttpClient.maxTimeout))
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this.CheckDisposedOrStarted();
				this.timeout = value;
			}
		}

		public long MaxResponseContentBufferSize
		{
			get
			{
				return this.maxResponseContentBufferSize;
			}
			set
			{
				if (value <= 0L)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				if (value > 2147483647L)
				{
					throw new ArgumentOutOfRangeException("value", value, string.Format(CultureInfo.InvariantCulture, SR.net_http_content_buffersize_limit, new object[]
					{
						2147483647L
					}));
				}
				this.CheckDisposedOrStarted();
				this.maxResponseContentBufferSize = value;
			}
		}

		private static void TimeoutCallback(TimerThread.Timer timer, int timeNoticed, object context)
		{
			try
			{
				((CancellationTokenSource)context).Cancel();
			}
			catch (ObjectDisposedException)
			{
			}
			catch (AggregateException e)
			{
				if (Logging.On)
				{
					Logging.Exception(Logging.Http, context, "TimeoutCallback", e);
				}
			}
		}

		public HttpClient() : this(new HttpClientHandler())
		{
		}

		public HttpClient(HttpMessageHandler handler) : this(handler, true)
		{
		}

		public HttpClient(HttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler)
		{
			if (Logging.On)
			{
				Logging.Enter(Logging.Http, this, ".ctor", handler);
			}
			this.timeout = HttpClient.defaultTimeout;
			this.maxResponseContentBufferSize = 2147483647L;
			this.pendingRequestsCts = new CancellationTokenSource();
			if (Logging.On)
			{
				Logging.Exit(Logging.Http, this, ".ctor", null);
			}
		}

		public Task<string> GetStringAsync(string requestUri)
		{
			return this.GetStringAsync(this.CreateUri(requestUri));
		}

		public Task<string> GetStringAsync(Uri requestUri)
		{
			return this.GetContentAsync<string>(requestUri, HttpCompletionOption.ResponseContentRead, string.Empty, (HttpContent content) => content.ReadAsStringAsync());
		}

		public Task<byte[]> GetByteArrayAsync(string requestUri)
		{
			return this.GetByteArrayAsync(this.CreateUri(requestUri));
		}

		public Task<byte[]> GetByteArrayAsync(Uri requestUri)
		{
			return this.GetContentAsync<byte[]>(requestUri, HttpCompletionOption.ResponseContentRead, HttpUtilities.EmptyByteArray, (HttpContent content) => content.ReadAsByteArrayAsync());
		}

		public Task<Stream> GetStreamAsync(string requestUri)
		{
			return this.GetStreamAsync(this.CreateUri(requestUri));
		}

		public Task<Stream> GetStreamAsync(Uri requestUri)
		{
			return this.GetContentAsync<Stream>(requestUri, HttpCompletionOption.ResponseHeadersRead, Stream.Null, (HttpContent content) => content.ReadAsStreamAsync());
		}

		private Task<T> GetContentAsync<T>(Uri requestUri, HttpCompletionOption completionOption, T defaultValue, Func<HttpContent, Task<T>> readAs)
		{
			TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
			this.GetAsync(requestUri, completionOption).ContinueWithStandard(delegate(Task<HttpResponseMessage> requestTask)
			{
				if (HttpClient.HandleRequestFaultsAndCancelation<T>(requestTask, tcs))
				{
					return;
				}
				HttpResponseMessage result = requestTask.Result;
				if (result.Content == null)
				{
					tcs.TrySetResult(defaultValue);
					return;
				}
				try
				{
					readAs(result.Content).ContinueWithStandard(delegate(Task<T> contentTask)
					{
						if (!HttpUtilities.HandleFaultsAndCancelation<T>(contentTask, tcs))
						{
							tcs.TrySetResult(contentTask.Result);
						}
					});
				}
				catch (Exception exception)
				{
					tcs.TrySetException(exception);
				}
			});
			return tcs.Task;
		}

		public Task<HttpResponseMessage> GetAsync(string requestUri)
		{
			return this.GetAsync(this.CreateUri(requestUri));
		}

		public Task<HttpResponseMessage> GetAsync(Uri requestUri)
		{
			return this.GetAsync(requestUri, HttpCompletionOption.ResponseContentRead);
		}

		public Task<HttpResponseMessage> GetAsync(string requestUri, HttpCompletionOption completionOption)
		{
			return this.GetAsync(this.CreateUri(requestUri), completionOption);
		}

		public Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption)
		{
			return this.GetAsync(requestUri, completionOption, CancellationToken.None);
		}

		public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken)
		{
			return this.GetAsync(this.CreateUri(requestUri), cancellationToken);
		}

		public Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken)
		{
			return this.GetAsync(requestUri, HttpCompletionOption.ResponseContentRead, cancellationToken);
		}

		public Task<HttpResponseMessage> GetAsync(string requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
		{
			return this.GetAsync(this.CreateUri(requestUri), completionOption, cancellationToken);
		}

		public Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
		{
			return this.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), completionOption, cancellationToken);
		}

		public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
		{
			return this.PostAsync(this.CreateUri(requestUri), content);
		}

		public Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content)
		{
			return this.PostAsync(requestUri, content, CancellationToken.None);
		}

		public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken)
		{
			return this.PostAsync(this.CreateUri(requestUri), content, cancellationToken);
		}

		public Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
		{
			return this.SendAsync(new HttpRequestMessage(HttpMethod.Post, requestUri)
			{
				Content = content
			}, cancellationToken);
		}

		public Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content)
		{
			return this.PutAsync(this.CreateUri(requestUri), content);
		}

		public Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content)
		{
			return this.PutAsync(requestUri, content, CancellationToken.None);
		}

		public Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, CancellationToken cancellationToken)
		{
			return this.PutAsync(this.CreateUri(requestUri), content, cancellationToken);
		}

		public Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
		{
			return this.SendAsync(new HttpRequestMessage(HttpMethod.Put, requestUri)
			{
				Content = content
			}, cancellationToken);
		}

		public Task<HttpResponseMessage> DeleteAsync(string requestUri)
		{
			return this.DeleteAsync(this.CreateUri(requestUri));
		}

		public Task<HttpResponseMessage> DeleteAsync(Uri requestUri)
		{
			return this.DeleteAsync(requestUri, CancellationToken.None);
		}

		public Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken)
		{
			return this.DeleteAsync(this.CreateUri(requestUri), cancellationToken);
		}

		public Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken)
		{
			return this.SendAsync(new HttpRequestMessage(HttpMethod.Delete, requestUri), cancellationToken);
		}

		public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
		{
			return this.SendAsync(request, HttpCompletionOption.ResponseContentRead, CancellationToken.None);
		}

		public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return this.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
		}

		public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption)
		{
			return this.SendAsync(request, completionOption, CancellationToken.None);
		}

		public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}
			this.CheckDisposed();
			HttpClient.CheckRequestMessage(request);
			this.SetOperationStarted();
			this.PrepareRequestMessage(request);
			CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.pendingRequestsCts.Token);
			TimerThread.Timer timeoutTimer = this.SetTimeout(linkedCts);
			TaskCompletionSource<HttpResponseMessage> tcs = new TaskCompletionSource<HttpResponseMessage>();
			try
			{
				base.SendAsync(request, linkedCts.Token).ContinueWithStandard(delegate(Task<HttpResponseMessage> task)
				{
					try
					{
						this.DisposeRequestContent(request);
						if (task.IsFaulted)
						{
							this.SetTaskFaulted(request, linkedCts, tcs, task.Exception.GetBaseException(), timeoutTimer);
						}
						else if (task.IsCanceled)
						{
							this.SetTaskCanceled(request, linkedCts, tcs, timeoutTimer);
						}
						else
						{
							HttpResponseMessage result = task.Result;
							if (result == null)
							{
								this.SetTaskFaulted(request, linkedCts, tcs, new InvalidOperationException(SR.net_http_handler_noresponse), timeoutTimer);
							}
							else if (result.Content == null || completionOption == HttpCompletionOption.ResponseHeadersRead)
							{
								this.SetTaskCompleted(request, linkedCts, tcs, result, timeoutTimer);
							}
							else if (request.Method == HttpMethod.Head)
							{
								this.SetTaskCompleted(request, linkedCts, tcs, result, timeoutTimer);
							}
							else
							{
								this.StartContentBuffering(request, linkedCts, tcs, result, timeoutTimer);
							}
						}
					}
					catch (Exception ex)
					{
						if (Logging.On)
						{
							Logging.Exception(Logging.Http, this, "SendAsync", ex);
						}
						tcs.TrySetException(ex);
					}
				});
			}
			catch
			{
				HttpClient.DisposeTimer(timeoutTimer);
				throw;
			}
			return tcs.Task;
		}

		public void CancelPendingRequests()
		{
			this.CheckDisposed();
			if (Logging.On)
			{
				Logging.Enter(Logging.Http, this, "CancelPendingRequests", "");
			}
			CancellationTokenSource cancellationTokenSource = Interlocked.Exchange<CancellationTokenSource>(ref this.pendingRequestsCts, new CancellationTokenSource());
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
			if (Logging.On)
			{
				Logging.Exit(Logging.Http, this, "CancelPendingRequests", "");
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !this.disposed)
			{
				this.disposed = true;
				this.pendingRequestsCts.Cancel();
				this.pendingRequestsCts.Dispose();
			}
			base.Dispose(disposing);
		}

		private void DisposeRequestContent(HttpRequestMessage request)
		{
			HttpContent content = request.Content;
			if (content != null)
			{
				content.Dispose();
			}
		}

		private void StartContentBuffering(HttpRequestMessage request, CancellationTokenSource cancellationTokenSource, TaskCompletionSource<HttpResponseMessage> tcs, HttpResponseMessage response, TimerThread.Timer timeoutTimer)
		{
			response.Content.LoadIntoBufferAsync(this.maxResponseContentBufferSize).ContinueWithStandard(delegate(Task contentTask)
			{
				try
				{
					bool isCancellationRequested = cancellationTokenSource.Token.IsCancellationRequested;
					if (contentTask.IsFaulted)
					{
						response.Dispose();
						if (isCancellationRequested && contentTask.Exception.GetBaseException() is HttpRequestException)
						{
							this.SetTaskCanceled(request, cancellationTokenSource, tcs, timeoutTimer);
						}
						else
						{
							this.SetTaskFaulted(request, cancellationTokenSource, tcs, contentTask.Exception.GetBaseException(), timeoutTimer);
						}
					}
					else if (contentTask.IsCanceled)
					{
						response.Dispose();
						this.SetTaskCanceled(request, cancellationTokenSource, tcs, timeoutTimer);
					}
					else
					{
						this.SetTaskCompleted(request, cancellationTokenSource, tcs, response, timeoutTimer);
					}
				}
				catch (Exception ex)
				{
					response.Dispose();
					tcs.TrySetException(ex);
					if (Logging.On)
					{
						Logging.Exception(Logging.Http, this, "SendAsync", ex);
					}
				}
			});
		}

		private void SetOperationStarted()
		{
			if (!this.operationStarted)
			{
				this.operationStarted = true;
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

		private void CheckDisposed()
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(base.GetType().FullName);
			}
		}

		private static void CheckRequestMessage(HttpRequestMessage request)
		{
			if (!request.MarkAsSent())
			{
				throw new InvalidOperationException(SR.net_http_client_request_already_sent);
			}
		}

		private void PrepareRequestMessage(HttpRequestMessage request)
		{
			Uri uri = null;
			if (request.RequestUri == null && this.baseAddress == null)
			{
				throw new InvalidOperationException(SR.net_http_client_invalid_requesturi);
			}
			if (request.RequestUri == null)
			{
				uri = this.baseAddress;
			}
			else if (!request.RequestUri.IsAbsoluteUri)
			{
				if (this.baseAddress == null)
				{
					throw new InvalidOperationException(SR.net_http_client_invalid_requesturi);
				}
				uri = new Uri(this.baseAddress, request.RequestUri);
			}
			if (uri != null)
			{
				request.RequestUri = uri;
			}
			if (this.defaultRequestHeaders != null)
			{
				request.Headers.AddHeaders(this.defaultRequestHeaders);
			}
		}

		private static void CheckBaseAddress(Uri baseAddress, string parameterName)
		{
			if (baseAddress == null)
			{
				return;
			}
			if (!baseAddress.IsAbsoluteUri)
			{
				throw new ArgumentException(SR.net_http_client_absolute_baseaddress_required, parameterName);
			}
			if (!HttpUtilities.IsHttpUri(baseAddress))
			{
				throw new ArgumentException(SR.net_http_client_http_baseaddress_required, parameterName);
			}
		}

		private void SetTaskFaulted(HttpRequestMessage request, CancellationTokenSource cancellationTokenSource, TaskCompletionSource<HttpResponseMessage> tcs, Exception e, TimerThread.Timer timeoutTimer)
		{
			this.LogSendError(request, cancellationTokenSource, "SendAsync", e);
			tcs.TrySetException(e);
			HttpClient.DisposeCancellationTokenAndTimer(cancellationTokenSource, timeoutTimer);
		}

		private void SetTaskCanceled(HttpRequestMessage request, CancellationTokenSource cancellationTokenSource, TaskCompletionSource<HttpResponseMessage> tcs, TimerThread.Timer timeoutTimer)
		{
			this.LogSendError(request, cancellationTokenSource, "SendAsync", null);
			tcs.TrySetCanceled();
			HttpClient.DisposeCancellationTokenAndTimer(cancellationTokenSource, timeoutTimer);
		}

		private void SetTaskCompleted(HttpRequestMessage request, CancellationTokenSource cancellationTokenSource, TaskCompletionSource<HttpResponseMessage> tcs, HttpResponseMessage response, TimerThread.Timer timeoutTimer)
		{
			if (Logging.On)
			{
				Logging.PrintInfo(Logging.Http, this, string.Format(CultureInfo.InvariantCulture, SR.net_http_client_send_completed, new object[]
				{
					Logging.GetObjectLogHash(request),
					Logging.GetObjectLogHash(response),
					response
				}));
			}
			tcs.TrySetResult(response);
			HttpClient.DisposeCancellationTokenAndTimer(cancellationTokenSource, timeoutTimer);
		}

		private static void DisposeCancellationTokenAndTimer(CancellationTokenSource cancellationTokenSource, TimerThread.Timer timeoutTimer)
		{
			try
			{
				cancellationTokenSource.Dispose();
			}
			catch (ObjectDisposedException)
			{
			}
			finally
			{
				HttpClient.DisposeTimer(timeoutTimer);
			}
		}

		private static void DisposeTimer(TimerThread.Timer timeoutTimer)
		{
			if (timeoutTimer != null)
			{
				timeoutTimer.Dispose();
			}
		}

		private TimerThread.Timer SetTimeout(CancellationTokenSource cancellationTokenSource)
		{
			TimerThread.Timer result = null;
			if (this.timeout != HttpClient.infiniteTimeout)
			{
				result = this.TimerQueue.CreateTimer(HttpClient.timeoutCallback, cancellationTokenSource);
			}
			return result;
		}

		private void LogSendError(HttpRequestMessage request, CancellationTokenSource cancellationTokenSource, string method, Exception e)
		{
			if (cancellationTokenSource.IsCancellationRequested)
			{
				if (Logging.On)
				{
					Logging.PrintError(Logging.Http, this, method, string.Format(CultureInfo.InvariantCulture, SR.net_http_client_send_canceled, new object[]
					{
						Logging.GetObjectLogHash(request)
					}));
					return;
				}
			}
			else if (Logging.On)
			{
				Logging.PrintError(Logging.Http, this, method, string.Format(CultureInfo.InvariantCulture, SR.net_http_client_send_error, new object[]
				{
					Logging.GetObjectLogHash(request),
					e
				}));
			}
		}

		private Uri CreateUri(string uri)
		{
			if (string.IsNullOrEmpty(uri))
			{
				return null;
			}
			return new Uri(uri, UriKind.RelativeOrAbsolute);
		}

		private static bool HandleRequestFaultsAndCancelation<T>(Task<HttpResponseMessage> task, TaskCompletionSource<T> tcs)
		{
			if (HttpUtilities.HandleFaultsAndCancelation<T>(task, tcs))
			{
				return true;
			}
			HttpResponseMessage result = task.Result;
			if (!result.IsSuccessStatusCode)
			{
				if (result.Content != null)
				{
					result.Content.Dispose();
				}
				tcs.TrySetException(new HttpRequestException(string.Format(CultureInfo.InvariantCulture, SR.net_http_message_not_success_statuscode, new object[]
				{
					(int)result.StatusCode,
					result.ReasonPhrase
				})));
				return true;
			}
			return false;
		}
	}
}
