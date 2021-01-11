using System;
using System.Collections.Generic;
using System.Text;

namespace ReInject.Implementation.Attributes
{
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
  public class InjectAttribute : Attribute
  {
    public Type Type { get; set; }
    public string Name { get; set; }

    public InjectAttribute(Type type = null, string name = null)
    {
      this.Type = type;
      this.Name = name;
    }

  }
}
