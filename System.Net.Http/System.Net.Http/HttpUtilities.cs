using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
	internal static class HttpUtilities
	{
		internal static readonly Version DefaultVersion = HttpVersion.Version11;

		internal static readonly byte[] EmptyByteArray = new byte[0];

		internal static bool IsHttpUri(Uri uri)
		{
			string scheme = uri.Scheme;
			return string.Compare("http", scheme, StringComparison.OrdinalIgnoreCase) == 0 || string.Compare("https", scheme, StringComparison.OrdinalIgnoreCase) == 0;
		}

		internal static bool HandleFaultsAndCancelation<T>(Task task, TaskCompletionSource<T> tcs)
		{
			if (task.IsFaulted)
			{
				tcs.TrySetException(task.Exception.GetBaseException());
				return true;
			}
			if (task.IsCanceled)
			{
				tcs.TrySetCanceled();
				return true;
			}
			return false;
		}

		internal static Task ContinueWithStandard(this Task task, Action<Task> continuation)
		{
			return task.ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}

		internal static Task ContinueWithStandard<T>(this Task<T> task, Action<Task<T>> continuation)
		{
			return task.ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}
	}
}
