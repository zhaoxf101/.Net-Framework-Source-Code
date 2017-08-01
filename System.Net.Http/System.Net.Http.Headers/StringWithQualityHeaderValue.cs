using System;
using System.Globalization;

namespace System.Net.Http.Headers
{
	public class StringWithQualityHeaderValue : ICloneable
	{
		private string value;

		private double? quality;

		public string Value
		{
			get
			{
				return this.value;
			}
		}

		public double? Quality
		{
			get
			{
				return this.quality;
			}
		}

		public StringWithQualityHeaderValue(string value)
		{
			HeaderUtilities.CheckValidToken(value, "value");
			this.value = value;
		}

		public StringWithQualityHeaderValue(string value, double quality)
		{
			HeaderUtilities.CheckValidToken(value, "value");
			if (quality < 0.0 || quality > 1.0)
			{
				throw new ArgumentOutOfRangeException("quality");
			}
			this.value = value;
			this.quality = new double?(quality);
		}

		private StringWithQualityHeaderValue(StringWithQualityHeaderValue source)
		{
			this.value = source.value;
			this.quality = source.quality;
		}

		private StringWithQualityHeaderValue()
		{
		}

		public override string ToString()
		{
			if (this.quality.HasValue)
			{
				return this.value + "; q=" + this.quality.Value.ToString("0.0##", NumberFormatInfo.InvariantInfo);
			}
			return this.value;
		}

		public override bool Equals(object obj)
		{
			StringWithQualityHeaderValue stringWithQualityHeaderValue = obj as StringWithQualityHeaderValue;
			if (stringWithQualityHeaderValue == null)
			{
				return false;
			}
			if (string.Compare(this.value, stringWithQualityHeaderValue.value, StringComparison.OrdinalIgnoreCase) != 0)
			{
				return false;
			}
			if (this.quality.HasValue)
			{
				return stringWithQualityHeaderValue.quality.HasValue && this.quality.Value == stringWithQualityHeaderValue.quality.Value;
			}
			return !stringWithQualityHeaderValue.quality.HasValue;
		}

		public override int GetHashCode()
		{
			int num = this.value.ToLowerInvariant().GetHashCode();
			if (this.quality.HasValue)
			{
				num ^= this.quality.Value.GetHashCode();
			}
			return num;
		}

		public static StringWithQualityHeaderValue Parse(string input)
		{
			int num = 0;
			return (StringWithQualityHeaderValue)GenericHeaderParser.SingleValueStringWithQualityParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out StringWithQualityHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (GenericHeaderParser.SingleValueStringWithQualityParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (StringWithQualityHeaderValue)obj;
				return true;
			}
			return false;
		}

		internal static int GetStringWithQualityLength(string input, int startIndex, out object parsedValue)
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
			StringWithQualityHeaderValue stringWithQualityHeaderValue = new StringWithQualityHeaderValue();
			stringWithQualityHeaderValue.value = input.Substring(startIndex, tokenLength);
			int num = startIndex + tokenLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			if (num == input.Length || input[num] != ';')
			{
				parsedValue = stringWithQualityHeaderValue;
				return num - startIndex;
			}
			num++;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			if (!StringWithQualityHeaderValue.TryReadQuality(input, stringWithQualityHeaderValue, ref num))
			{
				return 0;
			}
			parsedValue = stringWithQualityHeaderValue;
			return num - startIndex;
		}

		private static bool TryReadQuality(string input, StringWithQualityHeaderValue result, ref int index)
		{
			int num = index;
			if (num == input.Length || (input[num] != 'q' && input[num] != 'Q'))
			{
				return false;
			}
			num++;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			if (num == input.Length || input[num] != '=')
			{
				return false;
			}
			num++;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			if (num == input.Length)
			{
				return false;
			}
			int numberLength = HttpRuleParser.GetNumberLength(input, num, true);
			if (numberLength == 0)
			{
				return false;
			}
			double num2 = 0.0;
			if (!double.TryParse(input.Substring(num, numberLength), NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo, out num2))
			{
				return false;
			}
			if (num2 < 0.0 || num2 > 1.0)
			{
				return false;
			}
			result.quality = new double?(num2);
			num += numberLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			index = num;
			return true;
		}

		object ICloneable.Clone()
		{
			return new StringWithQualityHeaderValue(this);
		}
	}
}
