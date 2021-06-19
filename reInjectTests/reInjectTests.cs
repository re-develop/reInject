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

  public class EventSource
  {
    public delegate int TestDelegate(int num, string test, object obj1, object obj2, TestData obj3);
    public event TestDelegate TestEvent;
    public event Action<string, int> SimpleEvent;

    public struct TestData
    {
      public int Field1;
      public string Field2;
    }

    public int CallEvent(int num)
    {
      SimpleEvent?.Invoke("test", num);
      return TestEvent?.Invoke(num, "lol", null, null, new TestData() { Field1 = 13, Field2 = "test" }) ?? -1;
    }
  }

  public class EventTarget
  {
    public int LastEventValue { get; private set; }
    public string LastSimpleEventValue { get; private set; }

    [InjectEvent("test")]
    public int HandleEvent(int num, string test, object obj1, object obj2, TestData obj3)
    {
      LastEventValue = num;
      return num * 2;
    }

    [InjectEvent("simple")]
    public void HandleSimple(string str, int num)
    {
      LastSimpleEventValue = str + num;
    }

  }


  public class ReInjectIntegrationTests
  {

    [Fact]
    public void ReInject_TestEventInjection_EventCallbackWorks()
    {
      // Arrange
      int num = 1337;
      var source = new EventSource();
      var target = new EventTarget();

      var proxy = new EventProxy(source, nameof(source.TestEvent), "test");
      var proxy2 = new EventProxy(source, nameof(source.SimpleEvent), "simple");
      var proxyTarget = new EventProxyTarget(target, ReflectionHelper.GetMember<MethodInfo>(target.GetType(), "HandleEvent"));
      var proxyTarget2 = new EventProxyTarget(target, ReflectionHelper.GetMember<MethodInfo>(target.GetType(), "HandleSimple"));
      proxy.AddTarget(proxyTarget);
      proxy2.AddTarget(proxyTarget2);

      // Act
      var res = source.CallEvent(num);

      // Assert
      Assert.Equal(num, target.LastEventValue);
      Assert.Equal(num * 2, res);
      Assert.Equal($"test{num}", target.LastSimpleEventValue);
    }

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
      container.Register<int>(DependencyStrategy.AtomicInstance, true, num, "num");

      // Act
      var instance = container.GetInstance<ReadOnlyPropertyClass>();

      // Assert
      Assert.Equal(num, instance.Property);

    }


    [Fact]
    public void ReInject_TestEventInjection_DisablingEventsWorks()
    {
      // Arrange
      var container = Injector.GetContainer(Guid.NewGuid().ToString());
      int num = 1337;
      var source = new EventSource();
      container.RegisterEventSource(source, "TestEvent", "test");

      // Act
      var target = container.GetInstance<EventTarget>();
      var res0 = source.CallEvent(num);
      container.SetEventTargetEnabled(target, false, "test");
      var res1 = source.CallEvent(num);
      container.SetEventTargetEnabled(target, true);
      var res2 = source.CallEvent(num);



      // Assert
      Assert.Equal(num, target.LastEventValue);
      Assert.Equal(num * 2, res0);
      Assert.Equal(default(int), res1);
      Assert.Equal(num * 2, res2);
    }

    [Fact]
    public void ReInject_TestEventInjection_AttributeBasedInjectionWorks()
    {
      // Arrange
      var container = Injector.GetContainer(Guid.NewGuid().ToString());
      int num = 1337;
      var source = new EventSource();
      container.RegisterEventSource(source, "TestEvent", "test");

      // Act
      var target = container.GetInstance<EventTarget>();
      var res = source.CallEvent(num);


      // Assert
      Assert.Equal(num, target.LastEventValue);
      Assert.Equal(num * 2, res);
    }

    [Fact]
    public void ReInject_HandelsAbstract()
    {
      var container = Injector.GetContainer(Guid.NewGuid().ToString());
      container.Register<IHelloService>(DependencyStrategy.AtomicInstance, true, new HelloService());
      container.GetInstance<StupidCtor>();
    }

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
      container.Register<IHelloService, HelloService>().Register<GoodByeService>(DependencyStrategy.NewInstance);
      var inst = container.GetInstance<Greeter>();
      container.Register<IHelloService, HelloService2>(name: "hello");
      container.PostInject(inst);
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
