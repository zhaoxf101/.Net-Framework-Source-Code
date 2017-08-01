using System;
using System.Collections.Generic;

namespace System.Net.Http.Headers
{
	public class TransferCodingHeaderValue : ICloneable
	{
		private ICollection<NameValueHeaderValue> parameters;

		private string value;

		public string Value
		{
			get
			{
				return this.value;
			}
		}

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

		internal TransferCodingHeaderValue()
		{
		}

		protected TransferCodingHeaderValue(TransferCodingHeaderValue source)
		{
			this.value = source.value;
			if (source.parameters != null)
			{
				foreach (NameValueHeaderValue current in source.parameters)
				{
					this.Parameters.Add((NameValueHeaderValue)((ICloneable)current).Clone());
				}
			}
		}

		public TransferCodingHeaderValue(string value)
		{
			HeaderUtilities.CheckValidToken(value, "value");
			this.value = value;
		}

		public static TransferCodingHeaderValue Parse(string input)
		{
			int num = 0;
			return (TransferCodingHeaderValue)TransferCodingHeaderParser.SingleValueParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out TransferCodingHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (TransferCodingHeaderParser.SingleValueParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (TransferCodingHeaderValue)obj;
				return true;
			}
			return false;
		}

		internal static int GetTransferCodingLength(string input, int startIndex, Func<TransferCodingHeaderValue> transferCodingCreator, out TransferCodingHeaderValue parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
			{
				return 0;
			}
			int tokenLength = HttpRuleParser.GetTokenLength(input, startIndex);
			if (tokenLength == 0)
			{
				return 0;
			}
			string text = input.Substring(startIndex, tokenLength);
			int num = startIndex + tokenLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			TransferCodingHeaderValue transferCodingHeaderValue;
			if (num >= input.Length || input[num] != ';')
			{
				transferCodingHeaderValue = transferCodingCreator();
				transferCodingHeaderValue.value = text;
				parsedValue = transferCodingHeaderValue;
				return num - startIndex;
			}
			transferCodingHeaderValue = transferCodingCreator();
			transferCodingHeaderValue.value = text;
			num++;
			int nameValueListLength = NameValueHeaderValue.GetNameValueListLength(input, num, ';', transferCodingHeaderValue.Parameters);
			if (nameValueListLength == 0)
			{
				return 0;
			}
			parsedValue = transferCodingHeaderValue;
			return num + nameValueListLength - startIndex;
		}

		public override string ToString()
		{
			return this.value + NameValueHeaderValue.ToString(this.parameters, ';', true);
		}

		public override bool Equals(object obj)
		{
			TransferCodingHeaderValue transferCodingHeaderValue = obj as TransferCodingHeaderValue;
			return transferCodingHeaderValue != null && string.Compare(this.value, transferCodingHeaderValue.value, StringComparison.OrdinalIgnoreCase) == 0 && HeaderUtilities.AreEqualCollections<NameValueHeaderValue>(this.parameters, transferCodingHeaderValue.parameters);
		}

		public override int GetHashCode()
		{
			return this.value.ToLowerInvariant().GetHashCode() ^ NameValueHeaderValue.GetHashCode(this.parameters);
		}

		object ICloneable.Clone()
		{
			return new TransferCodingHeaderValue(this);
		}
	}
}
