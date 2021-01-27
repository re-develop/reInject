using ReInject.Implementation.Utils;
using ReInject.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.Implementation.Core
{
  public class EventProxyTarget
  {
    private WeakReference<object> _target;
    public MethodInfo TargetMethod { get; init; }

    private object GetRef()
    {
      if (_target.TryGetTarget(out var obj))
        return obj;

      return null;
    }

    public EventProxyTarget(object target, MethodInfo receiver)
    {
      this._target = new WeakReference<object>(target);
      TargetMethod = receiver;
    }

    public object Target => GetRef();
    public bool IsAlive => GetRef() != null;
    public int Priority { get; set; }

    public object Call(object[] parameters)
    {
      if (_target.TryGetTarget(out var obj))
      {
        return TargetMethod.Invoke(obj, parameters);
      }

      return null;
    }
  }

  public class EventProxy : IDisposable
  {
    public object EventSource { get; private set; }
    public string EventName { get; private set; }
    public MethodInfo DelegateInvokeMethod { get; private set; }

    private List<EventProxyTarget> _targets = new List<EventProxyTarget>();

    private Delegate _boundDelegate = null;
    private EventInfo _eventInfo = null;

    public EventProxy(object source, string bindTo, string eventName)
    {
      if (source == null)
        throw new ArgumentNullException(nameof(source));

      _eventInfo = ReflectionHelper.GetMember<EventInfo>(source.GetType(), bindTo);
      if (_eventInfo == null)
        throw new ArgumentException($"No event with name {bindTo} found in type {source.GetType().Name}", nameof(bindTo));

      EventName = eventName;
      EventSource = source;
      var delegateType = _eventInfo.EventHandlerType;
      var delegateInfo = delegateType.GetMethod("Invoke");
      DelegateInvokeMethod = delegateInfo;
      _boundDelegate = CompileDynamicReceiver(delegateType, delegateInfo.ReturnType, delegateInfo.GetParameters());
      _eventInfo.AddEventHandler(source, _boundDelegate);
    }

    private object RaiseEvent(object[] parameters)
    {
      if (DelegateInvokeMethod.ReturnType == typeof(Task))
      {
        var tasks = new List<Task>();
        foreach (var target in _targets.Where(x => x.IsAlive).OrderByDescending(x => x.Priority))
        {
          try
          {
            tasks.Add((Task)target.Call(parameters));
          }
          catch (Exception ex)
          {
            Debug.WriteLine(ex);
          }
        }

        return Task.WhenAll(tasks);
      }
      else
      {

        object result = null;
        foreach (var target in _targets.Where(x => x.IsAlive).OrderByDescending(x => x.Priority))
        {
          try
          {
            result = target.Call(parameters);
          }
          catch (Exception ex)
          {
            Debug.WriteLine(ex);
          }
        }

        return result;
      }
    }

    public void AddTarget(EventProxyTarget target)
    {
      if (DelegateInvokeMethod.HasSameSignatures(target.TargetMethod) == false)
        throw new ArgumentException($"wrong method signature, expected {DelegateInvokeMethod.ReturnType.Name} {target.TargetMethod.Name}({string.Join(", ", DelegateInvokeMethod.GetParameters().Select(x => x.ParameterType.Name + " " + x.Name))})", nameof(EventProxyTarget));

      _targets.Add(target);
    }


    private static readonly OpCode[] Ld_OpCodes = new OpCode[] { OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3 };

    private Delegate CompileDynamicReceiver(Type delegateType, Type returnType, ParameterInfo[] parameters)
    {
      var types = parameters?.Select(x => x.ParameterType)?.ToList();
      types.Insert(0, typeof(EventProxy));

      DynamicMethod method = new DynamicMethod($"__ReInject_DynamicCompiled_{nameof(EventProxy)}_{EventName}", returnType, types.ToArray(), typeof(EventProxy), true);
      method.InitLocals = true;
      var gen = method.GetILGenerator();
      gen.DeclareLocal(typeof(object[]), false);
      gen.Emit(OpCodes.Ldc_I4, parameters.Length);
      gen.Emit(OpCodes.Newarr, typeof(object));
      gen.Emit(OpCodes.Stloc_0);

      for (int i = 0; i < parameters.Length; i++)
      {
        var type = parameters[i].ParameterType;
        gen.Emit(OpCodes.Ldloc_0);
        gen.Emit(OpCodes.Ldc_I4, i);

        if (i + 1 >= 4)
        {
          gen.Emit(OpCodes.Ldarg_S, (byte)(i + 1));
        }
        else
        {
          gen.Emit(Ld_OpCodes[i + 1]);
        }

        if (type.IsValueType)
          gen.Emit(OpCodes.Box, type);

        gen.Emit(OpCodes.Stelem_Ref);
      }

      gen.Emit(OpCodes.Ldarg_0);
      gen.Emit(OpCodes.Ldloc_0);
      gen.Emit(OpCodes.Call, ReflectionHelper.GetMember<MethodInfo>(typeof(EventProxy), nameof(RaiseEvent)));

      if (returnType != null && returnType != typeof(object) && returnType != typeof(void))
      {
        gen.Emit(returnType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, returnType);
      }
      else
      {
        gen.Emit(OpCodes.Pop);
      }

      gen.Emit(OpCodes.Ret);
      return method.CreateDelegate(delegateType, this);
    }

    public void Dispose()
    {
      _targets.Clear();
      _eventInfo.RemoveEventHandler(EventSource, _boundDelegate);
    }
  }
}
