using Microsoft.Extensions.DependencyInjection;
using ReInject;
using ReInject.Implementation.Attributes;
using ReInject.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace reInject.Scopes
{
  public class DependencyContainerScope : IDependencyContainer, IServiceScope
  {

    private IDependencyContainer _container;
    public System.IServiceProvider ServiceProvider => this;
    private List<IDisposable> _disposables = new List<IDisposable>();
    public DependencyContainerScope(IDependencyContainer container)
    {
      _container = container;
    }

    public string Name => _container.Name;

    public void Clear()
    {
      _container.Clear();
    }

    public void Dispose()
    {
      _disposables.ForEach(disposable => disposable.Dispose());
      _disposables.Clear();
    }

    public IEnumerable<(T instance, string name)> GetAllKnownInstances<T>(bool searchParents = true)
    {
      return _container.GetAllKnownInstances<T>(searchParents);
    }

    public IEnumerable<IPostInjector> GetPostInjecors()
    {
      return _container.GetPostInjecors();
    }

    private object handleDisposable(object inst, Type type, string name = null)
    {
      var strategy = _container.GetDependencyStrategy(type, name);
      if (inst != null && inst is IDisposable disposable)
      {
        if (strategy.Value == DependencyStrategy.NewInstance || strategy.HasValue == false)
          _disposables.Add(disposable);
      }

      return inst;
    }

    public T GetInstance<T>(Action<T> action = null, string name = null)
    {
      return (T)handleDisposable(_container.GetInstance(action, name), typeof(T), name);
    }

    public object GetInstance(Type type, string name = null)
    {
      return handleDisposable(_container.GetInstance(type, name), type, name);
    }

    public object GetService(Type serviceType)
    {
      return handleDisposable(_container.GetInstance(serviceType), serviceType);
    }

    public bool IsKnownType<T>(string name = null)
    {
      return _container.IsKnownType<T>(name);
    }

    public bool IsKnownType(Type type, string name = null)
    {
      return _container.IsKnownType(type, name);
    }

    public void PostInject(object obj)
    {
      _container.PostInject(obj);
    }

    public IDependencyContainer Register<I, T>(DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instance = null, string name = null)
    {
      return _container.Register<I, T>(strategy, overwrite, instance, name);
    }

    public IDependencyContainer Register<T>(DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instance = null, string name = null)
    {
      return _container.Register<T>(strategy, overwrite, instance, name);
    }

    public IDependencyContainer Register(Type type, DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instance = null, string name = null)
    {
      return _container.Register(type, strategy, overwrite, instance, name);
    }

    public IDependencyContainer Register(Type type, Type interfaceType, DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instance = null, string name = null)
    {
      return _container.Register(type, interfaceType, strategy, overwrite, instance, name);
    }

    public void RegisterEventSource(object sender, string bindTo, string eventName, bool overwrite = false)
    {
      _container.RegisterEventSource(sender, bindTo, eventName, overwrite);
    }

    public bool RegisterEventTarget(string eventName, object instance, MethodInfo info)
    {
      return _container.RegisterEventTarget(eventName, instance, info);
    }

    public bool RegisterPostInjector(IPostInjector injector, bool overwrite = false)
    {
      return _container.RegisterPostInjector(injector, overwrite);
    }

    public T RegisterPostInjector<T>(Action<T> configure = null, bool overwrite = false) where T : IPostInjector
    {
      return _container.RegisterPostInjector(configure, overwrite);
    }

    public void SetEventTargetEnabled(object target, bool enabled, params string[] events)
    {
      _container.SetEventTargetEnabled(target, enabled, events);
    }

    public void SetPostInjectionsEnabled(object instance, bool enabled)
    {
      _container.SetPostInjectionsEnabled(instance, enabled);
    }

    public void UnregisterEventSource(string uniqueEventName)
    {
      _container.UnregisterEventSource(uniqueEventName);
    }

    public void UnregisterEventSources(object sender)
    {
      _container.UnregisterEventSources(sender);
    }

    public void UnregisterEventSources<T>()
    {
      _container.UnregisterEventSources<T>();
    }

    public void UnregisterPostInjector<T>(string name = null) where T : IPostInjector
    {
      _container.UnregisterPostInjector<T>(name);
    }

    public void UnregisterPostInjector(IPostInjector injector)
    {
      _container.UnregisterPostInjector(injector);
    }

    public DependencyStrategy? GetDependencyStrategy(Type type, string name = null)
    {
      return _container.GetDependencyStrategy(type, name);
    }
  }
}
