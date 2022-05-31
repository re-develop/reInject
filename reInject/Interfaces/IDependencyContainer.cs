﻿using System;
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
    /// Registers an object, it's class and it's interface as dependency
    /// </summary>
    /// <typeparam name="I">The interface type</typeparam>
    /// <typeparam name="T">The depdendency type</typeparam>
    /// <param name="strategy">The caching strategy</param>
    /// <param name="overwrite">If any already existing depdendencie's with this type and name should be overwritten</param>
    /// <param name="instance">An instance of the class for initialisation or atomic instances</param>
    /// <param name="name">An optional name to register multiple dependencies of the same type</param>
    /// <returns>This container for builder pattern</returns>
    IDependencyContainer Register<I, T>(DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instance = null, string name = null);

    /// <summary>
    /// Registers an object and/or it's class 
    /// </summary>
    /// <typeparam name="T">The depdendency type</typeparam>
    /// <param name="strategy">The caching strategy</param>
    /// <param name="overwrite">If any already existing depdendencie's with this type and name should be overwritten</param>
    /// <param name="instance">An instance of the class for initialisation or atomic instances</param>
    /// <param name="name">An optional name to register multiple dependencies of the same type</param>
    /// <returns>This container for builder pattern</returns>
    IDependencyContainer Register<T>(DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instance = null, string name = null);

    /// <summary>
    /// Registers an object, it's class and it's interface as dependency
    /// </summary>
    /// <param name="type">The depdendency type</typeparam>
    /// <param name="strategy">The caching strategy</param>
    /// <param name="overwrite">If any already existing depdendencie's with this type and name should be overwritten</param>
    /// <param name="instance">An instance of the class for initialisation or atomic instances</param>
    /// <param name="name">An optional name to register multiple dependencies of the same type</param>
    /// <returns>This container for builder pattern</returns>
    IDependencyContainer Register(Type type, DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instance = null, string name = null);

    /// <summary>
    /// Registers an object, it's class and it's interface as dependency
    /// </summary>
    /// <param name="interfaceType">The interface type</typeparam>
    /// <param name="type">The depdendency type</typeparam>
    /// <param name="strategy">The caching strategy</param>
    /// <param name="overwrite">If any already existing depdendencie's with this type and name should be overwritten</param>
    /// <param name="instance">An instance of the class for initialisation or atomic instances</param>
    /// <param name="name">An optional name to register multiple dependencies of the same type</param>
    /// <returns>This container for builder pattern</returns>
    IDependencyContainer Register(Type type, Type interfaceType, DependencyStrategy strategy = DependencyStrategy.SingleInstance, bool overwrite = false, object instance = null, string name = null);

    /// <summary>
    /// Returns all known instances assignable to the given type an their names
    /// </summary>
    /// <typeparam name="T">The type of instances to search for</typeparam>
    /// <param name="searchParents">Wheter the parents should be searched as well</param>
    /// <returns></returns>
    public IEnumerable<(T instance, string name)> GetAllKnownInstances<T>(bool searchParents = true);


    /// <summary>
    /// Inject dependencies in an already existing object using attributes
    /// </summary>
    /// <param name="obj">The object to inject into</param>
    void PostInject(object obj);


    /// <summary>
    /// Get the strategy of a registered type
    /// </summary>
    /// <param name="type">Type</param>
    /// <param name="name">optional name of the type</param>
    /// <returns>DependencyStrategy of a known type or null if no type was found</returns>
    DependencyStrategy? GetDependencyStrategy(Type type, string name = null);
  }
}
