using Cronos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.PostInjectors.BackgroundWorker
{
	public interface IBackgroundTask : IDisposable
	{
		public string Tag { get; set; }
		public Guid Id { get; }
		public bool Enabled { get; set; }
		public CronExpression Schedule { get; }
	}
}
