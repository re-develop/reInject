using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic;
using ReInject.Implementation;
using ReInject.Implementation.Utils;
using ReInject.Interfaces;

namespace ReInject
{
	/// <summary>
	/// Main class to interact with the reInject framework
	/// </summary>
	public class Injector
	{
		// internal container cache 
		private static IDependencyContainer _defaultContainer;
		private static ConcurrentDictionary<string, IDependencyContainer> _instances = new ConcurrentDictionary<string, IDependencyContainer>();

		static Injector()
		{
			_defaultContainer = new DependencyContainer(null);
		}

		/// <summary>
		/// Clear all cached containers and their dependencies
		/// </summary>
		public static void ClearCache()
		{
			foreach (var container in _instances.Values)
				container.Clear();

			_instances.Clear();
			_defaultContainer?.Clear();
		}

		/// <summary>
		/// Remove all containers with the given name
		/// </summary>
		/// <param name="name">The name of the containers to be removed</param>
		public static void Remove(string name)
		{
			if (_instances.TryRemove(name, out var container))
			{
				container.Clear();
			}
		}

		/// <summary>
		/// Removes the give continer
		/// </summary>
		/// <param name="container">Container to remove</param>
		public static void Remove(IDependencyContainer container) => Remove(container.Name);

		/// <summary>
		/// returns a container for the given name or creates a new one if none exists
		/// </summary>
		/// <param name="name">The name of the container, null is the default container</param>
		/// <param name="parent">Parent container to add if new container is created</param>
		/// <returns>The container for the given name</returns>
		public static IDependencyContainer GetContainer(string name = null, IDependencyContainer parent = null)
		{
			if (name == null)
				return _defaultContainer;

			return _instances.GetOrAdd(name, x => new DependencyContainer(x, parent));
		}

		/// <summary>
		/// returns a new with a random guid set as name
		/// </summary>
		/// <param name="parent">Parent container to add if new container is created</param>
		/// <returns>A new container</returns>
		public static IDependencyContainer NewContainer(IDependencyContainer parent = null) => GetContainer(Guid.NewGuid().ToString(), parent);

		/// <summary>
		/// returns a container hidden behind IServiceProvider interface for the given name or creates a new one if none exists
		/// </summary>
		/// <param name="name">The name of the container, null is the default container</param>
		/// <returns>The container for the given name</returns>
		public static Interfaces.IServiceProvider GetProvider(string name = null)
		{
			return GetContainer(name);
		}
	}
}
