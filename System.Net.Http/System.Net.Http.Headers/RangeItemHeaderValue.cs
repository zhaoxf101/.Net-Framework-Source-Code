using System;
using System.Collections.Generic;
using System.Globalization;

namespace System.Net.Http.Headers
{
	public class RangeItemHeaderValue : ICloneable
	{
		private long? from;

		private long? to;

		public long? From
		{
			get
			{
				return this.from;
			}
		}

		public long? To
		{
			get
			{
				return this.to;
			}
		}

		public RangeItemHeaderValue(long? from, long? to)
		{
			if (!from.HasValue && !to.HasValue)
			{
				throw new ArgumentException(SR.net_http_headers_invalid_range);
			}
			if (from.HasValue && from.Value < 0L)
			{
				throw new ArgumentOutOfRangeException("from");
			}
			if (to.HasValue && to.Value < 0L)
			{
				throw new ArgumentOutOfRangeException("to");
			}
			if (from.HasValue && to.HasValue && from.Value > to.Value)
			{
				throw new ArgumentOutOfRangeException("from");
			}
			this.from = from;
			this.to = to;
		}

		private RangeItemHeaderValue(RangeItemHeaderValue source)
		{
			this.from = source.from;
			this.to = source.to;
		}

		public override string ToString()
		{
			if (!this.from.HasValue)
			{
				return "-" + this.to.Value.ToString(NumberFormatInfo.InvariantInfo);
			}
			if (!this.to.HasValue)
			{
				return this.from.Value.ToString(NumberFormatInfo.InvariantInfo) + "-";
			}
			return this.from.Value.ToString(NumberFormatInfo.InvariantInfo) + "-" + this.to.Value.ToString(NumberFormatInfo.InvariantInfo);
		}

		public override bool Equals(object obj)
		{
			RangeItemHeaderValue rangeItemHeaderValue = obj as RangeItemHeaderValue;
			return rangeItemHeaderValue != null && this.from == rangeItemHeaderValue.from && this.to == rangeItemHeaderValue.to;
		}

		public override int GetHashCode()
		{
			if (!this.from.HasValue)
			{
				return this.to.GetHashCode();
			}
			if (!this.to.HasValue)
			{
				return this.from.GetHashCode();
			}
			return this.from.GetHashCode() ^ this.to.GetHashCode();
		}

		internal static int GetRangeItemListLength(string input, int startIndex, ICollection<RangeItemHeaderValue> rangeCollection)
		{
			if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
			{
				return 0;
			}
			bool flag = false;
			int num = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(input, startIndex, true, out flag);
			if (num == input.Length)
			{
				return 0;
			}
			RangeItemHeaderValue item = null;
			while (true)
			{
				int rangeItemLength = RangeItemHeaderValue.GetRangeItemLength(input, num, out item);
				if (rangeItemLength == 0)
				{
					break;
				}
				rangeCollection.Add(item);
				num += rangeItemLength;
				num = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(input, num, true, out flag);
				if (num < input.Length && !flag)
				{
					return 0;
				}
				if (num == input.Length)
				{
					goto Block_6;
				}
			}
			return 0;
			Block_6:
			return num - startIndex;
		}

		internal static int GetRangeItemLength(string input, int startIndex, out RangeItemHeaderValue parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
			{
				return 0;
			}
			int numberLength = HttpRuleParser.GetNumberLength(input, startIndex, false);
			if (numberLength > 19)
			{
				return 0;
			}
			int num = startIndex + numberLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			if (num == input.Length || input[num] != '-')
			{
				return 0;
			}
			num++;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			int startIndex2 = num;
			int num2 = 0;
			if (num < input.Length)
			{
				num2 = HttpRuleParser.GetNumberLength(input, num, false);
				if (num2 > 19)
				{
					return 0;
				}
				num += num2;
				num += HttpRuleParser.GetWhitespaceLength(input, num);
			}
			if (numberLength == 0 && num2 == 0)
			{
				return 0;
			}
			long num3 = 0L;
			if (numberLength > 0 && !HeaderUtilities.TryParseInt64(input.Substring(startIndex, numberLength), out num3))
			{
				return 0;
			}
			long num4 = 0L;
			if (num2 > 0 && !HeaderUtilities.TryParseInt64(input.Substring(startIndex2, num2), out num4))
			{
				return 0;
			}
			if (numberLength > 0 && num2 > 0 && num3 > num4)
			{
				return 0;
			}
			parsedValue = new RangeItemHeaderValue((numberLength == 0) ? null : new long?(num3), (num2 == 0) ? null : new long?(num4));
			return num - startIndex;
		}

		object ICloneable.Clone()
		{
			return new RangeItemHeaderValue(this);
		}
	}
}
