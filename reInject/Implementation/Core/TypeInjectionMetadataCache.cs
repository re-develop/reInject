using ReInject.Implementation.Attributes;
using ReInject.Interfaces;
using ReInject.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace ReInject.Core
{
  /// <summary>
  /// Class to cache metadata used by the depdendency container
  /// </summary>
  public class TypeInjectionMetadataCache
  {
    private static Dictionary<Type, TypeInjectionMetadataCache> _cache = new Dictionary<Type, TypeInjectionMetadataCache>();

    /// <summary>
    /// Clear all cached types
    /// </summary>
    public static void ClearCache()
    {
      _cache.Clear();
    }

    /// <summary>
    /// Recalculate all cached types
    /// </summary>
    public static void UpdateCache()
    {
      foreach (var val in _cache.Values)
        val.CalculateInjectionMetadata();
    }

    /// <summary>
    /// Get Metadata for a given type
    /// </summary>
    /// <param name="type">The type to get metadata from</param>
    /// <returns>Generated metadata</returns>
    public static TypeInjectionMetadataCache GetMetadataCache(Type type)
    {
      if (_cache.ContainsKey(type) == false)
        _cache[type] = new TypeInjectionMetadataCache(type);

      return _cache[type];
    }

    // keeps track of all injection relevant class members
    private Dictionary<MemberInfo, Type> _members = new Dictionary<MemberInfo, Type>();

    // keeps track of all InjectAttributes 
    private Dictionary<ICustomAttributeProvider, InjectAttribute> _memberAttributes = new Dictionary<ICustomAttributeProvider, InjectAttribute>();

    // keeps track of all setter functions for the given members
    private Dictionary<MemberInfo, Action<object, object>> _setters = new Dictionary<MemberInfo, Action<object, object>>();

    // keeps track of all possible constructors
    private Dictionary<ConstructorInfo, ParameterInfo[]> _constructors = new Dictionary<ConstructorInfo, ParameterInfo[]>();

    // keeps track of all bindable events
    private List<(MethodInfo method, InjectEventAttribute attribute)> _bindableEvents = new List<(MethodInfo, InjectEventAttribute)>();

    /// <summary>
    /// Returns the type this metadata was generated for
    /// </summary>
    public Type CachedType { get; init; }

    // hidden ctor should only be called by the static method GetMetadataCache
    private TypeInjectionMetadataCache(Type type)
    {
      CachedType = type;
      CalculateInjectionMetadata();
    }

    /// <summary>
    /// Creates an instance of the type and injects depdendencies from the given container
    /// </summary>
    /// <param name="container">The dependency container</param>
    /// <returns>An instance of CachedType with all dependencies injected contained in <paramref name="container"/></returns>
    public object CreateInstance(IDependencyContainer container)
    {
      var ctor = _constructors.Where(x => x.Value.All(y => y.HasDefaultValue || container.IsKnownType(y.ParameterType, _memberAttributes.GetValueOrDefault(y)?.Name))).OrderByDescending(x => x.Value.Count()).FirstOrDefault().Key;
      object inst;
      if (ctor != null)
      {
        var parameters = ctor.GetParameters().Select(x =>
        {
          if (container.IsKnownType(x.ParameterType, _memberAttributes.GetValueOrDefault(x)?.Name))
          {
            return container.GetInstance(x.ParameterType, _memberAttributes.GetValueOrDefault(x)?.Name);
          }
          else if (x.HasDefaultValue == true)
          {
            return x.RawDefaultValue;
          }

          throw new Exception($"Couldn't resolve type for {x.ParameterType.Name} to call ctor of {CachedType.Name}");
        }).ToArray();
        inst = ctor.Invoke(parameters);
      }
      else
      {
        inst = FormatterServices.GetUninitializedObject(CachedType);
      }

      PostInject(container, inst);

      return inst;
    }

    public void PostInject(IDependencyContainer container, object obj)
    {
      foreach (var member in _members)
        if (container.IsKnownType(member.Value, _memberAttributes.GetValueOrDefault(member.Key)?.Name))
          _setters[member.Key](obj, container.GetInstance(member.Value, _memberAttributes.GetValueOrDefault(member.Key)?.Name));

      foreach (var @event in _bindableEvents)
      {
        if (container.RegisterEventTarget(@event.attribute.EventName, obj, @event.method) == false)
        {
          // TODO: log
        }
      }

      foreach(var injector in container.GetPostInjecors().OrderBy(x => x.Priority))
      {
        // TODO: log
        var members = injector.PostInject(container, CachedType, obj).ToList();
      }

    }

    /// <summary>
    /// Recalculates the cached metadata, call this if you've changed members of CachedType at runtime
    /// </summary>
    public void CalculateInjectionMetadata()
    {
      _members.Clear();
      _constructors.Clear();

      var fields = ReflectionHelper.GetAllMembers(CachedType, MemberTypes.Field).Cast<FieldInfo>().Select(x => (info: x, attribute: x.GetCustomAttribute<InjectAttribute>(true)));
      var props = ReflectionHelper.GetAllMembers(CachedType, MemberTypes.Property).Cast<PropertyInfo>().Select(x => (info: x, attribute: x.GetCustomAttribute<InjectAttribute>(true)));
      var events = ReflectionHelper.GetAllMembers(CachedType, MemberTypes.Method).Cast<MethodInfo>().SelectMany(x => x.GetCustomAttributes<InjectEventAttribute>(true).Select(a => (info: x, attribute: a)));
      var ctors = CachedType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

      foreach (var field in fields.Where(x => x.attribute != null))
      {
        _members[field.info] = field.attribute.Type ?? field.info.FieldType;
        _memberAttributes[field.info] = field.attribute;
        _setters[field.info] = (obj, val) => field.info.SetValue(obj, val);
      }

      foreach (var prop in props.Where(x => x.attribute != null))
      {
        _members[prop.info] = prop.attribute.Type ?? prop.info.PropertyType;
        _memberAttributes[prop.info] = prop.attribute;
        if (prop.info.CanWrite)
        {
          _setters[prop.info] = (obj, val) => prop.info.SetValue(obj, val);
        }
        else
        {
          var field = ReflectionHelper.GetMember<FieldInfo>(CachedType, $"<{prop.info.Name}>k__BackingField");
          if (field != null)
            _setters[prop.info] = (obj, val) => field.SetValue(obj, val);
        }
      }

      foreach (var ctor in ctors)
      {
        var parameters = ctor.GetParameters();
        _constructors[ctor] = parameters;
        foreach (var parameter in parameters)
        {
          var attribute = parameter.GetCustomAttribute<InjectAttribute>();
          if (attribute != null)
            _memberAttributes[parameter] = attribute;
        }
      }

      foreach (var @event in events.Where(x => x.attribute != null))
        _bindableEvents.Add((@event.info, @event.attribute));

    }
  }
}
