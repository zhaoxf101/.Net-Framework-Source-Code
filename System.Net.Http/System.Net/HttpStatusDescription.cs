using System;

namespace System.Net
{
	internal static class HttpStatusDescription
	{
		private static readonly string[][] httpStatusDescriptions;

		internal static string Get(HttpStatusCode code)
		{
			return HttpStatusDescription.Get((int)code);
		}

		internal static string Get(int code)
		{
			if (code >= 100 && code < 600)
			{
				int num = code / 100;
				int num2 = code % 100;
				if (num2 < HttpStatusDescription.httpStatusDescriptions[num].Length)
				{
					return HttpStatusDescription.httpStatusDescriptions[num][num2];
				}
			}
			return null;
		}

		static HttpStatusDescription()
		{
			// Note: this type is marked as 'beforefieldinit'.
			string[][] array = new string[6][];
			array[1] = new string[]
			{
				"Continue",
				"Switching Protocols",
				"Processing"
			};
			array[2] = new string[]
			{
				"OK",
				"Created",
				"Accepted",
				"Non-Authoritative Information",
				"No Content",
				"Reset Content",
				"Partial Content",
				"Multi-Status"
			};
			array[3] = new string[]
			{
				"Multiple Choices",
				"Moved Permanently",
				"Found",
				"See Other",
				"Not Modified",
				"Use Proxy",
				null,
				"Temporary Redirect"
			};
			string[][] arg_18E_0 = array;
			int arg_18E_1 = 4;
			string[] array2 = new string[26];
			array2[0] = "Bad Request";
			array2[1] = "Unauthorized";
			array2[2] = "Payment Required";
			array2[3] = "Forbidden";
			array2[4] = "Not Found";
			array2[5] = "Method Not Allowed";
			array2[6] = "Not Acceptable";
			array2[7] = "Proxy Authentication Required";
			array2[8] = "Request Timeout";
			array2[9] = "Conflict";
			array2[10] = "Gone";
			array2[11] = "Length Required";
			array2[12] = "Precondition Failed";
			array2[13] = "Request Entity Too Large";
			array2[14] = "Request-Uri Too Long";
			array2[15] = "Unsupported Media Type";
			array2[16] = "Requested Range Not Satisfiable";
			array2[17] = "Expectation Failed";
			array2[22] = "Unprocessable Entity";
			array2[23] = "Locked";
			array2[24] = "Failed Dependency";
			arg_18E_0[arg_18E_1] = array2;
			array[5] = new string[]
			{
				"Internal Server Error",
				"Not Implemented",
				"Bad Gateway",
				"Service Unavailable",
				"Gateway Timeout",
				"Http Version Not Supported",
				null,
				"Insufficient Storage"
			};
			HttpStatusDescription.httpStatusDescriptions = array;
		}
	}
}
