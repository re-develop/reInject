using System;
using System.Collections.Generic;
using System.Text;

namespace ReInject
{
  public enum DependencyStrategy
  {
    AtomicInstance,
    SingleInstance,
    CachedInstance,
    NewInstance
  }
}
