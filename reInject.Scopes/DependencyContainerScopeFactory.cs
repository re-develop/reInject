using Microsoft.Extensions.DependencyInjection;
using ReInject.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.Scopes
{
  public class DependencyContainerScopeFactory : IServiceScopeFactory
  {
    private IDependencyContainer _container;

    public DependencyContainerScopeFactory(IDependencyContainer container)
    {
      _container = container;
    }

    public IServiceScope CreateScope()
    {
      return new DependencyContainerScope(_container);
    }
  }
}
