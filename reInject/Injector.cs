using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReInject.Implementation;
using ReInject.Interfaces;

namespace ReInject
{
  /// <summary>
  /// Main class to interact with the reInject framework
  /// </summary>
  public class Injector
  {
    // internal container cache 
    private static List<IDependencyContainer> _instances = new List<IDependencyContainer>();

    /// <summary>
    /// Clear all cached containers
    /// </summary>
    public static void ClearCache()
    {
      _instances.Clear();
    }

    /// <summary>
    /// Remove all containers with the given name
    /// </summary>
    /// <param name="name">The name of the containers to be removed</param>
    public static void Remove(string name )
    {
      _instances.RemoveAll( x => x.Name == name );
    }

    /// <summary>
    /// returns a container for the given name or creates a new one if none exists
    /// </summary>
    /// <param name="name">The name of the container, null is the default container</param>
    /// <param name="parent">Parent container to add if new container is created</param>
    /// <returns>The container for the given name</returns>
    public static IDependencyContainer GetContainer( string name = null, IDependencyContainer parent = null )
    {
      var inst = _instances.FirstOrDefault( x => x.Name == name );
      if( inst == null )
      {
        inst = new DependencyContainer( name, parent );
        _instances.Add( inst );
      }

      return inst;
    }

    /// <summary>
    /// returns a container hidden behind IServiceProvider interface for the given name or creates a new one if none exists
    /// </summary>
    /// <param name="name">The name of the container, null is the default container</param>
    /// <returns>The container for the given name</returns>
    public static Interfaces.IServiceProvider GetProvider( string name = null )
    {
      return GetContainer( name );
    }
  }
}
