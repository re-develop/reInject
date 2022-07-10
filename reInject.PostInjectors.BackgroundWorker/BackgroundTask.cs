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
    private bool _enabled = false;
    private Timer _nextCall;
    private IBackgroundTaskScheduler _scheduler;

    public Guid Id { get; init; } = Guid.NewGuid();
    public object Target { get; init; }
    public CronExpression Schedule { get; init; }
    public bool Enabled { get => _enabled; set => setEnabled(value); }
    public Action Callable { get; set; }
    public Func<Task> AsyncCallable { get; set; }
    public string Tag { get; set; }

    public BackgroundTask(IBackgroundTaskScheduler scheduler, MethodInfo method, object obj, CronExpression schedule, bool start = true)
    {
      _scheduler = scheduler;
      this.Target = obj;
      if (method.ReturnType == typeof(Task))
        AsyncCallable = method.CreateDelegate<Func<Task>>(obj);
      else
        Callable = method.CreateDelegate<Action>(obj);

      this.Schedule = schedule;
      setEnabled(start);
    }

    public BackgroundTask(IBackgroundTaskScheduler scheduler, Action callable, CronExpression schedule, bool start, string tag = null)
    {
      _scheduler = scheduler;
      this.Callable = callable;
      this.Schedule = schedule;
      this.Tag = tag;
      setEnabled(start);
    }

    public BackgroundTask(IBackgroundTaskScheduler scheduler, Func<Task> callable, CronExpression schedule, bool start = true, string tag = null)
    {
      _scheduler = scheduler;
      this.AsyncCallable = callable;
      this.Schedule = schedule;
      this.Tag = tag;
      setEnabled(start);
    }

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
          ScheduleNextCall();
        }
      }
    }

    internal void ScheduleNextCall()
    {
      _nextCall?.Dispose();
      var now = DateTime.UtcNow;
      var next = Schedule.GetNextOccurrence(now);
      if (next.HasValue == false || next > _scheduler.NextUpdate)
        return;

      var dueTime = next.Value - now;
      if (Enabled)
        _nextCall = new Timer(callback, null, dueTime, Timeout.InfiniteTimeSpan);
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

      ScheduleNextCall();
    }

    public void Dispose()
    {
      _nextCall?.Dispose();
      _nextCall= null;
      _enabled = false;
    }
  }
}
