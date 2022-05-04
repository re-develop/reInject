using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.Interfaces
{
  public interface IPostInjectProvider
  {
    /// <summary>
    /// Registers a PostInjector, PostInjectors are also available as Atomic Instances
    /// </summary>
    /// <param name="injector">An instance of the PostInjector</param>
    /// <param name="overwrite">Wheter a already registerd PostInjector identified by the given params should be replaced with the given instance</param>
    /// <returns>If the injector was registered</returns>
    public bool RegisterPostInjector(IPostInjector injector, bool overwrite = false);

    /// <summary>
    /// Registers a PostInjector of Type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of the post injector</typeparam>
    /// <param name="configure">Action to configure the created instance</param>
    /// <param name="overwrite">Wheter a already registerd PostInjector identified by the given params should be replaced with the given instance</param>
    /// <returns>The instance of the created injector</returns>
    public T RegisterPostInjector<T>(Action<T> configure = null, bool overwrite = false) where T : IPostInjector;

    /// <summary>
    /// Unregisters all PostInjectors assignalbe to the given Type <typeparamref name="T"/> matching the given <paramref name="name"/> if it isnt null
    /// </summary>
    /// <param name="name">Optional name to tighten the search</param>
    /// <typeparam name="T">The type of the post injector</typeparam>
    public void UnregisterPostInjector<T>(string name = null) where T : IPostInjector;

    /// <summary>
    /// Unregisters the PostInjector identified by instance
    /// </summary>
    /// <param name="injector">The instance of the PostInjector</param>
    public void UnregisterPostInjector(IPostInjector injector);


    /// <summary>
    /// Sets if all available post injectors for a given object instance are enabled
    /// </summary>
    /// <param name="instance">The object instance</param>
    /// <param name="enabled">If the PostInjectors for this instance are enabled</param>
    public void SetPostInjectionsEnabled(object instance, bool enabled);

    /// <summary>
    /// Returns a IEnumerable containing all available PostInjectors in the current provider
    /// </summary>
    /// <returns></returns>
    public IEnumerable<IPostInjector> GetPostInjecors(); 
  }
}
