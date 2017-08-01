using System;

namespace System.Net.Http.Headers
{
	public sealed class MediaTypeWithQualityHeaderValue : MediaTypeHeaderValue, ICloneable
	{
		public double? Quality
		{
			get
			{
				return HeaderUtilities.GetQuality(base.Parameters);
			}
			set
			{
				HeaderUtilities.SetQuality(base.Parameters, value);
			}
		}

		internal MediaTypeWithQualityHeaderValue()
		{
		}

		public MediaTypeWithQualityHeaderValue(string mediaType) : base(mediaType)
		{
		}

		public MediaTypeWithQualityHeaderValue(string mediaType, double quality) : base(mediaType)
		{
			this.Quality = new double?(quality);
		}

		private MediaTypeWithQualityHeaderValue(MediaTypeWithQualityHeaderValue source) : base(source)
		{
		}

		object ICloneable.Clone()
		{
			return new MediaTypeWithQualityHeaderValue(this);
		}

		public new static MediaTypeWithQualityHeaderValue Parse(string input)
		{
			int num = 0;
			return (MediaTypeWithQualityHeaderValue)MediaTypeHeaderParser.SingleValueWithQualityParser.ParseValue(input, null, ref num);
		}

		public static bool TryParse(string input, out MediaTypeWithQualityHeaderValue parsedValue)
		{
			int num = 0;
			parsedValue = null;
			object obj;
			if (MediaTypeHeaderParser.SingleValueWithQualityParser.TryParseValue(input, null, ref num, out obj))
			{
				parsedValue = (MediaTypeWithQualityHeaderValue)obj;
				return true;
			}
			return false;
		}
	}
}
