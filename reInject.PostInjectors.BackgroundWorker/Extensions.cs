using Microsoft.Extensions.Logging;
using ReInject;
using ReInject.Interfaces;
using ReInject.PostInjectors.BackgroundWorker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.PostInjectors.BackgroundWorker
{
	public static class Extensions
	{
		public static IDependencyContainer AddBackgroundWorker(this IDependencyContainer container, Action<BackgroundWorkerInjector> setup = null, string name = null, ILoggerFactory factory = null)
		{
			factory ??= container.GetInstance<ILoggerFactory>();
			var injector = new BackgroundWorkerInjector(factory: factory);
			if (setup != null)
				setup(injector);

			container.AddSingleton<IBackgroundWorkerManager>(injector, true, name);
			container.RegisterPostInjector(injector, true);
			return container;
		}

		public static IDependencyContainer AddBackgroundWorker(this IDependencyContainer container, string name = null, int priority = 0, TimeSpan? schedulerPeriod = null, Action<BackgroundWorkerInjector> setup = null, ILoggerFactory factory = null)
		{
			factory ??= container.GetInstance<ILoggerFactory>();
			var injector = new BackgroundWorkerInjector(name, priority, schedulerPeriod, factory);
			setup?.Invoke(injector);
			container.AddSingleton<IBackgroundWorkerManager>(injector, true, name);
			container.RegisterPostInjector(injector, true);
			return container;
		}
	}
}
