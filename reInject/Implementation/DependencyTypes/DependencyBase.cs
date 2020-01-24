using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReInject.Interfaces;

namespace ReInject.Implementation.DependencyTypes
{
  abstract class DependencyBase : IDependencyType
  {
    public DependencyBase( IDependencyContainer container, DependencyStrategy strategy, Type type, Type interfaceType = null )
    {
      this.Strategy = strategy;
      this.Type = type;
      this.InterfaceType = interfaceType;
      this._container = container;
    }



    private IDependencyContainer _container;
    public DependencyStrategy Strategy { get; private set; }

    public Type Type { get; private set; }

    public Type InterfaceType { get; private set; }

    public abstract object Instance { get; }

    public abstract void Clear();




    protected object createInstance()
    {
      var type = Type;
      var ctor = type.GetConstructors().Where( x => x.GetParameters().All( y => y.HasDefaultValue || _container.IsKnownType( y.ParameterType ) ) ).OrderByDescending( x => x.GetParameters().Count() ).FirstOrDefault();
      if( ctor == null )
        return null;

      return ctor.Invoke( ctor.GetParameters().Select( x =>
      {
        if( _container.IsKnownType( x.ParameterType ) )
        {
          return _container.GetInstance( x.ParameterType );
        }
        else if( x.HasDefaultValue == true )
        {
          return x.RawDefaultValue;
        }

        throw new Exception( $"Couldn't resolve type for {x.ParameterType.Name} to call ctor of {type.Name}" );
      } ).ToArray() );
    }

  }
}
