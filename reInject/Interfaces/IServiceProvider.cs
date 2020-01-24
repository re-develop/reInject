using System;

namespace ReInject.Interfaces
{
  public interface IServiceProvider
  {
    object GetInstance( Type type );
    T GetInstance<T>(Action<T> action = null);
    void Clear();
    string Name { get; }
  }
}
