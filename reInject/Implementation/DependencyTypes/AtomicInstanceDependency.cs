using System;
using System.Collections.Generic;
using System.Text;
using ReInject.Interfaces;

namespace ReInject.Implementation.DependencyTypes
{
  class AtomicInstanceDependency : DependencyBase
  {
    public AtomicInstanceDependency( IDependencyContainer container, object instance, Type type, Type interfaceType = null, string name = null ) : base( container, DependencyStrategy.AtomicInstance, type, interfaceType, name )
    {
      this._instance = instance;
    }

    private object _instance;
    public override object Instance => _instance;

    public override void Clear()
    {
     
    }
  }
}
