using System;
using System.Collections.Generic;
using System.Text;

namespace ReInject.Interfaces
{
  public interface IDependencyContainer : IServiceProvider
  {
    bool IsKnownType<T>();
    bool IsKnownType( Type type );
    IDependencyContainer Register<I, T>( DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instacen = null );
    IDependencyContainer Register<T>( DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instacen = null );
  }
}
