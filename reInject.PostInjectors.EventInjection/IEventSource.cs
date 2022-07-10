using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.PostInjectors.EventInjection
{
  public interface IEventSource
  {
    public Guid Id { get; }
    public bool Enabled { get; set; }
  }
}
