using System;
using System.Collections.Generic;
using System.Text;

namespace ReInject
{
  /// <summary>
  /// Enum containing all DependencyStrategies
  /// </summary>
  public enum DependencyStrategy
  {
    /// <summary>
    /// Keeps track of a singleton which stays always the same even on cache clears, instance has to be passen on registration
    /// </summary>
    AtomicInstance,

    /// <summary>
    /// Creates an single instance or takes a given one passed on registration and reuses it until the container cache is cleared
    /// </summary>
    SingleInstance,

    /// <summary>
    /// Creates an singled instance and a keeps it as a WeakReference, a new instance is always created when the reference was garbage collected or the container cache was cleared
    /// </summary>
    CachedInstance,

    /// <summary>
    /// Always creates a new instance
    /// </summary>
    NewInstance
  }
}
