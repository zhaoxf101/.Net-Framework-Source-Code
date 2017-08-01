using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers
{
	public class CacheControlHeaderValue : ICloneable
	{
		private const string maxAgeString = "max-age";

		private const string maxStaleString = "max-stale";

		private const string minFreshString = "min-fresh";

		private const string mustRevalidateString = "must-revalidate";

		private const string noCacheString = "no-cache";

		private const string noStoreString = "no-store";

		private const string noTransformString = "no-transform";

		private const string onlyIfCachedString = "only-if-cached";

		private const string privateString = "private";

		private const string proxyRevalidateString = "proxy-revalidate";

		private const string publicString = "public";

		private const string sharedMaxAgeString = "s-maxage";

		private static readonly HttpHeaderParser nameValueListParser = GenericHeaderParser.MultipleValueNameValueParser;

		private static readonly Action<string> checkIsValidToken = new Action<string>(CacheControlHeaderValue.CheckIsValidToken);

		private bool noCache;

		private ICollection<string> noCacheHeaders;

		private bool noStore;

		private TimeSpan? maxAge;

		private TimeSpan? sharedMaxAge;

		private bool maxStale;

		private TimeSpan? maxStaleLimit;

		private TimeSpan? minFresh;

		private bool noTransform;

		private bool onlyIfCached;

		private bool publicField;

		private bool privateField;

		private ICollection<string> privateHeaders;

		private bool mustRevalidate;

		private bool proxyRevalidate;

		private ICollection<NameValueHeaderValue> extensions;

		public bool NoCache
		{
			get
			{
				return this.noCache;
			}
			set
			{
				this.noCache = value;
			}
		}

		public ICollection<string> NoCacheHeaders
		{
			get
			{
				if (this.noCacheHeaders == null)
				{
					this.noCacheHeaders = new ObjectCollection<string>(CacheControlHeaderValue.checkIsValidToken);
				}
				return this.noCacheHeaders;
			}
		}

		public bool NoStore
		{
			get
			{
				return this.noStore;
			}
			set
			{
				this.noStore = value;
			}
		}

		public TimeSpan? MaxAge
		{
			get
			{
				return this.maxAge;
			}
			set
			{
				this.maxAge = value;
			}
		}

		public TimeSpan? SharedMaxAge
		{
			get
			{
				return this.sharedMaxAge;
			}
			set
			{
				this.sharedMaxAge = value;
			}
		}

		public bool MaxStale
		{
			get
			{
				return this.maxStale;
			}
			set
			{
				this.maxStale = value;
			}
		}

		public TimeSpan? MaxStaleLimit
		{
			get
			{
				return this.maxStaleLimit;
			}
			set
			{
				this.maxStaleLimit = value;
			}
		}

		public TimeSpan? MinFresh
		{
			get
			{
				return this.minFresh;
			}
			set
			{
				this.minFresh = value;
			}
		}

		public bool NoTransform
		{
			get
			{
				return this.noTransform;
			}
			set
			{
				this.noTransform = value;
			}
		}

		public bool OnlyIfCached
		{
			get
			{
				return this.onlyIfCached;
			}
			set
			{
				this.onlyIfCached = value;
			}
		}

		public bool Public
		{
			get
			{
				return this.publicField;
			}
			set
			{
				this.publicField = value;
			}
		}

		public bool Private
		{
			get
			{
				return this.privateField;
			}
			set
			{
				this.privateField = value;
			}
		}

		public ICollection<string> PrivateHeaders
		{
			get
			{
				if (this.privateHeaders == null)
				{
					this.privateHeaders = new ObjectCollection<string>(CacheControlHeaderValue.checkIsValidToken);
				}
				return this.privateHeaders;
			}
		}

		public bool MustRevalidate
		{
			get
			{
				return this.mustRevalidate;
			}
			set
			{
				this.mustRevalidate = value;
			}
		}

		public bool ProxyRevalidate
		{
			get
			{
				return this.proxyRevalidate;
			}
			set
			{
				this.proxyRevalidate = value;
			}
		}

		public ICollection<NameValueHeaderValue> Extensions
		{
			get
			{
				if (this.extensions == null)
				{
					this.extensions = new ObjectCollection<NameValueHeaderValue>();
				}
				return this.extensions;
			}
		}

		public CacheControlHeaderValue()
		{
		}

		private CacheControlHeaderValue(CacheControlHeaderValue source)
		{
			this.noCache = source.noCache;
			this.noStore = source.noStore;
			this.maxAge = source.maxAge;
			this.sharedMaxAge = source.sharedMaxAge;
			this.maxStale = source.maxStale;
			this.maxStaleLimit = source.maxStaleLimit;
			this.minFresh = source.minFresh;
			this.noTransform = source.noTransform;
			this.onlyIfCached = source.onlyIfCached;
			this.publicField = source.publicField;
			this.privateField = source.privateField;
			this.mustRevalidate = source.mustRevalidate;
			this.proxyRevalidate = source.proxyRevalidate;
			if (source.noCacheHeaders != null)
			{
				foreach (string current in source.noCacheHeaders)
				{
					this.NoCacheHeaders.Add(current);
				}
			}
			if (source.privateHeaders != null)
			{
				foreach (string current2 in source.privateHeaders)
				{
					this.PrivateHeaders.Add(current2);
				}
			}
			if (source.extensions != null)
			{
				foreach (NameValueHeaderValue current3 in source.extensions)
				{
					this.Extensions.Add((NameValueHeaderValue)((ICloneable)current3).Clone());
				}
			}
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			CacheControlHeaderValue.AppendValueIfRequired(stringBuilder, this.noStore, "no-store");
			CacheControlHeaderValue.AppendValueIfRequired(stringBuilder, this.noTransform, "no-transform");
			CacheControlHeaderValue.AppendValueIfRequired(stringBuilder, this.onlyIfCached, "only-if-cached");
			CacheControlHeaderValue.AppendValueIfRequired(stringBuilder, this.publicField, "public");
			CacheControlHeaderValue.AppendValueIfRequired(stringBuilder, this.mustRevalidate, "must-revalidate");
			CacheControlHeaderValue.AppendValueIfRequired(stringBuilder, this.proxyRevalidate, "proxy-revalidate");
			if (this.noCache)
			{
				CacheControlHeaderValue.AppendValueWithSeparatorIfRequired(stringBuilder, "no-cache");
				if (this.noCacheHeaders != null && this.noCacheHeaders.Count > 0)
				{
					stringBuilder.Append("=\"");
					CacheControlHeaderValue.AppendValues(stringBuilder, this.noCacheHeaders);
					stringBuilder.Append('"');
				}
			}
			if (this.maxAge.HasValue)
			{
				CacheControlHeaderValue.AppendValueWithSeparatorIfRequired(stringBuilder, "max-age");
				stringBuilder.Append('=');
				stringBuilder.Append(((int)this.maxAge.Value.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo));
			}
			if (this.sharedMaxAge.HasValue)
			{
				CacheControlHeaderValue.AppendValueWithSeparatorIfRequired(stringBuilder, "s-maxage");
				stringBuilder.Append('=');
				stringBuilder.Append(((int)this.sharedMaxAge.Value.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo));
			}
			if (this.maxStale)
			{
				CacheControlHeaderValue.AppendValueWithSeparatorIfRequired(stringBuilder, "max-stale");
				if (this.maxStaleLimit.HasValue)
				{
					stringBuilder.Append('=');
					stringBuilder.Append(((int)this.maxStaleLimit.Value.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo));
				}
			}
			if (this.minFresh.HasValue)
			{
				CacheControlHeaderValue.AppendValueWithSeparatorIfRequired(stringBuilder, "min-fresh");
				stringBuilder.Append('=');
				stringBuilder.Append(((int)this.minFresh.Value.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo));
			}
			if (this.privateField)
			{
				CacheControlHeaderValue.AppendValueWithSeparatorIfRequired(stringBuilder, "private");
				if (this.privateHeaders != null && this.privateHeaders.Count > 0)
				{
					stringBuilder.Append("=\"");
					CacheControlHeaderValue.AppendValues(stringBuilder, this.privateHeaders);
					stringBuilder.Append('"');
				}
			}
			NameValueHeaderValue.ToString(this.extensions, ',', false, stringBuilder);
			return stringBuilder.ToString();
		}

		public override bool Equals(object obj)
		{
			CacheControlHeaderValue cacheControlHeaderValue = obj as CacheControlHeaderValue;
			return cacheControlHeaderValue != null && (this.noCache == cacheControlHeaderValue.noCache && this.noStore == cacheControlHeaderValue.noStore) && !(this.maxAge != cacheControlHeaderValue.maxAge) && (!(this.sharedMaxAge != cacheControlHeaderValue.sharedMaxAge) && this.maxStale == cacheControlHeaderValue.maxStale) && !(this.maxStaleLimit != cacheControlHeaderValue.maxStaleLimit) && !(this.minFresh != cacheControlHeaderValue.minFresh) && this.noTransform == cacheControlHeaderValue.noTransform && this.onlyIfCached == cacheControlHeaderValue.onlyIfCached && this.publicField == cacheControlHeaderValue.publicField && this.privateField == cacheControlHeaderValue.privateField && this.mustRevalidate == cacheControlHeaderValue.mustRevalidate && this.proxyRevalidate == cacheControlHeaderValue.proxyRevalidate && HeaderUtilities.AreEqualCollections<string>(this.noCacheHeaders, cacheControlHeaderValue.noCacheHeaders, StringComparer.OrdinalIgnoreCase) && HeaderUtilities.AreEqualCollections<string>(this.privateHeaders, cacheControlHeaderValue.privateHeaders, StringComparer.OrdinalIgnoreCase) && HeaderUtilities.AreEqualCollections<NameValueHeaderValue>(this.extensions, cacheControlHeaderValue.extensions);
		}

		public override int GetHashCode()
		{
			int num = this.noCache.GetHashCode() ^ this.noStore.GetHashCode() << 1 ^ this.maxStale.GetHashCode() << 2 ^ this.noTransform.GetHashCode() << 3 ^ this.onlyIfCached.GetHashCode() << 4 ^ this.publicField.GetHashCode() << 5 ^ this.privateField.GetHashCode() << 6 ^ this.mustRevalidate.GetHashCode() << 7 ^ this.proxyRevalidate.GetHashCode() << 8;
			num = (num ^ (this.maxAge.HasValue ? (this.maxAge.Value.GetHashCode() ^ 1) : 0) ^ (this.sharedMaxAge.HasValue ? (this.sharedMaxAge.Value.GetHashCode() ^ 2) : 0) ^ (this.maxStaleLimit.HasValue ? (this.maxStaleLimit.Value.GetHashCode() ^ 4) : 0) ^ (this.minFresh.HasValue ? (this.minFresh.Value.GetHashCode() ^ 8) : 0));
			if (this.noCacheHeaders != null && this.noCacheHeaders.Count > 0)
			{
				foreach (string current in this.noCacheHeaders)
				{
					num ^= current.ToLowerInvariant().GetHashCode();
				}
			}
			if (this.privateHeaders != null && this.privateHeaders.Count > 0)
			{
				foreach (string current2 in this.privateHeaders)
				{
					num ^= current2.ToLowerInvariant().GetHashCode();
				}
			}
			if (this.extensions != null && this.extensions.Count > 0)
			{
				foreach (NameValueHeaderValue current3 in this.extensions)
				{
					num ^= current3.GetHashCode();
				}
			}
			return num;
		}

		public static CacheControlHeaderValue Parse(string input)
		{
			int num = 0;
			return (CacheControlHeaderValue)CacheControlHeaderParser.Parser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out CacheControlHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (CacheControlHeaderParser.Parser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (CacheControlHeaderValue)obj;
				return true;
			}
			return false;
		}

		internal static int GetCacheControlLength(string input, int startIndex, CacheControlHeaderValue storeValue, out CacheControlHeaderValue parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
			{
				return 0;
			}
			int i = startIndex;
			object obj = null;
			List<NameValueHeaderValue> list = new List<NameValueHeaderValue>();
			while (i < input.Length)
			{
				if (!CacheControlHeaderValue.nameValueListParser.TryParseValue(input, null, ref i, out obj))
				{
					return 0;
				}
				list.Add(obj as NameValueHeaderValue);
			}
			CacheControlHeaderValue cacheControlHeaderValue = storeValue;
			if (cacheControlHeaderValue == null)
			{
				cacheControlHeaderValue = new CacheControlHeaderValue();
			}
			if (!CacheControlHeaderValue.TrySetCacheControlValues(cacheControlHeaderValue, list))
			{
				return 0;
			}
			if (storeValue == null)
			{
				parsedValue = cacheControlHeaderValue;
			}
			return input.Length - startIndex;
		}

		private static bool TrySetCacheControlValues(CacheControlHeaderValue cc, List<NameValueHeaderValue> nameValueList)
		{
			foreach (NameValueHeaderValue current in nameValueList)
			{
				bool flag = true;
				string text = current.Name.ToLowerInvariant();
				string key;
				if ((key = text) == null)
				{
					goto IL_20E;
				}
				if (<PrivateImplementationDetails>{346078E2-2A88-42E8-9CA7-E89D5ACFCC81}.$$method0x60001d3-1 == null)
				{
					<PrivateImplementationDetails>{346078E2-2A88-42E8-9CA7-E89D5ACFCC81}.$$method0x60001d3-1 = new Dictionary<string, int>(12)
					{
						{
							"no-cache",
							0
						},
						{
							"no-store",
							1
						},
						{
							"max-age",
							2
						},
						{
							"max-stale",
							3
						},
						{
							"min-fresh",
							4
						},
						{
							"no-transform",
							5
						},
						{
							"only-if-cached",
							6
						},
						{
							"public",
							7
						},
						{
							"private",
							8
						},
						{
							"must-revalidate",
							9
						},
						{
							"proxy-revalidate",
							10
						},
						{
							"s-maxage",
							11
						}
					};
				}
				int num;
				if (!<PrivateImplementationDetails>{346078E2-2A88-42E8-9CA7-E89D5ACFCC81}.$$method0x60001d3-1.TryGetValue(key, out num))
				{
					goto IL_20E;
				}
				switch (num)
				{
				case 0:
					flag = CacheControlHeaderValue.TrySetOptionalTokenList(current, ref cc.noCache, ref cc.noCacheHeaders);
					break;
				case 1:
					flag = CacheControlHeaderValue.TrySetTokenOnlyValue(current, ref cc.noStore);
					break;
				case 2:
					flag = CacheControlHeaderValue.TrySetTimeSpan(current, ref cc.maxAge);
					break;
				case 3:
					flag = (current.Value == null || CacheControlHeaderValue.TrySetTimeSpan(current, ref cc.maxStaleLimit));
					if (flag)
					{
						cc.maxStale = true;
					}
					break;
				case 4:
					flag = CacheControlHeaderValue.TrySetTimeSpan(current, ref cc.minFresh);
					break;
				case 5:
					flag = CacheControlHeaderValue.TrySetTokenOnlyValue(current, ref cc.noTransform);
					break;
				case 6:
					flag = CacheControlHeaderValue.TrySetTokenOnlyValue(current, ref cc.onlyIfCached);
					break;
				case 7:
					flag = CacheControlHeaderValue.TrySetTokenOnlyValue(current, ref cc.publicField);
					break;
				case 8:
					flag = CacheControlHeaderValue.TrySetOptionalTokenList(current, ref cc.privateField, ref cc.privateHeaders);
					break;
				case 9:
					flag = CacheControlHeaderValue.TrySetTokenOnlyValue(current, ref cc.mustRevalidate);
					break;
				case 10:
					flag = CacheControlHeaderValue.TrySetTokenOnlyValue(current, ref cc.proxyRevalidate);
					break;
				case 11:
					flag = CacheControlHeaderValue.TrySetTimeSpan(current, ref cc.sharedMaxAge);
					break;
				default:
					goto IL_20E;
				}
				IL_21A:
				if (!flag)
				{
					return false;
				}
				continue;
				IL_20E:
				cc.Extensions.Add(current);
				goto IL_21A;
			}
			return true;
		}

		private static bool TrySetTokenOnlyValue(NameValueHeaderValue nameValue, ref bool boolField)
		{
			if (nameValue.Value != null)
			{
				return false;
			}
			boolField = true;
			return true;
		}

		private static bool TrySetOptionalTokenList(NameValueHeaderValue nameValue, ref bool boolField, ref ICollection<string> destination)
		{
			if (nameValue.Value == null)
			{
				boolField = true;
				return true;
			}
			string value = nameValue.Value;
			if (value.Length < 3 || value[0] != '"' || value[value.Length - 1] != '"')
			{
				return false;
			}
			int i = 1;
			int num = value.Length - 1;
			bool flag = false;
			int num2 = (destination == null) ? 0 : destination.Count;
			while (i < num)
			{
				i = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(value, i, true, out flag);
				if (i == num)
				{
					break;
				}
				int tokenLength = HttpRuleParser.GetTokenLength(value, i);
				if (tokenLength == 0)
				{
					return false;
				}
				if (destination == null)
				{
					destination = new ObjectCollection<string>(CacheControlHeaderValue.checkIsValidToken);
				}
				destination.Add(value.Substring(i, tokenLength));
				i += tokenLength;
			}
			if (destination != null && destination.Count > num2)
			{
				boolField = true;
				return true;
			}
			return false;
		}

		private static bool TrySetTimeSpan(NameValueHeaderValue nameValue, ref TimeSpan? timeSpan)
		{
			if (nameValue.Value == null)
			{
				return false;
			}
			int seconds;
			if (!HeaderUtilities.TryParseInt32(nameValue.Value, out seconds))
			{
				return false;
			}
			timeSpan = new TimeSpan?(new TimeSpan(0, 0, seconds));
			return true;
		}

		private static void AppendValueIfRequired(StringBuilder sb, bool appendValue, string value)
		{
			if (appendValue)
			{
				CacheControlHeaderValue.AppendValueWithSeparatorIfRequired(sb, value);
			}
		}

		private static void AppendValueWithSeparatorIfRequired(StringBuilder sb, string value)
		{
			if (sb.Length > 0)
			{
				sb.Append(", ");
			}
			sb.Append(value);
		}

		private static void AppendValues(StringBuilder sb, IEnumerable<string> values)
		{
			bool flag = true;
			foreach (string current in values)
			{
				if (flag)
				{
					flag = false;
				}
				else
				{
					sb.Append(", ");
				}
				sb.Append(current);
			}
		}

		private static void CheckIsValidToken(string item)
		{
			HeaderUtilities.CheckValidToken(item, "item");
		}

		object ICloneable.Clone()
		{
			return new CacheControlHeaderValue(this);
		}
	}
}
