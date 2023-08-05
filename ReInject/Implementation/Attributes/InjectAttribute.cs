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

    /// <summary>
    /// Attribute to signal this field or property should be injected with the specified dependency
    /// </summary>
    /// <param name="type">The type of the dependency if null the attribute targets type is taken</param>
    /// <param name="name">The name of the dependency to support multiple dependencies of the same type, default is null</param>
    public InjectAttribute(Type type = null, string name = null)
    {
      this.Type = type;
      this.Name = name;
    }

  }
}
