using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace System.Net
{
	internal static class TimerThread
	{
		internal abstract class Queue
		{
			private readonly int m_DurationMilliseconds;

			internal int Duration
			{
				get
				{
					return this.m_DurationMilliseconds;
				}
			}

			internal Queue(int durationMilliseconds)
			{
				this.m_DurationMilliseconds = durationMilliseconds;
			}

			internal TimerThread.Timer CreateTimer()
			{
				return this.CreateTimer(null, null);
			}

			internal abstract TimerThread.Timer CreateTimer(TimerThread.Callback callback, object context);
		}

		internal abstract class Timer : IDisposable
		{
			private readonly int m_StartTimeMilliseconds;

			private readonly int m_DurationMilliseconds;

			internal int Duration
			{
				get
				{
					return this.m_DurationMilliseconds;
				}
			}

			internal int StartTime
			{
				get
				{
					return this.m_StartTimeMilliseconds;
				}
			}

			internal int Expiration
			{
				get
				{
					return this.m_StartTimeMilliseconds + this.m_DurationMilliseconds;
				}
			}

			internal int TimeRemaining
			{
				get
				{
					if (this.HasExpired)
					{
						return 0;
					}
					if (this.Duration == -1)
					{
						return -1;
					}
					int tickCount = Environment.TickCount;
					int num = (int)(TimerThread.IsTickBetween(this.StartTime, this.Expiration, tickCount) ? Math.Min((uint)(this.Expiration - tickCount), 2147483647u) : 0u);
					if (num >= 2)
					{
						return num;
					}
					return num + 1;
				}
			}

			internal abstract bool HasExpired
			{
				get;
			}

			internal Timer(int durationMilliseconds)
			{
				this.m_DurationMilliseconds = durationMilliseconds;
				this.m_StartTimeMilliseconds = Environment.TickCount;
			}

			internal abstract bool Cancel();

			public void Dispose()
			{
				this.Cancel();
			}
		}

		internal delegate void Callback(TimerThread.Timer timer, int timeNoticed, object context);

		private enum TimerThreadState
		{
			Idle,
			Running,
			Stopped
		}

		private class TimerQueue : TimerThread.Queue
		{
			private readonly TimerThread.TimerNode m_Timers;

			internal TimerQueue(int durationMilliseconds) : base(durationMilliseconds)
			{
				this.m_Timers = new TimerThread.TimerNode();
				this.m_Timers.Next = this.m_Timers;
				this.m_Timers.Prev = this.m_Timers;
			}

			internal override TimerThread.Timer CreateTimer(TimerThread.Callback callback, object context)
			{
				TimerThread.TimerNode timerNode = new TimerThread.TimerNode(callback, context, base.Duration, this.m_Timers);
				bool flag = false;
				lock (this.m_Timers)
				{
					if (this.m_Timers.Next == this.m_Timers)
					{
						flag = true;
					}
					timerNode.Next = this.m_Timers;
					timerNode.Prev = this.m_Timers.Prev;
					this.m_Timers.Prev.Next = timerNode;
					this.m_Timers.Prev = timerNode;
				}
				if (flag)
				{
					TimerThread.Prod();
				}
				return timerNode;
			}

			internal bool Fire(out int nextExpiration)
			{
				TimerThread.TimerNode next;
				do
				{
					next = this.m_Timers.Next;
					if (next == this.m_Timers)
					{
						lock (this.m_Timers)
						{
							next = this.m_Timers.Next;
							if (next == this.m_Timers)
							{
								nextExpiration = 0;
								return false;
							}
						}
					}
				}
				while (next.Fire());
				nextExpiration = next.Expiration;
				return true;
			}
		}

		private class InfiniteTimerQueue : TimerThread.Queue
		{
			internal InfiniteTimerQueue() : base(-1)
			{
			}

			internal override TimerThread.Timer CreateTimer(TimerThread.Callback callback, object context)
			{
				return new TimerThread.InfiniteTimer();
			}
		}

		private class TimerNode : TimerThread.Timer
		{
			private enum TimerState
			{
				Ready,
				Fired,
				Cancelled,
				Sentinel
			}

			private TimerThread.TimerNode.TimerState m_TimerState;

			private TimerThread.Callback m_Callback;

			private object m_Context;

			private object m_QueueLock;

			private TimerThread.TimerNode next;

			private TimerThread.TimerNode prev;

			internal override bool HasExpired
			{
				get
				{
					return this.m_TimerState == TimerThread.TimerNode.TimerState.Fired;
				}
			}

			internal TimerThread.TimerNode Next
			{
				get
				{
					return this.next;
				}
				set
				{
					this.next = value;
				}
			}

			internal TimerThread.TimerNode Prev
			{
				get
				{
					return this.prev;
				}
				set
				{
					this.prev = value;
				}
			}

			internal TimerNode(TimerThread.Callback callback, object context, int durationMilliseconds, object queueLock) : base(durationMilliseconds)
			{
				if (callback != null)
				{
					this.m_Callback = callback;
					this.m_Context = context;
				}
				this.m_TimerState = TimerThread.TimerNode.TimerState.Ready;
				this.m_QueueLock = queueLock;
			}

			internal TimerNode() : base(0)
			{
				this.m_TimerState = TimerThread.TimerNode.TimerState.Sentinel;
			}

			internal override bool Cancel()
			{
				if (this.m_TimerState == TimerThread.TimerNode.TimerState.Ready)
				{
					lock (this.m_QueueLock)
					{
						if (this.m_TimerState == TimerThread.TimerNode.TimerState.Ready)
						{
							this.Next.Prev = this.Prev;
							this.Prev.Next = this.Next;
							this.Next = null;
							this.Prev = null;
							this.m_Callback = null;
							this.m_Context = null;
							this.m_TimerState = TimerThread.TimerNode.TimerState.Cancelled;
							return true;
						}
					}
					return false;
				}
				return false;
			}

			internal bool Fire()
			{
				if (this.m_TimerState != TimerThread.TimerNode.TimerState.Ready)
				{
					return true;
				}
				int tickCount = Environment.TickCount;
				if (TimerThread.IsTickBetween(base.StartTime, base.Expiration, tickCount))
				{
					return false;
				}
				bool flag = false;
				lock (this.m_QueueLock)
				{
					if (this.m_TimerState == TimerThread.TimerNode.TimerState.Ready)
					{
						this.m_TimerState = TimerThread.TimerNode.TimerState.Fired;
						this.Next.Prev = this.Prev;
						this.Prev.Next = this.Next;
						this.Next = null;
						this.Prev = null;
						flag = (this.m_Callback != null);
					}
				}
				if (flag)
				{
					try
					{
						TimerThread.Callback callback = this.m_Callback;
						object context = this.m_Context;
						this.m_Callback = null;
						this.m_Context = null;
						callback(this, tickCount, context);
					}
					catch (Exception ex)
					{
						if (NclUtilities.IsFatal(ex))
						{
							throw;
						}
						if (Logging.On)
						{
							Logging.PrintError(Logging.Web, string.Concat(new object[]
							{
								"TimerThreadTimer#",
								base.StartTime.ToString(NumberFormatInfo.InvariantInfo),
								"::Fire() - ",
								ex
							}));
						}
					}
				}
				return true;
			}
		}

		private class InfiniteTimer : TimerThread.Timer
		{
			private int cancelled;

			internal override bool HasExpired
			{
				get
				{
					return false;
				}
			}

			internal InfiniteTimer() : base(-1)
			{
			}

			internal override bool Cancel()
			{
				return Interlocked.Exchange(ref this.cancelled, 1) == 0;
			}
		}

		private const int c_ThreadIdleTimeoutMilliseconds = 30000;

		private const int c_CacheScanPerIterations = 32;

		private const int c_TickCountResolution = 15;

		private static LinkedList<WeakReference> s_Queues;

		private static LinkedList<WeakReference> s_NewQueues;

		private static int s_ThreadState;

		private static AutoResetEvent s_ThreadReadyEvent;

		private static ManualResetEvent s_ThreadShutdownEvent;

		private static WaitHandle[] s_ThreadEvents;

		private static int s_CacheScanIteration;

		private static Hashtable s_QueuesCache;

		static TimerThread()
		{
			TimerThread.s_Queues = new LinkedList<WeakReference>();
			TimerThread.s_NewQueues = new LinkedList<WeakReference>();
			TimerThread.s_ThreadState = 0;
			TimerThread.s_ThreadReadyEvent = new AutoResetEvent(false);
			TimerThread.s_ThreadShutdownEvent = new ManualResetEvent(false);
			TimerThread.s_QueuesCache = new Hashtable();
			TimerThread.s_ThreadEvents = new WaitHandle[]
			{
				TimerThread.s_ThreadShutdownEvent,
				TimerThread.s_ThreadReadyEvent
			};
			AppDomain.CurrentDomain.DomainUnload += new EventHandler(TimerThread.OnDomainUnload);
		}

		internal static TimerThread.Queue CreateQueue(int durationMilliseconds)
		{
			if (durationMilliseconds == -1)
			{
				return new TimerThread.InfiniteTimerQueue();
			}
			if (durationMilliseconds < 0)
			{
				throw new ArgumentOutOfRangeException("durationMilliseconds");
			}
			TimerThread.TimerQueue timerQueue;
			lock (TimerThread.s_NewQueues)
			{
				timerQueue = new TimerThread.TimerQueue(durationMilliseconds);
				WeakReference value = new WeakReference(timerQueue);
				TimerThread.s_NewQueues.AddLast(value);
			}
			return timerQueue;
		}

		internal static TimerThread.Queue GetOrCreateQueue(int durationMilliseconds)
		{
			if (durationMilliseconds == -1)
			{
				return new TimerThread.InfiniteTimerQueue();
			}
			if (durationMilliseconds < 0)
			{
				throw new ArgumentOutOfRangeException("durationMilliseconds");
			}
			WeakReference weakReference = (WeakReference)TimerThread.s_QueuesCache[durationMilliseconds];
			TimerThread.TimerQueue timerQueue;
			if (weakReference == null || (timerQueue = (TimerThread.TimerQueue)weakReference.Target) == null)
			{
				lock (TimerThread.s_NewQueues)
				{
					weakReference = (WeakReference)TimerThread.s_QueuesCache[durationMilliseconds];
					if (weakReference == null || (timerQueue = (TimerThread.TimerQueue)weakReference.Target) == null)
					{
						timerQueue = new TimerThread.TimerQueue(durationMilliseconds);
						weakReference = new WeakReference(timerQueue);
						TimerThread.s_NewQueues.AddLast(weakReference);
						TimerThread.s_QueuesCache[durationMilliseconds] = weakReference;
						if (++TimerThread.s_CacheScanIteration % 32 == 0)
						{
							List<int> list = new List<int>();
							foreach (DictionaryEntry dictionaryEntry in TimerThread.s_QueuesCache)
							{
								if (((WeakReference)dictionaryEntry.Value).Target == null)
								{
									list.Add((int)dictionaryEntry.Key);
								}
							}
							for (int i = 0; i < list.Count; i++)
							{
								TimerThread.s_QueuesCache.Remove(list[i]);
							}
						}
					}
				}
			}
			return timerQueue;
		}

		private static void Prod()
		{
			TimerThread.s_ThreadReadyEvent.Set();
			if (Interlocked.CompareExchange(ref TimerThread.s_ThreadState, 1, 0) == 0)
			{
				new Thread(new ThreadStart(TimerThread.ThreadProc)).Start();
			}
		}

		private static void ThreadProc()
		{
			Thread.CurrentThread.IsBackground = true;
			lock (TimerThread.s_Queues)
			{
				if (Interlocked.CompareExchange(ref TimerThread.s_ThreadState, 1, 1) == 1)
				{
					bool flag2 = true;
					while (flag2)
					{
						try
						{
							TimerThread.s_ThreadReadyEvent.Reset();
							while (true)
							{
								if (TimerThread.s_NewQueues.Count > 0)
								{
									lock (TimerThread.s_NewQueues)
									{
										for (LinkedListNode<WeakReference> first = TimerThread.s_NewQueues.First; first != null; first = TimerThread.s_NewQueues.First)
										{
											TimerThread.s_NewQueues.Remove(first);
											TimerThread.s_Queues.AddLast(first);
										}
									}
								}
								int tickCount = Environment.TickCount;
								int num = 0;
								bool flag4 = false;
								LinkedListNode<WeakReference> linkedListNode = TimerThread.s_Queues.First;
								while (linkedListNode != null)
								{
									TimerThread.TimerQueue timerQueue = (TimerThread.TimerQueue)linkedListNode.Value.Target;
									if (timerQueue == null)
									{
										LinkedListNode<WeakReference> next = linkedListNode.Next;
										TimerThread.s_Queues.Remove(linkedListNode);
										linkedListNode = next;
									}
									else
									{
										int num2;
										if (timerQueue.Fire(out num2) && (!flag4 || TimerThread.IsTickBetween(tickCount, num, num2)))
										{
											num = num2;
											flag4 = true;
										}
										linkedListNode = linkedListNode.Next;
									}
								}
								int tickCount2 = Environment.TickCount;
								int millisecondsTimeout = (int)(flag4 ? (TimerThread.IsTickBetween(tickCount, num, tickCount2) ? (Math.Min((uint)(num - tickCount2), 2147483632u) + 15u) : 0u) : 30000u);
								int num3 = WaitHandle.WaitAny(TimerThread.s_ThreadEvents, millisecondsTimeout, false);
								if (num3 == 0)
								{
									break;
								}
								if (num3 == 258 && !flag4)
								{
									Interlocked.CompareExchange(ref TimerThread.s_ThreadState, 0, 1);
									if (!TimerThread.s_ThreadReadyEvent.WaitOne(0, false) || Interlocked.CompareExchange(ref TimerThread.s_ThreadState, 1, 0) != 0)
									{
										goto IL_1A5;
									}
								}
							}
							flag2 = false;
							continue;
							IL_1A5:
							flag2 = false;
						}
						catch (Exception ex)
						{
							if (NclUtilities.IsFatal(ex))
							{
								throw;
							}
							if (Logging.On)
							{
								Logging.PrintError(Logging.Web, "TimerThread#" + Thread.CurrentThread.ManagedThreadId.ToString(NumberFormatInfo.InvariantInfo) + "::ThreadProc() - Exception:" + ex.ToString());
							}
							Thread.Sleep(1000);
						}
					}
				}
			}
		}

		private static void StopTimerThread()
		{
			Interlocked.Exchange(ref TimerThread.s_ThreadState, 2);
			TimerThread.s_ThreadShutdownEvent.Set();
		}

		private static bool IsTickBetween(int start, int end, int comparand)
		{
			return start <= comparand == end <= comparand != start <= end;
		}

		private static void OnDomainUnload(object sender, EventArgs e)
		{
			try
			{
				TimerThread.StopTimerThread();
			}
			catch
			{
			}
		}
	}
}
