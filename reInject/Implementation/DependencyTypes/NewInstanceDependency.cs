using System;
using System.Collections.Generic;
using System.Text;
using ReInject.Interfaces;

namespace ReInject.Implementation.DependencyTypes
{
  internal class NewInstanceDependency : DependencyBase
  {
    public NewInstanceDependency(IDependencyContainer container, Type type, Type interfaceType = null) : base(container, DependencyStrategy.NewInstance, type, interfaceType )
    {

    }

    public override object Instance => createInstance();

    public override void Clear()
    {
      
    }
  }
}
