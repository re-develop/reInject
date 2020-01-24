using System;
using System.Collections.Generic;
using System.Text;

namespace ReInject.Interfaces
{
  public interface IDependencyType
  {
    Type Type { get; }
    Type InterfaceType { get; }
    object Instance { get; }
    DependencyStrategy Strategy { get; }
    void Clear();
  }
}
