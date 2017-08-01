using System;
using System.Globalization;
using System.Text;

namespace System.Net.Http
{
	internal static class HttpRuleParser
	{
		private const int maxNestedCount = 5;

		internal const char CR = '\r';

		internal const char LF = '\n';

		internal const int MaxInt64Digits = 19;

		internal const int MaxInt32Digits = 10;

		private static readonly bool[] tokenChars;

		private static readonly string[] dateFormats;

		internal static readonly Encoding DefaultHttpEncoding;

		static HttpRuleParser()
		{
			HttpRuleParser.dateFormats = new string[]
			{
				"ddd, d MMM yyyy H:m:s 'GMT'",
				"ddd, d MMM yyyy H:m:s",
				"d MMM yyyy H:m:s 'GMT'",
				"d MMM yyyy H:m:s",
				"ddd, d MMM yy H:m:s 'GMT'",
				"ddd, d MMM yy H:m:s",
				"d MMM yy H:m:s 'GMT'",
				"d MMM yy H:m:s",
				"dddd, d'-'MMM'-'yy H:m:s 'GMT'",
				"dddd, d'-'MMM'-'yy H:m:s",
				"ddd MMM d H:m:s yyyy",
				"ddd, d MMM yyyy H:m:s zzz",
				"ddd, d MMM yyyy H:m:s",
				"d MMM yyyy H:m:s zzz",
				"d MMM yyyy H:m:s"
			};
			HttpRuleParser.DefaultHttpEncoding = Encoding.GetEncoding(28591);
			HttpRuleParser.tokenChars = new bool[128];
			for (int i = 33; i < 127; i++)
			{
				HttpRuleParser.tokenChars[i] = true;
			}
			HttpRuleParser.tokenChars[40] = false;
			HttpRuleParser.tokenChars[41] = false;
			HttpRuleParser.tokenChars[60] = false;
			HttpRuleParser.tokenChars[62] = false;
			HttpRuleParser.tokenChars[64] = false;
			HttpRuleParser.tokenChars[44] = false;
			HttpRuleParser.tokenChars[59] = false;
			HttpRuleParser.tokenChars[58] = false;
			HttpRuleParser.tokenChars[92] = false;
			HttpRuleParser.tokenChars[34] = false;
			HttpRuleParser.tokenChars[47] = false;
			HttpRuleParser.tokenChars[91] = false;
			HttpRuleParser.tokenChars[93] = false;
			HttpRuleParser.tokenChars[63] = false;
			HttpRuleParser.tokenChars[61] = false;
			HttpRuleParser.tokenChars[123] = false;
			HttpRuleParser.tokenChars[125] = false;
		}

		internal static bool IsTokenChar(char character)
		{
			return character <= '\u007f' && HttpRuleParser.tokenChars[(int)character];
		}

		internal static int GetTokenLength(string input, int startIndex)
		{
			if (startIndex >= input.Length)
			{
				return 0;
			}
			for (int i = startIndex; i < input.Length; i++)
			{
				if (!HttpRuleParser.IsTokenChar(input[i]))
				{
					return i - startIndex;
				}
			}
			return input.Length - startIndex;
		}

		internal static int GetWhitespaceLength(string input, int startIndex)
		{
			if (startIndex >= input.Length)
			{
				return 0;
			}
			for (int i = startIndex; i < input.Length; i++)
			{
				char c = input[i];
				if (c != ' ' && c != '\t')
				{
					if (c == '\r' && i + 2 < input.Length && input[i + 1] == '\n')
					{
						char c2 = input[i + 2];
						if (c2 == ' ' || c2 == '\t')
						{
							i += 3;
							continue;
						}
					}
					return i - startIndex;
				}
			}
			return input.Length - startIndex;
		}

		internal static bool ContainsInvalidNewLine(string value)
		{
			return HttpRuleParser.ContainsInvalidNewLine(value, 0);
		}

		internal static bool ContainsInvalidNewLine(string value, int startIndex)
		{
			for (int i = startIndex; i < value.Length; i++)
			{
				if (value[i] == '\r')
				{
					int num = i + 1;
					if (num < value.Length && value[num] == '\n')
					{
						i = num + 1;
						if (i == value.Length)
						{
							return true;
						}
						char c = value[i];
						if (c != ' ' && c != '\t')
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		internal static int GetNumberLength(string input, int startIndex, bool allowDecimal)
		{
			int i = startIndex;
			bool flag = !allowDecimal;
			if (input[i] == '.')
			{
				return 0;
			}
			while (i < input.Length)
			{
				char c = input[i];
				if (c >= '0' && c <= '9')
				{
					i++;
				}
				else
				{
					if (flag || c != '.')
					{
						break;
					}
					flag = true;
					i++;
				}
			}
			return i - startIndex;
		}

		internal static int GetHostLength(string input, int startIndex, bool allowToken, out string host)
		{
			host = null;
			if (startIndex >= input.Length)
			{
				return 0;
			}
			int i = startIndex;
			bool flag = true;
			while (i < input.Length)
			{
				char c = input[i];
				if (c == '/')
				{
					return 0;
				}
				if (c == ' ' || c == '\t' || c == '\r' || c == ',')
				{
					break;
				}
				flag = (flag && HttpRuleParser.IsTokenChar(c));
				i++;
			}
			int num = i - startIndex;
			if (num == 0)
			{
				return 0;
			}
			string text = input.Substring(startIndex, num);
			if ((!allowToken || !flag) && !HttpRuleParser.IsValidHostName(text))
			{
				return 0;
			}
			host = text;
			return num;
		}

		internal static HttpParseResult GetCommentLength(string input, int startIndex, out int length)
		{
			int num = 0;
			return HttpRuleParser.GetExpressionLength(input, startIndex, '(', ')', true, ref num, out length);
		}

		internal static HttpParseResult GetQuotedStringLength(string input, int startIndex, out int length)
		{
			int num = 0;
			return HttpRuleParser.GetExpressionLength(input, startIndex, '"', '"', false, ref num, out length);
		}

		internal static HttpParseResult GetQuotedPairLength(string input, int startIndex, out int length)
		{
			length = 0;
			if (input[startIndex] != '\\')
			{
				return HttpParseResult.NotParsed;
			}
			if (startIndex + 2 > input.Length || input[startIndex + 1] > '\u007f')
			{
				return HttpParseResult.InvalidFormat;
			}
			length = 2;
			return HttpParseResult.Parsed;
		}

		internal static string DateToString(DateTimeOffset dateTime)
		{
			return dateTime.ToUniversalTime().ToString("r", CultureInfo.InvariantCulture);
		}

		internal static bool TryStringToDate(string input, out DateTimeOffset result)
		{
			return DateTimeOffset.TryParseExact(input, HttpRuleParser.dateFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowInnerWhite | DateTimeStyles.AssumeUniversal, out result);
		}

		private static HttpParseResult GetExpressionLength(string input, int startIndex, char openChar, char closeChar, bool supportsNesting, ref int nestedCount, out int length)
		{
			length = 0;
			if (input[startIndex] != openChar)
			{
				return HttpParseResult.NotParsed;
			}
			int i = startIndex + 1;
			while (i < input.Length)
			{
				int num = 0;
				if (i + 2 < input.Length && HttpRuleParser.GetQuotedPairLength(input, i, out num) == HttpParseResult.Parsed)
				{
					i += num;
				}
				else
				{
					if (supportsNesting && input[i] == openChar)
					{
						nestedCount++;
						try
						{
							if (nestedCount > 5)
							{
								HttpParseResult result = HttpParseResult.InvalidFormat;
								return result;
							}
							int num2 = 0;
							switch (HttpRuleParser.GetExpressionLength(input, i, openChar, closeChar, supportsNesting, ref nestedCount, out num2))
							{
							case HttpParseResult.Parsed:
								i += num2;
								break;
							case HttpParseResult.InvalidFormat:
							{
								HttpParseResult result = HttpParseResult.InvalidFormat;
								return result;
							}
							}
						}
						finally
						{
							nestedCount--;
						}
					}
					if (input[i] == closeChar)
					{
						length = i - startIndex + 1;
						return HttpParseResult.Parsed;
					}
					i++;
				}
			}
			return HttpParseResult.InvalidFormat;
		}

		private static bool IsValidHostName(string host)
		{
			Uri uri;
			return Uri.TryCreate("http://u@" + host + "/", UriKind.Absolute, out uri);
		}
	}
}
