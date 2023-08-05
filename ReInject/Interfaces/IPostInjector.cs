using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.Interfaces
{
  public interface IPostInjector : IDisposable
  {
    public IEnumerable<MemberInfo> PostInject(IDependencyContainer container, Type type, object instance);
    public bool SetInjectionEnabled(object instance, bool enabled);
    public int Priority { get; }
    public string Name { get; }
  }
}
