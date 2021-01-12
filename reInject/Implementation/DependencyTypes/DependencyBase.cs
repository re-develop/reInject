using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using ReInject.Core;
using ReInject.Interfaces;

namespace ReInject.Implementation.DependencyTypes
{

  /// <summary>
  /// internal abstract base class to implement IDependecyType
  /// </summary>
  internal abstract class DependencyBase : IDependencyType
  {
    public DependencyBase( IDependencyContainer container, DependencyStrategy strategy, Type type, Type interfaceType = null, string name = null )
    {
      this.Strategy = strategy;
      this.Type = type;
      this.InterfaceType = interfaceType;
      this._container = container;
      this.Name = name;
    }


    public string Name { get; init; }


    private IDependencyContainer _container;
    public DependencyStrategy Strategy { get; private set; }

    public Type Type { get; private set; }

    public Type InterfaceType { get; private set; }

    public abstract object Instance { get; }

    public abstract void Clear();




    protected object createInstance()
    {
      return TypeInjectionMetadataCache.GetMetadataCache(Type).CreateInstance(_container);
    }

  }
}
