using System;
using System.Collections;
using System.Globalization;

namespace System.Net.Http.Headers
{
	internal abstract class HttpHeaderParser
	{
		internal const string DefaultSeparator = ", ";

		private bool supportsMultipleValues;

		private string separator;

		public bool SupportsMultipleValues
		{
			get
			{
				return this.supportsMultipleValues;
			}
		}

		public string Separator
		{
			get
			{
				return this.separator;
			}
		}

		public virtual IEqualityComparer Comparer
		{
			get
			{
				return null;
			}
		}

		protected HttpHeaderParser(bool supportsMultipleValues)
		{
			this.supportsMultipleValues = supportsMultipleValues;
			if (supportsMultipleValues)
			{
				this.separator = ", ";
			}
		}

		protected HttpHeaderParser(bool supportsMultipleValues, string separator)
		{
			this.supportsMultipleValues = supportsMultipleValues;
			this.separator = separator;
		}

		public abstract bool TryParseValue(string value, object storeValue, ref int index, out object parsedValue);

		public object ParseValue(string value, object storeValue, ref int index)
		{
			object result = null;
			if (!this.TryParseValue(value, storeValue, ref index, out result))
			{
				throw new FormatException(string.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, new object[]
				{
					(value == null) ? "<null>" : value.Substring(index)
				}));
			}
			return result;
		}

		public virtual string ToString(object value)
		{
			return value.ToString();
		}
	}
}
