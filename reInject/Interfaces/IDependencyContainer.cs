using System;
using System.Collections.Generic;
using System.Text;

namespace ReInject.Interfaces
{
  /// <summary>
  /// Interface to represent an depdendency container
  /// </summary>
  public interface IDependencyContainer : IServiceProvider, IPostInjectProvider
  {
    /// <summary>
    /// Check if a given type is a registered dependency
    /// </summary>
    /// <typeparam name="T">The type of the depedency to search for</typeparam>
    /// <param name="name">Optional name to register multiple depedencies of the same type, default is null</param>
    /// <returns>True if a depdendency of the given type and name could be found, otherwise false</returns>
    bool IsKnownType<T>(string name = null);

    /// <summary>
    /// Check if a given type is a registered dependency
    /// </summary>
    /// <param name="type">The type of the depedency to search for</param>
    /// <param name="name">Optional name to register multiple depedencies of the same type, default is null</param>
    /// <returns>True if a depdendency of the given type and name could be found, otherwise false</returns>
    bool IsKnownType(Type type, string name = null);

    /// <summary>
    /// Returns all known instances assignable to the given type an their names
    /// </summary>
    /// <typeparam name="T">The type of instances to search for</typeparam>
    /// <param name="searchParents">Wheter the parents should be searched as well</param>
    /// <returns></returns>
    public IEnumerable<(T instance, string name)> GetAllKnownInstances<T>(bool searchParents = true);

		IDependencyContainer Add(IDependency dependency, bool overwrite = false, string name = null);
		IDependencyContainer AddCached<T>(Func<T> factory = null, bool overwrite = false, string name = null);
		IDependencyContainer AddCached<TInterface, TType>(Func<TType> factory = null, bool overwrite = false, string name = null) where TType : TInterface;
		IDependencyContainer AddLazySingleton<T>(Func<T> factory = null, bool overwrite = false, string name = null);
		IDependencyContainer AddLazySingleton<TInterface, TType>(Func<TType> factory = null, bool overwrite = false, string name = null) where TType : TInterface;
		IDependencyContainer AddSingleton<T>(bool overwrite = false, string name = null);
		IDependencyContainer AddSingleton<T>(T value, bool overwrite = false, string name = null);
		IDependencyContainer AddSingleton<TInterface, TType>(bool overwrite = false, string name = null) where TType : TInterface;
		IDependencyContainer AddTransient<T>(Func<T> factory = null, bool overwrite = false, string name = null);
		IDependencyContainer AddTransient<TInterface, TType>(Func<TType> factory = null, bool overwrite = false, string name = null) where TType : TInterface;

		IDependencyContainer AddCached(Type type, Func<object> factory = null, bool overwrite = false, string name = null);
		IDependencyContainer AddLazySingleton(Type type, Func<object> factory = null, bool overwrite = false, string name = null);
		IDependencyContainer AddSingleton(Type type, object value, bool overwrite = false, string name = null);
		IDependencyContainer AddTransient(Type type, Func<object> factory = null, bool overwrite = false, string name = null);
		
    IDependencyContainer AddCached(Type interfaceType, Type actualType, Func<object> factory = null, bool overwrite = false, string name = null);
		IDependencyContainer AddLazySingleton(Type interfaceType, Type actualType, Func<object> factory = null, bool overwrite = false, string name = null);
		IDependencyContainer AddTransient(Type interfaceType, Type actualType, Func<object> factory = null, bool overwrite = false, string name = null);

		IDependency GetDependency<T>(string name = null);
    IDependency GetDependency(Type type, string name = null);
		/// <summary>
		/// Inject dependencies in an already existing object using attributes
		/// </summary>
		/// <param name="obj">The object to inject into</param>
		void PostInject(object obj);
  }
}
