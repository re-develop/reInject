using System;
using System.Collections.Generic;
using System.Text;
using ReInject.Interfaces;

namespace ReInject.Implementation.DependencyTypes
{
  class SingleInstanceDependency : DependencyBase
  {
    private object _instance;
    public SingleInstanceDependency(IDependencyContainer container, Type type, Type interfaceType = null, string name = null) : base(container, DependencyStrategy.SingleInstance, type, interfaceType, name)
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
