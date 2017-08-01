using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
	public class HttpClientHandler : HttpMessageHandler
	{
		private class RequestState
		{
			internal HttpWebRequest webRequest;

			internal TaskCompletionSource<HttpResponseMessage> tcs;

			internal CancellationToken cancellationToken;

			internal HttpRequestMessage requestMessage;

			internal Stream requestStream;

			internal WindowsIdentity identity;
		}

		private class WebExceptionWrapperStream : DelegatingStream
		{
			internal WebExceptionWrapperStream(Stream innerStream) : base(innerStream)
			{
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				int result;
				try
				{
					result = base.Read(buffer, offset, count);
				}
				catch (WebException innerException)
				{
					throw new IOException(SR.net_http_read_error, innerException);
				}
				return result;
			}

			public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				IAsyncResult result;
				try
				{
					result = base.BeginRead(buffer, offset, count, callback, state);
				}
				catch (WebException innerException)
				{
					throw new IOException(SR.net_http_read_error, innerException);
				}
				return result;
			}

			public override int EndRead(IAsyncResult asyncResult)
			{
				int result;
				try
				{
					result = base.EndRead(asyncResult);
				}
				catch (WebException innerException)
				{
					throw new IOException(SR.net_http_read_error, innerException);
				}
				return result;
			}

			public override int ReadByte()
			{
				int result;
				try
				{
					result = base.ReadByte();
				}
				catch (WebException innerException)
				{
					throw new IOException(SR.net_http_read_error, innerException);
				}
				return result;
			}
		}

		private static readonly Action<object> onCancel = new Action<object>(HttpClientHandler.OnCancel);

		private readonly Action<object> startRequest;

		private readonly AsyncCallback getRequestStreamCallback;

		private readonly AsyncCallback getResponseCallback;

		private volatile bool operationStarted;

		private volatile bool disposed;

		private long maxRequestContentBufferSize;

		private CookieContainer cookieContainer;

		private bool useCookies;

		private DecompressionMethods automaticDecompression;

		private IWebProxy proxy;

		private bool useProxy;

		private bool preAuthenticate;

		private bool useDefaultCredentials;

		private ICredentials credentials;

		private bool allowAutoRedirect;

		private int maxAutomaticRedirections;

		private string connectionGroupName;

		private ClientCertificateOption clientCertOptions;

		private Uri lastUsedRequestUri;

		public virtual bool SupportsAutomaticDecompression
		{
			get
			{
				return true;
			}
		}

		public virtual bool SupportsProxy
		{
			get
			{
				return true;
			}
		}

		public virtual bool SupportsRedirectConfiguration
		{
			get
			{
				return true;
			}
		}

		public bool UseCookies
		{
			get
			{
				return this.useCookies;
			}
			set
			{
				this.CheckDisposedOrStarted();
				this.useCookies = value;
			}
		}

		public CookieContainer CookieContainer
		{
			get
			{
				return this.cookieContainer;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (!this.UseCookies)
				{
					throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.net_http_invalid_enable_first, new object[]
					{
						"UseCookies",
						"true"
					}));
				}
				this.CheckDisposedOrStarted();
				this.cookieContainer = value;
			}
		}

		public ClientCertificateOption ClientCertificateOptions
		{
			get
			{
				return this.clientCertOptions;
			}
			set
			{
				if (value != ClientCertificateOption.Manual && value != ClientCertificateOption.Automatic)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this.CheckDisposedOrStarted();
				this.clientCertOptions = value;
			}
		}

		public DecompressionMethods AutomaticDecompression
		{
			get
			{
				return this.automaticDecompression;
			}
			set
			{
				this.CheckDisposedOrStarted();
				this.automaticDecompression = value;
			}
		}

		public bool UseProxy
		{
			get
			{
				return this.useProxy;
			}
			set
			{
				this.CheckDisposedOrStarted();
				this.useProxy = value;
			}
		}

		public IWebProxy Proxy
		{
			get
			{
				return this.proxy;
			}
			[SecuritySafeCritical]
			set
			{
				if (!this.UseProxy && value != null)
				{
					throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.net_http_invalid_enable_first, new object[]
					{
						"UseProxy",
						"true"
					}));
				}
				this.CheckDisposedOrStarted();
				ExceptionHelper.WebPermissionUnrestricted.Demand();
				this.proxy = value;
			}
		}

		public bool PreAuthenticate
		{
			get
			{
				return this.preAuthenticate;
			}
			set
			{
				this.CheckDisposedOrStarted();
				this.preAuthenticate = value;
			}
		}

		public bool UseDefaultCredentials
		{
			get
			{
				return this.useDefaultCredentials;
			}
			set
			{
				this.CheckDisposedOrStarted();
				this.useDefaultCredentials = value;
			}
		}

		public ICredentials Credentials
		{
			get
			{
				return this.credentials;
			}
			set
			{
				this.CheckDisposedOrStarted();
				this.credentials = value;
			}
		}

		public bool AllowAutoRedirect
		{
			get
			{
				return this.allowAutoRedirect;
			}
			set
			{
				this.CheckDisposedOrStarted();
				this.allowAutoRedirect = value;
			}
		}

		public int MaxAutomaticRedirections
		{
			get
			{
				return this.maxAutomaticRedirections;
			}
			set
			{
				if (value <= 0)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this.CheckDisposedOrStarted();
				this.maxAutomaticRedirections = value;
			}
		}

		public long MaxRequestContentBufferSize
		{
			get
			{
				return this.maxRequestContentBufferSize;
			}
			set
			{
				if (value < 0L)
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
				this.maxRequestContentBufferSize = value;
			}
		}

		public HttpClientHandler()
		{
			this.startRequest = new Action<object>(this.StartRequest);
			this.getRequestStreamCallback = new AsyncCallback(this.GetRequestStreamCallback);
			this.getResponseCallback = new AsyncCallback(this.GetResponseCallback);
			this.connectionGroupName = RuntimeHelpers.GetHashCode(this).ToString(NumberFormatInfo.InvariantInfo);
			this.allowAutoRedirect = true;
			this.maxRequestContentBufferSize = 2147483647L;
			this.automaticDecompression = DecompressionMethods.None;
			this.cookieContainer = new CookieContainer();
			this.credentials = null;
			this.maxAutomaticRedirections = 50;
			this.preAuthenticate = false;
			this.proxy = null;
			this.useProxy = true;
			this.useCookies = true;
			this.useDefaultCredentials = false;
			this.clientCertOptions = ClientCertificateOption.Manual;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !this.disposed)
			{
				this.disposed = true;
				if (this.lastUsedRequestUri != null)
				{
					ServicePoint servicePoint = ServicePointManager.FindServicePoint(this.lastUsedRequestUri);
					if (servicePoint != null)
					{
						servicePoint.CloseConnectionGroup(this.connectionGroupName);
					}
				}
			}
			base.Dispose(disposing);
		}

		private HttpWebRequest CreateAndPrepareWebRequest(HttpRequestMessage request)
		{
			HttpWebRequest httpWebRequest = WebRequest.CreateDefault(request.RequestUri) as HttpWebRequest;
			httpWebRequest.ConnectionGroupName = this.connectionGroupName;
			if (Logging.On)
			{
				Logging.Associate(Logging.Http, request, httpWebRequest);
			}
			httpWebRequest.Method = request.Method.Method;
			httpWebRequest.ProtocolVersion = request.Version;
			this.SetDefaultOptions(httpWebRequest);
			HttpClientHandler.SetConnectionOptions(httpWebRequest, request);
			this.SetServicePointOptions(httpWebRequest, request);
			HttpClientHandler.SetRequestHeaders(httpWebRequest, request);
			HttpClientHandler.SetContentHeaders(httpWebRequest, request);
			this.InitializeWebRequest(request, httpWebRequest);
			return httpWebRequest;
		}

		internal virtual void InitializeWebRequest(HttpRequestMessage request, HttpWebRequest webRequest)
		{
		}

		private void SetDefaultOptions(HttpWebRequest webRequest)
		{
			webRequest.Timeout = -1;
			webRequest.AllowAutoRedirect = this.allowAutoRedirect;
			webRequest.AutomaticDecompression = this.automaticDecompression;
			webRequest.PreAuthenticate = this.preAuthenticate;
			if (this.useDefaultCredentials)
			{
				webRequest.UseDefaultCredentials = true;
			}
			else
			{
				webRequest.Credentials = this.credentials;
			}
			if (this.allowAutoRedirect)
			{
				webRequest.MaximumAutomaticRedirections = this.maxAutomaticRedirections;
			}
			if (this.useProxy)
			{
				if (this.proxy != null)
				{
					webRequest.Proxy = this.proxy;
				}
			}
			else
			{
				webRequest.Proxy = null;
			}
			if (this.useCookies)
			{
				webRequest.CookieContainer = this.cookieContainer;
			}
		}

		private static void SetConnectionOptions(HttpWebRequest webRequest, HttpRequestMessage request)
		{
			if (request.Version <= HttpVersion.Version10)
			{
				bool keepAlive = false;
				foreach (string current in request.Headers.Connection)
				{
					if (string.Compare(current, "Keep-Alive", StringComparison.OrdinalIgnoreCase) == 0)
					{
						keepAlive = true;
						break;
					}
				}
				webRequest.KeepAlive = keepAlive;
				return;
			}
			if (request.Headers.ConnectionClose == true)
			{
				webRequest.KeepAlive = false;
			}
		}

		private void SetServicePointOptions(HttpWebRequest webRequest, HttpRequestMessage request)
		{
			HttpRequestHeaders headers = request.Headers;
			bool? expectContinue = headers.ExpectContinue;
			if (expectContinue.HasValue)
			{
				ServicePoint servicePoint = webRequest.ServicePoint;
				servicePoint.Expect100Continue = expectContinue.Value;
			}
		}

		private static void SetRequestHeaders(HttpWebRequest webRequest, HttpRequestMessage request)
		{
			WebHeaderCollection headers = webRequest.Headers;
			HttpRequestHeaders headers2 = request.Headers;
			bool flag = headers2.Contains("Host");
			bool flag2 = headers2.Contains("Expect");
			bool flag3 = headers2.Contains("Transfer-Encoding");
			bool flag4 = headers2.Contains("Connection");
			bool flag5 = headers2.Contains("Accept");
			bool flag6 = headers2.Contains("Range");
			bool flag7 = headers2.Contains("Referer");
			bool flag8 = headers2.Contains("User-Agent");
			bool flag9 = headers2.Contains("Date");
			bool flag10 = headers2.Contains("If-Modified-Since");
			if (flag9)
			{
				DateTimeOffset? date = headers2.Date;
				if (date.HasValue)
				{
					webRequest.Date = date.Value.UtcDateTime;
				}
			}
			if (flag10)
			{
				DateTimeOffset? ifModifiedSince = headers2.IfModifiedSince;
				if (ifModifiedSince.HasValue)
				{
					webRequest.IfModifiedSince = ifModifiedSince.Value.UtcDateTime;
				}
			}
			if (flag6)
			{
				RangeHeaderValue range = headers2.Range;
				if (range != null)
				{
					foreach (RangeItemHeaderValue current in range.Ranges)
					{
						if (!current.To.HasValue || !current.From.HasValue)
						{
							long? num = -current.To;
							webRequest.AddRange((num.HasValue ? new long?(num.GetValueOrDefault()) : current.From).Value);
						}
						else
						{
							webRequest.AddRange(current.From.Value, current.To.Value);
						}
					}
				}
			}
			if (flag7)
			{
				Uri referrer = headers2.Referrer;
				if (referrer != null)
				{
					webRequest.Referer = referrer.OriginalString;
				}
			}
			if (flag5 && headers2.Accept.Count > 0)
			{
				webRequest.Accept = headers2.Accept.ToString();
			}
			if (flag8 && headers2.UserAgent.Count > 0)
			{
				webRequest.UserAgent = headers2.UserAgent.ToString();
			}
			if (flag)
			{
				string host = headers2.Host;
				if (host != null)
				{
					webRequest.Host = host;
				}
			}
			if (flag2)
			{
				string headerStringWithoutSpecial = headers2.Expect.GetHeaderStringWithoutSpecial();
				if (!string.IsNullOrEmpty(headerStringWithoutSpecial) || !headers2.Expect.IsSpecialValueSet)
				{
					webRequest.Expect = headerStringWithoutSpecial;
				}
			}
			if (flag3)
			{
				string headerStringWithoutSpecial2 = headers2.TransferEncoding.GetHeaderStringWithoutSpecial();
				if (!string.IsNullOrEmpty(headerStringWithoutSpecial2) || !headers2.TransferEncoding.IsSpecialValueSet)
				{
					webRequest.SendChunked = true;
					webRequest.TransferEncoding = headerStringWithoutSpecial2;
					webRequest.SendChunked = false;
				}
			}
			if (flag4)
			{
				string text = string.Join(", ", from item in headers2.Connection
				where string.Compare(item, "Keep-Alive", StringComparison.OrdinalIgnoreCase) != 0 && string.Compare(item, "close", StringComparison.OrdinalIgnoreCase) != 0
				select item);
				if (!string.IsNullOrEmpty(text) || !headers2.Connection.IsSpecialValueSet)
				{
					webRequest.Connection = text;
				}
			}
			foreach (KeyValuePair<string, string> current2 in request.Headers.GetHeaderStrings())
			{
				string key = current2.Key;
				if ((!flag || !HttpClientHandler.AreEqual("Host", key)) && (!flag2 || !HttpClientHandler.AreEqual("Expect", key)) && (!flag3 || !HttpClientHandler.AreEqual("Transfer-Encoding", key)) && (!flag5 || !HttpClientHandler.AreEqual("Accept", key)) && (!flag6 || !HttpClientHandler.AreEqual("Range", key)) && (!flag7 || !HttpClientHandler.AreEqual("Referer", key)) && (!flag8 || !HttpClientHandler.AreEqual("User-Agent", key)) && (!flag9 || !HttpClientHandler.AreEqual("Date", key)) && (!flag10 || !HttpClientHandler.AreEqual("If-Modified-Since", key)) && (!flag4 || !HttpClientHandler.AreEqual("Connection", key)))
				{
					headers.Add(current2.Key, current2.Value);
				}
			}
		}

		private static void SetContentHeaders(HttpWebRequest webRequest, HttpRequestMessage request)
		{
			if (request.Content != null)
			{
				HttpContentHeaders headers = request.Content.Headers;
				if (headers.Contains("Content-Length"))
				{
					using (IEnumerator<KeyValuePair<string, IEnumerable<string>>> enumerator = request.Content.Headers.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							KeyValuePair<string, IEnumerable<string>> current = enumerator.Current;
							if (string.Compare("Content-Length", current.Key, StringComparison.OrdinalIgnoreCase) != 0)
							{
								HttpClientHandler.SetContentHeader(webRequest, current);
							}
						}
						return;
					}
				}
				foreach (KeyValuePair<string, IEnumerable<string>> current2 in request.Content.Headers)
				{
					HttpClientHandler.SetContentHeader(webRequest, current2);
				}
			}
		}

		private static void SetContentHeader(HttpWebRequest webRequest, KeyValuePair<string, IEnumerable<string>> header)
		{
			if (string.Compare("Content-Type", header.Key, StringComparison.OrdinalIgnoreCase) == 0)
			{
				webRequest.ContentType = string.Join(", ", header.Value);
				return;
			}
			webRequest.Headers.Add(header.Key, string.Join(", ", header.Value));
		}

		protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request", SR.net_http_handler_norequest);
			}
			this.CheckDisposed();
			if (Logging.On)
			{
				Logging.Enter(Logging.Http, this, "SendAsync", request);
			}
			this.SetOperationStarted();
			TaskCompletionSource<HttpResponseMessage> taskCompletionSource = new TaskCompletionSource<HttpResponseMessage>();
			HttpClientHandler.RequestState requestState = new HttpClientHandler.RequestState();
			requestState.tcs = taskCompletionSource;
			requestState.cancellationToken = cancellationToken;
			requestState.requestMessage = request;
			this.lastUsedRequestUri = request.RequestUri;
			try
			{
				HttpWebRequest httpWebRequest = this.CreateAndPrepareWebRequest(request);
				requestState.webRequest = httpWebRequest;
				cancellationToken.Register(HttpClientHandler.onCancel, httpWebRequest);
				if (ExecutionContext.IsFlowSuppressed())
				{
					IWebProxy webProxy = null;
					if (this.useProxy)
					{
						webProxy = (this.proxy ?? WebRequest.DefaultWebProxy);
					}
					if (this.UseDefaultCredentials || this.Credentials != null || (webProxy != null && webProxy.Credentials != null))
					{
						this.SafeCaptureIdenity(requestState);
					}
				}
				Task.Factory.StartNew(this.startRequest, requestState);
			}
			catch (Exception e)
			{
				this.HandleAsyncException(requestState, e);
			}
			if (Logging.On)
			{
				Logging.Exit(Logging.Http, this, "SendAsync", taskCompletionSource.Task);
			}
			return taskCompletionSource.Task;
		}

		private void StartRequest(object obj)
		{
			HttpClientHandler.RequestState requestState = obj as HttpClientHandler.RequestState;
			try
			{
				if (requestState.requestMessage.Content != null)
				{
					this.PrepareAndStartContentUpload(requestState);
				}
				else
				{
					requestState.webRequest.ContentLength = 0L;
					this.StartGettingResponse(requestState);
				}
			}
			catch (Exception e)
			{
				this.HandleAsyncException(requestState, e);
			}
		}

		private void PrepareAndStartContentUpload(HttpClientHandler.RequestState state)
		{
			HttpContent requestContent = state.requestMessage.Content;
			try
			{
				if (state.requestMessage.Headers.TransferEncodingChunked == true)
				{
					state.webRequest.SendChunked = true;
					this.StartGettingRequestStream(state);
				}
				else
				{
					long? contentLength = requestContent.Headers.ContentLength;
					if (contentLength.HasValue)
					{
						state.webRequest.ContentLength = contentLength.Value;
						this.StartGettingRequestStream(state);
					}
					else
					{
						if (this.maxRequestContentBufferSize == 0L)
						{
							throw new HttpRequestException(SR.net_http_handler_nocontentlength);
						}
						requestContent.LoadIntoBufferAsync(this.maxRequestContentBufferSize).ContinueWithStandard(delegate(Task task)
						{
							try
							{
								if (task.IsFaulted)
								{
									this.HandleAsyncException(state, task.Exception.GetBaseException());
								}
								else
								{
									contentLength = requestContent.Headers.ContentLength;
									state.webRequest.ContentLength = contentLength.Value;
									this.StartGettingRequestStream(state);
								}
							}
							catch (Exception e2)
							{
								this.HandleAsyncException(state, e2);
							}
						});
					}
				}
			}
			catch (Exception e)
			{
				this.HandleAsyncException(state, e);
			}
		}

		private void StartGettingRequestStream(HttpClientHandler.RequestState state)
		{
			if (state.identity != null)
			{
				using (state.identity.Impersonate())
				{
					state.webRequest.BeginGetRequestStream(this.getRequestStreamCallback, state);
					return;
				}
			}
			state.webRequest.BeginGetRequestStream(this.getRequestStreamCallback, state);
		}

		private void GetRequestStreamCallback(IAsyncResult ar)
		{
			HttpClientHandler.RequestState state = ar.AsyncState as HttpClientHandler.RequestState;
			try
			{
				TransportContext context = null;
				Stream stream = state.webRequest.EndGetRequestStream(ar, out context);
				state.requestStream = stream;
				state.requestMessage.Content.CopyToAsync(stream, context).ContinueWithStandard(delegate(Task task)
				{
					try
					{
						if (task.IsFaulted)
						{
							this.HandleAsyncException(state, task.Exception.GetBaseException());
						}
						else if (task.IsCanceled)
						{
							state.tcs.TrySetCanceled();
						}
						else
						{
							state.requestStream.Close();
							this.StartGettingResponse(state);
						}
					}
					catch (Exception e2)
					{
						this.HandleAsyncException(state, e2);
					}
				});
			}
			catch (Exception e)
			{
				this.HandleAsyncException(state, e);
			}
		}

		private void StartGettingResponse(HttpClientHandler.RequestState state)
		{
			if (state.identity != null)
			{
				using (state.identity.Impersonate())
				{
					state.webRequest.BeginGetResponse(this.getResponseCallback, state);
					return;
				}
			}
			state.webRequest.BeginGetResponse(this.getResponseCallback, state);
		}

		private void GetResponseCallback(IAsyncResult ar)
		{
			HttpClientHandler.RequestState requestState = ar.AsyncState as HttpClientHandler.RequestState;
			try
			{
				HttpWebResponse webResponse = requestState.webRequest.EndGetResponse(ar) as HttpWebResponse;
				requestState.tcs.TrySetResult(this.CreateResponseMessage(webResponse, requestState.requestMessage));
			}
			catch (Exception e)
			{
				this.HandleAsyncException(requestState, e);
			}
		}

		private bool TryGetExceptionResponse(WebException webException, HttpRequestMessage requestMessage, out HttpResponseMessage httpResponseMessage)
		{
			if (webException != null && webException.Response != null)
			{
				HttpWebResponse httpWebResponse = webException.Response as HttpWebResponse;
				if (httpWebResponse != null)
				{
					httpResponseMessage = this.CreateResponseMessage(httpWebResponse, requestMessage);
					return true;
				}
			}
			httpResponseMessage = null;
			return false;
		}

		private HttpResponseMessage CreateResponseMessage(HttpWebResponse webResponse, HttpRequestMessage request)
		{
			HttpResponseMessage httpResponseMessage = new HttpResponseMessage(webResponse.StatusCode);
			httpResponseMessage.ReasonPhrase = webResponse.StatusDescription;
			httpResponseMessage.Version = webResponse.ProtocolVersion;
			httpResponseMessage.RequestMessage = request;
			httpResponseMessage.Content = new StreamContent(new HttpClientHandler.WebExceptionWrapperStream(webResponse.GetResponseStream()));
			request.RequestUri = webResponse.ResponseUri;
			WebHeaderCollection headers = webResponse.Headers;
			HttpContentHeaders headers2 = httpResponseMessage.Content.Headers;
			HttpResponseHeaders headers3 = httpResponseMessage.Headers;
			if (webResponse.ContentLength >= 0L)
			{
				headers2.ContentLength = new long?(webResponse.ContentLength);
			}
			for (int i = 0; i < headers.Count; i++)
			{
				string key = headers.GetKey(i);
				if (string.Compare(key, "Content-Length", StringComparison.OrdinalIgnoreCase) != 0)
				{
					string[] values = headers.GetValues(i);
					if (!headers3.TryAddWithoutValidation(key, values))
					{
						headers2.TryAddWithoutValidation(key, values);
					}
				}
			}
			return httpResponseMessage;
		}

		private void HandleAsyncException(HttpClientHandler.RequestState state, Exception e)
		{
			if (Logging.On)
			{
				Logging.Exception(Logging.Http, this, "SendAsync", e);
			}
			HttpResponseMessage result;
			if (this.TryGetExceptionResponse(e as WebException, state.requestMessage, out result))
			{
				state.tcs.TrySetResult(result);
				return;
			}
			if (state.cancellationToken.IsCancellationRequested)
			{
				state.tcs.TrySetCanceled();
				return;
			}
			if (e is WebException || e is IOException)
			{
				state.tcs.TrySetException(new HttpRequestException(SR.net_http_client_execution_error, e));
				return;
			}
			state.tcs.TrySetException(e);
		}

		private static void OnCancel(object state)
		{
			HttpWebRequest httpWebRequest = state as HttpWebRequest;
			httpWebRequest.Abort();
		}

		private void SetOperationStarted()
		{
			if (!this.operationStarted)
			{
				this.operationStarted = true;
			}
		}

		private void CheckDisposed()
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException(base.GetType().FullName);
			}
		}

		internal void CheckDisposedOrStarted()
		{
			this.CheckDisposed();
			if (this.operationStarted)
			{
				throw new InvalidOperationException(SR.net_http_operation_started);
			}
		}

		private static bool AreEqual(string x, string y)
		{
			return string.Compare(x, y, StringComparison.OrdinalIgnoreCase) == 0;
		}

		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.ControlPrincipal)]
		private void SafeCaptureIdenity(HttpClientHandler.RequestState state)
		{
			state.identity = WindowsIdentity.GetCurrent();
		}
	}
}
