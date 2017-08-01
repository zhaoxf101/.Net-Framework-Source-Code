using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Net.Http.Headers
{
	public sealed class HttpHeaderValueCollection<T> : ICollection<T>, IEnumerable<T>, IEnumerable where T : class
	{
		private string headerName;

		private HttpHeaders store;

		private T specialValue;

		private Action<HttpHeaderValueCollection<T>, T> validator;

		public int Count
		{
			get
			{
				return this.GetCount();
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		internal bool IsSpecialValueSet
		{
			get
			{
				return this.specialValue != null && this.store.ContainsParsedValue(this.headerName, this.specialValue);
			}
		}

		internal HttpHeaderValueCollection(string headerName, HttpHeaders store) : this(headerName, store, default(T), null)
		{
		}

		internal HttpHeaderValueCollection(string headerName, HttpHeaders store, Action<HttpHeaderValueCollection<T>, T> validator) : this(headerName, store, default(T), validator)
		{
		}

		internal HttpHeaderValueCollection(string headerName, HttpHeaders store, T specialValue) : this(headerName, store, specialValue, null)
		{
		}

		internal HttpHeaderValueCollection(string headerName, HttpHeaders store, T specialValue, Action<HttpHeaderValueCollection<T>, T> validator)
		{
			this.store = store;
			this.headerName = headerName;
			this.specialValue = specialValue;
			this.validator = validator;
		}

		public void Add(T item)
		{
			this.CheckValue(item);
			this.store.AddParsedValue(this.headerName, item);
		}

		public void ParseAdd(string input)
		{
			this.store.Add(this.headerName, input);
		}

		public bool TryParseAdd(string input)
		{
			return this.store.TryParseAndAddValue(this.headerName, input);
		}

		public void Clear()
		{
			this.store.Remove(this.headerName);
		}

		public bool Contains(T item)
		{
			this.CheckValue(item);
			return this.store.ContainsParsedValue(this.headerName, item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (arrayIndex < 0 || arrayIndex > array.Length)
			{
				throw new ArgumentOutOfRangeException("arrayIndex");
			}
			object parsedValues = this.store.GetParsedValues(this.headerName);
			if (parsedValues == null)
			{
				return;
			}
			List<object> list = parsedValues as List<object>;
			if (list != null)
			{
				list.CopyTo((object[])array, arrayIndex);
				return;
			}
			if (arrayIndex == array.Length)
			{
				throw new ArgumentException(SR.net_http_copyto_array_too_small);
			}
			array[arrayIndex] = (parsedValues as T);
		}

		public bool Remove(T item)
		{
			this.CheckValue(item);
			return this.store.RemoveParsedValue(this.headerName, item);
		}

		public IEnumerator<T> GetEnumerator()
		{
			object parsedValues = this.store.GetParsedValues(this.headerName);
			if (parsedValues != null)
			{
				List<object> list = parsedValues as List<object>;
				if (list == null)
				{
					yield return parsedValues as T;
				}
				else
				{
					foreach (object current in list)
					{
						yield return current as T;
					}
				}
			}
			yield break;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public override string ToString()
		{
			return this.store.GetHeaderString(this.headerName);
		}

		internal string GetHeaderStringWithoutSpecial()
		{
			if (!this.IsSpecialValueSet)
			{
				return this.ToString();
			}
			return this.store.GetHeaderString(this.headerName, this.specialValue);
		}

		internal void SetSpecialValue()
		{
			if (!this.store.ContainsParsedValue(this.headerName, this.specialValue))
			{
				this.store.AddParsedValue(this.headerName, this.specialValue);
			}
		}

		internal void RemoveSpecialValue()
		{
			this.store.RemoveParsedValue(this.headerName, this.specialValue);
		}

		private void CheckValue(T item)
		{
			if (item == null)
			{
				throw new ArgumentNullException("item");
			}
			if (this.validator != null)
			{
				this.validator(this, item);
			}
		}

		private int GetCount()
		{
			object parsedValues = this.store.GetParsedValues(this.headerName);
			if (parsedValues == null)
			{
				return 0;
			}
			List<object> list = parsedValues as List<object>;
			if (list == null)
			{
				return 1;
			}
			return list.Count;
		}
	}
}
