using Cronos;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReInject.PostInjectors.BackgroundWorker
{
	public class BackgroundTask : IDisposable, IBackgroundTask
	{
		public BackgroundTask(MethodInfo method, object obj, CronExpression schedule, bool start = true)
		{
			this.Target = obj;
			if (method.ReturnType == typeof(Task))
				AsyncCallable = method.CreateDelegate<Func<Task>>(obj);
			else
				Callable = method.CreateDelegate<Action>(obj);

			this.Schedule = schedule;
			setEnabled(start);
		}

		public BackgroundTask(Action callable, CronExpression schedule, bool start, string tag = null)
		{
			this.Callable = callable;
			this.Schedule = schedule;
			this.Tag = tag;
			setEnabled(start);
		}

		public BackgroundTask(Func<Task> callable, CronExpression schedule, bool start = true, string tag = null)
		{
			this.AsyncCallable = callable;
			this.Schedule = schedule;
			this.Tag = tag;
			setEnabled(start);
		}


		private bool _enabled = false;
		private Timer _nextCall;

		public Guid Id { get; init; } = Guid.NewGuid();
		public object Target { get; init; }
		public CronExpression Schedule { get; init; }
		public bool Enabled { get => _enabled; set => setEnabled(value); }
		public Action Callable { get; set; }
		public Func<Task> AsyncCallable { get; set; }
		public string Tag { get; set; }

		private void setEnabled(bool enabled)
		{
			if (_enabled != enabled)
			{
				_enabled = enabled;
				if (enabled == false)
				{
					_nextCall?.Dispose();
					_nextCall = null;
				}
				else
				{
					var dueTime = Schedule.GetNextOccurrence(DateTime.UtcNow) - DateTime.UtcNow;
					if (dueTime.HasValue)
						_nextCall = new Timer(callback, null, dueTime.Value, Timeout.InfiniteTimeSpan);
				}
			}
		}

		private void setupTimer()
		{
			_nextCall?.Dispose();
			var dueTime = Schedule.GetNextOccurrence(DateTime.UtcNow) - DateTime.UtcNow;
			if (dueTime.HasValue)
				_nextCall = new Timer(callback, null, dueTime.Value, Timeout.InfiniteTimeSpan);
		}

		private async void callback(object _)
		{
			try
			{
				if (AsyncCallable != null)
					await AsyncCallable();
				else
					Callable?.Invoke();
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Caught Exception in BackgroundTask: {ex}");
			}

			setupTimer();
		}

		public void Dispose()
		{
			_nextCall?.Dispose();
			_nextCall= null;
			_enabled = false;
		}
	}
}
