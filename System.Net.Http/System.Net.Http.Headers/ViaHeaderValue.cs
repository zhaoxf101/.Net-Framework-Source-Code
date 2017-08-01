using System;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers
{
	public class ViaHeaderValue : ICloneable
	{
		private string protocolName;

		private string protocolVersion;

		private string receivedBy;

		private string comment;

		public string ProtocolName
		{
			get
			{
				return this.protocolName;
			}
		}

		public string ProtocolVersion
		{
			get
			{
				return this.protocolVersion;
			}
		}

		public string ReceivedBy
		{
			get
			{
				return this.receivedBy;
			}
		}

		public string Comment
		{
			get
			{
				return this.comment;
			}
		}

		public ViaHeaderValue(string protocolVersion, string receivedBy) : this(protocolVersion, receivedBy, null, null)
		{
		}

		public ViaHeaderValue(string protocolVersion, string receivedBy, string protocolName) : this(protocolVersion, receivedBy, protocolName, null)
		{
		}

		public ViaHeaderValue(string protocolVersion, string receivedBy, string protocolName, string comment)
		{
			HeaderUtilities.CheckValidToken(protocolVersion, "protocolVersion");
			ViaHeaderValue.CheckReceivedBy(receivedBy);
			if (!string.IsNullOrEmpty(protocolName))
			{
				HeaderUtilities.CheckValidToken(protocolName, "protocolName");
				this.protocolName = protocolName;
			}
			if (!string.IsNullOrEmpty(comment))
			{
				HeaderUtilities.CheckValidComment(comment, "comment");
				this.comment = comment;
			}
			this.protocolVersion = protocolVersion;
			this.receivedBy = receivedBy;
		}

		private ViaHeaderValue()
		{
		}

		private ViaHeaderValue(ViaHeaderValue source)
		{
			this.protocolName = source.protocolName;
			this.protocolVersion = source.protocolVersion;
			this.receivedBy = source.receivedBy;
			this.comment = source.comment;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (!string.IsNullOrEmpty(this.protocolName))
			{
				stringBuilder.Append(this.protocolName);
				stringBuilder.Append('/');
			}
			stringBuilder.Append(this.protocolVersion);
			stringBuilder.Append(' ');
			stringBuilder.Append(this.receivedBy);
			if (!string.IsNullOrEmpty(this.comment))
			{
				stringBuilder.Append(' ');
				stringBuilder.Append(this.comment);
			}
			return stringBuilder.ToString();
		}

		public override bool Equals(object obj)
		{
			ViaHeaderValue viaHeaderValue = obj as ViaHeaderValue;
			return viaHeaderValue != null && (string.Compare(this.protocolVersion, viaHeaderValue.protocolVersion, StringComparison.OrdinalIgnoreCase) == 0 && string.Compare(this.receivedBy, viaHeaderValue.receivedBy, StringComparison.OrdinalIgnoreCase) == 0 && string.Compare(this.protocolName, viaHeaderValue.protocolName, StringComparison.OrdinalIgnoreCase) == 0) && string.CompareOrdinal(this.comment, viaHeaderValue.comment) == 0;
		}

		public override int GetHashCode()
		{
			int num = this.protocolVersion.ToLowerInvariant().GetHashCode() ^ this.receivedBy.ToLowerInvariant().GetHashCode();
			if (!string.IsNullOrEmpty(this.protocolName))
			{
				num ^= this.protocolName.ToLowerInvariant().GetHashCode();
			}
			if (!string.IsNullOrEmpty(this.comment))
			{
				num ^= this.comment.GetHashCode();
			}
			return num;
		}

		public static ViaHeaderValue Parse(string input)
		{
			int num = 0;
			return (ViaHeaderValue)GenericHeaderParser.SingleValueViaParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out ViaHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (GenericHeaderParser.SingleValueViaParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (ViaHeaderValue)obj;
				return true;
			}
			return false;
		}

		internal static int GetViaLength(string input, int startIndex, out object parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
			{
				return 0;
			}
			string text = null;
			string text2 = null;
			int num = ViaHeaderValue.GetProtocolEndIndex(input, startIndex, out text, out text2);
			if (num == startIndex || num == input.Length)
			{
				return 0;
			}
			string text3 = null;
			int hostLength = HttpRuleParser.GetHostLength(input, num, true, out text3);
			if (hostLength == 0)
			{
				return 0;
			}
			num += hostLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			string text4 = null;
			if (num < input.Length && input[num] == '(')
			{
				int num2 = 0;
				if (HttpRuleParser.GetCommentLength(input, num, out num2) != HttpParseResult.Parsed)
				{
					return 0;
				}
				text4 = input.Substring(num, num2);
				num += num2;
				num += HttpRuleParser.GetWhitespaceLength(input, num);
			}
			parsedValue = new ViaHeaderValue
			{
				protocolVersion = text2,
				protocolName = text,
				receivedBy = text3,
				comment = text4
			};
			return num - startIndex;
		}

		private static int GetProtocolEndIndex(string input, int startIndex, out string protocolName, out string protocolVersion)
		{
			protocolName = null;
			protocolVersion = null;
			int tokenLength = HttpRuleParser.GetTokenLength(input, startIndex);
			if (tokenLength == 0)
			{
				return 0;
			}
			int num = startIndex + tokenLength;
			int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, num);
			num += whitespaceLength;
			if (num == input.Length)
			{
				return 0;
			}
			if (input[num] == '/')
			{
				protocolName = input.Substring(startIndex, tokenLength);
				num++;
				num += HttpRuleParser.GetWhitespaceLength(input, num);
				tokenLength = HttpRuleParser.GetTokenLength(input, num);
				if (tokenLength == 0)
				{
					return 0;
				}
				protocolVersion = input.Substring(num, tokenLength);
				num += tokenLength;
				whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, num);
				num += whitespaceLength;
			}
			else
			{
				protocolVersion = input.Substring(startIndex, tokenLength);
			}
			if (whitespaceLength == 0)
			{
				return 0;
			}
			return num;
		}

		object ICloneable.Clone()
		{
			return new ViaHeaderValue(this);
		}

		private static void CheckReceivedBy(string receivedBy)
		{
			if (string.IsNullOrEmpty(receivedBy))
			{
				throw new ArgumentException(SR.net_http_argument_empty_string, "receivedBy");
			}
			string text = null;
			if (HttpRuleParser.GetHostLength(receivedBy, 0, true, out text) != receivedBy.Length)
			{
				throw new FormatException(string.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, new object[]
				{
					receivedBy
				}));
			}
		}
	}
}
