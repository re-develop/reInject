using System;

namespace ReInject.Interfaces
{
  /// <summary>
  /// Interface to consume an dependencies
  /// </summary>
  public interface IServiceProvider : System.IServiceProvider
  {

    /// <summary>
    /// Returns an instance of the given type initialized with it's dependencies and offers an Action to configure the instanced object
    /// </summary>
    /// <typeparam name="T">The type of the instance</typeparam>
    /// <param name="action">Nullable action to configure the instance</param>
    /// <param name="name">Optional name to handle multiple dependencies of the same name, null is default</param>
    /// <returns>An instance of the given type</returns>
    T GetInstance<T>(Action<T> action = null, string name = null);

    /// <summary>
    /// Returns an instance of the given type initialized with it's dependencies
    /// </summary>
    /// <param name="type">The type of the instance</param>
    /// <param name="name">Optional name to handle multiple dependencies of the same name, null is default</param>
    /// <returns>An instance of the given type</returns>
    object GetInstance(Type type, string name = null);


    /// <summary>
    /// Clear all cached object except registered dependencies of type AtomicInstance
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the name of the current dependency container
    /// </summary>
    string Name { get; }
  }
}
