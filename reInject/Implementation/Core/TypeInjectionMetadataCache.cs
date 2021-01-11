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
  public class TypeInjectionMetadataCache
  {
    private static Dictionary<Type, TypeInjectionMetadataCache> _cache = new Dictionary<Type, TypeInjectionMetadataCache>();

    public static void ClearCache()
    {
      _cache.Clear();
    }

    public static void UpdateCache()
    {
      foreach (var val in _cache.Values)
        val.CalculateInjectionMetadata();
    }

    public static TypeInjectionMetadataCache GetMetadataCache(Type type)
    {
      if (_cache.ContainsKey(type) == false)
        _cache[type] = new TypeInjectionMetadataCache(type);

      return _cache[type];
    }

    private Dictionary<MemberInfo, Type> _members = new Dictionary<MemberInfo, Type>();
    private Dictionary<ICustomAttributeProvider, InjectAttribute> _memberAttributes = new Dictionary<ICustomAttributeProvider, InjectAttribute>();
    private Dictionary<MemberInfo, Action<object, object>> _setters = new Dictionary<MemberInfo, Action<object, object>>();
    private Dictionary<ConstructorInfo, ParameterInfo[]> _constructors = new Dictionary<ConstructorInfo, ParameterInfo[]>();

    public Type CachedType { get; init; }

    private TypeInjectionMetadataCache(Type type)
    {
      CachedType = type;
      CalculateInjectionMetadata();
    }

    public object CreateInstance(IDependencyContainer container)
    {
      var ctor = _constructors.Where(x => x.Value.All(y => y.HasDefaultValue || container.IsKnownType(y.ParameterType, _memberAttributes.GetValueOrDefault(y)?.Name))).OrderByDescending(x => x.Value.Count()).FirstOrDefault().Key;
      object inst;
      if (ctor != null)
      {
        inst = ctor.Invoke(ctor.GetParameters().Select(x =>
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
        }).ToArray());
      }
      else
      {
        inst = FormatterServices.GetUninitializedObject(CachedType);
      }


      foreach (var member in _members)
        if (container.IsKnownType(member.Value, _memberAttributes.GetValueOrDefault(member.Key)?.Name))
          _setters[member.Key](inst, container.GetInstance(member.Value, _memberAttributes.GetValueOrDefault(member.Key)?.Name));

      return inst;
    }

    public void CalculateInjectionMetadata()
    {
      _members.Clear();
      _constructors.Clear();

      var fields = ReflectionHelper.GetAllMembers(CachedType, MemberTypes.Field).Cast<FieldInfo>().Select(x => (info: x, attribute: x.GetCustomAttribute<InjectAttribute>()));
      var props = ReflectionHelper.GetAllMembers(CachedType, MemberTypes.Property).Cast<PropertyInfo>().Select(x => (info: x, attribute: x.GetCustomAttribute<InjectAttribute>()));
      var ctors = ReflectionHelper.GetAllMembers(CachedType, MemberTypes.Constructor).Cast<ConstructorInfo>();

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
        if(prop.info.CanWrite)
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
        foreach(var parameter in parameters)
        {
          var attribute = parameter.GetCustomAttribute<InjectAttribute>();
          if (attribute != null)
            _memberAttributes[parameter] = attribute;
        }
      }
    }
  }
}
