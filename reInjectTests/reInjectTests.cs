using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ReInject;
using ReInject.Implementation.DependencyTypes;
using Xunit;

namespace ReInjectTests
{
  public class ReInjectIntegrationTests
  {
    [Fact]
    public void ReInject_CreatesIntance_IfTypesAreGiven()
    {
      var container = Injector.GetContainer();
      container.Register<IHelloService, HelloService>().Register<GoodByeService>(DependencyStrategy.NewInstance);
      var inst = Injector.GetProvider().GetInstance<Greeter>();
      var msg = inst.Greet();

      Assert.Equal("Hello World\nI am Greeter\nGood Bye", msg);
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
  }

  class ReferenceHolder
  {
    public object Reference { get; set; }
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

  class GoodByeService
  {
    public string SayGoodBye()
    {
      return "Good Bye";
    }
  }

  class Greeter
  {
    private IHelloService _helloService;
    private GoodByeService _goodByeService;
    private string _message;
    public Greeter() { }

    public Greeter(IHelloService helloService, GoodByeService goodByeService)
    {
      this._helloService = helloService;
      this._goodByeService = goodByeService;
    }

    public Greeter(IHelloService helloService, GoodByeService goodByeService, string message = "I am Greeter")
    {
      this._helloService = helloService;
      this._goodByeService = goodByeService;
      this._message = message;
    }

    public string Greet()
    {
      return string.Join("\n", _helloService.SayHello(), _message, _goodByeService.SayGoodBye());
    }
  }
}
