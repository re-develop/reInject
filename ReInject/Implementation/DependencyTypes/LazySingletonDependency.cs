using System;
using System.Collections.Generic;
using System.Text;
using ReInject.Interfaces;

namespace ReInject.Implementation.DependencyTypes
{
  /// <summary>
  /// Implementation of DependencyStrategy single instance
  /// </summary>
  internal class LazySingletonDependency<T> : DependencyBase<T>
  {
    private object _instance;
    public LazySingletonDependency(IDependencyContainer container, Func<T> factory, Type type,  string name = null) : base(container, factory, type, name)
    {
    }

    public override object Instance
    {
      get
      {
        if (_instance == null)
          _instance = createInstance();

        return _instance;
      }
    }

    public override void Clear()
    {
      _instance = null;
    }
  }
}
