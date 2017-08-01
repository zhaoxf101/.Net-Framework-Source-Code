using System;

namespace System.Net.Http.Headers
{
	internal class TransferCodingHeaderParser : BaseHeaderParser
	{
		private Func<TransferCodingHeaderValue> transferCodingCreator;

		internal static readonly TransferCodingHeaderParser SingleValueParser = new TransferCodingHeaderParser(false, new Func<TransferCodingHeaderValue>(TransferCodingHeaderParser.CreateTransferCoding));

		internal static readonly TransferCodingHeaderParser MultipleValueParser = new TransferCodingHeaderParser(true, new Func<TransferCodingHeaderValue>(TransferCodingHeaderParser.CreateTransferCoding));

		internal static readonly TransferCodingHeaderParser SingleValueWithQualityParser = new TransferCodingHeaderParser(false, new Func<TransferCodingHeaderValue>(TransferCodingHeaderParser.CreateTransferCodingWithQuality));

		internal static readonly TransferCodingHeaderParser MultipleValueWithQualityParser = new TransferCodingHeaderParser(true, new Func<TransferCodingHeaderValue>(TransferCodingHeaderParser.CreateTransferCodingWithQuality));

		private TransferCodingHeaderParser(bool supportsMultipleValues, Func<TransferCodingHeaderValue> transferCodingCreator) : base(supportsMultipleValues)
		{
			this.transferCodingCreator = transferCodingCreator;
		}

		protected override int GetParsedValueLength(string value, int startIndex, object storeValue, out object parsedValue)
		{
			TransferCodingHeaderValue transferCodingHeaderValue = null;
			int transferCodingLength = TransferCodingHeaderValue.GetTransferCodingLength(value, startIndex, this.transferCodingCreator, out transferCodingHeaderValue);
			parsedValue = transferCodingHeaderValue;
			return transferCodingLength;
		}

		private static TransferCodingHeaderValue CreateTransferCoding()
		{
			return new TransferCodingHeaderValue();
		}

		private static TransferCodingHeaderValue CreateTransferCodingWithQuality()
		{
			return new TransferCodingWithQualityHeaderValue();
		}
	}
}
