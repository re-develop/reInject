using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.PostInjectors.EventInjection
{
  public interface IEventProvider
  {
    /// <summary>
    /// Registers an event of an specific object as dependency so it later can be injected via InjectEventAttribute
    /// </summary>
    /// <param name="sender">Object which provides the event</param>
    /// <param name="bindTo">Name of the event to bind to</param>
    /// <param name="eventName">Unique name for dependency</param>
    /// <param name="overwrite">If an already existing event should be overwritten or not</param>
    public IEventProvider RegisterEventSource(object sender, string bindTo, string eventName, bool overwrite = false);

    /// <summary>
    /// Registers all events of an specific object as dependency so it later can be injected via InjectEventAttribute
    /// </summary>
    /// <param name="sender">Object which dispatches the events</param>
    /// <param name="prefix">A common prefix for all event names</param>
    /// <param name="nameFunc">An optional function to generate EventNames from the EventInfo, if null is given the name of the EventInfo will be used</param>
    /// <param name="filterFunc">An optional function to filter out EventInfos from registration</param>
    /// <param name="overwriteFunc">An optional function to check if an EventInfo should be overwriting an already existing one, by default nothing will be overwritten</param>
    public IEventProvider RegisterEventSources(object sender, string prefix, Func<EventInfo, string> nameFunc = null, Func<EventInfo, bool> filterFunc = null, Func<EventInfo, bool> overwriteFunc = null);

    /// <summary>
    /// Unregisters an eventsource an all injected handlers
    /// </summary>
    /// <param name="uniqueEventName">The unique name given of the event</param>
    public IEventProvider UnregisterEventSource(string uniqueEventName);

    /// <summary>
    /// Unregisters all eventsources provided by the given object
    /// </summary>
    /// <param name="sender">The object which eventsources should be unregistered</param>
    public IEventProvider UnregisterEventSources(object sender);

    /// <summary>
    /// Unregisters all eventsources of all object of type T
    /// </summary>
    /// <typeparam name="T">The Type to search eventsources by</typeparam>
    public IEventProvider UnregisterEventSources<T>();

    /// <summary>
    /// Registers an method to receive events of a given source
    /// </summary>
    /// <param name="eventName">The Unique name of the eventsource</param>
    /// <param name="instance">The object which should receive the events</param>
    /// <param name="info">The target method which should be invoked</param>
    /// <returns>If an event with name <paramref name="eventName"/> exists and could be bound to the Target</returns>
    /// <exception cref="ArgumentException">Thrown when the signature of <paramref name="info"/> missmatches with the eventsource</exception>
    public bool RegisterEventTarget(string eventName, object instance, MethodInfo info);

    /// <summary>
    /// Sets if an EventTarget is enabled
    /// </summary>
    /// <param name="target">The target object</param>
    /// <param name="enabled">Wheter the object will receive events or not</param>
    /// <param name="events">A list of events to enable/disable if empty or null all events are targeted</param>
    public void SetEventTargetEnabled(object target, bool enabled, params string[] events);
  }
}
