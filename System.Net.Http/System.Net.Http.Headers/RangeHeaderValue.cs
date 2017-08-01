using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.Http.Headers
{
	public class RangeHeaderValue : ICloneable
	{
		private string unit;

		private ICollection<RangeItemHeaderValue> ranges;

		public string Unit
		{
			get
			{
				return this.unit;
			}
			set
			{
				HeaderUtilities.CheckValidToken(value, "value");
				this.unit = value;
			}
		}

		public ICollection<RangeItemHeaderValue> Ranges
		{
			get
			{
				if (this.ranges == null)
				{
					this.ranges = new ObjectCollection<RangeItemHeaderValue>();
				}
				return this.ranges;
			}
		}

		public RangeHeaderValue()
		{
			this.unit = "bytes";
		}

		public RangeHeaderValue(long? from, long? to)
		{
			this.unit = "bytes";
			this.Ranges.Add(new RangeItemHeaderValue(from, to));
		}

		private RangeHeaderValue(RangeHeaderValue source)
		{
			this.unit = source.unit;
			if (source.ranges != null)
			{
				foreach (RangeItemHeaderValue current in source.ranges)
				{
					this.Ranges.Add((RangeItemHeaderValue)((ICloneable)current).Clone());
				}
			}
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(this.unit);
			stringBuilder.Append('=');
			bool flag = true;
			foreach (RangeItemHeaderValue current in this.Ranges)
			{
				if (flag)
				{
					flag = false;
				}
				else
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(current.From);
				stringBuilder.Append('-');
				stringBuilder.Append(current.To);
			}
			return stringBuilder.ToString();
		}

		public override bool Equals(object obj)
		{
			RangeHeaderValue rangeHeaderValue = obj as RangeHeaderValue;
			return rangeHeaderValue != null && string.Compare(this.unit, rangeHeaderValue.unit, StringComparison.OrdinalIgnoreCase) == 0 && HeaderUtilities.AreEqualCollections<RangeItemHeaderValue>(this.Ranges, rangeHeaderValue.Ranges);
		}

		public override int GetHashCode()
		{
			int num = this.unit.ToLowerInvariant().GetHashCode();
			foreach (RangeItemHeaderValue current in this.Ranges)
			{
				num ^= current.GetHashCode();
			}
			return num;
		}

		public static RangeHeaderValue Parse(string input)
		{
			int num = 0;
			return (RangeHeaderValue)GenericHeaderParser.RangeParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out RangeHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (GenericHeaderParser.RangeParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (RangeHeaderValue)obj;
				return true;
			}
			return false;
		}

		internal static int GetRangeLength(string input, int startIndex, out object parsedValue)
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
			RangeHeaderValue rangeHeaderValue = new RangeHeaderValue();
			rangeHeaderValue.unit = input.Substring(startIndex, tokenLength);
			int num = startIndex + tokenLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			if (num == input.Length || input[num] != '=')
			{
				return 0;
			}
			num++;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			int rangeItemListLength = RangeItemHeaderValue.GetRangeItemListLength(input, num, rangeHeaderValue.Ranges);
			if (rangeItemListLength == 0)
			{
				return 0;
			}
			num += rangeItemListLength;
			parsedValue = rangeHeaderValue;
			return num - startIndex;
		}

		object ICloneable.Clone()
		{
			return new RangeHeaderValue(this);
		}
	}
}
