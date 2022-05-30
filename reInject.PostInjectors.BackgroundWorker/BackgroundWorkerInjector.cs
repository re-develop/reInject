using Cronos;
using ReInject.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.PostInjectors.BackgroundWorker
{
	public class BackgroundWorkerInjector : IPostInjector, IBackgroundWorkerManager
	{
		public BackgroundWorkerInjector() : this(null, 0)
		{
			
		}

		public BackgroundWorkerInjector(string name = null, int priority = 0)
		{
			this.Name = name ?? Guid.NewGuid().ToString();
			this.Priority = priority;
		}

		private List<BackgroundTask> _tasks = new List<BackgroundTask>();
		public string Name { get; init; }
		public int Priority { get; set; }

		public void Dispose()
		{
			_tasks.ForEach(worker => worker.Dispose());
			_tasks.Clear();
		}

		public IEnumerable<MemberInfo> PostInject(IDependencyContainer container, Type type, object instance)
		{
			foreach(var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
			{
				var attributes = method.GetCustomAttributes<BackgroundWorkerAttribute>().ToArray();
				if(attributes.Length > 0)
				{
					foreach (var attribute in attributes)
						_tasks.Add(new BackgroundTask(method, instance, attribute.GetSchedule(instance)));

					yield return method;
				}
			}
		}

		public bool SetInjectionEnabled(object instance, bool enabled)
		{
			var list = _tasks.Where(x => x.Target == instance).ToList();
			list.ForEach(task => task.Enabled = false);
			return list.Count > 0;
		}

		public IBackgroundTask RegisterBackgroundTask(CronExpression schedule, Func<Task> callable, bool start = true, string tag = null)
		{
			var task = new BackgroundTask(callable, schedule, start, tag);
			_tasks.Add(task);
			return task;
		}

		public IBackgroundTask RegisterBackgroundTask(CronExpression schedule, Action callable, bool start = true, string tag = null)
		{
			var task = new BackgroundTask(callable, schedule, start, tag);
			_tasks.Add(task);
			return task;
		}

		public void UnregisterBackgroundTask(IBackgroundTask task)
		{
			_tasks.RemoveAll(x => x.Id == task.Id);
		}
	}
}
