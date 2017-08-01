using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace System.Net.Http
{
	public class FormUrlEncodedContent : ByteArrayContent
	{
		public FormUrlEncodedContent(IEnumerable<KeyValuePair<string, string>> nameValueCollection) : base(FormUrlEncodedContent.GetContentByteArray(nameValueCollection))
		{
			base.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
		}

		private static byte[] GetContentByteArray(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
		{
			if (nameValueCollection == null)
			{
				throw new ArgumentNullException("nameValueCollection");
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<string, string> current in nameValueCollection)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append('&');
				}
				stringBuilder.Append(FormUrlEncodedContent.Encode(current.Key));
				stringBuilder.Append('=');
				stringBuilder.Append(FormUrlEncodedContent.Encode(current.Value));
			}
			return HttpRuleParser.DefaultHttpEncoding.GetBytes(stringBuilder.ToString());
		}

		private static string Encode(string data)
		{
			if (string.IsNullOrEmpty(data))
			{
				return string.Empty;
			}
			return Uri.EscapeDataString(data).Replace("%20", "+");
		}
	}
}
