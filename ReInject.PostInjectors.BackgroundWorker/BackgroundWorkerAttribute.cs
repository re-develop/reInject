using Cronos;
using ReInject.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReInject.PostInjectors.BackgroundWorker
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	public class BackgroundWorkerAttribute : Attribute
	{
		public CronExpression GetSchedule(object instance) => string.IsNullOrEmpty(ScheduleGetterExpression) ? CronExpression.Parse(Schedule) : ReflectionHelper.GetGetter<CronExpression>(instance, ScheduleGetterExpression)();
		public string ScheduleGetterExpression { get; set; }
		public string Schedule { get; set; }
		public bool StartImmediate { get; set; } = true;
	}
}
