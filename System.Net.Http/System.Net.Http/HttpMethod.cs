using System;

namespace System.Net.Http
{
	public class HttpMethod : IEquatable<HttpMethod>
	{
		private string method;

		private static readonly HttpMethod getMethod = new HttpMethod("GET");

		private static readonly HttpMethod putMethod = new HttpMethod("PUT");

		private static readonly HttpMethod postMethod = new HttpMethod("POST");

		private static readonly HttpMethod deleteMethod = new HttpMethod("DELETE");

		private static readonly HttpMethod headMethod = new HttpMethod("HEAD");

		private static readonly HttpMethod optionsMethod = new HttpMethod("OPTIONS");

		private static readonly HttpMethod traceMethod = new HttpMethod("TRACE");

		public static HttpMethod Get
		{
			get
			{
				return HttpMethod.getMethod;
			}
		}

		public static HttpMethod Put
		{
			get
			{
				return HttpMethod.putMethod;
			}
		}

		public static HttpMethod Post
		{
			get
			{
				return HttpMethod.postMethod;
			}
		}

		public static HttpMethod Delete
		{
			get
			{
				return HttpMethod.deleteMethod;
			}
		}

		public static HttpMethod Head
		{
			get
			{
				return HttpMethod.headMethod;
			}
		}

		public static HttpMethod Options
		{
			get
			{
				return HttpMethod.optionsMethod;
			}
		}

		public static HttpMethod Trace
		{
			get
			{
				return HttpMethod.traceMethod;
			}
		}

		public string Method
		{
			get
			{
				return this.method;
			}
		}

		public HttpMethod(string method)
		{
			if (string.IsNullOrEmpty(method))
			{
				throw new ArgumentException(SR.net_http_argument_empty_string, "method");
			}
			if (HttpRuleParser.GetTokenLength(method, 0) != method.Length)
			{
				throw new FormatException(SR.net_http_httpmethod_format_error);
			}
			this.method = method;
		}

		public bool Equals(HttpMethod other)
		{
			return other != null && (object.ReferenceEquals(this.method, other.method) || string.Compare(this.method, other.method, StringComparison.OrdinalIgnoreCase) == 0);
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as HttpMethod);
		}

		public override int GetHashCode()
		{
			return this.method.ToUpperInvariant().GetHashCode();
		}

		public override string ToString()
		{
			return this.method.ToString();
		}

		public static bool operator ==(HttpMethod left, HttpMethod right)
		{
			if (left == null)
			{
				return right == null;
			}
			if (right == null)
			{
				return left == null;
			}
			return left.Equals(right);
		}

		public static bool operator !=(HttpMethod left, HttpMethod right)
		{
			return !(left == right);
		}
	}
}
