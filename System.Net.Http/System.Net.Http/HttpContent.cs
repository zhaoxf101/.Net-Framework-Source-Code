using System;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http
{
	public abstract class HttpContent : IDisposable
	{
		private class LimitMemoryStream : MemoryStream
		{
			private int maxSize;

			public LimitMemoryStream(int maxSize, int capacity) : base(capacity)
			{
				this.maxSize = maxSize;
			}

			public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				this.CheckSize(count);
				return base.BeginWrite(buffer, offset, count, callback, state);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				this.CheckSize(count);
				base.Write(buffer, offset, count);
			}

			public override void WriteByte(byte value)
			{
				this.CheckSize(1);
				base.WriteByte(value);
			}

			private void CheckSize(int countToAdd)
			{
				if ((long)this.maxSize - this.Length < (long)countToAdd)
				{
					throw new HttpRequestException(string.Format(CultureInfo.InvariantCulture, SR.net_http_content_buffersize_exceeded, new object[]
					{
						this.maxSize
					}));
				}
			}
		}

		internal const long MaxBufferSize = 2147483647L;

		private HttpContentHeaders headers;

		private MemoryStream bufferedContent;

		private bool disposed;

		private Stream contentReadStream;

		private bool canCalculateLength;

		internal static readonly Encoding DefaultStringEncoding = Encoding.UTF8;

		private static Encoding[] EncodingsWithBom = new Encoding[]
		{
			Encoding.UTF8,
			Encoding.UTF32,
			Encoding.Unicode,
			Encoding.BigEndianUnicode
		};

		public HttpContentHeaders Headers
		{
			get
			{
				if (this.headers == null)
				{
					this.headers = new HttpContentHeaders(new Func<long?>(this.GetComputedOrBufferLength));
				}
				return this.headers;
			}
		}

		private bool IsBuffered
		{
			get
			{
				return this.bufferedContent != null;
			}
		}

		protected HttpContent()
		{
			if (Logging.On)
			{
				Logging.Enter(Logging.Http, this, ".ctor", null);
			}
			this.canCalculateLength = true;
			if (Logging.On)
			{
				Logging.Exit(Logging.Http, this, ".ctor", null);
			}
		}

		public Task<string> ReadAsStringAsync()
		{
			this.CheckDisposed();
			TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
			this.LoadIntoBufferAsync().ContinueWithStandard(delegate(Task task)
			{
				if (HttpUtilities.HandleFaultsAndCancelation<string>(task, tcs))
				{
					return;
				}
				if (this.bufferedContent.Length == 0L)
				{
					tcs.TrySetResult(string.Empty);
					return;
				}
				Encoding encoding = null;
				int num = -1;
				byte[] buffer = this.bufferedContent.GetBuffer();
				int num2 = (int)this.bufferedContent.Length;
				if (this.Headers.ContentType != null && this.Headers.ContentType.CharSet != null)
				{
					try
					{
						encoding = Encoding.GetEncoding(this.Headers.ContentType.CharSet);
					}
					catch (ArgumentException innerException)
					{
						tcs.TrySetException(new InvalidOperationException(SR.net_http_content_invalid_charset, innerException));
						return;
					}
				}
				if (encoding == null)
				{
					Encoding[] encodingsWithBom = HttpContent.EncodingsWithBom;
					for (int i = 0; i < encodingsWithBom.Length; i++)
					{
						Encoding encoding2 = encodingsWithBom[i];
						byte[] preamble = encoding2.GetPreamble();
						if (HttpContent.ByteArrayHasPrefix(buffer, num2, preamble))
						{
							encoding = encoding2;
							num = preamble.Length;
							break;
						}
					}
				}
				encoding = (encoding ?? HttpContent.DefaultStringEncoding);
				if (num == -1)
				{
					byte[] preamble2 = encoding.GetPreamble();
					if (HttpContent.ByteArrayHasPrefix(buffer, num2, preamble2))
					{
						num = preamble2.Length;
					}
					else
					{
						num = 0;
					}
				}
				try
				{
					string @string = encoding.GetString(buffer, num, num2 - num);
					tcs.TrySetResult(@string);
				}
				catch (Exception exception)
				{
					tcs.TrySetException(exception);
				}
			});
			return tcs.Task;
		}

		public Task<byte[]> ReadAsByteArrayAsync()
		{
			this.CheckDisposed();
			TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>();
			this.LoadIntoBufferAsync().ContinueWithStandard(delegate(Task task)
			{
				if (!HttpUtilities.HandleFaultsAndCancelation<byte[]>(task, tcs))
				{
					tcs.TrySetResult(this.bufferedContent.ToArray());
				}
			});
			return tcs.Task;
		}

		public Task<Stream> ReadAsStreamAsync()
		{
			this.CheckDisposed();
			TaskCompletionSource<Stream> tcs = new TaskCompletionSource<Stream>();
			if (this.contentReadStream == null && this.IsBuffered)
			{
				this.contentReadStream = new MemoryStream(this.bufferedContent.GetBuffer(), 0, (int)this.bufferedContent.Length, false);
			}
			if (this.contentReadStream != null)
			{
				tcs.TrySetResult(this.contentReadStream);
				return tcs.Task;
			}
			this.CreateContentReadStreamAsync().ContinueWithStandard(delegate(Task<Stream> task)
			{
				if (!HttpUtilities.HandleFaultsAndCancelation<Stream>(task, tcs))
				{
					this.contentReadStream = task.Result;
					tcs.TrySetResult(this.contentReadStream);
				}
			});
			return tcs.Task;
		}

		protected abstract Task SerializeToStreamAsync(Stream stream, TransportContext context);

		public Task CopyToAsync(Stream stream, TransportContext context)
		{
			this.CheckDisposed();
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
			try
			{
				Task task;
				if (this.IsBuffered)
				{
					task = Task.Factory.FromAsync<byte[], int, int>(new Func<byte[], int, int, AsyncCallback, object, IAsyncResult>(stream.BeginWrite), new Action<IAsyncResult>(stream.EndWrite), this.bufferedContent.GetBuffer(), 0, (int)this.bufferedContent.Length, null);
				}
				else
				{
					task = this.SerializeToStreamAsync(stream, context);
					this.CheckTaskNotNull(task);
				}
				task.ContinueWithStandard(delegate(Task copyTask)
				{
					if (copyTask.IsFaulted)
					{
						tcs.TrySetException(HttpContent.GetStreamCopyException(copyTask.Exception.GetBaseException()));
						return;
					}
					if (copyTask.IsCanceled)
					{
						tcs.TrySetCanceled();
						return;
					}
					tcs.TrySetResult(null);
				});
			}
			catch (IOException originalException)
			{
				tcs.TrySetException(HttpContent.GetStreamCopyException(originalException));
			}
			catch (ObjectDisposedException originalException2)
			{
				tcs.TrySetException(HttpContent.GetStreamCopyException(originalException2));
			}
			return tcs.Task;
		}

		public Task CopyToAsync(Stream stream)
		{
			return this.CopyToAsync(stream, null);
		}

		internal void CopyTo(Stream stream)
		{
			this.CopyToAsync(stream).Wait();
		}

		public Task LoadIntoBufferAsync()
		{
			return this.LoadIntoBufferAsync(2147483647L);
		}

		public Task LoadIntoBufferAsync(long maxBufferSize)
		{
			this.CheckDisposed();
			if (maxBufferSize > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("maxBufferSize", maxBufferSize, string.Format(CultureInfo.InvariantCulture, SR.net_http_content_buffersize_limit, new object[]
				{
					2147483647L
				}));
			}
			if (this.IsBuffered)
			{
				return HttpContent.CreateCompletedTask();
			}
			TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
			Exception exception = null;
			MemoryStream tempBuffer = this.CreateMemoryStream(maxBufferSize, out exception);
			if (tempBuffer == null)
			{
				tcs.TrySetException(exception);
			}
			else
			{
				try
				{
					Task task = this.SerializeToStreamAsync(tempBuffer, null);
					this.CheckTaskNotNull(task);
					task.ContinueWithStandard(delegate(Task copyTask)
					{
						try
						{
							if (copyTask.IsFaulted)
							{
								tempBuffer.Dispose();
								tcs.TrySetException(HttpContent.GetStreamCopyException(copyTask.Exception.GetBaseException()));
							}
							else if (copyTask.IsCanceled)
							{
								tempBuffer.Dispose();
								tcs.TrySetCanceled();
							}
							else
							{
								tempBuffer.Seek(0L, SeekOrigin.Begin);
								this.bufferedContent = tempBuffer;
								tcs.TrySetResult(null);
							}
						}
						catch (Exception ex)
						{
							tcs.TrySetException(ex);
							if (Logging.On)
							{
								Logging.Exception(Logging.Http, this, "LoadIntoBufferAsync", ex);
							}
						}
					});
				}
				catch (IOException originalException)
				{
					tcs.TrySetException(HttpContent.GetStreamCopyException(originalException));
				}
				catch (ObjectDisposedException originalException2)
				{
					tcs.TrySetException(HttpContent.GetStreamCopyException(originalException2));
				}
			}
			return tcs.Task;
		}

		protected virtual Task<Stream> CreateContentReadStreamAsync()
		{
			TaskCompletionSource<Stream> tcs = new TaskCompletionSource<Stream>();
			this.LoadIntoBufferAsync().ContinueWithStandard(delegate(Task task)
			{
				if (!HttpUtilities.HandleFaultsAndCancelation<Stream>(task, tcs))
				{
					tcs.TrySetResult(this.bufferedContent);
				}
			});
			return tcs.Task;
		}

		protected internal abstract bool TryComputeLength(out long length);

		private long? GetComputedOrBufferLength()
		{
			this.CheckDisposed();
			if (this.IsBuffered)
			{
				return new long?(this.bufferedContent.Length);
			}
			if (this.canCalculateLength)
			{
				long value = 0L;
				if (this.TryComputeLength(out value))
				{
					return new long?(value);
				}
				this.canCalculateLength = false;
			}
			return null;
		}

		private MemoryStream CreateMemoryStream(long maxBufferSize, out Exception error)
		{
			error = null;
			long? contentLength = this.Headers.ContentLength;
			if (!contentLength.HasValue)
			{
				return new HttpContent.LimitMemoryStream((int)maxBufferSize, 0);
			}
			if (contentLength > maxBufferSize)
			{
				error = new HttpRequestException(string.Format(CultureInfo.InvariantCulture, SR.net_http_content_buffersize_exceeded, new object[]
				{
					maxBufferSize
				}));
				return null;
			}
			return new HttpContent.LimitMemoryStream((int)maxBufferSize, (int)contentLength.Value);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing && !this.disposed)
			{
				this.disposed = true;
				if (this.contentReadStream != null)
				{
					this.contentReadStream.Dispose();
				}
				if (this.IsBuffered)
				{
					this.bufferedContent.Dispose();
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

		private void CheckTaskNotNull(Task task)
		{
			if (task == null)
			{
				if (Logging.On)
				{
					Logging.PrintError(Logging.Http, string.Format(CultureInfo.InvariantCulture, SR.net_http_log_content_no_task_returned_copytoasync, new object[]
					{
						base.GetType().FullName
					}));
				}
				throw new InvalidOperationException(SR.net_http_content_no_task_returned);
			}
		}

		private static Task CreateCompletedTask()
		{
			TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
			taskCompletionSource.TrySetResult(null);
			return taskCompletionSource.Task;
		}

		private static Exception GetStreamCopyException(Exception originalException)
		{
			Exception ex = originalException;
			if (ex is IOException || ex is ObjectDisposedException)
			{
				ex = new HttpRequestException(SR.net_http_content_stream_copy_error, ex);
			}
			return ex;
		}

		private static bool ByteArrayHasPrefix(byte[] byteArray, int dataLength, byte[] prefix)
		{
			if (prefix == null || byteArray == null || prefix.Length > dataLength || prefix.Length == 0)
			{
				return false;
			}
			for (int i = 0; i < prefix.Length; i++)
			{
				if (prefix[i] != byteArray[i])
				{
					return false;
				}
			}
			return true;
		}
	}
}
