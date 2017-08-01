using System;

namespace System.Net.Http.Headers
{
	public class RangeConditionHeaderValue : ICloneable
	{
		private DateTimeOffset? date;

		private EntityTagHeaderValue entityTag;

		public DateTimeOffset? Date
		{
			get
			{
				return this.date;
			}
		}

		public EntityTagHeaderValue EntityTag
		{
			get
			{
				return this.entityTag;
			}
		}

		public RangeConditionHeaderValue(DateTimeOffset date)
		{
			this.date = new DateTimeOffset?(date);
		}

		public RangeConditionHeaderValue(EntityTagHeaderValue entityTag)
		{
			if (entityTag == null)
			{
				throw new ArgumentNullException("entityTag");
			}
			this.entityTag = entityTag;
		}

		public RangeConditionHeaderValue(string entityTag) : this(new EntityTagHeaderValue(entityTag))
		{
		}

		private RangeConditionHeaderValue(RangeConditionHeaderValue source)
		{
			this.entityTag = source.entityTag;
			this.date = source.date;
		}

		private RangeConditionHeaderValue()
		{
		}

		public override string ToString()
		{
			if (this.entityTag == null)
			{
				return HttpRuleParser.DateToString(this.date.Value);
			}
			return this.entityTag.ToString();
		}

		public override bool Equals(object obj)
		{
			RangeConditionHeaderValue rangeConditionHeaderValue = obj as RangeConditionHeaderValue;
			if (rangeConditionHeaderValue == null)
			{
				return false;
			}
			if (this.entityTag == null)
			{
				return rangeConditionHeaderValue.date.HasValue && this.date.Value == rangeConditionHeaderValue.date.Value;
			}
			return this.entityTag.Equals(rangeConditionHeaderValue.entityTag);
		}

		public override int GetHashCode()
		{
			if (this.entityTag == null)
			{
				return this.date.Value.GetHashCode();
			}
			return this.entityTag.GetHashCode();
		}

		public static RangeConditionHeaderValue Parse(string input)
		{
			int num = 0;
			return (RangeConditionHeaderValue)GenericHeaderParser.RangeConditionParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out RangeConditionHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (GenericHeaderParser.RangeConditionParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (RangeConditionHeaderValue)obj;
				return true;
			}
			return false;
		}

		internal static int GetRangeConditionLength(string input, int startIndex, out object parsedValue)
		{
			parsedValue = null;
			if (string.IsNullOrEmpty(input) || startIndex + 1 >= input.Length)
			{
				return 0;
			}
			DateTimeOffset minValue = DateTimeOffset.MinValue;
			EntityTagHeaderValue entityTagHeaderValue = null;
			char c = input[startIndex];
			char c2 = input[startIndex + 1];
			int num;
			if (c == '"' || ((c == 'w' || c == 'W') && c2 == '/'))
			{
				int entityTagLength = EntityTagHeaderValue.GetEntityTagLength(input, startIndex, out entityTagHeaderValue);
				if (entityTagLength == 0)
				{
					return 0;
				}
				num = startIndex + entityTagLength;
				if (num != input.Length)
				{
					return 0;
				}
			}
			else
			{
				if (!HttpRuleParser.TryStringToDate(input.Substring(startIndex), out minValue))
				{
					return 0;
				}
				num = input.Length;
			}
			RangeConditionHeaderValue rangeConditionHeaderValue = new RangeConditionHeaderValue();
			if (entityTagHeaderValue == null)
			{
				rangeConditionHeaderValue.date = new DateTimeOffset?(minValue);
			}
			else
			{
				rangeConditionHeaderValue.entityTag = entityTagHeaderValue;
			}
			parsedValue = rangeConditionHeaderValue;
			return num - startIndex;
		}

		object ICloneable.Clone()
		{
			return new RangeConditionHeaderValue(this);
		}
	}
}
