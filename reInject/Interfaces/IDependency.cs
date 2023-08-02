using System;
using System.Collections.Generic;
using System.Text;

namespace ReInject.Interfaces
{
  /// <summary>
  /// interface to represent single dependency strategies
  /// </summary>
  public interface IDependency
  {
    /// <summary>
    /// The type of the dependency
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets and instance of this dependency
    /// </summary>
    object Instance { get; }

    public bool IsSingleton { get; }
    /// <summary>
    /// Clear cached values
    /// </summary>
    void Clear();
  }
}
