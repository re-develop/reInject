using Cronos;
using Microsoft.Extensions.Logging;
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
    private ILogger _logger = null;

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

    public BackgroundWorkerInjector(string name = null, int priority = 0, TimeSpan? checkTimerPeriod = null, ILogger logger = null)
    {
      this.Name = name ?? Guid.NewGuid().ToString();
      this.Priority = priority;
      _logger = logger;
      SchedulePeriod = checkTimerPeriod ?? TimeSpan.FromHours(1);
      _logger?.LogDebug($"Created new background worker name={name}, priority={priority}, period={checkTimerPeriod}");
    }

    public BackgroundWorkerInjector(string name = null, int priority = 0, TimeSpan? checkTimerPeriod = null, ILoggerFactory factory = null) : this(name, priority, checkTimerPeriod, factory?.CreateLogger<BackgroundWorkerInjector>())
    {

    }

    private void updateTimer()
    {
      _scheduleTimer?.Dispose();
      _lastCall = DateTime.UtcNow;
      _scheduleTimer = new Timer(setupNextTriggers, null, SchedulePeriod, SchedulePeriod);
      _logger?.LogDebug($"Updated master scheduler, period={SchedulePeriod}");
    }

    private List<BackgroundTask> _tasks = new List<BackgroundTask>();
    public string Name { get; init; }
    public int Priority { get; set; }

    public DateTime NextUpdate => _lastCall + SchedulePeriod;

    private void setupNextTriggers(object _)
    {
      _lastCall = DateTime.UtcNow;
      _logger?.LogDebug($"Schedule tasks for period {_lastCall} - {NextUpdate}");
      foreach (var task in _tasks)
        if (task.ScheduleNextCall(out var next))
          _logger?.LogTrace($"Scheduled next call for task {task} at {next}");
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
          _logger?.LogTrace($"Run postinjections on object ({instance}) of type={type.FullName} with container {container.Name}");
          foreach (var attribute in attributes)
            _tasks.Add(new BackgroundTask(this, method, instance, attribute.GetSchedule(instance)));

          _logger?.LogTrace($"Registered {attributes.Length} background task{(attributes.Length > 1 ? "s" : "")} for method {method.Name}");
          yield return method;
        }
      }
    }

    public bool SetInjectionEnabled(object instance, bool enabled)
    {
      var list = _tasks.Where(x => x.Target == instance).ToList();
      list.ForEach(task => task.Enabled = false);
      _logger?.LogDebug($"{(enabled ? "Enabled" : "Disabled")} {list.Count} background tasks for {instance}");
      return list.Count > 0;
    }

    public IBackgroundTask RegisterBackgroundTask(CronExpression schedule, Func<Task> callable, bool start = true, string tag = null)
    {
      var task = new BackgroundTask(this, callable, schedule, start, tag);
      _tasks.Add(task);
      _logger?.LogDebug($"Registered background task tag={tag},id={task.Id} with schedule {schedule}");  
      return task;
    }

    public IBackgroundTask RegisterBackgroundTask(CronExpression schedule, Action callable, bool start = true, string tag = null)
    {
      var task = new BackgroundTask(this, callable, schedule, start, tag);
      _tasks.Add(task);
      _logger?.LogDebug($"Registered background task tag={tag},id={task.Id} with schedule {schedule}");
      return task;
    }

    public void UnregisterBackgroundTask(IBackgroundTask task)
    {
      _tasks.RemoveAll(x => x.Id == task.Id);
      _logger?.LogDebug($"Unregistered background task tag={task.Tag} {task.Id}");
    }
  }
}
