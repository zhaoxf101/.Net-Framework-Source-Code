using System;
using System.Collections.Generic;

namespace System.Net.Http.Headers
{
	public class NameValueWithParametersHeaderValue : NameValueHeaderValue, ICloneable
	{
		private static readonly Func<NameValueHeaderValue> nameValueCreator = new Func<NameValueHeaderValue>(NameValueWithParametersHeaderValue.CreateNameValue);

		private ICollection<NameValueHeaderValue> parameters;

		public ICollection<NameValueHeaderValue> Parameters
		{
			get
			{
				if (this.parameters == null)
				{
					this.parameters = new ObjectCollection<NameValueHeaderValue>();
				}
				return this.parameters;
			}
		}

		public NameValueWithParametersHeaderValue(string name) : base(name)
		{
		}

		public NameValueWithParametersHeaderValue(string name, string value) : base(name, value)
		{
		}

		internal NameValueWithParametersHeaderValue()
		{
		}

		protected NameValueWithParametersHeaderValue(NameValueWithParametersHeaderValue source) : base(source)
		{
			if (source.parameters != null)
			{
				foreach (NameValueHeaderValue current in source.parameters)
				{
					this.Parameters.Add((NameValueHeaderValue)((ICloneable)current).Clone());
				}
			}
		}

		public override bool Equals(object obj)
		{
			bool flag = base.Equals(obj);
			if (flag)
			{
				NameValueWithParametersHeaderValue nameValueWithParametersHeaderValue = obj as NameValueWithParametersHeaderValue;
				return nameValueWithParametersHeaderValue != null && HeaderUtilities.AreEqualCollections<NameValueHeaderValue>(this.parameters, nameValueWithParametersHeaderValue.parameters);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode() ^ NameValueHeaderValue.GetHashCode(this.parameters);
		}

		public override string ToString()
		{
			return base.ToString() + NameValueHeaderValue.ToString(this.parameters, ';', true);
		}

		public new static NameValueWithParametersHeaderValue Parse(string input)
		{
			int num = 0;
			return (NameValueWithParametersHeaderValue)GenericHeaderParser.SingleValueNameValueWithParametersParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out NameValueWithParametersHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (GenericHeaderParser.SingleValueNameValueWithParametersParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (NameValueWithParametersHeaderValue)obj;
				return true;
			}
			return false;
		}

		internal static int GetNameValueWithParametersLength(string input, int startIndex, out object parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
			{
				return 0;
			}
			NameValueHeaderValue nameValueHeaderValue = null;
			int nameValueLength = NameValueHeaderValue.GetNameValueLength(input, startIndex, NameValueWithParametersHeaderValue.nameValueCreator, out nameValueHeaderValue);
			if (nameValueLength == 0)
			{
				return 0;
			}
			int num = startIndex + nameValueLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			NameValueWithParametersHeaderValue nameValueWithParametersHeaderValue = nameValueHeaderValue as NameValueWithParametersHeaderValue;
			if (num >= input.Length || input[num] != ';')
			{
				parsedValue = nameValueWithParametersHeaderValue;
				return num - startIndex;
			}
			num++;
			int nameValueListLength = NameValueHeaderValue.GetNameValueListLength(input, num, ';', nameValueWithParametersHeaderValue.Parameters);
			if (nameValueListLength == 0)
			{
				return 0;
			}
			parsedValue = nameValueWithParametersHeaderValue;
			return num + nameValueListLength - startIndex;
		}

		private static NameValueHeaderValue CreateNameValue()
		{
			return new NameValueWithParametersHeaderValue();
		}

		object ICloneable.Clone()
		{
			return new NameValueWithParametersHeaderValue(this);
		}
	}
}
