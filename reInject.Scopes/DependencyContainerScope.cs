using Microsoft.Extensions.DependencyInjection;
using ReInject;
using ReInject.Implementation.Attributes;
using ReInject.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace reInject.Scopes
{
	public class DependencyContainerScope : IDependencyContainer, IServiceScope
	{

		private IDependencyContainer _container;
		public System.IServiceProvider ServiceProvider => this;
		private List<IDisposable> _disposables = new List<IDisposable>();
		public DependencyContainerScope(IDependencyContainer container)
		{
			_container = Injector.NewContainer(container);
		}

		public string Name => _container.Name;

		public void Clear()
		{
			_container.Clear();
		}

		public void Dispose()
		{
			Injector.Remove(_container);
			_disposables.ForEach(disposable => disposable.Dispose());
			_disposables.Clear();
		}

		public IEnumerable<(T instance, string name)> GetAllKnownInstances<T>(bool searchParents = true)
		{
			return _container.GetAllKnownInstances<T>(searchParents);
		}

		public IEnumerable<IPostInjector> GetPostInjecors()
		{
			return _container.GetPostInjecors();
		}

		private object handleDisposable(object inst, Type type, string name = null)
		{
			if (inst != null && inst is IDisposable disposable)
			{
				var dependency = _container.GetDependency(type, name);
				if (dependency.IsSingleton == false)
					_disposables.Add(disposable);
			}

			return inst;
		}

		public T GetInstance<T>(Action<T> action = null, string name = null)
		{
			return (T)handleDisposable(_container.GetInstance(action, name), typeof(T), name);
		}

		public object GetInstance(Type type, string name = null)
		{
			return handleDisposable(_container.GetInstance(type, name), type, name);
		}

		public object GetService(Type serviceType)
		{
			return handleDisposable(_container.GetInstance(serviceType), serviceType);
		}

		public bool IsKnownType<T>(string name = null)
		{
			return _container.IsKnownType<T>(name);
		}

		public bool IsKnownType(Type type, string name = null)
		{
			return _container.IsKnownType(type, name);
		}

		public void PostInject(object obj)
		{
			_container.PostInject(obj);
		}

		public bool RegisterPostInjector(IPostInjector injector, bool overwrite = false)
		{
			return _container.RegisterPostInjector(injector, overwrite);
		}

		public T RegisterPostInjector<T>(Action<T> configure = null, bool overwrite = false) where T : IPostInjector
		{
			return _container.RegisterPostInjector(configure, overwrite);
		}

		public void SetPostInjectionsEnabled(object instance, bool enabled)
		{
			_container.SetPostInjectionsEnabled(instance, enabled);
		}


		public void UnregisterPostInjector<T>(string name = null) where T : IPostInjector
		{
			_container.UnregisterPostInjector<T>(name);
		}

		public void UnregisterPostInjector(IPostInjector injector)
		{
			_container.UnregisterPostInjector(injector);
		}

		public void SetPostInjectionsEnabled(object instance, bool enabled, string name = null)
		{
			_container.SetPostInjectionsEnabled(instance, enabled, name);
		}

		public void SetPostInjectionsEnabled<T>(object instance, bool enabled, string name = null)
		{
			_container.SetPostInjectionsEnabled<T>(instance, enabled, name);
		}

		public IDependencyContainer Add(IDependency dependency, bool overwrite = false, string name = null)
		{
			_container.Add(dependency, overwrite, name);
			return this;
		}

		public IDependencyContainer AddCached<T>(Func<T> factory, bool overwrite = false, string name = null)
		{
			_container.AddCached<T>(factory, overwrite, name);
			return this;
		}

		public IDependencyContainer AddCached<TInterface, TType>(Func<TType> factory = null, bool overwrite = false, string name = null) where TType : TInterface
		{
			_container.AddCached<TInterface, TType>(factory, overwrite, name);
			return this;
		}

		public IDependencyContainer AddLazySingleton<T>(Func<T> factory = null, bool overwrite = false, string name = null)
		{
			_container.AddLazySingleton<T>(factory, overwrite, name);
			return this;
		}

		public IDependencyContainer AddLazySingleton<TInterface, TType>(Func<TType> factory = null, bool overwrite = false, string name = null) where TType : TInterface
		{
			_container.AddLazySingleton<TInterface, TType>(factory, overwrite, name);
			return this;
		}

		public IDependencyContainer AddSingleton<T>(bool overwrite = false, string name = null)
		{
			_container.AddSingleton<T>(overwrite, name);
			var dependency = _container.GetDependency<T>(name);
			if (dependency is IDisposable disposable)
				_disposables.Add(disposable);
			return this;
		}

		public IDependencyContainer AddSingleton<T>(T value, bool overwrite = false, string name = null)
		{
			_container.AddSingleton<T>(value, overwrite, name);
			var dependency = _container.GetDependency<T>(name);
			if (dependency is IDisposable disposable)
				_disposables.Add(disposable);
			return this;
		}

		public IDependencyContainer AddSingleton<TInterface, TType>(bool overwrite = false, string name = null) where TType : TInterface
		{
			_container.AddSingleton<TInterface, TType>(overwrite, name);
			var dependency = _container.GetDependency<TInterface>(name);
			if (dependency is IDisposable disposable)
				_disposables.Add(disposable);
			return this;
		}

		public IDependencyContainer AddTransient<T>(Func<T> factory = null, bool overwrite = false, string name = null)
		{
			_container.AddTransient<T>(factory, overwrite, name);
			return this;
		}

		public IDependencyContainer AddTransient<TInterface, TType>(Func<TType> factory = null, bool overwrite = false, string name = null) where TType : TInterface
		{
			_container.AddTransient<TInterface, TType>(factory, overwrite, name);
			return this;
		}

		public IDependency GetDependency<T>(string name = null) => _container.GetDependency<T>(name);

		public IDependency GetDependency(Type type, string name = null) => _container.GetDependency(type, name);

		public IDependencyContainer AddCached(Type type, Func<object> factory = null, bool overwrite = false, string name = null)
		{
			_container.AddCached(type, factory, overwrite, name);
			return this;
		}

		public IDependencyContainer AddLazySingleton(Type type, Func<object> factory = null, bool overwrite = false, string name = null)
		{
			_container.AddLazySingleton(type, factory, overwrite, name);
			return this;
		}

		public IDependencyContainer AddSingleton(Type type, object value, bool overwrite = false, string name = null)
		{
			_container.AddSingleton(type, value, overwrite, name);
			var dependency = _container.GetDependency(type, name);
			if (dependency is IDisposable disposable)
				_disposables.Add(disposable);
			return this;
		}

		public IDependencyContainer AddTransient(Type type, Func<object> factory = null, bool overwrite = false, string name = null)
		{
			_container.AddTransient(type, factory, overwrite, name);
			return this;
		}

		public IDependencyContainer AddCached(Type interfaceType, Type actualType, Func<object> factory = null, bool overwrite = false, string name = null)
		{
			_container.AddCached(interfaceType, actualType, factory, overwrite, name);
			return this;
		}

		public IDependencyContainer AddLazySingleton(Type interfaceType, Type actualType, Func<object> factory = null, bool overwrite = false, string name = null)
		{
			_container.AddLazySingleton(interfaceType, actualType, factory, overwrite, name);
			return this;
		}

		public IDependencyContainer AddTransient(Type interfaceType, Type actualType, Func<object> factory = null, bool overwrite = false, string name = null)
		{
			_container.AddTransient(interfaceType, actualType, factory, overwrite, name);
			return this;
		}
	}
}
