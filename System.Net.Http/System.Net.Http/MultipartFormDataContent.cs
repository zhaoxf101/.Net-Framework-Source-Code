using System;
using System.Net.Http.Headers;

namespace System.Net.Http
{
	public class MultipartFormDataContent : MultipartContent
	{
		private const string formData = "form-data";

		public MultipartFormDataContent() : base("form-data")
		{
		}

		public MultipartFormDataContent(string boundary) : base("form-data", boundary)
		{
		}

		public override void Add(HttpContent content)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}
			if (content.Headers.ContentDisposition == null)
			{
				content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
			}
			base.Add(content);
		}

		public void Add(HttpContent content, string name)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}
			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentException(SR.net_http_argument_empty_string, "name");
			}
			this.AddInternal(content, name, null);
		}

		public void Add(HttpContent content, string name, string fileName)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}
			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentException(SR.net_http_argument_empty_string, "name");
			}
			if (string.IsNullOrWhiteSpace(fileName))
			{
				throw new ArgumentException(SR.net_http_argument_empty_string, "fileName");
			}
			this.AddInternal(content, name, fileName);
		}

		private void AddInternal(HttpContent content, string name, string fileName)
		{
			if (content.Headers.ContentDisposition == null)
			{
				ContentDispositionHeaderValue contentDispositionHeaderValue = new ContentDispositionHeaderValue("form-data");
				contentDispositionHeaderValue.Name = name;
				contentDispositionHeaderValue.FileName = fileName;
				contentDispositionHeaderValue.FileNameStar = fileName;
				content.Headers.ContentDisposition = contentDispositionHeaderValue;
			}
			base.Add(content);
		}
	}
}
