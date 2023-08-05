using System;
using System.Collections.Generic;
using System.Text;
using ReInject.Interfaces;

namespace ReInject.Implementation.DependencyTypes
{
	/// <summary>
	/// internal implementation of the atomic depedency strategy
	/// </summary>
	internal class SingletonDependency<T> : DependencyBase<T>, IDisposable
	{
		public SingletonDependency(IDependencyContainer container, T instance, Type type, string name = null) : base(container, null, type, name)
		{
			this._instance = instance;
		}

		public override bool IsSingleton => true;
		private T _instance;
		public override object Instance
		{
			get
			{
				if (_instance == null)
					throw new ObjectDisposedException("Instance was disposed");

				return _instance;
			}
		}




		public override void Clear()
		{
		}

		public void Dispose()
		{
			if (_instance is IDisposable disposable)
				disposable.Dispose();

			_instance = default;
		}
	}
}
