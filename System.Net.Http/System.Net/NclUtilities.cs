using System;
using System.Threading;

namespace System.Net
{
	internal static class NclUtilities
	{
		internal static bool IsFatal(Exception exception)
		{
			return exception != null && (exception is OutOfMemoryException || exception is StackOverflowException || exception is ThreadAbortException);
		}
	}
}
