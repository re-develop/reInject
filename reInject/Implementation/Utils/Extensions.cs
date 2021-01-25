using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.Implementation.Utils
{
  public static class Extensions
  {
    /// <summary>
    /// Returns if two method infos have the same signature
    /// </summary>
    /// <param name="info">self</param>
    /// <param name="other">other method</param>
    /// <returns>true if the signatures matches otherwise false</returns>
    public static bool HasSameSignatures(this MethodInfo info, MethodInfo other)
    {
      if (other == null)
        return false;

      var selfParams = info.GetParameters();
      var otherParams = info.GetParameters();

      return info.ReturnType == other.ReturnType && selfParams.Length == otherParams.Length && Enumerable.Range(0, selfParams.Length).Any(x => selfParams[x].ParameterType == otherParams[x].ParameterType && selfParams[x].IsOut == otherParams[x].IsOut);
    }
  }
}
