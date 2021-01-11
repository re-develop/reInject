using System;
using System.Collections.Generic;
using System.Text;

namespace ReInject.Interfaces
{
  public interface IDependencyContainer : IServiceProvider
  {
    bool IsKnownType<T>(string name = null);
    bool IsKnownType( Type type, string name = null );
    IDependencyContainer Register<I, T>( DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instance = null, string name = null );
    IDependencyContainer Register<T>( DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instance = null, string name = null);

    IDependencyContainer Register(Type type, DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instance = null, string name = null);
    IDependencyContainer Register(Type type, Type interfaceType, DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instance = null, string name = null);
  }
}
