using ReInject.Interfaces;
using ReInject.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.PostInjectors.EventInjection
{
  public class EventInjector : IPostInjector, IEventProvider
  {
    private Dictionary<string, EventProxy> _eventProxies = new Dictionary<string, EventProxy>();
    public int Priority { get; set; }

    public string Name { get; init; }

    public EventInjector(string name, int priority = 0)
    {
      Priority = priority;
      Name = name;
    }

    internal bool HasEventProxy(string name)
    {
      return _eventProxies.ContainsKey(name);
    }

    internal EventProxy GetEventProxy(string name)
    {
      return _eventProxies[name];
    }

    internal IEnumerable<EventProxy> GetEventProxies(params string[] events)
    {
      var proxies = (IEnumerable<EventProxy>)_eventProxies.Values;
      if (events != null && events.Length > 0)
        proxies = _eventProxies.Where(x => events.Contains(x.Key)).Select(x => x.Value);

      return proxies;
    }

    public IEventProvider RegisterEventSource(object sender, string bindTo, string uniqueEventName, bool overwrite = false)
    {
      if (overwrite || _eventProxies.ContainsKey(uniqueEventName) == false)
      {
        UnregisterEventSource(uniqueEventName);
        _eventProxies[uniqueEventName] = new EventProxy(sender, bindTo, uniqueEventName);
      }

      return this;
    }

    public IEventProvider RegisterEventSources(object sender, string prefix, Func<EventInfo, string> nameFunc = null, Func<EventInfo, bool> filterFunc = null, Func<EventInfo, bool> overwriteFunc = null)
    {
      var events = ReflectionHelper.GetAllMembers(sender.GetType(), MemberTypes.Event).Cast<EventInfo>();
      nameFunc ??= (EventInfo ev) => ev.Name;
      filterFunc ??= (_) => true;
      overwriteFunc ??= (_) => false;

      foreach (var @event in events.Where(filterFunc))
      {
        var name = prefix + nameFunc(@event);
        if (HasEventProxy(name) == false || overwriteFunc(@event))
        {
          UnregisterEventSource(name);
          _eventProxies[name] = new EventProxy(sender, @event, name);
        }
      }

      return this;
    }

    public IEventProvider UnregisterEventSource(string uniqueEventName)
    {
      if (_eventProxies.TryGetValue(uniqueEventName, out var proxy))
      {
        _eventProxies.Remove(uniqueEventName);
        proxy.Dispose();
      }

      return this;
    }

    public IEventProvider UnregisterEventSources(object sender)
    {
      var proxies = _eventProxies.Where(x => x.Value.EventSource.Equals(sender)).ToList();
      foreach (var proxy in proxies)
      {
        _eventProxies.Remove(proxy.Key);
        proxy.Value.Dispose();
      }

      return this;
    }

    public IEventProvider UnregisterEventSources<T>()
    {
      var proxies = _eventProxies.Where(x => x.Value.EventSource.GetType().Equals(typeof(T))).ToList();
      foreach (var proxy in proxies)
      {
        _eventProxies.Remove(proxy.Key);
        proxy.Value.Dispose();
      }

      return this;
    }

    public bool RegisterEventTarget(string uniqueEventName, object instance, MethodInfo info)
    {
      if (HasEventProxy(uniqueEventName))
      {
        var proxy = GetEventProxy(uniqueEventName);
        var proxyTarget = new EventProxyTarget(instance, info);
        proxy.AddTarget(proxyTarget);
        return true;
      }

      return false;
    }

    public void SetEventTargetEnabled(object target, bool enabled, params string[] events)
    {
      var proxies = (IEnumerable<EventProxy>)_eventProxies.Values;
      if (events != null && events.Length > 0)
        proxies = _eventProxies.Where(x => events.Contains(x.Key)).Select(x => x.Value);

      proxies.ToList().ForEach(x => x.SetTargetEnabled(target, enabled));
    }

    public IEnumerable<MemberInfo> PostInject(IDependencyContainer container, Type type, object instance)
    {
      var targets = ReflectionHelper.GetAllMembers(type, MemberTypes.Method)
        .Cast<MethodInfo>()
        .Select(x => (mehtod: x, attributes: x.GetCustomAttributes<InjectEventAttribute>()))
        .Where(x => x.attributes.Count() > 0);

      foreach (var target in targets)
      {
        foreach (var attribute in target.attributes)
        {
          if (HasEventProxy(attribute.EventName))
          {
            GetEventProxy(attribute.EventName).AddTarget(new EventProxyTarget(instance, target.mehtod));
            yield return target.mehtod;
          }
        }
      }
    }

    public bool SetInjectionEnabled(object instance, bool enabled)
    {
      SetEventTargetEnabled(enabled, enabled);
      return true;
    }

    public void Dispose()
    {
      _eventProxies.ToList().ForEach(x => x.Value.Dispose());
      _eventProxies.Clear();
    }
  }
}
