using System;
using System.Collections.Generic;
using System.Text;
using ReInject.Interfaces;

namespace ReInject.Implementation.DependencyTypes
{
  /// <summary>
  /// Implementation of the NewInstance dependency strategy
  /// </summary>
  internal class TransientDependency<T> : DependencyBase<T>
  {
    public TransientDependency(IDependencyContainer container, Func<T> factory, Type type, string name = null) : base(container, factory, type,  name )
    {
    }

    public override object Instance => createInstance();

    public override void Clear()
    {
      
    }
  }
}
