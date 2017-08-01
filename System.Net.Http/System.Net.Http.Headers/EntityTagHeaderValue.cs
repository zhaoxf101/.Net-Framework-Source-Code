using System;

namespace System.Net.Http.Headers
{
	public class EntityTagHeaderValue : ICloneable
	{
		private static EntityTagHeaderValue any;

		private string tag;

		private bool isWeak;

		public string Tag
		{
			get
			{
				return this.tag;
			}
		}

		public bool IsWeak
		{
			get
			{
				return this.isWeak;
			}
		}

		public static EntityTagHeaderValue Any
		{
			get
			{
				if (EntityTagHeaderValue.any == null)
				{
					EntityTagHeaderValue.any = new EntityTagHeaderValue();
					EntityTagHeaderValue.any.tag = "*";
					EntityTagHeaderValue.any.isWeak = false;
				}
				return EntityTagHeaderValue.any;
			}
		}

		public EntityTagHeaderValue(string tag) : this(tag, false)
		{
		}

		public EntityTagHeaderValue(string tag, bool isWeak)
		{
			if (string.IsNullOrEmpty(tag))
			{
				throw new ArgumentException(SR.net_http_argument_empty_string, "tag");
			}
			int num = 0;
			if (HttpRuleParser.GetQuotedStringLength(tag, 0, out num) != HttpParseResult.Parsed || num != tag.Length)
			{
				throw new FormatException(SR.net_http_headers_invalid_etag_name);
			}
			this.tag = tag;
			this.isWeak = isWeak;
		}

		private EntityTagHeaderValue(EntityTagHeaderValue source)
		{
			this.tag = source.tag;
			this.isWeak = source.isWeak;
		}

		private EntityTagHeaderValue()
		{
		}

		public override string ToString()
		{
			if (this.isWeak)
			{
				return "W/" + this.tag;
			}
			return this.tag;
		}

		public override bool Equals(object obj)
		{
			EntityTagHeaderValue entityTagHeaderValue = obj as EntityTagHeaderValue;
			return entityTagHeaderValue != null && this.isWeak == entityTagHeaderValue.isWeak && string.CompareOrdinal(this.tag, entityTagHeaderValue.tag) == 0;
		}

		public override int GetHashCode()
		{
			return this.tag.GetHashCode() ^ this.isWeak.GetHashCode();
		}

		public static EntityTagHeaderValue Parse(string input)
		{
			int num = 0;
			return (EntityTagHeaderValue)GenericHeaderParser.SingleValueEntityTagParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out EntityTagHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (GenericHeaderParser.SingleValueEntityTagParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (EntityTagHeaderValue)obj;
				return true;
			}
			return false;
		}

		internal static int GetEntityTagLength(string input, int startIndex, out EntityTagHeaderValue parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
			{
				return 0;
			}
			bool flag = false;
			int num = startIndex;
			char c = input[startIndex];
			if (c == '*')
			{
				parsedValue = EntityTagHeaderValue.Any;
				num++;
			}
			else
			{
				if (c == 'W' || c == 'w')
				{
					num++;
					if (num + 2 >= input.Length || input[num] != '/')
					{
						return 0;
					}
					flag = true;
					num++;
					num += HttpRuleParser.GetWhitespaceLength(input, num);
				}
				int startIndex2 = num;
				int num2 = 0;
				if (HttpRuleParser.GetQuotedStringLength(input, num, out num2) != HttpParseResult.Parsed)
				{
					return 0;
				}
				parsedValue = new EntityTagHeaderValue();
				if (num2 == input.Length)
				{
					parsedValue.tag = input;
					parsedValue.isWeak = false;
				}
				else
				{
					parsedValue.tag = input.Substring(startIndex2, num2);
					parsedValue.isWeak = flag;
				}
				num += num2;
			}
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			return num - startIndex;
		}

		object ICloneable.Clone()
		{
			if (this == EntityTagHeaderValue.any)
			{
				return EntityTagHeaderValue.any;
			}
			return new EntityTagHeaderValue(this);
		}
	}
}
