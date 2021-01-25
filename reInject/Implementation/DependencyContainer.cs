using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using ReInject.Core;
using ReInject.Implementation.Core;
using ReInject.Implementation.DependencyTypes;
using ReInject.Interfaces;

namespace ReInject.Implementation
{

  /// <summary>
  /// default shipped implementation of IDependencyContainer
  /// </summary>
  internal class DependencyContainer : IDependencyContainer
  {
    IDependencyContainer Parent { get; init; }

    /// <summary>
    /// Constructs and DependencyContainer with the given name
    /// </summary>
    /// <param name="name">The name of this conainer</param>
    /// <param name="parent">Parent to fallback on type search</param>
    public DependencyContainer(string name, IDependencyContainer parent = null)
    {
      this.Name = name;
    }

    // internal dictionary to keep track of registered dependencies
    private Dictionary<(Type, string), IDependencyType> _typeCache = new Dictionary<(Type, string), IDependencyType>();
    private Dictionary<string, EventProxy> _eventProxies = new Dictionary<string, EventProxy>();

    /// <summary>
    /// Gets the name of the current dependency container
    /// </summary>
    public string Name { get; private set; }


    /// <summary>
    /// Check if a given type is a registered dependency
    /// </summary>
    /// <typeparam name="T">The type of the depedency to search for</typeparam>
    /// <param name="name">Optional name to register multiple depedencies of the same type, default is null</param>
    /// <returns>True if a depdendency of the given type and name could be found, otherwise false</returns>
    public bool IsKnownType<T>(string name = null)
    {
      return IsKnownType(typeof(T), name) || (Parent?.IsKnownType<T>(name) ?? false);
    }


    /// <summary>
    /// Check if a given type is a registered dependency
    /// </summary>
    /// <param name="type">The type of the depedency to search for</param>
    /// <param name="name">Optional name to register multiple depedencies of the same type, default is null</param>
    /// <returns>True if a depdendency of the given type and name could be found, otherwise false</returns>
    public bool IsKnownType(Type type, string name = null)
    {
      if (type == null)
        return false;

      return _typeCache.ContainsKey((type, name)) || (Parent?.IsKnownType(type, name) ?? false);
    }

    /// <summary>
    /// Registers an object, it's class and it's interface as dependency
    /// </summary>
    /// <typeparam name="I">The interface type</typeparam>
    /// <typeparam name="T">The depdendency type</typeparam>
    /// <param name="strategy">The caching strategy</param>
    /// <param name="overwrite">If any already existing depdendencie's with this type and name should be overwritten</param>
    /// <param name="instance">An instance of the class for initialisation or atomic instances</param>
    /// <param name="name">An optional name to register multiple dependencies of the same type</param>
    /// <returns>This container for builder pattern</returns>
    public IDependencyContainer Register<I, T>(DependencyStrategy strategy, bool overwrite = false, object instance = null, string name = null)
    {
      register(typeof(T), typeof(I), strategy, overwrite, instance, name);
      return this;
    }

    /// <summary>
    /// Registers an object and/or it's class 
    /// </summary>
    /// <typeparam name="T">The depdendency type</typeparam>
    /// <param name="strategy">The caching strategy</param>
    /// <param name="overwrite">If any already existing depdendencie's with this type and name should be overwritten</param>
    /// <param name="instance">An instance of the class for initialisation or atomic instances</param>
    /// <param name="name">An optional name to register multiple dependencies of the same type</param>
    /// <returns>This container for builder pattern</returns>
    public IDependencyContainer Register<T>(DependencyStrategy strategy, bool overwrite = false, object instance = null, string name = null)
    {
      register(typeof(T), null, strategy, overwrite, instance, name);
      return this;
    }

    /// <summary>
    /// Registers an object, it's class and it's interface as dependency
    /// </summary>
    /// <param name="type">The depdendency type</typeparam>
    /// <param name="strategy">The caching strategy</param>
    /// <param name="overwrite">If any already existing depdendencie's with this type and name should be overwritten</param>
    /// <param name="instance">An instance of the class for initialisation or atomic instances</param>
    /// <param name="name">An optional name to register multiple dependencies of the same type</param>
    /// <returns>This container for builder pattern</returns>
    public IDependencyContainer Register(Type type, DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instance = null, string name = null)
    {
      register(type, null, strategy, overwrite, instance, name);
      return this;
    }

    /// <summary>
    /// Registers an object, it's class and it's interface as dependency
    /// </summary>
    /// <param name="interfaceType">The interface type</typeparam>
    /// <param name="type">The depdendency type</typeparam>
    /// <param name="strategy">The caching strategy</param>
    /// <param name="overwrite">If any already existing depdendencie's with this type and name should be overwritten</param>
    /// <param name="instance">An instance of the class for initialisation or atomic instances</param>
    /// <param name="name">An optional name to register multiple dependencies of the same type</param>
    /// <returns>This container for builder pattern</returns>
    public IDependencyContainer Register(Type type, Type interfaceType, DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instance = null, string name = null)
    {
      register(type, interfaceType, strategy, overwrite, instance, name);
      return this;
    }

    private void register(Type type, Type interfaceType, DependencyStrategy strategy, bool overwrite, object instance, string name)
    {
      if (strategy == DependencyStrategy.AtomicInstance && instance == null)
        throw new ArgumentException("AtomicDependencies need an instance provided");

      if ((IsKnownType(type, name) || IsKnownType(interfaceType, name)) && overwrite == false)
        return;

      IDependencyType dependency = null;
      switch (strategy)
      {
        case DependencyStrategy.AtomicInstance:
          dependency = new AtomicInstanceDependency(this, instance, type, interfaceType, name);
          break;

        case DependencyStrategy.CachedInstance:
          dependency = new CachedInstanceDependency(this, type, interfaceType, name);
          break;

        case DependencyStrategy.NewInstance:
          dependency = new NewInstanceDependency(this, type, interfaceType, name);
          break;

        case DependencyStrategy.SingleInstance:
          dependency = new SingleInstanceDependency(this, type, interfaceType, name);
          break;

        default:
          throw new ArgumentException("Invalid Strategy", nameof(strategy));
      }

      if (dependency != null)
      {
        _typeCache[(type, name)] = dependency;
        if (interfaceType != null)
          _typeCache[(interfaceType, name)] = dependency;
      }
    }

    /// <summary>
    /// Returns an instance of the given type initialized with it's dependencies and offers an Action to configure the instanced object
    /// </summary>
    /// <typeparam name="T">The type of the instance</typeparam>
    /// <param name="action">Nullable action to configure the instance</param>
    /// <param name="name">Optional name to handle multiple dependencies of the same name, null is default</param>
    /// <returns>An instance of the given type</returns>
    public T GetInstance<T>(Action<T> onAccess = null, string name = null)
    {
      var inst = (T)GetInstance(typeof(T), name);
      if (inst == null && Parent != null)
        inst = Parent.GetInstance(onAccess, name);

      if (onAccess != null)
        onAccess(inst);

      return inst;
    }

    /// <summary>
    /// Returns an instance of the given type initialized with it's dependencies
    /// </summary>
    /// <param name="type">The type of the instance</param>
    /// <param name="name">Optional name to handle multiple dependencies of the same name, null is default</param>
    /// <returns>An instance of the given type</returns>
    public object GetInstance(Type type, string name = null)
    {
      if (type == null)
        return null;

      if (IsKnownType(type, name))
      {
        return _typeCache[(type, name)].Instance;
      }
      else if (Parent != null && Parent.IsKnownType(type, name))
      {
        return Parent.GetInstance(type, name);
      }
      else
      {
        return TypeInjectionMetadataCache.GetMetadataCache(type).CreateInstance(this);
      }
    }

    /// <summary>
    /// Returns an instance of the given type initialized with it's dependencies
    /// </summary>
    /// <param name="type">The type of the instance</param>
    /// <param name="name">Optional name to handle multiple dependencies of the same name, null is default</param>
    /// <returns>An instance of the given type</returns>
    public object GetService(Type type, string name = null)
    {
      return GetInstance(type, name);
    }

    /// <summary>
    /// Returns an instance of the given type initialized with it's dependencies and offers an Action to configure the instanced object
    /// </summary>
    /// <typeparam name="T">The type of the instance</typeparam>
    /// <param name="name">Optional name to handle multiple dependencies of the same name, null is default</param>
    /// <returns>An instance of the given type</returns>
    public object GetService(Type type)
    {
      return GetInstance(type, null);
    }

    /// <summary>
    /// Clear all cached object except registered dependencies of type AtomicInstance
    /// </summary>
    public void Clear()
    {
      _typeCache.Values.ToList().ForEach(x => x.Clear());
    }

    internal bool HasEventProxy(string name)
    {
      return _eventProxies.ContainsKey(name);
    }

    internal EventProxy GetEventProxy(string name)
    {
      return _eventProxies[name];
    }

    public void RegisterEventSource(object sender, string bindTo, string uniqueEventName, bool overwrite = false)
    {
      if(overwrite || _eventProxies.ContainsKey(uniqueEventName) == false)
      {
        UnregisterEventSource(uniqueEventName);
        _eventProxies[uniqueEventName] = new EventProxy(sender, bindTo, uniqueEventName);
      }
    }

    public void UnregisterEventSource(string uniqueEventName)
    {
      if(_eventProxies.TryGetValue(uniqueEventName, out var proxy))
      {
        _eventProxies.Remove(uniqueEventName);
        proxy.Dispose();
      }
    }

    public void UnregisterEventSources(object sender)
    {
      var proxies = _eventProxies.Where(x => x.Value.EventSource.Equals(sender)).ToList();
      foreach (var proxy in proxies)
      {
        _eventProxies.Remove(proxy.Key);
        proxy.Value.Dispose();
      }
    }

    public void UnregisterEventSources<T>()
    {
      var proxies = _eventProxies.Where(x => x.Value.EventSource.GetType().Equals(typeof(T))).ToList();
      foreach (var proxy in proxies)
      {
        _eventProxies.Remove(proxy.Key);
        proxy.Value.Dispose();
      }
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
  }
}
