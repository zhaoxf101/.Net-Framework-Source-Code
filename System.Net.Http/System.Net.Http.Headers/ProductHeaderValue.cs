using System;

namespace System.Net.Http.Headers
{
	public class ProductHeaderValue : ICloneable
	{
		private string name;

		private string version;

		public string Name
		{
			get
			{
				return this.name;
			}
		}

		public string Version
		{
			get
			{
				return this.version;
			}
		}

		public ProductHeaderValue(string name) : this(name, null)
		{
		}

		public ProductHeaderValue(string name, string version)
		{
			HeaderUtilities.CheckValidToken(name, "name");
			if (!string.IsNullOrEmpty(version))
			{
				HeaderUtilities.CheckValidToken(version, "version");
				this.version = version;
			}
			this.name = name;
		}

		private ProductHeaderValue(ProductHeaderValue source)
		{
			this.name = source.name;
			this.version = source.version;
		}

		private ProductHeaderValue()
		{
		}

		public override string ToString()
		{
			if (string.IsNullOrEmpty(this.version))
			{
				return this.name;
			}
			return this.name + "/" + this.version;
		}

		public override bool Equals(object obj)
		{
			ProductHeaderValue productHeaderValue = obj as ProductHeaderValue;
			return productHeaderValue != null && string.Compare(this.name, productHeaderValue.name, StringComparison.OrdinalIgnoreCase) == 0 && string.Compare(this.version, productHeaderValue.version, StringComparison.OrdinalIgnoreCase) == 0;
		}

		public override int GetHashCode()
		{
			int num = this.name.ToLowerInvariant().GetHashCode();
			if (!string.IsNullOrEmpty(this.version))
			{
				num ^= this.version.ToLowerInvariant().GetHashCode();
			}
			return num;
		}

		public static ProductHeaderValue Parse(string input)
		{
			int num = 0;
			return (ProductHeaderValue)GenericHeaderParser.SingleValueProductParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out ProductHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (GenericHeaderParser.SingleValueProductParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (ProductHeaderValue)obj;
				return true;
			}
			return false;
		}

		internal static int GetProductLength(string input, int startIndex, out ProductHeaderValue parsedValue)
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
			ProductHeaderValue productHeaderValue = new ProductHeaderValue();
			productHeaderValue.name = input.Substring(startIndex, tokenLength);
			int num = startIndex + tokenLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			if (num == input.Length || input[num] != '/')
			{
				parsedValue = productHeaderValue;
				return num - startIndex;
			}
			num++;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			int tokenLength2 = HttpRuleParser.GetTokenLength(input, num);
			if (tokenLength2 == 0)
			{
				return 0;
			}
			productHeaderValue.version = input.Substring(num, tokenLength2);
			num += tokenLength2;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			parsedValue = productHeaderValue;
			return num - startIndex;
		}

		object ICloneable.Clone()
		{
			return new ProductHeaderValue(this);
		}
	}
}
