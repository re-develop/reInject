using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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


    private Dictionary<Type, IDependencyType> _typeCache = new Dictionary<Type, IDependencyType>();
    public string Name { get; private set; }

    public object GetInstance(Type type)
    {
      if (type == null)
        return null;

      if (IsKnownType(type))
      {
        return _typeCache[type].Instance;
      }
      else
      {
        var dep = new SingleInstanceDependency(this, type);
        return dep.Instance;
      }
    }

    public bool IsKnownType(Type type)
    {
      if (type == null)
        return false;

      return _typeCache.ContainsKey(type);
    }

    public IDependencyContainer Register<I, T>(DependencyStrategy strategy, bool overwrite = false, object instance = null)
    {
      register(typeof(T), typeof(I), strategy, overwrite, instance);
      return this;
    }

    public IDependencyContainer Register<T>(DependencyStrategy strategy, bool overwrite = false, object instance = null)
    {
      register(typeof(T), null, strategy, overwrite, instance);
      return this;
    }

    private void register(Type type, Type interfaceType, DependencyStrategy strategy, bool overwrite, object instance)
    {
      if (strategy == DependencyStrategy.AtomicInstance && instance == null)
        throw new ArgumentException("AtomicDependencies need an instance provided");

      if ((IsKnownType(type) || IsKnownType(interfaceType)) && overwrite == false)
        return;

      IDependencyType dependency = null;
      switch (strategy)
      {
        case DependencyStrategy.AtomicInstance:
          dependency = new AtomicInstanceDependency(this, instance, type, interfaceType);
          break;

        case DependencyStrategy.CachedInstance:
          dependency = new CachedInstanceDependency(this, type, interfaceType);
          break;

        case DependencyStrategy.NewInstance:
          dependency = new NewInstanceDependency(this, type, interfaceType);
          break;

        case DependencyStrategy.SingleInstance:
          dependency = new SingleInstanceDependency(this, type, interfaceType);
          break;

        default:
          throw new ArgumentException("Invalid Strategy", nameof(strategy));
      }

      if (dependency != null)
      {
        _typeCache[type] = dependency;
        if (interfaceType != null)
          _typeCache[interfaceType] = dependency;
      }
    }


    public T GetInstance<T>(Action<T> onAccess = null)
    {
      var inst = (T)GetInstance(typeof(T));
      if (onAccess != null)
        onAccess(inst);

      return inst;
    }

    public bool IsKnownType<T>()
    {
      return IsKnownType(typeof(T));
    }

    public object GetService(Type type)
    {
      return GetInstance(type);
    }

    public void Clear()
    {
      _typeCache.Values.ToList().ForEach(x => x.Clear());
    }
  }
}
