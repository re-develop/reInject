using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using ReInject;
using ReInject.Implementation.Attributes;
using ReInject.Implementation.Core;
using ReInject.Implementation.DependencyTypes;
using ReInject.PostInjectors.EventInjection;
using ReInject.Utils;
using Xunit;
using static ReInjectTests.EventSource;

namespace ReInjectTests
{

  abstract class TestAbsCtor
  {
    IHelloService _service;

    public TestAbsCtor(IHelloService service)
    {
      _service = service;
    }

  }

  class StupidCtor : TestAbsCtor
  {
    public StupidCtor(IHelloService service) : base(service)
    {
    }
  }

 


  public class ReInjectIntegrationTests
  {

  

    class ReadOnlyPropertyClass
    {
      [Inject(name: "num")]
      public int Property { get; } = 0;
    }

    [Fact]
    public void ReInject_TestReadonlyPropertyInjections_WorksIfDefaultGetOnlyProperty()
    {
      // Arrange
      var container = Injector.GetContainer(Guid.NewGuid().ToString());
      var num = 1337;
      container.AddSingleton(num, true, "num");

      // Act
      var instance = container.GetInstance<ReadOnlyPropertyClass>();

      // Assert
      Assert.Equal(num, instance.Property);

    }


   

    [Fact]
    public void ReInject_HandelsAbstract()
    {
      var container = Injector.GetContainer(Guid.NewGuid().ToString());
      container.AddSingleton<IHelloService, HelloService>(true);
      container.GetInstance<StupidCtor>();
    }

    [Fact]
    public void ReInject_CreatesIntance_IfTypesAreGiven()
    {
      var container = Injector.GetContainer(Guid.NewGuid().ToString()); // get independent container
      container.AddTransient<IHelloService, HelloService>()
        .AddTransient<GoodByeService>();
      var inst = container.GetInstance<Greeter>();
      var msg = inst.Greet();

      Assert.Equal("Hello World\nI am Greeter\nGood Bye", msg);
    }


    [Fact]
    public void ReInject_UsesNamedParameter_IfTypesAndNameAreGiven()
    {
      var container = Injector.GetContainer(Guid.NewGuid().ToString());// get independent container
      container.AddTransient<IHelloService, HelloService>().AddTransient<GoodByeService>();
      var inst = container.GetInstance<Greeter>();
      container.AddTransient<IHelloService, HelloService2>(name: "hello");
      container.PostInject(inst);
      var msg = inst.Greet();

      Assert.Equal("Hello Matrix\nI am Greeter\nGood Bye", msg);
    }

    [Fact]
    public void NewInstance_AlwayReturnsOtherInstance_OnGetInstance()
    {
      // Setup
      var container = Injector.GetContainer();
      var dep = new TransientDependency<HelloService>(container, null, typeof(IHelloService));

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
      container
        .AddTransient<IHelloService, HelloService>()
        .AddTransient<HelloService2>()
        .AddTransient<GoodByeService>();

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
