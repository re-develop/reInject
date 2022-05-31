using ReInject.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.PostInjectors.EventInjection
{
  public static class Extensions
  {
    public static IDependencyContainer AddEventInjector(this IDependencyContainer container, Action<EventInjector> setup, string name = null, int priority = 0, bool overwrite = false)
    {
      var injector = new EventInjector(name, priority);
      setup?.Invoke(injector);
      container.Register<IEventProvider>(DependencyStrategy.AtomicInstance, true, injector, name);
      container.RegisterPostInjector(injector, overwrite);
      return container;
    }


    public static IDependencyContainer SetEventTargetEnabled(this IDependencyContainer container, object target, bool enabled, params string[] events)
    {
      var injectors = container.GetPostInjecors().OfType<EventInjector>();
      var proxies = injectors.SelectMany(x => x.GetEventProxies(events));
      proxies.ToList().ForEach(x => x.SetTargetEnabled(target, enabled));
      return container;
    }



  }
}
