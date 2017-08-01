using System;

namespace System.Net.Http.Headers
{
	internal class CacheControlHeaderParser : BaseHeaderParser
	{
		internal static readonly CacheControlHeaderParser Parser = new CacheControlHeaderParser();

		private CacheControlHeaderParser() : base(true)
		{
		}

		protected override int GetParsedValueLength(string value, int startIndex, object storeValue, out object parsedValue)
		{
			CacheControlHeaderValue cacheControlHeaderValue = storeValue as CacheControlHeaderValue;
			int cacheControlLength = CacheControlHeaderValue.GetCacheControlLength(value, startIndex, cacheControlHeaderValue, out cacheControlHeaderValue);
			parsedValue = cacheControlHeaderValue;
			return cacheControlLength;
		}
	}
}
