using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers
{
	public abstract class HttpHeaders : IEnumerable<KeyValuePair<string, IEnumerable<string>>>, IEnumerable
	{
		private enum StoreLocation
		{
			Raw,
			Invalid,
			Parsed
		}

		private class HeaderStoreItemInfo
		{
			private object rawValue;

			private object invalidValue;

			private object parsedValue;

			private HttpHeaderParser parser;

			internal object RawValue
			{
				get
				{
					return this.rawValue;
				}
				set
				{
					this.rawValue = value;
				}
			}

			internal object InvalidValue
			{
				get
				{
					return this.invalidValue;
				}
				set
				{
					this.invalidValue = value;
				}
			}

			internal object ParsedValue
			{
				get
				{
					return this.parsedValue;
				}
				set
				{
					this.parsedValue = value;
				}
			}

			internal HttpHeaderParser Parser
			{
				get
				{
					return this.parser;
				}
			}

			internal bool CanAddValue
			{
				get
				{
					return this.parser.SupportsMultipleValues || (this.invalidValue == null && this.parsedValue == null);
				}
			}

			internal bool IsEmpty
			{
				get
				{
					return this.rawValue == null && this.invalidValue == null && this.parsedValue == null;
				}
			}

			internal HeaderStoreItemInfo(HttpHeaderParser parser)
			{
				this.parser = parser;
			}
		}

		private Dictionary<string, HttpHeaders.HeaderStoreItemInfo> headerStore;

		private Dictionary<string, HttpHeaderParser> parserStore;

		private HashSet<string> invalidHeaders;

		public void Add(string name, string value)
		{
			this.CheckHeaderName(name);
			HttpHeaders.HeaderStoreItemInfo headerStoreItemInfo;
			bool flag;
			this.PrepareHeaderInfoForAdd(name, out headerStoreItemInfo, out flag);
			this.ParseAndAddValue(name, headerStoreItemInfo, value);
			if (flag && headerStoreItemInfo.ParsedValue != null)
			{
				this.AddHeaderToStore(name, headerStoreItemInfo);
			}
		}

		public void Add(string name, IEnumerable<string> values)
		{
			if (values == null)
			{
				throw new ArgumentNullException("values");
			}
			this.CheckHeaderName(name);
			HttpHeaders.HeaderStoreItemInfo headerStoreItemInfo;
			bool flag;
			this.PrepareHeaderInfoForAdd(name, out headerStoreItemInfo, out flag);
			try
			{
				foreach (string current in values)
				{
					this.ParseAndAddValue(name, headerStoreItemInfo, current);
				}
			}
			finally
			{
				if (flag && headerStoreItemInfo.ParsedValue != null)
				{
					this.AddHeaderToStore(name, headerStoreItemInfo);
				}
			}
		}

		public bool TryAddWithoutValidation(string name, string value)
		{
			if (!this.TryCheckHeaderName(name))
			{
				return false;
			}
			if (value == null)
			{
				value = string.Empty;
			}
			HttpHeaders.HeaderStoreItemInfo orCreateHeaderInfo = this.GetOrCreateHeaderInfo(name, false);
			HttpHeaders.AddValue(orCreateHeaderInfo, value, HttpHeaders.StoreLocation.Raw);
			return true;
		}

		public bool TryAddWithoutValidation(string name, IEnumerable<string> values)
		{
			if (values == null)
			{
				throw new ArgumentNullException("values");
			}
			if (!this.TryCheckHeaderName(name))
			{
				return false;
			}
			HttpHeaders.HeaderStoreItemInfo orCreateHeaderInfo = this.GetOrCreateHeaderInfo(name, false);
			foreach (string current in values)
			{
				HttpHeaders.AddValue(orCreateHeaderInfo, current ?? string.Empty, HttpHeaders.StoreLocation.Raw);
			}
			return true;
		}

		public void Clear()
		{
			if (this.headerStore != null)
			{
				this.headerStore.Clear();
			}
		}

		public bool Remove(string name)
		{
			this.CheckHeaderName(name);
			return this.headerStore != null && this.headerStore.Remove(name);
		}

		public IEnumerable<string> GetValues(string name)
		{
			this.CheckHeaderName(name);
			IEnumerable<string> result;
			if (!this.TryGetValues(name, out result))
			{
				throw new InvalidOperationException(SR.net_http_headers_not_found);
			}
			return result;
		}

		public bool TryGetValues(string name, out IEnumerable<string> values)
		{
			if (!this.TryCheckHeaderName(name))
			{
				values = null;
				return false;
			}
			if (this.headerStore == null)
			{
				values = null;
				return false;
			}
			HttpHeaders.HeaderStoreItemInfo info = null;
			if (this.TryGetAndParseHeaderInfo(name, out info))
			{
				values = HttpHeaders.GetValuesAsStrings(info);
				return true;
			}
			values = null;
			return false;
		}

		public bool Contains(string name)
		{
			this.CheckHeaderName(name);
			if (this.headerStore == null)
			{
				return false;
			}
			HttpHeaders.HeaderStoreItemInfo headerStoreItemInfo = null;
			return this.TryGetAndParseHeaderInfo(name, out headerStoreItemInfo);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<string, IEnumerable<string>> current in this)
			{
				stringBuilder.Append(current.Key);
				stringBuilder.Append(": ");
				stringBuilder.Append(this.GetHeaderString(current.Key));
				stringBuilder.Append("\r\n");
			}
			return stringBuilder.ToString();
		}

		internal IEnumerable<KeyValuePair<string, string>> GetHeaderStrings()
		{
			if (this.headerStore != null)
			{
				foreach (KeyValuePair<string, HttpHeaders.HeaderStoreItemInfo> current in this.headerStore)
				{
					KeyValuePair<string, HttpHeaders.HeaderStoreItemInfo> keyValuePair = current;
					HttpHeaders.HeaderStoreItemInfo value = keyValuePair.Value;
					string headerString = this.GetHeaderString(value);
					KeyValuePair<string, HttpHeaders.HeaderStoreItemInfo> keyValuePair2 = current;
					yield return new KeyValuePair<string, string>(keyValuePair2.Key, headerString);
				}
			}
			yield break;
		}

		internal string GetHeaderString(string headerName)
		{
			return this.GetHeaderString(headerName, null);
		}

		internal string GetHeaderString(string headerName, object exclude)
		{
			HttpHeaders.HeaderStoreItemInfo info;
			if (!this.TryGetHeaderInfo(headerName, out info))
			{
				return string.Empty;
			}
			return this.GetHeaderString(info, exclude);
		}

		private string GetHeaderString(HttpHeaders.HeaderStoreItemInfo info)
		{
			return this.GetHeaderString(info, null);
		}

		private string GetHeaderString(HttpHeaders.HeaderStoreItemInfo info, object exclude)
		{
			string result = string.Empty;
			string[] valuesAsStrings = HttpHeaders.GetValuesAsStrings(info, exclude);
			if (valuesAsStrings.Length == 1)
			{
				result = valuesAsStrings[0];
			}
			else
			{
				string separator = ", ";
				if (info.Parser != null && info.Parser.SupportsMultipleValues)
				{
					separator = info.Parser.Separator;
				}
				result = string.Join(separator, valuesAsStrings);
			}
			return result;
		}

		public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator()
		{
			if (this.headerStore != null)
			{
				List<string> list = null;
				foreach (KeyValuePair<string, HttpHeaders.HeaderStoreItemInfo> current in this.headerStore)
				{
					KeyValuePair<string, HttpHeaders.HeaderStoreItemInfo> keyValuePair = current;
					HttpHeaders.HeaderStoreItemInfo value = keyValuePair.Value;
					KeyValuePair<string, HttpHeaders.HeaderStoreItemInfo> keyValuePair2 = current;
					if (!this.ParseRawHeaderValues(keyValuePair2.Key, value, false))
					{
						if (list == null)
						{
							list = new List<string>();
						}
						List<string> arg_CE_0 = list;
						KeyValuePair<string, HttpHeaders.HeaderStoreItemInfo> keyValuePair3 = current;
						arg_CE_0.Add(keyValuePair3.Key);
					}
					else
					{
						string[] valuesAsStrings = HttpHeaders.GetValuesAsStrings(value);
						KeyValuePair<string, HttpHeaders.HeaderStoreItemInfo> keyValuePair4 = current;
						yield return new KeyValuePair<string, IEnumerable<string>>(keyValuePair4.Key, valuesAsStrings);
					}
				}
				if (list != null)
				{
					foreach (string current2 in list)
					{
						this.headerStore.Remove(current2);
					}
				}
			}
			yield break;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		internal void SetConfiguration(Dictionary<string, HttpHeaderParser> parserStore, HashSet<string> invalidHeaders)
		{
			this.parserStore = parserStore;
			this.invalidHeaders = invalidHeaders;
		}

		internal void AddParsedValue(string name, object value)
		{
			HttpHeaders.HeaderStoreItemInfo orCreateHeaderInfo = this.GetOrCreateHeaderInfo(name, true);
			HttpHeaders.AddValue(orCreateHeaderInfo, value, HttpHeaders.StoreLocation.Parsed);
		}

		internal void SetParsedValue(string name, object value)
		{
			HttpHeaders.HeaderStoreItemInfo orCreateHeaderInfo = this.GetOrCreateHeaderInfo(name, true);
			orCreateHeaderInfo.InvalidValue = null;
			orCreateHeaderInfo.ParsedValue = null;
			orCreateHeaderInfo.RawValue = null;
			HttpHeaders.AddValue(orCreateHeaderInfo, value, HttpHeaders.StoreLocation.Parsed);
		}

		internal void SetOrRemoveParsedValue(string name, object value)
		{
			if (value == null)
			{
				this.Remove(name);
				return;
			}
			this.SetParsedValue(name, value);
		}

		internal bool RemoveParsedValue(string name, object value)
		{
			if (this.headerStore == null)
			{
				return false;
			}
			HttpHeaders.HeaderStoreItemInfo headerStoreItemInfo = null;
			if (!this.TryGetAndParseHeaderInfo(name, out headerStoreItemInfo))
			{
				return false;
			}
			bool result = false;
			if (headerStoreItemInfo.ParsedValue == null)
			{
				return false;
			}
			IEqualityComparer comparer = headerStoreItemInfo.Parser.Comparer;
			List<object> list = headerStoreItemInfo.ParsedValue as List<object>;
			if (list == null)
			{
				if (this.AreEqual(value, headerStoreItemInfo.ParsedValue, comparer))
				{
					headerStoreItemInfo.ParsedValue = null;
					result = true;
				}
			}
			else
			{
				foreach (object current in list)
				{
					if (this.AreEqual(value, current, comparer))
					{
						result = list.Remove(current);
						break;
					}
				}
				if (list.Count == 0)
				{
					headerStoreItemInfo.ParsedValue = null;
				}
			}
			if (headerStoreItemInfo.IsEmpty)
			{
				this.Remove(name);
			}
			return result;
		}

		internal bool ContainsParsedValue(string name, object value)
		{
			if (this.headerStore == null)
			{
				return false;
			}
			HttpHeaders.HeaderStoreItemInfo headerStoreItemInfo = null;
			if (!this.TryGetAndParseHeaderInfo(name, out headerStoreItemInfo))
			{
				return false;
			}
			if (headerStoreItemInfo.ParsedValue == null)
			{
				return false;
			}
			List<object> list = headerStoreItemInfo.ParsedValue as List<object>;
			IEqualityComparer comparer = headerStoreItemInfo.Parser.Comparer;
			if (list == null)
			{
				return this.AreEqual(value, headerStoreItemInfo.ParsedValue, comparer);
			}
			foreach (object current in list)
			{
				if (this.AreEqual(value, current, comparer))
				{
					return true;
				}
			}
			return false;
		}

		internal virtual void AddHeaders(HttpHeaders sourceHeaders)
		{
			if (sourceHeaders.headerStore == null)
			{
				return;
			}
			List<string> list = null;
			foreach (KeyValuePair<string, HttpHeaders.HeaderStoreItemInfo> current in sourceHeaders.headerStore)
			{
				if (this.headerStore == null || !this.headerStore.ContainsKey(current.Key))
				{
					HttpHeaders.HeaderStoreItemInfo value = current.Value;
					if (!sourceHeaders.ParseRawHeaderValues(current.Key, value, false))
					{
						if (list == null)
						{
							list = new List<string>();
						}
						list.Add(current.Key);
					}
					else
					{
						this.AddHeaderInfo(current.Key, value);
					}
				}
			}
			if (list != null)
			{
				foreach (string current2 in list)
				{
					sourceHeaders.headerStore.Remove(current2);
				}
			}
		}

		private void AddHeaderInfo(string headerName, HttpHeaders.HeaderStoreItemInfo sourceInfo)
		{
			HttpHeaders.HeaderStoreItemInfo headerStoreItemInfo = this.CreateAndAddHeaderToStore(headerName);
			if (headerStoreItemInfo.Parser == null)
			{
				headerStoreItemInfo.ParsedValue = HttpHeaders.CloneStringHeaderInfoValues(sourceInfo.ParsedValue);
				return;
			}
			headerStoreItemInfo.InvalidValue = HttpHeaders.CloneStringHeaderInfoValues(sourceInfo.InvalidValue);
			if (sourceInfo.ParsedValue != null)
			{
				List<object> list = sourceInfo.ParsedValue as List<object>;
				if (list == null)
				{
					HttpHeaders.CloneAndAddValue(headerStoreItemInfo, sourceInfo.ParsedValue);
					return;
				}
				foreach (object current in list)
				{
					HttpHeaders.CloneAndAddValue(headerStoreItemInfo, current);
				}
			}
		}

		private static void CloneAndAddValue(HttpHeaders.HeaderStoreItemInfo destinationInfo, object source)
		{
			ICloneable cloneable = source as ICloneable;
			if (cloneable != null)
			{
				HttpHeaders.AddValue(destinationInfo, cloneable.Clone(), HttpHeaders.StoreLocation.Parsed);
				return;
			}
			HttpHeaders.AddValue(destinationInfo, source, HttpHeaders.StoreLocation.Parsed);
		}

		private static object CloneStringHeaderInfoValues(object source)
		{
			if (source == null)
			{
				return null;
			}
			List<object> list = source as List<object>;
			if (list == null)
			{
				return source;
			}
			return new List<object>(list);
		}

		private HttpHeaders.HeaderStoreItemInfo GetOrCreateHeaderInfo(string name, bool parseRawValues)
		{
			HttpHeaders.HeaderStoreItemInfo result = null;
			bool flag;
			if (parseRawValues)
			{
				flag = this.TryGetAndParseHeaderInfo(name, out result);
			}
			else
			{
				flag = this.TryGetHeaderInfo(name, out result);
			}
			if (!flag)
			{
				result = this.CreateAndAddHeaderToStore(name);
			}
			return result;
		}

		private HttpHeaders.HeaderStoreItemInfo CreateAndAddHeaderToStore(string name)
		{
			HttpHeaders.HeaderStoreItemInfo headerStoreItemInfo = new HttpHeaders.HeaderStoreItemInfo(this.GetParser(name));
			this.AddHeaderToStore(name, headerStoreItemInfo);
			return headerStoreItemInfo;
		}

		private void AddHeaderToStore(string name, HttpHeaders.HeaderStoreItemInfo info)
		{
			if (this.headerStore == null)
			{
				this.headerStore = new Dictionary<string, HttpHeaders.HeaderStoreItemInfo>(StringComparer.OrdinalIgnoreCase);
			}
			this.headerStore.Add(name, info);
		}

		private bool TryGetHeaderInfo(string name, out HttpHeaders.HeaderStoreItemInfo info)
		{
			if (this.headerStore == null)
			{
				info = null;
				return false;
			}
			return this.headerStore.TryGetValue(name, out info);
		}

		private bool TryGetAndParseHeaderInfo(string name, out HttpHeaders.HeaderStoreItemInfo info)
		{
			return this.TryGetHeaderInfo(name, out info) && this.ParseRawHeaderValues(name, info, true);
		}

		private bool ParseRawHeaderValues(string name, HttpHeaders.HeaderStoreItemInfo info, bool removeEmptyHeader)
		{
			lock (info)
			{
				if (info.RawValue != null)
				{
					List<string> list = info.RawValue as List<string>;
					if (list == null)
					{
						HttpHeaders.ParseSingleRawHeaderValue(name, info);
					}
					else
					{
						HttpHeaders.ParseMultipleRawHeaderValues(name, info, list);
					}
					info.RawValue = null;
					if (info.InvalidValue == null && info.ParsedValue == null)
					{
						if (removeEmptyHeader)
						{
							this.headerStore.Remove(name);
						}
						return false;
					}
				}
			}
			return true;
		}

		private static void ParseMultipleRawHeaderValues(string name, HttpHeaders.HeaderStoreItemInfo info, List<string> rawValues)
		{
			if (info.Parser == null)
			{
				using (List<string>.Enumerator enumerator = rawValues.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						string current = enumerator.Current;
						if (!HttpHeaders.ContainsInvalidNewLine(current, name))
						{
							HttpHeaders.AddValue(info, current, HttpHeaders.StoreLocation.Parsed);
						}
					}
					return;
				}
			}
			foreach (string current2 in rawValues)
			{
				if (!HttpHeaders.TryParseAndAddRawHeaderValue(name, info, current2, true) && Logging.On)
				{
					Logging.PrintWarning(Logging.Http, string.Format(CultureInfo.InvariantCulture, SR.net_http_log_headers_invalid_value, new object[]
					{
						name,
						current2
					}));
				}
			}
		}

		private static void ParseSingleRawHeaderValue(string name, HttpHeaders.HeaderStoreItemInfo info)
		{
			string text = info.RawValue as string;
			if (info.Parser == null)
			{
				if (!HttpHeaders.ContainsInvalidNewLine(text, name))
				{
					info.ParsedValue = info.RawValue;
					return;
				}
			}
			else if (!HttpHeaders.TryParseAndAddRawHeaderValue(name, info, text, true) && Logging.On)
			{
				Logging.PrintWarning(Logging.Http, string.Format(CultureInfo.InvariantCulture, SR.net_http_log_headers_invalid_value, new object[]
				{
					name,
					text
				}));
			}
		}

		internal bool TryParseAndAddValue(string name, string value)
		{
			HttpHeaders.HeaderStoreItemInfo headerStoreItemInfo;
			bool flag;
			this.PrepareHeaderInfoForAdd(name, out headerStoreItemInfo, out flag);
			bool flag2 = HttpHeaders.TryParseAndAddRawHeaderValue(name, headerStoreItemInfo, value, false);
			if (flag2 && flag && headerStoreItemInfo.ParsedValue != null)
			{
				this.AddHeaderToStore(name, headerStoreItemInfo);
			}
			return flag2;
		}

		private static bool TryParseAndAddRawHeaderValue(string name, HttpHeaders.HeaderStoreItemInfo info, string value, bool addWhenInvalid)
		{
			if (!info.CanAddValue)
			{
				if (addWhenInvalid)
				{
					HttpHeaders.AddValue(info, value ?? string.Empty, HttpHeaders.StoreLocation.Invalid);
				}
				return false;
			}
			int i = 0;
			object obj = null;
			if (!info.Parser.TryParseValue(value, info.ParsedValue, ref i, out obj))
			{
				if (!HttpHeaders.ContainsInvalidNewLine(value, name) && addWhenInvalid)
				{
					HttpHeaders.AddValue(info, value ?? string.Empty, HttpHeaders.StoreLocation.Invalid);
				}
				return false;
			}
			if (value == null || i == value.Length)
			{
				if (obj != null)
				{
					HttpHeaders.AddValue(info, obj, HttpHeaders.StoreLocation.Parsed);
				}
				return true;
			}
			List<object> list = new List<object>();
			if (obj != null)
			{
				list.Add(obj);
			}
			while (i < value.Length)
			{
				if (!info.Parser.TryParseValue(value, info.ParsedValue, ref i, out obj))
				{
					if (!HttpHeaders.ContainsInvalidNewLine(value, name) && addWhenInvalid)
					{
						HttpHeaders.AddValue(info, value, HttpHeaders.StoreLocation.Invalid);
					}
					return false;
				}
				if (obj != null)
				{
					list.Add(obj);
				}
			}
			foreach (object current in list)
			{
				HttpHeaders.AddValue(info, current, HttpHeaders.StoreLocation.Parsed);
			}
			return true;
		}

		private static void AddValue(HttpHeaders.HeaderStoreItemInfo info, object value, HttpHeaders.StoreLocation location)
		{
			object obj = null;
			switch (location)
			{
			case HttpHeaders.StoreLocation.Raw:
				obj = info.RawValue;
				HttpHeaders.AddValueToStoreValue<string>(info, value, ref obj);
				info.RawValue = obj;
				return;
			case HttpHeaders.StoreLocation.Invalid:
				obj = info.InvalidValue;
				HttpHeaders.AddValueToStoreValue<string>(info, value, ref obj);
				info.InvalidValue = obj;
				return;
			case HttpHeaders.StoreLocation.Parsed:
				obj = info.ParsedValue;
				HttpHeaders.AddValueToStoreValue<object>(info, value, ref obj);
				info.ParsedValue = obj;
				return;
			default:
				return;
			}
		}

		private static void AddValueToStoreValue<T>(HttpHeaders.HeaderStoreItemInfo info, object value, ref object currentStoreValue) where T : class
		{
			if (currentStoreValue == null)
			{
				currentStoreValue = value;
				return;
			}
			List<T> list = currentStoreValue as List<T>;
			if (list == null)
			{
				list = new List<T>(2);
				list.Add(currentStoreValue as T);
				currentStoreValue = list;
			}
			list.Add(value as T);
		}

		internal object GetParsedValues(string name)
		{
			HttpHeaders.HeaderStoreItemInfo headerStoreItemInfo = null;
			if (!this.TryGetAndParseHeaderInfo(name, out headerStoreItemInfo))
			{
				return null;
			}
			return headerStoreItemInfo.ParsedValue;
		}

		private void PrepareHeaderInfoForAdd(string name, out HttpHeaders.HeaderStoreItemInfo info, out bool addToStore)
		{
			info = null;
			addToStore = false;
			if (!this.TryGetAndParseHeaderInfo(name, out info))
			{
				info = new HttpHeaders.HeaderStoreItemInfo(this.GetParser(name));
				addToStore = true;
			}
		}

		private void ParseAndAddValue(string name, HttpHeaders.HeaderStoreItemInfo info, string value)
		{
			if (info.Parser == null)
			{
				HttpHeaders.CheckInvalidNewLine(value);
				HttpHeaders.AddValue(info, value ?? string.Empty, HttpHeaders.StoreLocation.Parsed);
				return;
			}
			if (!info.CanAddValue)
			{
				throw new FormatException(string.Format(CultureInfo.InvariantCulture, SR.net_http_headers_single_value_header, new object[]
				{
					name
				}));
			}
			int i = 0;
			object obj = info.Parser.ParseValue(value, info.ParsedValue, ref i);
			if (value == null || i == value.Length)
			{
				if (obj != null)
				{
					HttpHeaders.AddValue(info, obj, HttpHeaders.StoreLocation.Parsed);
				}
				return;
			}
			List<object> list = new List<object>();
			if (obj != null)
			{
				list.Add(obj);
			}
			while (i < value.Length)
			{
				obj = info.Parser.ParseValue(value, info.ParsedValue, ref i);
				if (obj != null)
				{
					list.Add(obj);
				}
			}
			foreach (object current in list)
			{
				HttpHeaders.AddValue(info, current, HttpHeaders.StoreLocation.Parsed);
			}
		}

		private HttpHeaderParser GetParser(string name)
		{
			if (this.parserStore == null)
			{
				return null;
			}
			HttpHeaderParser result = null;
			if (this.parserStore.TryGetValue(name, out result))
			{
				return result;
			}
			return null;
		}

		private void CheckHeaderName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException(SR.net_http_argument_empty_string, "name");
			}
			if (HttpRuleParser.GetTokenLength(name, 0) != name.Length)
			{
				throw new FormatException(SR.net_http_headers_invalid_header_name);
			}
			if (this.invalidHeaders != null && this.invalidHeaders.Contains(name))
			{
				throw new InvalidOperationException(SR.net_http_headers_not_allowed_header_name);
			}
		}

		private bool TryCheckHeaderName(string name)
		{
			return !string.IsNullOrEmpty(name) && HttpRuleParser.GetTokenLength(name, 0) == name.Length && (this.invalidHeaders == null || !this.invalidHeaders.Contains(name));
		}

		private static void CheckInvalidNewLine(string value)
		{
			if (value == null)
			{
				return;
			}
			if (HttpRuleParser.ContainsInvalidNewLine(value))
			{
				throw new FormatException(SR.net_http_headers_no_newlines);
			}
		}

		private static bool ContainsInvalidNewLine(string value, string name)
		{
			if (HttpRuleParser.ContainsInvalidNewLine(value))
			{
				if (Logging.On)
				{
					Logging.PrintError(Logging.Http, string.Format(CultureInfo.InvariantCulture, SR.net_http_log_headers_no_newlines, new object[]
					{
						name,
						value
					}));
				}
				return true;
			}
			return false;
		}

		private static string[] GetValuesAsStrings(HttpHeaders.HeaderStoreItemInfo info)
		{
			return HttpHeaders.GetValuesAsStrings(info, null);
		}

		private static string[] GetValuesAsStrings(HttpHeaders.HeaderStoreItemInfo info, object exclude)
		{
			int valueCount = HttpHeaders.GetValueCount(info);
			string[] array = new string[valueCount];
			if (valueCount > 0)
			{
				int num = 0;
				HttpHeaders.ReadStoreValues<string>(array, info.RawValue, null, null, ref num);
				HttpHeaders.ReadStoreValues<object>(array, info.ParsedValue, info.Parser, exclude, ref num);
				HttpHeaders.ReadStoreValues<string>(array, info.InvalidValue, null, null, ref num);
				if (num < valueCount)
				{
					string[] array2 = new string[num];
					Array.Copy(array, array2, num);
					array = array2;
				}
			}
			return array;
		}

		private static int GetValueCount(HttpHeaders.HeaderStoreItemInfo info)
		{
			int result = 0;
			HttpHeaders.UpdateValueCount<string>(info.RawValue, ref result);
			HttpHeaders.UpdateValueCount<string>(info.InvalidValue, ref result);
			HttpHeaders.UpdateValueCount<object>(info.ParsedValue, ref result);
			return result;
		}

		private static void UpdateValueCount<T>(object valueStore, ref int valueCount)
		{
			if (valueStore == null)
			{
				return;
			}
			List<T> list = valueStore as List<T>;
			if (list != null)
			{
				valueCount += list.Count;
				return;
			}
			valueCount++;
		}

		private static void ReadStoreValues<T>(string[] values, object storeValue, HttpHeaderParser parser, T exclude, ref int currentIndex)
		{
			if (storeValue != null)
			{
				List<T> list = storeValue as List<T>;
				if (list == null)
				{
					if (HttpHeaders.ShouldAdd<T>(storeValue, parser, exclude))
					{
						values[currentIndex] = ((parser == null) ? storeValue.ToString() : parser.ToString(storeValue));
						currentIndex++;
						return;
					}
				}
				else
				{
					foreach (object obj in list)
					{
						if (HttpHeaders.ShouldAdd<T>(obj, parser, exclude))
						{
							values[currentIndex] = ((parser == null) ? obj.ToString() : parser.ToString(obj));
							currentIndex++;
						}
					}
				}
			}
		}

		private static bool ShouldAdd<T>(object storeValue, HttpHeaderParser parser, T exclude)
		{
			bool result = true;
			if (parser != null && exclude != null)
			{
				if (parser.Comparer != null)
				{
					result = !parser.Comparer.Equals(exclude, storeValue);
				}
				else
				{
					result = !exclude.Equals(storeValue);
				}
			}
			return result;
		}

		private bool AreEqual(object value, object storeValue, IEqualityComparer comparer)
		{
			if (comparer != null)
			{
				return comparer.Equals(value, storeValue);
			}
			return value.Equals(storeValue);
		}
	}
}
