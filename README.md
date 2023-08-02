# reInject
reInject is a leightweight and flexible depedendency injection framework.

## Basic Usage
```csharp
class GreeterBasic
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
  .AddLazySingleton<IHelloService, HelloService>() // registers a lazy evaluated singleton which resolves for IHelloService with the implementation HelloService
  .AddSingleton<IHelloService, HelloService>(new HelloService(), true, "hello") // registers a lazy evaluated singleton which resolves for IHelloService with the implementation HelloService with the specific name "hello"
  .AddCached<HelloService2>() // registers caches instance
  .AddSingleton<GoodByeService>() // adds GoodByeService as singleton, creating a instance as the function is called using the container
  .AddSingleton<GoodByeService>(new GoodByeService("Custom goodbye message"), true, "otherService"); // register second goodbyeservice as a singleton which for example has specific settings and should be accessible with a name

Greeter instance = container.GetInstance<Greeter>(); // gets instance via generic parameter
object instance2 = container.GetInstance(typeof(Greeter)); // gets instance via type
var instance3 = container.GetInstance<Greeter>(greeter => { // gets instance via generic and calls passed configuration action
	greeter.ConfigureStuff(); 
});
  
var otherContainer = Injector.GetContainer("other", container); // creates a new container with the name other and set the parent as the already existing container. If a container doens't know a type it searches it's parent for it, parent can be null. Parent is only set if container with the given name is first created
Injector.RemoveContainer("other"); // remove container other from cache, if GetContainer with name other is called again a new container will be created
container.Clear(); // clear all cached instances of registered classes except AtomicInstance 
```
## Overview over Service Lifetime

| Type           | Description                                                                                                                                        | Clearable  |
|----------------|----------------------------------------------------------------------------------------------------------------------------------------------------|------------|
| LazySingleton  | Singleton which gets created as soone as it's needed for the first time, gets cleared if clear is called on the container                          | Yes        |
| Singleton      | Singleton which gets created when add is called if no instance is passed                                                                           | No         |
| Cached         | Keeps a WeekReference to the laste constructed value, only creates new instance if week reference was garbage collected or container was cleared   | Yes        |
| Transient      | Always returns a new instance                                                                                                                      | No         |

> For scoped services install the extension Aeolin.reInject.Scopes
> Scopes automatically keep track of all disposable instance created during the scope, clearing only them
> Singletons are only disposed if they were added to the scope itself

## Overview over EventInjectAttribute
With event injection events can be subscribed via DependencyInjection. For that an eventsource with an unique name has to be registered  by calling `IDependencyContainer.AddEvenInjector(setup => setup.RegisterEventSource())`. 
The nuget package Aeolin.reInject.PostInjectors.EventInjections is required for this to work
To inject an event an object got via `IDependencyContainer.GetInstance<T>` must have an method with the same signature as the event must be marked with an InjectEvent attribute where the name parameter must be the same as the `eventName` used for registering the eventsource. 

```csharp
public class EventSource
{
  public delegate int TestDelegate(int num, string test, object obj1, object obj2, object obj3);
  public event TestDelegate TestEvent;

  public int CallEvent(int num)
  {
    return TestEvent?.Invoke(num, "test", null, null, null) ?? -1;
  }
}

public class EventTarget
{
  public int LastEventValue { get; private set; }

  [InjectEvent("UniqueEventName", Priority = 1)]
  public int HandleEvent(int num, string test, object obj1, object obj2, object obj3)
  {
    LastEventValue = num;
    return num * 2;
  }
}

var container = Injector.GetContainer();
var source = new ObjectWithEvents();
_container.AddEventInjector(setup =>
{
  setup.RegisterEventSources(source, "Prefix:");
});
var target = container.GetInstance<EventTarget>();
var result = source.CallEvent(5); 
// target.LastEventValue is now 5
// result should be 10
```
### InjectEventAttribute Priority
The higher the given Priority is the sooner an handler will be called if multiple handlers are subscribed to the same event. If multiple targets are subscribed to the same event and the handle method has a return value, the value returned by the target with the lowest priority is returned
