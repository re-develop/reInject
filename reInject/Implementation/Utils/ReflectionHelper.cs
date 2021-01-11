using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ReInject.Utils
{
  public class ReflectionHelper
  {
    private static Dictionary<string, object> _caches = new Dictionary<string, object>();

    public static void ClearCaches()
    {
      _caches.Clear();
    }

    public static MemberInfo[] GetAllMembers<T>(MemberTypes filter = MemberTypes.All, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
    {
      return GetAllMembers(typeof(T), filter, flags);
    }

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
