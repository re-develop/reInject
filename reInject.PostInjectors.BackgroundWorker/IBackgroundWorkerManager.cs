using Cronos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.PostInjectors.BackgroundWorker
{
	public interface IBackgroundWorkerManager
	{
		public IBackgroundTask RegisterBackgroundTask(CronExpression schedule, Func<Task> callable, bool start = true, string tag = null);
		public IBackgroundTask RegisterBackgroundTask(CronExpression schedule, Action callable, bool start = true, string tag = null);
		public void UnregisterBackgroundTask(IBackgroundTask task);
	}
}
