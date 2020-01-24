using System;
using System.Collections.Generic;
using System.Text;
using ReInject.Interfaces;

namespace ReInject.Implementation.DependencyTypes
{
  class CachedInstanceDependency : DependencyBase
  {
    private WeakReference<object> _reference;

    public CachedInstanceDependency( IDependencyContainer container, Type type, Type interfaceType = null ) : base( container, DependencyStrategy.CachedInstance, type, interfaceType )
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
