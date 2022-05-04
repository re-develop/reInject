using ReInject.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ReInject.Utils;
using Xunit;
using ReInject;

namespace reInjectTests
{
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
  public class TestInjectorAttribute : Attribute
  {
    public TestInjectorAttribute(string parameter)
    {
      this.CallParameter = parameter;
    }

    public string CallParameter { get; set; }
  }

  public class TestInjector : IPostInjector
  {
    public int Priority { get; set; }
    public string Name { get; init; } = Guid.NewGuid().ToString();
    private List<Callable> _targets = new List<Callable>();


    private class Callable
    {
      public Callable(object instance, MethodInfo method, string parameter)
      {
        this.Instance = instance;
        this.Enabled = true;
        this.Parameter = parameter;
        this.Action = method.CreateDelegate<Action<string, string>>(instance);
      }

      public object Instance;
      public Action<string, string> Action { get; init; }
      public string Parameter { get; init; }
      public bool Enabled { get; set; }

      public void Call(string message)
      {
        if (Enabled)
          Action(Parameter, message);
      }
    }

    public void Call(string message)
    {
      _targets.ForEach(x => x.Call(message));
    }

    public void Dispose()
    {
      _targets.Clear();
    }

    public IEnumerable<MemberInfo> PostInject(IDependencyContainer container, Type type, object instance)
    {
      foreach (var method in ReflectionHelper.GetAllMembers(type, MemberTypes.Method).Cast<MethodInfo>())
      {
        var attr = method.GetCustomAttribute<TestInjectorAttribute>();
        if (attr != null)
        {
          _targets.Add(new Callable(instance, method, attr.CallParameter));
          yield return method;
        }

      }
    }

    public bool SetInjectionEnabled(object instance, bool enabled)
    {
      var callables = _targets.Where(x => x.Instance == instance).ToList();
      callables.ForEach(x => x.Enabled = enabled);
      return callables.Count() > 0;
    }
  }

  public class PostInjectorDummy
  {
    public int CountCalled { get; private set; }
    public string LastParameter { get; private set; }
    public string LastMessage { get; private set; }

    [TestInjector("Target1")]
    public void Call(string parameter, string message)
    {
      CountCalled++;
      LastParameter = parameter;
      LastMessage = message;
    }
  }

  public class PostInjectionTests
  {
    [Fact]
    public void PostInjectors_GetsInjected_AndCallTarget()
    {
      // Arrange
      var container = Injector.GetContainer(Guid.NewGuid().ToString());
      var injector = container.RegisterPostInjector<TestInjector>();

      // Act
      var instance = container.GetInstance<PostInjectorDummy>();
      injector.Call("Hello World");

      // Assert
      Assert.Equal(1, instance.CountCalled);
    }
  }
}
