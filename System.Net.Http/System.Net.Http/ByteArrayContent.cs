using System;
using System.IO;
using System.Threading.Tasks;

namespace System.Net.Http
{
	public class ByteArrayContent : HttpContent
	{
		private byte[] content;

		private int offset;

		private int count;

		public ByteArrayContent(byte[] content)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}
			this.content = content;
			this.offset = 0;
			this.count = content.Length;
		}

		public ByteArrayContent(byte[] content, int offset, int count)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}
			if (offset < 0 || offset > content.Length)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			if (count < 0 || count > content.Length - offset)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			this.content = content;
			this.offset = offset;
			this.count = count;
		}

		protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
		{
			return Task.Factory.FromAsync<byte[], int, int>(new Func<byte[], int, int, AsyncCallback, object, IAsyncResult>(stream.BeginWrite), new Action<IAsyncResult>(stream.EndWrite), this.content, this.offset, this.count, null);
		}

		protected internal override bool TryComputeLength(out long length)
		{
			length = (long)this.count;
			return true;
		}

		protected override Task<Stream> CreateContentReadStreamAsync()
		{
			TaskCompletionSource<Stream> taskCompletionSource = new TaskCompletionSource<Stream>();
			taskCompletionSource.TrySetResult(new MemoryStream(this.content, this.offset, this.count, false));
			return taskCompletionSource.Task;
		}
	}
}
