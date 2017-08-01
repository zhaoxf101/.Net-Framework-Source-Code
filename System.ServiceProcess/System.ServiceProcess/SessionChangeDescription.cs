using System;

namespace System.ServiceProcess
{
	public struct SessionChangeDescription
	{
		private SessionChangeReason _reason;

		private int _id;

		public SessionChangeReason Reason
		{
			get
			{
				return this._reason;
			}
		}

		public int SessionId
		{
			get
			{
				return this._id;
			}
		}

		internal SessionChangeDescription(SessionChangeReason reason, int id)
		{
			this._reason = reason;
			this._id = id;
		}

		public override bool Equals(object obj)
		{
			return obj != null && obj is SessionChangeDescription && this.Equals((SessionChangeDescription)obj);
		}

		public override int GetHashCode()
		{
			return (int)(this._reason ^ (SessionChangeReason)this._id);
		}

		public bool Equals(SessionChangeDescription changeDescription)
		{
			return this._reason == changeDescription._reason && this._id == changeDescription._id;
		}

		public static bool operator ==(SessionChangeDescription a, SessionChangeDescription b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(SessionChangeDescription a, SessionChangeDescription b)
		{
			return !a.Equals(b);
		}
	}
}
