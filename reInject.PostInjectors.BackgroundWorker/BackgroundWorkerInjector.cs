using Cronos;
using ReInject.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReInject.PostInjectors.BackgroundWorker
{
  public class BackgroundWorkerInjector : IPostInjector, IBackgroundWorkerManager, IBackgroundTaskScheduler
  {
    private Timer _scheduleTimer = null;
    private DateTime _lastCall = DateTime.MinValue;

    private TimeSpan _schedulePeriod = TimeSpan.Zero;
    public TimeSpan SchedulePeriod
    {
      get => _schedulePeriod;
      set
      {
        _schedulePeriod = value;
        updateTimer();
      }
    }

    public BackgroundWorkerInjector(string name = null, int priority = 0, TimeSpan? checkTimerPeriod = null)
    {
      this.Name = name ?? Guid.NewGuid().ToString();
      this.Priority = priority;
      SchedulePeriod = checkTimerPeriod ?? TimeSpan.FromHours(1);
    }

    private void updateTimer()
    {
      _scheduleTimer?.Dispose();
      _lastCall = DateTime.UtcNow;
      _scheduleTimer = new Timer(setupNextTriggers, null, SchedulePeriod, SchedulePeriod);
    }

    private List<BackgroundTask> _tasks = new List<BackgroundTask>();
    public string Name { get; init; }
    public int Priority { get; set; }

    public DateTime NextUpdate => _lastCall + SchedulePeriod;

    private void setupNextTriggers(object _)
    {
      _lastCall = DateTime.UtcNow;
      foreach (var task in _tasks)
      {
        var next = task.Schedule.GetNextOccurrence(_lastCall);
        if (next.HasValue && task.Enabled && next.Value < NextUpdate)
          task.ScheduleNextCall();
      }
    }

    public void Dispose()
    {
      _tasks.ForEach(worker => worker.Dispose());
      _tasks.Clear();
    }

    public IEnumerable<MemberInfo> PostInject(IDependencyContainer container, Type type, object instance)
    {
      foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
      {
        var attributes = method.GetCustomAttributes<BackgroundWorkerAttribute>().ToArray();
        if (attributes.Length > 0)
        {
          foreach (var attribute in attributes)
            _tasks.Add(new BackgroundTask(this, method, instance, attribute.GetSchedule(instance)));

          yield return method;
        }
      }
    }

    public bool SetInjectionEnabled(object instance, bool enabled)
    {
      var list = _tasks.Where(x => x.Target == instance).ToList();
      list.ForEach(task => task.Enabled = false);
      return list.Count > 0;
    }

    public IBackgroundTask RegisterBackgroundTask(CronExpression schedule, Func<Task> callable, bool start = true, string tag = null)
    {
      var task = new BackgroundTask(this, callable, schedule, start, tag);
      _tasks.Add(task);
      return task;
    }

    public IBackgroundTask RegisterBackgroundTask(CronExpression schedule, Action callable, bool start = true, string tag = null)
    {
      var task = new BackgroundTask(this, callable, schedule, start, tag);
      _tasks.Add(task);
      return task;
    }

    public void UnregisterBackgroundTask(IBackgroundTask task)
    {
      _tasks.RemoveAll(x => x.Id == task.Id);
    }
  }
}
