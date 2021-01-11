using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ReInject.Core;
using ReInject.Implementation.DependencyTypes;
using ReInject.Interfaces;

namespace ReInject.Implementation
{
  internal class DependencyContainer : IDependencyContainer
  {
    public DependencyContainer(string name)
    {
      this.Name = name;
    }


    private Dictionary<(Type, string), IDependencyType> _typeCache = new Dictionary<(Type, string), IDependencyType>();
    public string Name { get; private set; }

    public object GetInstance(Type type, string name = null)
    {
      if (type == null)
        return null;

      if (IsKnownType(type, name))
      {
        return _typeCache[(type, name)].Instance;
      }
      else
      {
        return TypeInjectionMetadataCache.GetMetadataCache(type).CreateInstance(this);
      }
    }

    public bool IsKnownType(Type type, string name = null)
    {
      if (type == null)
        return false;

      return _typeCache.ContainsKey((type, name));
    }

    public IDependencyContainer Register<I, T>(DependencyStrategy strategy, bool overwrite = false, object instance = null, string name = null)
    {
      register(typeof(T), typeof(I), strategy, overwrite, instance, name);
      return this;
    }

    public IDependencyContainer Register<T>(DependencyStrategy strategy, bool overwrite = false, object instance = null, string name = null)
    {
      register(typeof(T), null, strategy, overwrite, instance, name);
      return this;
    }

    public IDependencyContainer Register(Type type, DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instance = null, string name = null)
    {
      register(type, null, strategy, overwrite, instance, name);
      return this;
    }

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


    public T GetInstance<T>(Action<T> onAccess = null, string name = null)
    {
      var inst = (T)GetInstance(typeof(T), name);
      if (onAccess != null)
        onAccess(inst);

      return inst;
    }

    public bool IsKnownType<T>(string name = null)
    {
      return IsKnownType(typeof(T), name);
    }

    public object GetService(Type type, string name = null)
    {
      return GetInstance(type, name);
    }

    public object GetService(Type type)
    {
      return GetInstance(type, null);
    }

    public void Clear()
    {
      _typeCache.Values.ToList().ForEach(x => x.Clear());
    }

  }
}
