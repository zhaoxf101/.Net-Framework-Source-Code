using System;
using System.IO;
using System.Threading.Tasks;

namespace System.Net.Http
{
	public class StreamContent : HttpContent
	{
		private class ReadOnlyStream : DelegatingStream
		{
			public override bool CanWrite
			{
				get
				{
					return false;
				}
			}

			public override int WriteTimeout
			{
				get
				{
					throw new NotSupportedException(SR.net_http_content_readonly_stream);
				}
				set
				{
					throw new NotSupportedException(SR.net_http_content_readonly_stream);
				}
			}

			public ReadOnlyStream(Stream innerStream) : base(innerStream)
			{
			}

			public override void Flush()
			{
				throw new NotSupportedException(SR.net_http_content_readonly_stream);
			}

			public override void SetLength(long value)
			{
				throw new NotSupportedException(SR.net_http_content_readonly_stream);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException(SR.net_http_content_readonly_stream);
			}

			public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				throw new NotSupportedException(SR.net_http_content_readonly_stream);
			}

			public override void EndWrite(IAsyncResult asyncResult)
			{
				throw new NotSupportedException(SR.net_http_content_readonly_stream);
			}

			public override void WriteByte(byte value)
			{
				throw new NotSupportedException(SR.net_http_content_readonly_stream);
			}
		}

		private const int defaultBufferSize = 4096;

		private Stream content;

		private int bufferSize;

		private bool contentConsumed;

		private long start;

		public StreamContent(Stream content) : this(content, 4096)
		{
		}

		public StreamContent(Stream content, int bufferSize)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}
			if (bufferSize <= 0)
			{
				throw new ArgumentOutOfRangeException("bufferSize");
			}
			this.content = content;
			this.bufferSize = bufferSize;
			if (content.CanSeek)
			{
				this.start = content.Position;
			}
			if (Logging.On)
			{
				Logging.Associate(Logging.Http, this, content);
			}
		}

		protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
		{
			this.PrepareContent();
			StreamToStreamCopy streamToStreamCopy = new StreamToStreamCopy(this.content, stream, this.bufferSize, !this.content.CanSeek);
			return streamToStreamCopy.StartAsync();
		}

		protected internal override bool TryComputeLength(out long length)
		{
			if (this.content.CanSeek)
			{
				length = this.content.Length - this.start;
				return true;
			}
			length = 0L;
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				this.content.Dispose();
			}
			base.Dispose(disposing);
		}

		protected override Task<Stream> CreateContentReadStreamAsync()
		{
			TaskCompletionSource<Stream> taskCompletionSource = new TaskCompletionSource<Stream>();
			taskCompletionSource.TrySetResult(new StreamContent.ReadOnlyStream(this.content));
			return taskCompletionSource.Task;
		}

		private void PrepareContent()
		{
			if (this.contentConsumed)
			{
				if (!this.content.CanSeek)
				{
					throw new InvalidOperationException(SR.net_http_content_stream_already_read);
				}
				this.content.Position = this.start;
			}
			this.contentConsumed = true;
		}
	}
}
