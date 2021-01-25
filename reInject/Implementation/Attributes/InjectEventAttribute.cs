using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.Implementation.Attributes
{
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
  public class InjectEventAttribute : Attribute
  {
    public string EventName { get; set; }
    public int Priority { get; set; }

    public InjectEventAttribute(string name, int priority = 0)
    {
      this.EventName = name;
      this.Priority = priority;
    }
  }
}
