using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http
{
	public class MultipartContent : HttpContent, IEnumerable<HttpContent>, IEnumerable
	{
		private const string crlf = "\r\n";

		private List<HttpContent> nestedContent;

		private string boundary;

		private int nextContentIndex;

		private Stream outputStream;

		private TaskCompletionSource<object> tcs;

		public MultipartContent() : this("mixed", MultipartContent.GetDefaultBoundary())
		{
		}

		public MultipartContent(string subtype) : this(subtype, MultipartContent.GetDefaultBoundary())
		{
		}

		public MultipartContent(string subtype, string boundary)
		{
			if (string.IsNullOrWhiteSpace(subtype))
			{
				throw new ArgumentException(SR.net_http_argument_empty_string, "subtype");
			}
			MultipartContent.ValidateBoundary(boundary);
			this.boundary = boundary;
			string text = boundary;
			if (!text.StartsWith("\"", StringComparison.Ordinal))
			{
				text = "\"" + text + "\"";
			}
			MediaTypeHeaderValue mediaTypeHeaderValue = new MediaTypeHeaderValue("multipart/" + subtype);
			mediaTypeHeaderValue.Parameters.Add(new NameValueHeaderValue("boundary", text));
			base.Headers.ContentType = mediaTypeHeaderValue;
			this.nestedContent = new List<HttpContent>();
		}

		private static void ValidateBoundary(string boundary)
		{
			if (string.IsNullOrWhiteSpace(boundary))
			{
				throw new ArgumentException(SR.net_http_argument_empty_string, "boundary");
			}
			if (boundary.Length > 70)
			{
				throw new ArgumentOutOfRangeException("boundary", boundary, string.Format(CultureInfo.InvariantCulture, SR.net_http_content_field_too_long, new object[]
				{
					70
				}));
			}
			if (boundary.EndsWith(" ", StringComparison.Ordinal))
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, new object[]
				{
					boundary
				}), "boundary");
			}
			string text = "'()+_,-./:=? ";
			for (int i = 0; i < boundary.Length; i++)
			{
				char c = boundary[i];
				if (('0' > c || c > '9') && ('a' > c || c > 'z') && ('A' > c || c > 'Z') && text.IndexOf(c) < 0)
				{
					throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, new object[]
					{
						boundary
					}), "boundary");
				}
			}
		}

		private static string GetDefaultBoundary()
		{
			return Guid.NewGuid().ToString();
		}

		public virtual void Add(HttpContent content)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}
			this.nestedContent.Add(content);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				foreach (HttpContent current in this.nestedContent)
				{
					current.Dispose();
				}
				this.nestedContent.Clear();
			}
			base.Dispose(disposing);
		}

		public IEnumerator<HttpContent> GetEnumerator()
		{
			return this.nestedContent.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.nestedContent.GetEnumerator();
		}

		protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
		{
			TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
			this.tcs = taskCompletionSource;
			this.outputStream = stream;
			this.nextContentIndex = 0;
			MultipartContent.EncodeStringToStreamAsync(this.outputStream, "--" + this.boundary + "\r\n").ContinueWithStandard(new Action<Task>(this.WriteNextContentHeadersAsync));
			return taskCompletionSource.Task;
		}

		private void WriteNextContentHeadersAsync(Task task)
		{
			if (task.IsFaulted)
			{
				this.HandleAsyncException("WriteNextContentHeadersAsync", task.Exception.GetBaseException());
				return;
			}
			try
			{
				if (this.nextContentIndex >= this.nestedContent.Count)
				{
					this.WriteTerminatingBoundaryAsync();
				}
				else
				{
					string value = "\r\n--" + this.boundary + "\r\n";
					StringBuilder stringBuilder = new StringBuilder();
					if (this.nextContentIndex != 0)
					{
						stringBuilder.Append(value);
					}
					HttpContent httpContent = this.nestedContent[this.nextContentIndex];
					foreach (KeyValuePair<string, IEnumerable<string>> current in httpContent.Headers)
					{
						stringBuilder.Append(current.Key + ": " + string.Join(", ", current.Value) + "\r\n");
					}
					stringBuilder.Append("\r\n");
					MultipartContent.EncodeStringToStreamAsync(this.outputStream, stringBuilder.ToString()).ContinueWithStandard(new Action<Task>(this.WriteNextContentAsync));
				}
			}
			catch (Exception ex)
			{
				this.HandleAsyncException("WriteNextContentHeadersAsync", ex);
			}
		}

		private void WriteNextContentAsync(Task task)
		{
			if (task.IsFaulted)
			{
				this.HandleAsyncException("WriteNextContentAsync", task.Exception.GetBaseException());
				return;
			}
			try
			{
				HttpContent httpContent = this.nestedContent[this.nextContentIndex];
				this.nextContentIndex++;
				httpContent.CopyToAsync(this.outputStream).ContinueWithStandard(new Action<Task>(this.WriteNextContentHeadersAsync));
			}
			catch (Exception ex)
			{
				this.HandleAsyncException("WriteNextContentAsync", ex);
			}
		}

		private void WriteTerminatingBoundaryAsync()
		{
			try
			{
				MultipartContent.EncodeStringToStreamAsync(this.outputStream, "\r\n--" + this.boundary + "--\r\n").ContinueWithStandard(delegate(Task task)
				{
					if (task.IsFaulted)
					{
						this.HandleAsyncException("WriteTerminatingBoundaryAsync", task.Exception.GetBaseException());
						return;
					}
					TaskCompletionSource<object> taskCompletionSource = this.CleanupAsync();
					taskCompletionSource.TrySetResult(null);
				});
			}
			catch (Exception ex)
			{
				this.HandleAsyncException("WriteTerminatingBoundaryAsync", ex);
			}
		}

		private static Task EncodeStringToStreamAsync(Stream stream, string input)
		{
			byte[] bytes = HttpRuleParser.DefaultHttpEncoding.GetBytes(input);
			return Task.Factory.FromAsync<byte[], int, int>(new Func<byte[], int, int, AsyncCallback, object, IAsyncResult>(stream.BeginWrite), new Action<IAsyncResult>(stream.EndWrite), bytes, 0, bytes.Length, null);
		}

		private TaskCompletionSource<object> CleanupAsync()
		{
			TaskCompletionSource<object> result = this.tcs;
			this.outputStream = null;
			this.nextContentIndex = 0;
			this.tcs = null;
			return result;
		}

		private void HandleAsyncException(string method, Exception ex)
		{
			if (Logging.On)
			{
				Logging.Exception(Logging.Http, this, method, ex);
			}
			TaskCompletionSource<object> taskCompletionSource = this.CleanupAsync();
			taskCompletionSource.TrySetException(ex);
		}

		protected internal override bool TryComputeLength(out long length)
		{
			long num = 0L;
			long num2 = (long)MultipartContent.GetEncodedLength("\r\n--" + this.boundary + "\r\n");
			num += (long)MultipartContent.GetEncodedLength("--" + this.boundary + "\r\n");
			bool flag = true;
			foreach (HttpContent current in this.nestedContent)
			{
				if (flag)
				{
					flag = false;
				}
				else
				{
					num += num2;
				}
				foreach (KeyValuePair<string, IEnumerable<string>> current2 in current.Headers)
				{
					num += (long)MultipartContent.GetEncodedLength(current2.Key + ": " + string.Join(", ", current2.Value) + "\r\n");
				}
				num += (long)"\r\n".Length;
				long num3 = 0L;
				if (!current.TryComputeLength(out num3))
				{
					length = 0L;
					return false;
				}
				num += num3;
			}
			num += (long)MultipartContent.GetEncodedLength("\r\n--" + this.boundary + "--\r\n");
			length = num;
			return true;
		}

		private static int GetEncodedLength(string input)
		{
			return HttpRuleParser.DefaultHttpEncoding.GetByteCount(input);
		}
	}
}
