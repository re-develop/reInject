using System;

namespace ReInject.Interfaces
{
  public interface IServiceProvider : System.IServiceProvider
  {
    object GetInstance(Type type, string name = null);
    T GetInstance<T>(Action<T> action = null, string name = null);
    void Clear();
    string Name { get; }
  }
}
