using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using ReInject.Implementation.Core;
using ReInject.Interfaces;

namespace ReInject.Implementation.DependencyTypes
{

  /// <summary>
  /// internal abstract base class to implement IDependecyType
  /// </summary>
  internal abstract class DependencyBase<T> : IDependency
  {
    public DependencyBase( IDependencyContainer container, Func<T> factory, Type type, string name = null )
    {
      this.Type = type;
      this._container = container;
      this.Name = name;
      _factory = factory;
    }


    public string Name { get; init; }
    private Func<T> _factory;
    private IDependencyContainer _container;
    public Type Type { get; private set; }
    public virtual object Instance => createInstance();
    public abstract void Clear();
    public virtual bool IsSingleton => false;

    protected object createInstance()
    {
      return _factory != null ? _factory.Invoke() : TypeInjectionMetadataCache.GetMetadataCache<T>().CreateInstance(_container);
    }

  }
}
