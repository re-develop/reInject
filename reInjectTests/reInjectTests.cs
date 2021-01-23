using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using ReInject;
using ReInject.Implementation.Attributes;
using ReInject.Implementation.DependencyTypes;
using ReInject.Utils;
using Xunit;

namespace ReInjectTests
{
  public class ReInjectIntegrationTests
  {
    [Fact]
    public void ReInject_CreatesIntance_IfTypesAreGiven()
    {
      var container = Injector.GetContainer(Guid.NewGuid().ToString()); // get independent container
      container.Register<IHelloService, HelloService>().Register<GoodByeService>(DependencyStrategy.NewInstance);
      var inst = container.GetInstance<Greeter>();
      var msg = inst.Greet();

      Assert.Equal("Hello World\nI am Greeter\nGood Bye", msg);
    }


    [Fact]
    public void ReInject_UsesNamedParameter_IfTypesAndNameAreGiven()
    {
      var container = Injector.GetContainer(Guid.NewGuid().ToString());// get independent container
      container.Register<IHelloService, HelloService>().Register<IHelloService, HelloService2>(name: "hello").Register<GoodByeService>(DependencyStrategy.NewInstance);
      var inst = container.GetInstance<Greeter>();
      var msg = inst.Greet();

      Assert.Equal("Hello Matrix\nI am Greeter\nGood Bye", msg);
    }

    [Fact]
    public void NewInstance_AlwayReturnsOtherInstance_OnGetInstance()
    {
      // Setup
      var container = Injector.GetContainer();
      var dep = new NewInstanceDependency(container, typeof(HelloService), typeof(IHelloService));

      // Act
      var inst1 = dep.Instance;
      var inst2 = dep.Instance;



      // Assert
      Assert.NotEqual(inst1, inst2);
    }

    [Fact]
    public void InjectAttributeType_OverridesAutomaticDetectedType_IfAttributeFieldTypeIsSet()
    {
      var container = Injector.GetContainer(Guid.NewGuid().ToString());// get independent container
      container.Register<IHelloService, HelloService>().Register<HelloService2>().Register<GoodByeService>(DependencyStrategy.NewInstance);
      var inst = container.GetInstance<Greeter2>();
      var msg = inst.Greet();

      Assert.Equal("Hello Matrix", msg);
    }

    [Fact]
    public void ReflectionHelper_GetAllMembersFilter_FiltersCorrectly()
    {
      var members = ReflectionHelper.GetAllMembers<Greeter>(MemberTypes.Field | MemberTypes.Property);
      Assert.DoesNotContain(members, x => x.MemberType != MemberTypes.Field && x.MemberType != MemberTypes.Property);
    }
  }

  class ReferenceHolder
  {
    public object Reference { get; set; }
  }

  class Greeter2
  {
    [Inject(Type = typeof(HelloService2))]
    private IHelloService _helloService;

    public string Greet()
    {
      return _helloService.SayHello();
    }
  }

  interface IHelloService
  {
    string SayHello();
  }

  class HelloService : IHelloService
  {
    public string SayHello()
    {
      return "Hello World";
    }
  }

  class HelloService2 : IHelloService
  {
    public string SayHello()
    {
      return "Hello Matrix";
    }
  }

  class GoodByeService
  {
    public string SayGoodBye()
    {
      return "Good Bye";
    }
  }

  class Greeter
  {
    [Inject(Name = "hello")]
    private IHelloService _helloService;

    [Inject]
    private GoodByeService _goodByeService;

    private string _message;
    public Greeter() { }

    public Greeter(IHelloService helloService)
    {
      this._helloService = helloService;
    }

    public Greeter(IHelloService helloService, string message = "I am Greeter")
    {
      this._helloService = helloService;
      this._message = message;
    }

    public string Greet()
    {
      return string.Join("\n", _helloService.SayHello(), _message, _goodByeService.SayGoodBye());
    }
  }
}
