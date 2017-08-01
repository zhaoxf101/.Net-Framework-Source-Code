using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using System.Text;

namespace System.Net.Http.Headers
{
	internal static class HeaderUtilities
	{
		private const string qualityName = "q";

		internal const string ConnectionClose = "close";

		internal const string BytesUnit = "bytes";

		internal static readonly TransferCodingHeaderValue TransferEncodingChunked = new TransferCodingHeaderValue("chunked");

		internal static readonly NameValueWithParametersHeaderValue ExpectContinue = new NameValueWithParametersHeaderValue("100-continue");

		internal static readonly Action<HttpHeaderValueCollection<string>, string> TokenValidator = new Action<HttpHeaderValueCollection<string>, string>(HeaderUtilities.ValidateToken);

		internal static void SetQuality(ICollection<NameValueHeaderValue> parameters, double? value)
		{
			NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(parameters, "q");
			if (value.HasValue)
			{
				double? num = value;
				if (num.GetValueOrDefault() >= 0.0 || !num.HasValue)
				{
					double? num2 = value;
					if (num2.GetValueOrDefault() <= 1.0 || !num2.HasValue)
					{
						string value2 = value.Value.ToString("0.0##", NumberFormatInfo.InvariantInfo);
						if (nameValueHeaderValue != null)
						{
							nameValueHeaderValue.Value = value2;
							return;
						}
						parameters.Add(new NameValueHeaderValue("q", value2));
						return;
					}
				}
				throw new ArgumentOutOfRangeException("value");
			}
			if (nameValueHeaderValue != null)
			{
				parameters.Remove(nameValueHeaderValue);
			}
		}

		internal static double? GetQuality(ICollection<NameValueHeaderValue> parameters)
		{
			NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(parameters, "q");
			if (nameValueHeaderValue != null)
			{
				double value = 0.0;
				if (double.TryParse(nameValueHeaderValue.Value, NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo, out value))
				{
					return new double?(value);
				}
				if (Logging.On)
				{
					Logging.PrintError(Logging.Http, string.Format(CultureInfo.InvariantCulture, SR.net_http_log_headers_invalid_quality, new object[]
					{
						nameValueHeaderValue.Value
					}));
				}
			}
			return null;
		}

		internal static void CheckValidToken(string value, string parameterName)
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentException(SR.net_http_argument_empty_string, parameterName);
			}
			if (HttpRuleParser.GetTokenLength(value, 0) != value.Length)
			{
				throw new FormatException(string.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, new object[]
				{
					value
				}));
			}
		}

		internal static void CheckValidComment(string value, string parameterName)
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentException(SR.net_http_argument_empty_string, parameterName);
			}
			int num = 0;
			if (HttpRuleParser.GetCommentLength(value, 0, out num) != HttpParseResult.Parsed || num != value.Length)
			{
				throw new FormatException(string.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, new object[]
				{
					value
				}));
			}
		}

		internal static void CheckValidQuotedString(string value, string parameterName)
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentException(SR.net_http_argument_empty_string, parameterName);
			}
			int num = 0;
			if (HttpRuleParser.GetQuotedStringLength(value, 0, out num) != HttpParseResult.Parsed || num != value.Length)
			{
				throw new FormatException(string.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, new object[]
				{
					value
				}));
			}
		}

		internal static bool AreEqualCollections<T>(ICollection<T> x, ICollection<T> y)
		{
			return HeaderUtilities.AreEqualCollections<T>(x, y, null);
		}

		internal static bool AreEqualCollections<T>(ICollection<T> x, ICollection<T> y, IEqualityComparer<T> comparer)
		{
			if (x == null)
			{
				return y == null || y.Count == 0;
			}
			if (y == null)
			{
				return x.Count == 0;
			}
			if (x.Count != y.Count)
			{
				return false;
			}
			if (x.Count == 0)
			{
				return true;
			}
			bool[] array = new bool[x.Count];
			foreach (T current in x)
			{
				int num = 0;
				bool flag = false;
				foreach (T current2 in y)
				{
					if (!array[num] && ((comparer == null && current.Equals(current2)) || (comparer != null && comparer.Equals(current, current2))))
					{
						array[num] = true;
						flag = true;
						break;
					}
					num++;
				}
				if (!flag)
				{
					return false;
				}
			}
			return true;
		}

		internal static int GetNextNonEmptyOrWhitespaceIndex(string input, int startIndex, bool skipEmptyValues, out bool separatorFound)
		{
			separatorFound = false;
			int num = startIndex + HttpRuleParser.GetWhitespaceLength(input, startIndex);
			if (num == input.Length || input[num] != ',')
			{
				return num;
			}
			separatorFound = true;
			num++;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			if (skipEmptyValues)
			{
				while (num < input.Length && input[num] == ',')
				{
					num++;
					num += HttpRuleParser.GetWhitespaceLength(input, num);
				}
			}
			return num;
		}

		internal static DateTimeOffset? GetDateTimeOffsetValue(string headerName, HttpHeaders store)
		{
			object parsedValues = store.GetParsedValues(headerName);
			if (parsedValues != null)
			{
				return new DateTimeOffset?((DateTimeOffset)parsedValues);
			}
			return null;
		}

		internal static TimeSpan? GetTimeSpanValue(string headerName, HttpHeaders store)
		{
			object parsedValues = store.GetParsedValues(headerName);
			if (parsedValues != null)
			{
				return new TimeSpan?((TimeSpan)parsedValues);
			}
			return null;
		}

		internal static bool TryParseInt32(string value, out int result)
		{
			return int.TryParse(value, NumberStyles.None, NumberFormatInfo.InvariantInfo, out result);
		}

		internal static bool TryParseInt64(string value, out long result)
		{
			return long.TryParse(value, NumberStyles.None, NumberFormatInfo.InvariantInfo, out result);
		}

		internal static string DumpHeaders(params HttpHeaders[] headers)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("{\r\n");
			for (int i = 0; i < headers.Length; i++)
			{
				if (headers[i] != null)
				{
					foreach (KeyValuePair<string, IEnumerable<string>> current in headers[i])
					{
						foreach (string current2 in current.Value)
						{
							stringBuilder.Append("  ");
							stringBuilder.Append(current.Key);
							stringBuilder.Append(": ");
							stringBuilder.Append(current2);
							stringBuilder.Append("\r\n");
						}
					}
				}
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		internal static bool IsValidEmailAddress(string value)
		{
			try
			{
				new MailAddress(value);
				return true;
			}
			catch (FormatException ex)
			{
				if (Logging.On)
				{
					Logging.PrintError(Logging.Http, string.Format(CultureInfo.InvariantCulture, SR.net_http_log_headers_wrong_email_format, new object[]
					{
						value,
						ex.Message
					}));
				}
			}
			return false;
		}

		private static void ValidateToken(HttpHeaderValueCollection<string> collection, string value)
		{
			HeaderUtilities.CheckValidToken(value, "item");
		}
	}
}
