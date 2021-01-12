# reInject
reInject is a leightweight and flexible depedendency injection framework.

## Basic Usage
```csharp
class Greeter
{
  [Inject(Name = "hello")] // search for service of type IHelloService with name hello
  private IHelloService _helloService;

  [Inject(Type = typeof(HelloService2))] // search for service with type HelloService2 (overwrites autodetected type from field)
  private IHelloService _helloService2;

  [Inject] // detect type from field/property use default dependency (name = null)
  public IHelloService HelloService { get; } // can also handle readonly properties

  [Inject(Name="specific instance")] // Inject attributes are handeld after object construction and therby overwrite constructor set properties/fields
  private IHelloService _helloService3;
  
  private GoodByeService _goodByeService;

  private string _message;
  public Greeter(GoodByeService service, [Inject(Name = "otherService")]GoodByeService otherService, IHelloService genericInstance) // constructor injection also works
  { 
    _goodByeService = service;  
    _helloService3 = genericInstance;
  }
  
  public Greeter(GoodByeService service) // constructor overloading is handeld as well, always the constructor with the most known dependencies of the container is taken
  {
    _goodByeService = service; 
  }
}


var container = Injector.GetContainer(); // gets the default container where name is null, containers are compatible with System.IServiceProvider
container
  .Register<IHelloService, HelloService>() // adds IHelloService & HelloService to collection which both resolve to HelloService with strategy SingleInstance
  .Register<IHelloService, HelloService>(DependencyStrategy.AtomicInstance, true, new HelloService(), "hello") // registers an AtomicInstance
  .Register<HelloService2>(DependencyStrategy.CachedInstance) // register caches instance
  .Register<GoodByeService>()
  .Register<GoodByeService>(DependencyStrategy.AtomicInstance, true, new GoodByeService("Custom goodbye message"), "otherService"); // register second goodbyeservice which for example has specific settings and should be accessible with a name

Greeter instance = container.GetInstance<Greeter>(); // gets instance via generic parameter
object instance2 = container.GetInstance(typeof(Greeter)); // gets instance via type
var instance3 = container.GetInstance<Greeter>(greeter => { // gets instance via generic and calls passed configuration action
	greeter.ConfigureStuff(); 
});
  
var otherContainer = Injector.GetContainer("other", container); // creates a new container with the name other and set the parent as the already existing container. If a container doens't know a type it searches it's parent for it, parent can be null. Parent is only set if container with the given name is first created
Injector.RemoveContainer("other"); // remove container other from cache, if GetContainer with name other is called again a new container will be created
container.Clear(); // clear all cached instances of registered classes except AtomicInstance 
```
## Overview over `DependencyStrategy` enum

| Type           | Usecase                                                                                                                                | Initial Instance |
|----------------|----------------------------------------------------------------------------------------------------------------------------------------|------------------|
| SingleInstance | If no instance is passed one will be created and kept                                                                                  | Optional         |
| AtomicInstance | Always keeps the same instance, works like a singleton, ignores cache cleans                                                           | Required         |
| CachedInstance | Keeps a reference to the calculated or passes value with a WeekReference, only get new instantiated if old value was garbage collected | Optional         |
| NewInstance    | Always returns a new instance                                                                                                          | Ignored          |
