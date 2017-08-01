using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers
{
	public class ContentDispositionHeaderValue : ICloneable
	{
		private const string fileName = "filename";

		private const string name = "name";

		private const string fileNameStar = "filename*";

		private const string creationDate = "creation-date";

		private const string modificationDate = "modification-date";

		private const string readDate = "read-date";

		private const string size = "size";

		private ICollection<NameValueHeaderValue> parameters;

		private string dispositionType;

		public string DispositionType
		{
			get
			{
				return this.dispositionType;
			}
			set
			{
				ContentDispositionHeaderValue.CheckDispositionTypeFormat(value, "value");
				this.dispositionType = value;
			}
		}

		public ICollection<NameValueHeaderValue> Parameters
		{
			get
			{
				if (this.parameters == null)
				{
					this.parameters = new ObjectCollection<NameValueHeaderValue>();
				}
				return this.parameters;
			}
		}

		public string Name
		{
			get
			{
				return this.GetName("name");
			}
			set
			{
				this.SetName("name", value);
			}
		}

		public string FileName
		{
			get
			{
				return this.GetName("filename");
			}
			set
			{
				this.SetName("filename", value);
			}
		}

		public string FileNameStar
		{
			get
			{
				return this.GetName("filename*");
			}
			set
			{
				this.SetName("filename*", value);
			}
		}

		public DateTimeOffset? CreationDate
		{
			get
			{
				return this.GetDate("creation-date");
			}
			set
			{
				this.SetDate("creation-date", value);
			}
		}

		public DateTimeOffset? ModificationDate
		{
			get
			{
				return this.GetDate("modification-date");
			}
			set
			{
				this.SetDate("modification-date", value);
			}
		}

		public DateTimeOffset? ReadDate
		{
			get
			{
				return this.GetDate("read-date");
			}
			set
			{
				this.SetDate("read-date", value);
			}
		}

		public long? Size
		{
			get
			{
				NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(this.parameters, "size");
				if (nameValueHeaderValue != null)
				{
					string value = nameValueHeaderValue.Value;
					ulong value2;
					if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value2))
					{
						return new long?((long)value2);
					}
				}
				return null;
			}
			set
			{
				NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(this.parameters, "size");
				if (!value.HasValue)
				{
					if (nameValueHeaderValue != null)
					{
						this.parameters.Remove(nameValueHeaderValue);
						return;
					}
				}
				else
				{
					if (value < 0L)
					{
						throw new ArgumentOutOfRangeException("value");
					}
					if (nameValueHeaderValue != null)
					{
						nameValueHeaderValue.Value = value.Value.ToString(CultureInfo.InvariantCulture);
						return;
					}
					string value2 = value.Value.ToString(CultureInfo.InvariantCulture);
					this.parameters.Add(new NameValueHeaderValue("size", value2));
				}
			}
		}

		internal ContentDispositionHeaderValue()
		{
		}

		protected ContentDispositionHeaderValue(ContentDispositionHeaderValue source)
		{
			this.dispositionType = source.dispositionType;
			if (source.parameters != null)
			{
				foreach (NameValueHeaderValue current in source.parameters)
				{
					this.Parameters.Add((NameValueHeaderValue)((ICloneable)current).Clone());
				}
			}
		}

		public ContentDispositionHeaderValue(string dispositionType)
		{
			ContentDispositionHeaderValue.CheckDispositionTypeFormat(dispositionType, "dispositionType");
			this.dispositionType = dispositionType;
		}

		public override string ToString()
		{
			return this.dispositionType + NameValueHeaderValue.ToString(this.parameters, ';', true);
		}

		public override bool Equals(object obj)
		{
			ContentDispositionHeaderValue contentDispositionHeaderValue = obj as ContentDispositionHeaderValue;
			return contentDispositionHeaderValue != null && string.Compare(this.dispositionType, contentDispositionHeaderValue.dispositionType, StringComparison.OrdinalIgnoreCase) == 0 && HeaderUtilities.AreEqualCollections<NameValueHeaderValue>(this.parameters, contentDispositionHeaderValue.parameters);
		}

		public override int GetHashCode()
		{
			return this.dispositionType.ToLowerInvariant().GetHashCode() ^ NameValueHeaderValue.GetHashCode(this.parameters);
		}

		object ICloneable.Clone()
		{
			return new ContentDispositionHeaderValue(this);
		}

		public static ContentDispositionHeaderValue Parse(string input)
		{
			int num = 0;
			return (ContentDispositionHeaderValue)GenericHeaderParser.ContentDispositionParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out ContentDispositionHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (GenericHeaderParser.ContentDispositionParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (ContentDispositionHeaderValue)obj;
				return true;
			}
			return false;
		}

		internal static int GetDispositionTypeLength(string input, int startIndex, out object parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
			{
				return 0;
			}
			string text = null;
			int dispositionTypeExpressionLength = ContentDispositionHeaderValue.GetDispositionTypeExpressionLength(input, startIndex, out text);
			if (dispositionTypeExpressionLength == 0)
			{
				return 0;
			}
			int num = startIndex + dispositionTypeExpressionLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			ContentDispositionHeaderValue contentDispositionHeaderValue = new ContentDispositionHeaderValue();
			contentDispositionHeaderValue.dispositionType = text;
			if (num >= input.Length || input[num] != ';')
			{
				parsedValue = contentDispositionHeaderValue;
				return num - startIndex;
			}
			num++;
			int nameValueListLength = NameValueHeaderValue.GetNameValueListLength(input, num, ';', contentDispositionHeaderValue.Parameters);
			if (nameValueListLength == 0)
			{
				return 0;
			}
			parsedValue = contentDispositionHeaderValue;
			return num + nameValueListLength - startIndex;
		}

		private static int GetDispositionTypeExpressionLength(string input, int startIndex, out string dispositionType)
		{
			dispositionType = null;
			int tokenLength = HttpRuleParser.GetTokenLength(input, startIndex);
			if (tokenLength == 0)
			{
				return 0;
			}
			dispositionType = input.Substring(startIndex, tokenLength);
			return tokenLength;
		}

		private static void CheckDispositionTypeFormat(string dispositionType, string parameterName)
		{
			if (string.IsNullOrEmpty(dispositionType))
			{
				throw new ArgumentException(SR.net_http_argument_empty_string, parameterName);
			}
			string text;
			int dispositionTypeExpressionLength = ContentDispositionHeaderValue.GetDispositionTypeExpressionLength(dispositionType, 0, out text);
			if (dispositionTypeExpressionLength == 0 || text.Length != dispositionType.Length)
			{
				throw new FormatException(string.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, new object[]
				{
					dispositionType
				}));
			}
		}

		private DateTimeOffset? GetDate(string parameter)
		{
			NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(this.parameters, parameter);
			if (nameValueHeaderValue != null)
			{
				string text = nameValueHeaderValue.Value;
				if (this.IsQuoted(text))
				{
					text = text.Substring(1, text.Length - 2);
				}
				DateTimeOffset value;
				if (HttpRuleParser.TryStringToDate(text, out value))
				{
					return new DateTimeOffset?(value);
				}
			}
			return null;
		}

		private void SetDate(string parameter, DateTimeOffset? date)
		{
			NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(this.parameters, parameter);
			if (!date.HasValue)
			{
				if (nameValueHeaderValue != null)
				{
					this.parameters.Remove(nameValueHeaderValue);
					return;
				}
			}
			else
			{
				string value = string.Format(CultureInfo.InvariantCulture, "\"{0}\"", new object[]
				{
					HttpRuleParser.DateToString(date.Value)
				});
				if (nameValueHeaderValue != null)
				{
					nameValueHeaderValue.Value = value;
					return;
				}
				this.Parameters.Add(new NameValueHeaderValue(parameter, value));
			}
		}

		private string GetName(string parameter)
		{
			NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(this.parameters, parameter);
			if (nameValueHeaderValue == null)
			{
				return null;
			}
			if (parameter.EndsWith("*", StringComparison.Ordinal))
			{
				string result;
				if (this.TryDecode5987(nameValueHeaderValue.Value, out result))
				{
					return result;
				}
				return null;
			}
			else
			{
				string result;
				if (this.TryDecodeMime(nameValueHeaderValue.Value, out result))
				{
					return result;
				}
				return nameValueHeaderValue.Value;
			}
		}

		private void SetName(string parameter, string value)
		{
			NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(this.parameters, parameter);
			if (string.IsNullOrEmpty(value))
			{
				if (nameValueHeaderValue != null)
				{
					this.parameters.Remove(nameValueHeaderValue);
					return;
				}
			}
			else
			{
				string value2 = string.Empty;
				if (parameter.EndsWith("*", StringComparison.Ordinal))
				{
					value2 = this.Encode5987(value);
				}
				else
				{
					value2 = this.EncodeAndQuoteMime(value);
				}
				if (nameValueHeaderValue != null)
				{
					nameValueHeaderValue.Value = value2;
					return;
				}
				this.Parameters.Add(new NameValueHeaderValue(parameter, value2));
			}
		}

		private string EncodeAndQuoteMime(string input)
		{
			string text = input;
			bool flag = false;
			if (this.IsQuoted(text))
			{
				text = text.Substring(1, text.Length - 2);
				flag = true;
			}
			if (text.IndexOf("\"", 0, StringComparison.Ordinal) >= 0)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, new object[]
				{
					input
				}));
			}
			if (this.RequiresEncoding(text))
			{
				flag = true;
				text = this.EncodeMime(text);
			}
			else if (!flag && HttpRuleParser.GetTokenLength(text, 0) != text.Length)
			{
				flag = true;
			}
			if (flag)
			{
				text = string.Format(CultureInfo.InvariantCulture, "\"{0}\"", new object[]
				{
					text
				});
			}
			return text;
		}

		private bool IsQuoted(string value)
		{
			return value.Length > 1 && value.StartsWith("\"", StringComparison.Ordinal) && value.EndsWith("\"", StringComparison.Ordinal);
		}

		private bool RequiresEncoding(string input)
		{
			for (int i = 0; i < input.Length; i++)
			{
				char c = input[i];
				if (c > '\u007f')
				{
					return true;
				}
			}
			return false;
		}

		private string EncodeMime(string input)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(input);
			string text = Convert.ToBase64String(bytes);
			return string.Format(CultureInfo.InvariantCulture, "=?utf-8?B?{0}?=", new object[]
			{
				text
			});
		}

		private bool TryDecodeMime(string input, out string output)
		{
			output = null;
			if (!this.IsQuoted(input) || input.Length < 10)
			{
				return false;
			}
			string[] array = input.Split(new char[]
			{
				'?'
			});
			if (array.Length != 5 || array[0] != "\"=" || array[4] != "=\"" || array[2].ToLowerInvariant() != "b")
			{
				return false;
			}
			try
			{
				Encoding encoding = Encoding.GetEncoding(array[1]);
				byte[] bytes = Convert.FromBase64String(array[3]);
				output = encoding.GetString(bytes);
				return true;
			}
			catch (ArgumentException)
			{
			}
			catch (FormatException)
			{
			}
			return false;
		}

		private string Encode5987(string input)
		{
			StringBuilder stringBuilder = new StringBuilder("utf-8''");
			for (int i = 0; i < input.Length; i++)
			{
				char c = input[i];
				if (c > '\u007f')
				{
					byte[] bytes = Encoding.UTF8.GetBytes(c.ToString());
					byte[] array = bytes;
					for (int j = 0; j < array.Length; j++)
					{
						byte character = array[j];
						stringBuilder.Append(Uri.HexEscape((char)character));
					}
				}
				else if (!HttpRuleParser.IsTokenChar(c) || c == '*' || c == '\'' || c == '%')
				{
					stringBuilder.Append(Uri.HexEscape(c));
				}
				else
				{
					stringBuilder.Append(c);
				}
			}
			return stringBuilder.ToString();
		}

		private bool TryDecode5987(string input, out string output)
		{
			output = null;
			string[] array = input.Split(new char[]
			{
				'\''
			});
			if (array.Length != 3)
			{
				return false;
			}
			StringBuilder stringBuilder = new StringBuilder();
			try
			{
				Encoding encoding = Encoding.GetEncoding(array[0]);
				string text = array[2];
				byte[] array2 = new byte[text.Length];
				int num = 0;
				for (int i = 0; i < text.Length; i++)
				{
					if (Uri.IsHexEncoding(text, i))
					{
						array2[num++] = (byte)Uri.HexUnescape(text, ref i);
						i--;
					}
					else
					{
						if (num > 0)
						{
							stringBuilder.Append(encoding.GetString(array2, 0, num));
							num = 0;
						}
						stringBuilder.Append(text[i]);
					}
				}
				if (num > 0)
				{
					stringBuilder.Append(encoding.GetString(array2, 0, num));
				}
			}
			catch (ArgumentException)
			{
				return false;
			}
			output = stringBuilder.ToString();
			return true;
		}
	}
}
