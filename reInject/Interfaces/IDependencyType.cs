using System;
using System.Collections.Generic;
using System.Text;

namespace ReInject.Interfaces
{
  /// <summary>
  /// interface to represent single dependency strategies
  /// </summary>
  public interface IDependencyType
  {
    /// <summary>
    /// The type of the dependency
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Optional interface type to identify this depdency
    /// </summary>
    Type InterfaceType { get; }

    /// <summary>
    /// Gets and instance of this dependency
    /// </summary>
    object Instance { get; }

    /// <summary>
    /// The caching strategy for this dependency
    /// </summary>
    DependencyStrategy Strategy { get; }

    /// <summary>
    /// Clear cached values
    /// </summary>
    void Clear();
  }
}
