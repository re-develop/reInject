using ReInject;
using ReInject.PostInjectors.EventInjection;
using ReInject.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static ReInjectTests.EventSource;

namespace ReInjectTests
{

	public class EventSource
	{
		public delegate int TestDelegate(int num, string test, object obj1, object obj2, TestData obj3);
		public event TestDelegate TestEvent;
		public event Action<string, int> SimpleEvent;

		public struct TestData
		{
			public int Field1;
			public string Field2;
		}

		public int CallEvent(int num)
		{
			SimpleEvent?.Invoke("test", num);
			return TestEvent?.Invoke(num, "lol", null, null, new TestData() { Field1 = 13, Field2 = "test" }) ?? -1;
		}
	}

	public class EventTarget
	{
		public int LastEventValue { get; private set; }
		public string LastSimpleEventValue { get; private set; }
		public int CountSimpleEventCalled { get; private set; } = 0;

		[InjectEvent("test")]
		public int HandleEvent(int num, string test, object obj1, object obj2, TestData obj3)
		{
			LastEventValue = num;
			return num * 2;
		}

		[InjectEvent("simple")]
		[InjectEvent("simple2")]
		public void HandleSimple(string str, int num)
		{
			LastSimpleEventValue = str + num;
			CountSimpleEventCalled++;
		}

	}

	public class EventInjectionTests
	{
		[Fact]
		public void ReInject_TestEventInjection_AttributeBasedInjectionWorks()
		{
			// Arrange
			var container = Injector.GetContainer(Guid.NewGuid().ToString());
			int num = 1337;
			var source = new EventSource();
			container.AddEventInjector(setup =>
			{
				setup.RegisterEventSource(source, "TestEvent", "test")
				.RegisterEventSource(source, "SimpleEvent", "simple2")
				.RegisterEventSource(source, "SimpleEvent", "simple");
			});

			// Act
			var target = container.GetInstance<EventTarget>();
			var res = source.CallEvent(num);


			// Assert
			Assert.Equal($"test{num}", target.LastSimpleEventValue);
			Assert.Equal(2, target.CountSimpleEventCalled);
			Assert.Equal(num, target.LastEventValue);
			Assert.Equal(num * 2, res);
		}



		[Fact]
		public void ReInject_TestEventInjection_DisablingEventsWorks()
		{
			// Arrange
			var container = Injector.GetContainer(Guid.NewGuid().ToString());
			int num = 1337;
			var source = new EventSource();
			container.AddEventInjector(setup =>
			{
				setup.RegisterEventSource(source, "TestEvent", "test");
			});

			// Act
			var target = container.GetInstance<EventTarget>();
			var res0 = source.CallEvent(num);
			container.SetEventTargetEnabled(target, false, "test");
			var res1 = source.CallEvent(num);
			container.SetEventTargetEnabled(target, true);
			var res2 = source.CallEvent(num);



			// Assert
			Assert.Equal(num, target.LastEventValue);
			Assert.Equal(num * 2, res0);
			Assert.Equal(default(int), res1);
			Assert.Equal(num * 2, res2);
		}

		[Fact]
		public void ReInject_TestEventInjection_EventCallbackWorks()
		{
			// Arrange
			int num = 1337;
			var source = new EventSource();
			var target = new EventTarget();

			var proxy = new EventProxy(source, nameof(source.TestEvent), "test");
			var proxy2 = new EventProxy(source, nameof(source.SimpleEvent), "simple");
			var proxyTarget = new EventProxyTarget(target, ReflectionHelper.GetMember<MethodInfo>(target.GetType(), "HandleEvent"));
			var proxyTarget2 = new EventProxyTarget(target, ReflectionHelper.GetMember<MethodInfo>(target.GetType(), "HandleSimple"));
			proxy.AddTarget(proxyTarget);
			proxy2.AddTarget(proxyTarget2);

			// Act
			var res = source.CallEvent(num);

			// Assert
			Assert.Equal(num, target.LastEventValue);
			Assert.Equal(num * 2, res);
			Assert.Equal($"test{num}", target.LastSimpleEventValue);
		}
	}
}
