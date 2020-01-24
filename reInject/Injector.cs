using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReInject.Implementation;
using ReInject.Interfaces;

namespace ReInject
{
  public class Injector
  {
    private static List<IDependencyContainer> _instances = new List<IDependencyContainer>();



    public static void ClearCache()
    {
      _instances.Clear();
    }



    public static void Remove(string name )
    {
      _instances.RemoveAll( x => x.Name == name );
    }



    public static IDependencyContainer GetContainer( string name = "Default" )
    {
      var inst = _instances.FirstOrDefault( x => x.Name == name );
      if( inst == null )
      {
        inst = new DependencyContainer( name );
        _instances.Add( inst );
      }

      return inst;
    }



    public static Interfaces.IServiceProvider GetProvider( string name = "Default" )
    {
      return GetContainer( name );
    }
  }
}
