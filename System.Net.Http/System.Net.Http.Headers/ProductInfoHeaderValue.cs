using System;
using System.Globalization;

namespace System.Net.Http.Headers
{
	public class ProductInfoHeaderValue : ICloneable
	{
		private ProductHeaderValue product;

		private string comment;

		public ProductHeaderValue Product
		{
			get
			{
				return this.product;
			}
		}

		public string Comment
		{
			get
			{
				return this.comment;
			}
		}

		public ProductInfoHeaderValue(string productName, string productVersion) : this(new ProductHeaderValue(productName, productVersion))
		{
		}

		public ProductInfoHeaderValue(ProductHeaderValue product)
		{
			if (product == null)
			{
				throw new ArgumentNullException("product");
			}
			this.product = product;
		}

		public ProductInfoHeaderValue(string comment)
		{
			HeaderUtilities.CheckValidComment(comment, "comment");
			this.comment = comment;
		}

		private ProductInfoHeaderValue(ProductInfoHeaderValue source)
		{
			this.product = source.product;
			this.comment = source.comment;
		}

		private ProductInfoHeaderValue()
		{
		}

		public override string ToString()
		{
			if (this.product == null)
			{
				return this.comment;
			}
			return this.product.ToString();
		}

		public override bool Equals(object obj)
		{
			ProductInfoHeaderValue productInfoHeaderValue = obj as ProductInfoHeaderValue;
			if (productInfoHeaderValue == null)
			{
				return false;
			}
			if (this.product == null)
			{
				return string.CompareOrdinal(this.comment, productInfoHeaderValue.comment) == 0;
			}
			return this.product.Equals(productInfoHeaderValue.product);
		}

		public override int GetHashCode()
		{
			if (this.product == null)
			{
				return this.comment.GetHashCode();
			}
			return this.product.GetHashCode();
		}

		public static ProductInfoHeaderValue Parse(string input)
		{
			int num = 0;
			object obj = ProductInfoHeaderParser.SingleValueParser.ParseValue(input, null, ref num);
			if (num < input.Length)
			{
				throw new FormatException(string.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, new object[]
				{
					input.Substring(num)
				}));
			}
			return (ProductInfoHeaderValue)obj;
		}

		public static bool TryParse(string input, out ProductInfoHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (!ProductInfoHeaderParser.SingleValueParser.TryParseValue(input, null, ref num, out obj))
			{
				return false;
			}
			if (num < input.Length)
			{
				return false;
			}
			parsedValue = (ProductInfoHeaderValue)obj;
			return true;
		}

		internal static int GetProductInfoLength(string input, int startIndex, out ProductInfoHeaderValue parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
			{
				return 0;
			}
			string text = null;
			ProductHeaderValue productHeaderValue = null;
			int num2;
			if (input[startIndex] == '(')
			{
				int num = 0;
				if (HttpRuleParser.GetCommentLength(input, startIndex, out num) != HttpParseResult.Parsed)
				{
					return 0;
				}
				text = input.Substring(startIndex, num);
				num2 = startIndex + num;
				num2 += HttpRuleParser.GetWhitespaceLength(input, num2);
			}
			else
			{
				int productLength = ProductHeaderValue.GetProductLength(input, startIndex, out productHeaderValue);
				if (productLength == 0)
				{
					return 0;
				}
				num2 = startIndex + productLength;
			}
			parsedValue = new ProductInfoHeaderValue();
			parsedValue.product = productHeaderValue;
			parsedValue.comment = text;
			return num2 - startIndex;
		}

		object ICloneable.Clone()
		{
			return new ProductInfoHeaderValue(this);
		}
	}
}
