using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using ReInject.Implementation.Core;
using ReInject.Implementation.DependencyTypes;
using ReInject.Interfaces;
using ReInject.Utils;

namespace ReInject.Implementation
{

	/// <summary>
	/// default shipped implementation of IDependencyContainer
	/// </summary>
	internal class DependencyContainer : IDependencyContainer
	{
		IDependencyContainer Parent { get; init; }

		/// <summary>
		/// Constructs and DependencyContainer with the given name
		/// </summary>
		/// <param name="name">The name of this conainer</param>
		/// <param name="parent">Parent to fallback on type search</param>
		public DependencyContainer(string name, IDependencyContainer parent = null)
		{
			this.Name = name;
		}

		// internal dictionary to keep track of registered dependencies
		private Dictionary<(Type, string), IDependency> _typeCache = new Dictionary<(Type, string), IDependency>();
		private List<IPostInjector> _postInjectors = new List<IPostInjector>();

		/// <summary>
		/// Gets the name of the current dependency container
		/// </summary>
		public string Name { get; private set; }

		public IEnumerable<(T instance, string name)> GetAllKnownInstances<T>(bool searchParents = true)
		{
			foreach (var instance in _typeCache)
			{
				if (instance.Key.Item1.IsAssignableTo(typeof(T)))
					yield return ((T)instance.Value.Instance, instance.Key.Item2);
			}

			if (searchParents && Parent != null)
				foreach (var instance in Parent.GetAllKnownInstances<T>(searchParents))
					yield return instance;
		}


		protected void CheckFactoryNullAndInterface<T>(Func<T> factory)
		{
			var type = typeof(T);
			if ((type.IsInterface || type.IsInterface) && factory == null)
				throw new ArgumentException($"If a factory isn't given the type must be instantiable");
		}

		protected T CreateInstanceInternal<T>()
		{
			try
			{
				return (T)TypeInjectionMetadataCache.GetMetadataCache(typeof(T)).CreateInstance(this);
			}
			catch (Exception ex)
			{
				throw new AggregateException($"couldn't create instance internally", ex);
			}
		}

		public IDependencyContainer Add(IDependency dependency, bool overwrite = false, string name = null)
		{
			var isKnown = IsKnownType(dependency.Type, name);
			if (isKnown && overwrite == false)
				return this;

			var key = (dependency.Type, name);
			if (isKnown)
			{
				if (_typeCache.TryGetValue(key, out var type))
				{
					type.Clear();
					if (type is IDisposable disposable)
					{
						disposable.Dispose();
					}
				}
			}

			_typeCache.Add(key, dependency);
			return this;
		}

		public IDependencyContainer AddTransient<TInterface, TType>(Func<TType> factory = null, bool overwrite = false, string name = null) where TType : TInterface
		{
			factory ??= () => CreateInstanceInternal<TType>();
			var dependency = new TransientDependency<TType>(this, factory, typeof(TInterface), name);
			this.Add(dependency, overwrite, name);
			return this;
		}

		public IDependencyContainer AddCached<TInterface, TType>(Func<TType> factory = null, bool overwrite = false, string name = null) where TType : TInterface
		{
			factory ??= () => CreateInstanceInternal<TType>();
			var dependency = new CachedDependency<TType>(this, factory, typeof(TInterface), name);
			this.Add(dependency, overwrite, name);
			return this;
		}

		public IDependencyContainer AddLazySingleton<TInterface, TType>(Func<TType> factory = null, bool overwrite = false, string name = null) where TType : TInterface
		{
			factory ??= () => CreateInstanceInternal<TType>();
			var dependency = new LazySingletonDependency<TType>(this, factory, typeof(TInterface), name);
			this.Add(dependency, overwrite, name);
			return this;
		}

		public IDependencyContainer AddSingleton<TInterface, TType>(bool overwrite = false, string name = null) where TType : TInterface
		{
			try
			{
				var value = CreateInstanceInternal<TType>();
				this.AddSingleton<TInterface>(value, overwrite, name);
				return this;
			}
			catch (Exception ex)
			{
				throw new AggregateException($"couldn't create instance of {typeof(TType).FullName}, make sure all service required to instantiate this type are already registered", ex);
			}
		}

		public IDependencyContainer AddSingleton<T>(bool overwrite = false, string name = null)
		{
			try
			{
				var value = CreateInstanceInternal<T>();
				this.AddSingleton(value, overwrite, name);
				return this;
			}
			catch (Exception ex)
			{
				throw new AggregateException($"couldn't create instance of {typeof(T).FullName}, make sure all service required to instantiate this type are already registered", ex);
			}
		}

		public IDependencyContainer AddSingleton<T>(T value, bool overwrite = false, string name = null)
		{
			var type = value.GetType();
			var dependency = new SingletonDependency<T>(this, value, typeof(T), name);
			this.Add(dependency, overwrite, name);
			return this;
		}

		public IDependencyContainer AddLazySingleton<T>(Func<T> factory = null, bool overwrite = false, string name = null)
		{
			CheckFactoryNullAndInterface(factory);
			var dependency = new LazySingletonDependency<T>(this, factory, typeof(T), name);
			this.Add(dependency, overwrite, name);
			return this;
		}

		public IDependencyContainer AddTransient<T>(Func<T> factory = null, bool overwrite = false, string name = null)
		{
			CheckFactoryNullAndInterface(factory);
			var dependency = new TransientDependency<T>(this, factory, typeof(T), name);
			this.Add(dependency, overwrite, name);
			return this;
		}

		public IDependencyContainer AddCached<T>(Func<T> factory = null, bool overwrite = false, string name = null)
		{
			CheckFactoryNullAndInterface(factory);
			var dependency = new CachedDependency<T>(this, factory, typeof(T), name);
			this.Add(dependency, overwrite, name);
			return this;
		}

		public IDependencyContainer AddTransient(Type type, Func<object> factory = null, bool overwrite = false, string name = null)
		{
			var dependencyType = typeof(TransientDependency<>).MakeGenericType(type);
			var factoryCasted = ReflectionHelper.CastFactoryType(type, factory);
			var dependency = (IDependency)dependencyType.GetConstructors().First().Invoke(new object[] { this, factoryCasted, type, name });
			this.Add(dependency, overwrite, name);
			return this;
		}

		public IDependencyContainer AddCached(Type type, Func<object> factory = null, bool overwrite = false, string name = null)
		{
			var dependencyType = typeof(CachedDependency<>).MakeGenericType(type);
			var factoryCasted = ReflectionHelper.CastFactoryType(type, factory);
			var dependency = (IDependency)dependencyType.GetConstructors().First().Invoke(new object[] { this, factoryCasted, type, name });
			this.Add(dependency, overwrite, name);
			return this;
		}

		public IDependencyContainer AddSingleton(Type type, object value, bool overwrite = false, string name = null)
		{
			if (value == null || value.GetType().IsAssignableTo(type) == false)
				throw new ArgumentException("Value can't be null and must be assignable to type", nameof(value));

			var dependencyType = typeof(SingletonDependency<>).MakeGenericType(type);
			var dependency = (IDependency)dependencyType.GetConstructors().First().Invoke(new object[] { this, value, type, name });
			this.Add(dependency, overwrite, name);
			return this;
		}

		public IDependencyContainer AddLazySingleton(Type type, Func<object> factory = null, bool overwrite = false, string name = null)
		{
			var dependencyType = typeof(LazySingletonDependency<>).MakeGenericType(type);
			var factoryCasted = ReflectionHelper.CastFactoryType(type, factory);
			var dependency = (IDependency)dependencyType.GetConstructors().First().Invoke(new object[] { this, factoryCasted, type, name });
			this.Add(dependency, overwrite, name);
			return this;
		}

		public IDependencyContainer AddTransient(Type interfaceType, Type actualType, Func<object> factory = null, bool overwrite = false, string name = null)
		{
			var dependencyType = typeof(TransientDependency<>).MakeGenericType(actualType);
			var factoryCasted = ReflectionHelper.CastFactoryType(actualType, factory);
			var dependency = (IDependency)dependencyType.GetConstructors().First().Invoke(new object[] { this, factoryCasted, interfaceType, name });
			this.Add(dependency, overwrite, name);
			return this;
		}

		public IDependencyContainer AddCached(Type interfaceType, Type actualType, Func<object> factory = null, bool overwrite = false, string name = null)
		{
			var dependencyType = typeof(CachedDependency<>).MakeGenericType(actualType);
			var factoryCasted = ReflectionHelper.CastFactoryType(actualType, factory);
			var dependency = (IDependency)dependencyType.GetConstructors().First().Invoke(new object[] { this, factoryCasted, interfaceType, name });
			this.Add(dependency, overwrite, name);
			return this;
		}

		public IDependencyContainer AddLazySingleton(Type interfaceType, Type actualType, Func<object> factory = null, bool overwrite = false, string name = null)
		{
			var dependencyType = typeof(LazySingletonDependency<>).MakeGenericType(actualType);
			var factoryCasted = ReflectionHelper.CastFactoryType(actualType, factory);
			var dependency = (IDependency)dependencyType.GetConstructors().First().Invoke(new object[] { this, factoryCasted, interfaceType, name });
			this.Add(dependency, overwrite, name);
			return this;
		}


		/// <summary>
		/// Check if a given type is a registered dependency
		/// </summary>
		/// <typeparam name="T">The type of the depedency to search for</typeparam>
		/// <param name="name">Optional name to register multiple depedencies of the same type, default is null</param>
		/// <returns>True if a depdendency of the given type and name could be found, otherwise false</returns>
		public bool IsKnownType<T>(string name = null)
		{
			return IsKnownType(typeof(T), name) || (Parent?.IsKnownType<T>(name) ?? false);
		}


		/// <summary>
		/// Check if a given type is a registered dependency
		/// </summary>
		/// <param name="type">The type of the depedency to search for</param>
		/// <param name="name">Optional name to register multiple depedencies of the same type, default is null</param>
		/// <returns>True if a depdendency of the given type and name could be found, otherwise false</returns>
		public bool IsKnownType(Type type, string name = null)
		{
			if (type == null)
				return false;

			return _typeCache.ContainsKey((type, name)) || (Parent?.IsKnownType(type, name) ?? false);
		}


		/// <summary>
		/// Returns an instance of the given type initialized with it's dependencies and offers an Action to configure the instanced object
		/// </summary>
		/// <typeparam name="T">The type of the instance</typeparam>
		/// <param name="action">Nullable action to configure the instance</param>
		/// <param name="name">Optional name to handle multiple dependencies of the same name, null is default</param>
		/// <returns>An instance of the given type</returns>
		public T GetInstance<T>(Action<T> onAccess = null, string name = null)
		{
			var inst = (T)GetInstance(typeof(T), name);
			if (inst == null && Parent != null)
				inst = Parent.GetInstance(onAccess, name);

			if (onAccess != null)
				onAccess(inst);

			return inst;
		}

		/// <summary>
		/// Returns an instance of the given type initialized with it's dependencies
		/// </summary>
		/// <param name="type">The type of the instance</param>
		/// <param name="name">Optional name to handle multiple dependencies of the same name, null is default</param>
		/// <returns>An instance of the given type</returns>
		public object GetInstance(Type type, string name = null)
		{
			if (type == null)
				return null;

			if (IsKnownType(type, name))
			{
				return _typeCache[(type, name)].Instance;
			}
			else if (Parent != null && Parent.IsKnownType(type, name))
			{
				return Parent.GetInstance(type, name);
			}
			else if (type.IsClass && type.IsAbstract == false)
			{
				return TypeInjectionMetadataCache.GetMetadataCache(type).CreateInstance(this);
			}

			return null;
		}

		/// <summary>
		/// Returns an instance of the given type initialized with it's dependencies
		/// </summary>
		/// <param name="type">The type of the instance</param>
		/// <param name="name">Optional name to handle multiple dependencies of the same name, null is default</param>
		/// <returns>An instance of the given type</returns>
		public object GetService(Type type, string name = null)
		{
			return GetInstance(type, name);
		}

		/// <summary>
		/// Returns an instance of the given type initialized with it's dependencies and offers an Action to configure the instanced object
		/// </summary>
		/// <typeparam name="T">The type of the instance</typeparam>
		/// <param name="name">Optional name to handle multiple dependencies of the same name, null is default</param>
		/// <returns>An instance of the given type</returns>
		public object GetService(Type type)
		{
			return GetInstance(type, null);
		}

		/// <summary>
		/// Clear all cached object except registered dependencies of type AtomicInstance
		/// </summary>
		public void Clear()
		{
			_typeCache.Values.ToList().ForEach(x => x.Clear());
		}


		public void PostInject(object obj)
		{
			if (obj == null)
				return;

			var meta = TypeInjectionMetadataCache.GetMetadataCache(obj.GetType());
			if (meta != null)
				meta.PostInject(this, obj);
		}


		private IPostInjector getPostInjector(Type type, string name)
		{
			return _postInjectors.FirstOrDefault(x => x.GetType().IsAssignableTo(type) && (name == null || x.Name == name));
		}

		public void UnregisterPostInjector<T>(string name = null) where T : IPostInjector
		{
			var inst = getPostInjector(typeof(T), name);
			if (inst != null)
			{
				inst.Dispose();
				_postInjectors.Remove(inst);
			}
		}

		public void UnregisterPostInjector(IPostInjector injector)
		{
			injector.Dispose();
			_postInjectors.Remove(injector);
		}

		public bool RegisterPostInjector(IPostInjector injector, bool overwrite = false)
		{
			var existing = getPostInjector(injector.GetType(), injector.Name);
			if (existing != null)
			{
				if (overwrite == false)
					return false;

				existing.Dispose();
				_postInjectors.Remove(existing);
			}

			_postInjectors.Add(injector);
			return true;
		}

		public T RegisterPostInjector<T>(Action<T> configure = null, bool overwrite = false) where T : IPostInjector
		{
			var inst = GetInstance(configure);
			var success = RegisterPostInjector(inst, overwrite);
			return success ? inst : default(T);
		}

		IEnumerable<IPostInjector> IPostInjectProvider.GetPostInjecors()
		{
			return _postInjectors;
		}

		public void SetPostInjectionsEnabled(object instance, bool enabled, string name = null)
		{
			SetPostInjectionsEnabled<IPostInjector>(instance, enabled, name);
		}

		public void SetPostInjectionsEnabled<T>(object instance, bool enabled, string name = null)
		{
			_postInjectors.Where(x => x.GetType().IsAssignableTo(typeof(T)) && (name == null || x.Name == name)).ToList().ForEach(x =>
			{
				x.SetInjectionEnabled(instance, enabled);
			});
		}

		public IDependency GetDependency<T>(string name = null) => GetDependency(typeof(T), name);

		public IDependency GetDependency(Type type, string name = null)
		{
			var key = (type, name);
			if (_typeCache.TryGetValue(key, out var dependency))
				return dependency;

			return null;
		}
	}
}
