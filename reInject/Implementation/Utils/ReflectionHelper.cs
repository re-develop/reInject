using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ReInject.Utils
{
  public class ReflectionHelper
  {
    // cache for compile accesors
    private static Dictionary<string, object> _caches = new Dictionary<string, object>();

    /// <summary>
    /// Clears the cache of comiled accessors
    /// </summary>
    public static void ClearCaches()
    {
      _caches.Clear();
    }

    /// <summary>
    /// Find all members of the type and types it inherits from
    /// </summary>
    /// <typeparam name="T">The type to get members from</typeparam>
    /// <param name="filter">A whitelist filter to only allow certain membertypes, default is All</param>
    /// <param name="flags">The bindingflags to use when searching for members</param>
    /// <returns>An array of all members matching the given criteria</returns>
    public static MemberInfo[] GetAllMembers<T>(MemberTypes filter = MemberTypes.All, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
    {
      return GetAllMembers(typeof(T), filter, flags);
    }

    /// <summary>
    /// Find all members of the type and types it inherits from
    /// </summary>
    /// <param name="type">The type to get members from</typeparam>
    /// <param name="filter">A whitelist filter to only allow certain membertypes, default is All</param>
    /// <param name="flags">The bindingflags to use when searching for members, default is Public | NonPublic | Instance</param>
    /// <returns>An array of all members matching the given criteria</returns>
    public static MemberInfo[] GetAllMembers(Type type, MemberTypes filter = MemberTypes.All, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
    {
      List<MemberInfo> infos = new List<MemberInfo>();
      do
      {
        infos.AddRange(type.GetMembers(flags).Where(member => filter.HasFlag(member.MemberType)));
        type = type.BaseType;
      } while (type != null);

      return infos.ToArray();
    }

    /// <summary>
    /// Get a single member from a given type
    /// </summary>
    /// <typeparam name="T">The type of member which is searched</typeparam>
    /// <param name="type">The type the searched member is in</param>
    /// <param name="name">The name of the member</param>
    /// <param name="flags">The bindingflags to use when searching for the member, default is Public | NonPublic | Instance</param>
    /// <returns>The member with the given name and type or null when type is not matching or no member with the given name is found</returns>
    public static T GetMember<T>(Type type, String name, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) where T : MemberInfo
    {
      do
      {
        var members = type.GetMember(name, flags);
        if (members != null && members.Length == 1)
        {
          var member = members[0];
          if (typeof(T).IsAssignableFrom(member.GetType()))
            return (T)member;
        }

        type = type.BaseType;
      } while (type != null);

      return null;
    }

    /// <summary>
    /// Returns the value of an member of an object by its membername
    /// </summary>
    /// <typeparam name="T">The type of the value to be returned</typeparam>
    /// <param name="obj">The object to extract the value from</param>
    /// <param name="name">The name of the member holding the value to get</param>
    /// <returns>The value of the member with the given name or null if no field or property with this name is found</returns>
    public static T Get<T>(object obj, string name)
    {
      var member = GetMember<MemberInfo>(obj.GetType(), name);
      if (member.MemberType == MemberTypes.Field)
      {
        FieldInfo info = (FieldInfo)member;
        return (T)info.GetValue(obj);
      }

      if (member.MemberType == MemberTypes.Property)
      {
        PropertyInfo info = (PropertyInfo)member;
        return (T)info.GetValue(obj);
      }
      return default(T);
    }

    /// <summary>
    /// Returns the value of an member of an object by its membername
    /// </summary>
    /// <param name="obj">The object to extract the value from</param>
    /// <param name="name">The name of the member holding the value to get</param>
    /// <returns>The value of the member with the given name or null if no field or property with this name is found</returns>
    public static object Get(object obj, string name)
    {
      var member = GetMember<MemberInfo>(obj.GetType(), name);
      if (member.MemberType == MemberTypes.Field)
      {
        FieldInfo info = (FieldInfo)member;
        return info.GetValue(obj);
      }

      if (member.MemberType == MemberTypes.Property)
      {
        PropertyInfo info = (PropertyInfo)member;
        return info.GetValue(obj);
      }
      return null;
    }

    /// <summary>
    /// Compiles a getter function to navigate through multiple fields/properties
    /// </summary>
    /// <param name="obj">The object to compile the function for</param>
    /// <param name="propertyPath">A path of properties and fields denoted by dots, i.e. MyObj.SomeObject.SomeField</param>
    /// <returns>A dynamic compile function to get the value behind the property path</returns>
    public static Func<object> GetUngenericGetter(object obj, string propertyPath)
    {
      Type type = obj.GetType();
      string id = $"GET:{type.FullName}:{propertyPath}";

      if (_caches.ContainsKey(id))
        return () => ((Func<object, object>)_caches[id])(obj);

      var dynamicGetter = new DynamicMethod("method__get_" + propertyPath.Replace('.', '-'), typeof(object), new Type[] { typeof(object) }, true);
      var g = dynamicGetter.GetILGenerator();
      g.Emit(OpCodes.Ldarg_0);
      g.Emit(OpCodes.Castclass, obj.GetType());
      foreach (var name in propertyPath.Split('.'))
      {
        var members = type.GetMember(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (members.Length != 1)
          throw new ArgumentException($"Invalid Property {name} on type {type}");

        var member = members[0];
        if (member.MemberType == MemberTypes.Property)
        {
          var info = (PropertyInfo)member;
          g.Emit(OpCodes.Callvirt, info.GetGetMethod(true));
          type = info.PropertyType;
        }

        if (member.MemberType == MemberTypes.Field)
        {
          var info = (FieldInfo)member;
          g.Emit(OpCodes.Ldfld, info);
          type = info.FieldType;
        }
      }

      g.Emit(type.IsClass ? OpCodes.Castclass : OpCodes.Box, type);
      g.Emit(OpCodes.Ret);

      Func<object, object> fun = (Func<object, object>)dynamicGetter.CreateDelegate(typeof(Func<object, object>));
      _caches[id] = fun;
      return () => fun(obj);
    }

    /// <summary>
    /// Compiles a getter function to navigate through multiple fields/properties
    /// </summary>
    /// <typeparam name="T">The return type of this getter</typeparam>
    /// <param name="obj">The object to compile the function for</param>
    /// <param name="propertyPath">A path of properties and fields denoted by dots, i.e. MyObj.SomeObject.SomeField</param>
    /// <returns>A dynamic compile function to get the value behind the property path</returns>
    public static Func<T> GetGetter<T>(object obj, string propertyPath)
    {
      Type type = obj.GetType();
      string id = $"UG:GET:{type.FullName}:{propertyPath}";

      if (_caches.ContainsKey(id))
        return () => ((Func<object, T>)_caches[id])(obj);

      var dynamicGetter = new DynamicMethod("method__get_" + propertyPath.Replace('.', '-'), typeof(T), new Type[] { typeof(object) }, true);
      var g = dynamicGetter.GetILGenerator();
      g.Emit(OpCodes.Ldarg_0);
      g.Emit(OpCodes.Castclass, obj.GetType());
      foreach (var name in propertyPath.Split('.'))
      {
        var members = type.GetMember(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (members.Length != 1)
          throw new ArgumentException($"Invalid Property {name} on type {type}");

        var member = members[0];
        if (member.MemberType == MemberTypes.Property)
        {
          var info = (PropertyInfo)member;
          g.Emit(OpCodes.Callvirt, info.GetGetMethod(true));
          type = info.PropertyType;
        }

        if (member.MemberType == MemberTypes.Field)
        {
          var info = (FieldInfo)member;
          g.Emit(OpCodes.Ldfld, info);
          type = info.FieldType;
        }
      }
      if (type.IsAssignableFrom(typeof(T)) == false)
        throw new ArgumentException($"Incompatible Types: {typeof(T).Name} & {type.Name}");

      g.Emit(OpCodes.Ret);

      Func<object, T> fun = (Func<object, T>)dynamicGetter.CreateDelegate(typeof(Func<object, T>));
      _caches[id] = fun;
      return () => fun(obj);
    }


    /// <summary>
    /// Compiles an dynamic method to set a value behind a given property path
    /// </summary>
    /// <param name="obj">The object the setter should operate on</param>
    /// <param name="propertyPath">A path of properties and fields denoted by dots, i.e. MyObj.SomeObject.SomeField</param>
    /// <returns>An action to set the value at the given property path</returns>
    public static Action<object> GetUngenericSetter(object obj, string propertyPath)
    {
      Type type = obj.GetType();
      string id = $"UG:SET:{type.FullName}:{propertyPath}";

      if (_caches.ContainsKey(id))
        return (t) => ((Action<object, object>)_caches[id])(obj, t);

      var dynamicGetter = new DynamicMethod("method__get_" + propertyPath.Replace('.', '-'), null, new Type[] { typeof(object), typeof(object) }, true);
      var g = dynamicGetter.GetILGenerator();
      g.Emit(OpCodes.Ldarg_0);
      g.Emit(OpCodes.Castclass, obj.GetType());
      var names = propertyPath.Split('.');

      for (int i = 0; i < names.Length; i++)
      {
        var name = names[i];

        var members = type.GetMember(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (members.Length != 1)
          throw new ArgumentException($"Invalid Property {name} on type {type}");

        var member = members[0];

        if (names.Length - 1 == i)
        {
          g.Emit(OpCodes.Ldarg_1);
          if (member.MemberType == MemberTypes.Property)
          {
            var info = (PropertyInfo)member;
            g.Emit(info.PropertyType.IsClass ? OpCodes.Castclass : OpCodes.Unbox_Any, info.PropertyType);
            g.Emit(OpCodes.Callvirt, info.GetSetMethod(true));
            type = info.PropertyType;
          }

          if (member.MemberType == MemberTypes.Field)
          {
            var info = (FieldInfo)member;
            g.Emit(info.FieldType.IsClass ? OpCodes.Castclass : OpCodes.Unbox_Any, info.FieldType);
            g.Emit(OpCodes.Stfld, info);
            type = info.FieldType;
          }
        }
        else
        {
          if (member.MemberType == MemberTypes.Property)
          {
            var info = (PropertyInfo)member;
            g.Emit(OpCodes.Callvirt, info.GetGetMethod(true));
            type = info.PropertyType;
          }

          if (member.MemberType == MemberTypes.Field)
          {
            var info = (FieldInfo)member;
            g.Emit(OpCodes.Ldfld, info);
            type = info.FieldType;
          }
        }
      }
      g.Emit(OpCodes.Ret);

      Action<object, object> fun = (Action<object, object>)dynamicGetter.CreateDelegate(typeof(Action<object, object>));
      _caches[id] = fun;
      return (t) => fun(obj, t);
    }

    /// <summary>
    /// Compiles an dynamic method to set a value behind a given property path
    /// </summary>
    /// <typeparam name="T">The type of the value to set</typeparam>
    /// <param name="obj">The object the setter should operate on</param>
    /// <param name="propertyPath">A path of properties and fields denoted by dots, i.e. MyObj.SomeObject.SomeField</param>
    /// <returns>An action to set the value at the given property path</returns>
    public static Action<T> GetSetter<T>(object obj, string propertyPath)
    {
      Type type = obj.GetType();
      string id = $"SET:{type.FullName}:{propertyPath}";

      if (_caches.ContainsKey(id))
        return (t) => ((Action<object, T>)_caches[id])(obj, t);

      var dynamicGetter = new DynamicMethod("method__get_" + propertyPath.Replace('.', '-'), null, new Type[] { typeof(object), typeof(T) }, true);
      var g = dynamicGetter.GetILGenerator();
      g.Emit(OpCodes.Ldarg_0);
      g.Emit(OpCodes.Castclass, obj.GetType());
      var names = propertyPath.Split('.');

      for (int i = 0; i < names.Length; i++)
      {
        var name = names[i];

        var members = type.GetMember(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (members.Length != 1)
          throw new ArgumentException($"Invalid Property {name} on type {type}");

        var member = members[0];

        if (names.Length - 1 == i)
        {
          g.Emit(OpCodes.Ldarg_1);
          if (member.MemberType == MemberTypes.Property)
          {
            var info = (PropertyInfo)member;
            g.Emit(OpCodes.Callvirt, info.GetSetMethod(true));
            type = info.PropertyType;
          }

          if (member.MemberType == MemberTypes.Field)
          {
            var info = (FieldInfo)member;
            g.Emit(OpCodes.Stfld, info);
            type = info.FieldType;
          }
        }
        else
        {
          if (member.MemberType == MemberTypes.Property)
          {
            var info = (PropertyInfo)member;
            g.Emit(OpCodes.Callvirt, info.GetGetMethod(true));
            type = info.PropertyType;
          }

          if (member.MemberType == MemberTypes.Field)
          {
            var info = (FieldInfo)member;
            g.Emit(OpCodes.Ldfld, info);
            type = info.FieldType;
          }
        }
      }
      if (type.IsAssignableFrom(typeof(T)) == false)
        throw new ArgumentException($"Incompatible Types: {typeof(T).Name} & {type.Name}");

      g.Emit(OpCodes.Ret);

      Action<object, T> fun = (Action<object, T>)dynamicGetter.CreateDelegate(typeof(Action<object, T>));
      _caches[id] = fun;
      return (t) => fun(obj, t);
    }
  }
}
