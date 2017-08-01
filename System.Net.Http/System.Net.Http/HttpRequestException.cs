using System;
using System.Runtime.Serialization;

namespace System.Net.Http
{
	[Serializable]
	public class HttpRequestException : Exception
	{
		[Serializable]
		private class EmptyState : ISafeSerializationData
		{
			public void CompleteDeserialization(object deserialized)
			{
				HttpRequestException ex = (HttpRequestException)deserialized;
				ex.SerializeObjectState += HttpRequestException.handleSerialization;
			}
		}

		private static readonly EventHandler<SafeSerializationEventArgs> handleSerialization = new EventHandler<SafeSerializationEventArgs>(HttpRequestException.HandleSerialization);

		public HttpRequestException() : this(null, null)
		{
		}

		public HttpRequestException(string message) : this(message, null)
		{
		}

		public HttpRequestException(string message, Exception inner) : base(message, inner)
		{
			base.SerializeObjectState += HttpRequestException.handleSerialization;
		}

		private static void HandleSerialization(object exception, SafeSerializationEventArgs eventArgs)
		{
			eventArgs.AddSerializedState(new HttpRequestException.EmptyState());
		}
	}
}
