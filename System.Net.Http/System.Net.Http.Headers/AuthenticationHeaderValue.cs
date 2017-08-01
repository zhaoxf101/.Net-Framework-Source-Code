using System;

namespace System.Net.Http.Headers
{
	public class AuthenticationHeaderValue : ICloneable
	{
		private string scheme;

		private string parameter;

		public string Scheme
		{
			get
			{
				return this.scheme;
			}
		}

		public string Parameter
		{
			get
			{
				return this.parameter;
			}
		}

		public AuthenticationHeaderValue(string scheme) : this(scheme, null)
		{
		}

		public AuthenticationHeaderValue(string scheme, string parameter)
		{
			HeaderUtilities.CheckValidToken(scheme, "scheme");
			this.scheme = scheme;
			this.parameter = parameter;
		}

		private AuthenticationHeaderValue(AuthenticationHeaderValue source)
		{
			this.scheme = source.scheme;
			this.parameter = source.parameter;
		}

		private AuthenticationHeaderValue()
		{
		}

		public override string ToString()
		{
			if (string.IsNullOrEmpty(this.parameter))
			{
				return this.scheme;
			}
			return this.scheme + " " + this.parameter;
		}

		public override bool Equals(object obj)
		{
			AuthenticationHeaderValue authenticationHeaderValue = obj as AuthenticationHeaderValue;
			if (authenticationHeaderValue == null)
			{
				return false;
			}
			if (string.IsNullOrEmpty(this.parameter) && string.IsNullOrEmpty(authenticationHeaderValue.parameter))
			{
				return string.Compare(this.scheme, authenticationHeaderValue.scheme, StringComparison.OrdinalIgnoreCase) == 0;
			}
			return string.Compare(this.scheme, authenticationHeaderValue.scheme, StringComparison.OrdinalIgnoreCase) == 0 && string.CompareOrdinal(this.parameter, authenticationHeaderValue.parameter) == 0;
		}

		public override int GetHashCode()
		{
			int num = this.scheme.ToLowerInvariant().GetHashCode();
			if (!string.IsNullOrEmpty(this.parameter))
			{
				num ^= this.parameter.GetHashCode();
			}
			return num;
		}

		public static AuthenticationHeaderValue Parse(string input)
		{
			int num = 0;
			return (AuthenticationHeaderValue)GenericHeaderParser.SingleValueAuthenticationParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out AuthenticationHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (GenericHeaderParser.SingleValueAuthenticationParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (AuthenticationHeaderValue)obj;
				return true;
			}
			return false;
		}

		internal static int GetAuthenticationLength(string input, int startIndex, out object parsedValue)
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
			AuthenticationHeaderValue authenticationHeaderValue = new AuthenticationHeaderValue();
			authenticationHeaderValue.scheme = input.Substring(startIndex, tokenLength);
			int num = startIndex + tokenLength;
			int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, num);
			num += whitespaceLength;
			if (num == input.Length || input[num] == ',')
			{
				parsedValue = authenticationHeaderValue;
				return num - startIndex;
			}
			if (whitespaceLength == 0)
			{
				return 0;
			}
			int num2 = num;
			int num3 = num;
			if (!AuthenticationHeaderValue.TrySkipFirstBlob(input, ref num, ref num3))
			{
				return 0;
			}
			if (num < input.Length && !AuthenticationHeaderValue.TryGetParametersEndIndex(input, ref num, ref num3))
			{
				return 0;
			}
			authenticationHeaderValue.parameter = input.Substring(num2, num3 - num2 + 1);
			parsedValue = authenticationHeaderValue;
			return num - startIndex;
		}

		private static bool TrySkipFirstBlob(string input, ref int current, ref int parameterEndIndex)
		{
			while (current < input.Length && input[current] != ',')
			{
				if (input[current] == '"')
				{
					int num = 0;
					if (HttpRuleParser.GetQuotedStringLength(input, current, out num) != HttpParseResult.Parsed)
					{
						return false;
					}
					current += num;
					parameterEndIndex = current - 1;
				}
				else
				{
					int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
					if (whitespaceLength == 0)
					{
						parameterEndIndex = current;
						current++;
					}
					else
					{
						current += whitespaceLength;
					}
				}
			}
			return true;
		}

		private static bool TryGetParametersEndIndex(string input, ref int parseEndIndex, ref int parameterEndIndex)
		{
			int num = parseEndIndex;
			while (true)
			{
				num++;
				bool flag = false;
				num = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(input, num, true, out flag);
				if (num == input.Length)
				{
					break;
				}
				int tokenLength = HttpRuleParser.GetTokenLength(input, num);
				if (tokenLength == 0)
				{
					return false;
				}
				num += tokenLength;
				num += HttpRuleParser.GetWhitespaceLength(input, num);
				if (num == input.Length || input[num] != '=')
				{
					return true;
				}
				num++;
				num += HttpRuleParser.GetWhitespaceLength(input, num);
				int valueLength = NameValueHeaderValue.GetValueLength(input, num);
				if (valueLength == 0)
				{
					return false;
				}
				num += valueLength;
				parameterEndIndex = num - 1;
				num += HttpRuleParser.GetWhitespaceLength(input, num);
				parseEndIndex = num;
				if (num >= input.Length || input[num] != ',')
				{
					return true;
				}
			}
			return true;
		}

		object ICloneable.Clone()
		{
			return new AuthenticationHeaderValue(this);
		}
	}
}
