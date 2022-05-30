using ReInject;
using ReInject.Interfaces;
using ReInject.PostInjectors.BackgroundWorker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace reInject.PostInjectors.BackgroundWorker
{
  public static class Extensions
  {
    public static IDependencyContainer AddBackgroundWorker(this IDependencyContainer container, Action<BackgroundWorkerInjector> setup = null, string name = null)
    {
      var injector = new BackgroundWorkerInjector();
      if(setup != null)
        setup(injector);

      container.Register<IBackgroundWorkerManager>(DependencyStrategy.AtomicInstance, true, injector, name);
      container.RegisterPostInjector(injector, true);
      return container;
    }
  }
}
