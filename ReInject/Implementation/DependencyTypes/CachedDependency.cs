using System;
using System.Collections.Generic;
using System.Text;
using ReInject.Interfaces;

namespace ReInject.Implementation.DependencyTypes
{
  /// <summary>
  /// internal implementation of the cached dependency strategy
  /// </summary>
  internal class CachedDependency<T> : DependencyBase<T>
  {
    private WeakReference<object> _reference;

    public CachedDependency( IDependencyContainer container, Func<T> factory, Type type, string name = null ) : base( container, factory, type, name )
    {
    }

    public override object Instance
    {
      get
      {
        object instance = null;
        if(_reference == null || _reference?.TryGetTarget(out instance) == false )
        {
          instance = createInstance();
          _reference = new WeakReference<object>( instance );
        }

        return instance;
      }
    }

    public override void Clear()
    {
      _reference = null;
    }
  }
}
