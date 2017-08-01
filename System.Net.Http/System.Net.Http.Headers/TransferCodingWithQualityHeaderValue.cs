using System;

namespace System.Net.Http.Headers
{
	public sealed class TransferCodingWithQualityHeaderValue : TransferCodingHeaderValue, ICloneable
	{
		public double? Quality
		{
			get
			{
				return HeaderUtilities.GetQuality(base.Parameters);
			}
			set
			{
				HeaderUtilities.SetQuality(base.Parameters, value);
			}
		}

		internal TransferCodingWithQualityHeaderValue()
		{
		}

		public TransferCodingWithQualityHeaderValue(string value) : base(value)
		{
		}

		public TransferCodingWithQualityHeaderValue(string value, double quality) : base(value)
		{
			this.Quality = new double?(quality);
		}

		private TransferCodingWithQualityHeaderValue(TransferCodingWithQualityHeaderValue source) : base(source)
		{
		}

		object ICloneable.Clone()
		{
			return new TransferCodingWithQualityHeaderValue(this);
		}

		public new static TransferCodingWithQualityHeaderValue Parse(string input)
		{
			int num = 0;
			return (TransferCodingWithQualityHeaderValue)TransferCodingHeaderParser.SingleValueWithQualityParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out TransferCodingWithQualityHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (TransferCodingHeaderParser.SingleValueWithQualityParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (TransferCodingWithQualityHeaderValue)obj;
				return true;
			}
			return false;
		}
	}
}
