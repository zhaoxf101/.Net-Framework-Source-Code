using System;
using System.IO;
using System.Threading.Tasks;

namespace System.Net.Http
{
	internal class StreamToStreamCopy
	{
		private byte[] buffer;

		private int bufferSize;

		private Stream source;

		private Stream destination;

		private AsyncCallback bufferReadCallback;

		private AsyncCallback bufferWrittenCallback;

		private TaskCompletionSource<object> tcs;

		private bool sourceIsMemoryStream;

		private bool destinationIsMemoryStream;

		private bool disposeSource;

		public StreamToStreamCopy(Stream source, Stream destination, int bufferSize, bool disposeSource)
		{
			this.buffer = new byte[bufferSize];
			this.source = source;
			this.destination = destination;
			this.sourceIsMemoryStream = (source is MemoryStream);
			this.destinationIsMemoryStream = (destination is MemoryStream);
			this.bufferSize = bufferSize;
			this.bufferReadCallback = new AsyncCallback(this.BufferReadCallback);
			this.bufferWrittenCallback = new AsyncCallback(this.BufferWrittenCallback);
			this.disposeSource = disposeSource;
			this.tcs = new TaskCompletionSource<object>();
		}

		public Task StartAsync()
		{
			if (this.sourceIsMemoryStream && this.destinationIsMemoryStream)
			{
				MemoryStream memoryStream = this.source as MemoryStream;
				try
				{
					int num = (int)memoryStream.Position;
					this.destination.Write(memoryStream.ToArray(), num, (int)this.source.Length - num);
					this.SetCompleted(null);
					goto IL_5D;
				}
				catch (Exception completed)
				{
					this.SetCompleted(completed);
					goto IL_5D;
				}
			}
			this.StartRead();
			IL_5D:
			return this.tcs.Task;
		}

		private void StartRead()
		{
			try
			{
				while (true)
				{
					bool flag;
					if (this.sourceIsMemoryStream)
					{
						int num = this.source.Read(this.buffer, 0, this.bufferSize);
						if (num == 0)
						{
							break;
						}
						flag = this.TryStartWriteSync(num);
					}
					else
					{
						IAsyncResult asyncResult = this.source.BeginRead(this.buffer, 0, this.bufferSize, this.bufferReadCallback, null);
						flag = asyncResult.CompletedSynchronously;
						if (flag)
						{
							int num = this.source.EndRead(asyncResult);
							if (num == 0)
							{
								goto Block_4;
							}
							flag = this.TryStartWriteSync(num);
						}
					}
					if (!flag)
					{
						goto Block_5;
					}
				}
				this.SetCompleted(null);
				return;
				Block_4:
				this.SetCompleted(null);
				Block_5:;
			}
			catch (Exception completed)
			{
				this.SetCompleted(completed);
			}
		}

		private bool TryStartWriteSync(int bytesRead)
		{
			if (this.destinationIsMemoryStream)
			{
				this.destination.Write(this.buffer, 0, bytesRead);
				return true;
			}
			IAsyncResult asyncResult = this.destination.BeginWrite(this.buffer, 0, bytesRead, this.bufferWrittenCallback, null);
			if (asyncResult.CompletedSynchronously)
			{
				this.destination.EndWrite(asyncResult);
				return true;
			}
			return false;
		}

		private void BufferReadCallback(IAsyncResult ar)
		{
			if (!ar.CompletedSynchronously)
			{
				try
				{
					int num = this.source.EndRead(ar);
					if (num == 0)
					{
						this.SetCompleted(null);
					}
					else if (this.TryStartWriteSync(num))
					{
						this.StartRead();
					}
				}
				catch (Exception completed)
				{
					this.SetCompleted(completed);
				}
			}
		}

		private void BufferWrittenCallback(IAsyncResult ar)
		{
			if (!ar.CompletedSynchronously)
			{
				try
				{
					this.destination.EndWrite(ar);
					this.StartRead();
				}
				catch (Exception completed)
				{
					this.SetCompleted(completed);
				}
			}
		}

		private void SetCompleted(Exception error)
		{
			try
			{
				if (this.disposeSource)
				{
					this.source.Dispose();
				}
			}
			catch (Exception e)
			{
				if (Logging.On)
				{
					Logging.Exception(Logging.Http, this, "SetCompleted", e);
				}
			}
			if (error == null)
			{
				this.tcs.TrySetResult(null);
				return;
			}
			this.tcs.TrySetException(error);
		}
	}
}
