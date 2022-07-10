using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.PostInjectors.BackgroundWorker
{
  public interface IBackgroundTaskScheduler
  {
    public DateTime NextUpdate { get; }
  }
}
