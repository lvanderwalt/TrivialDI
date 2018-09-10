using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//defined in the System namespace, so extensions can work from anywhere, without a direct dependency to TrivialDI
namespace System
{

  /// <summary>
  /// A light-weight, zero-config, Dependency Injection container.
  /// 
  /// Ambient context pattern, with the "using" clause.
  /// Useful when constructor-injection is not practical.
  /// </summary>
  public static class TrivialDI
  {

    /// <summary>
    /// Map type T to be resolved as another type T.
    /// 
    /// Example:
    /// 
    ///   default(Person).Map(() => new Employee())
    ///   
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self">Not used. Can be null</param>
    /// <param name="resolver">Function returning resolved type</param>
    /// <returns>Instance to an ambient context. To be used in a using clause</returns>
    public static IDisposable Map<T>(this T self, Func<T> resolver)
    {
      //Ignoring the "self" parameter. For simpler calling syntax.
      return new MapScope<T>(resolver);
    }

    /// <summary>
    /// Create a new instance of a previously mapped type
    /// 
    /// Example:
    /// 
    ///   var instance = default(Person).Resolve();
    ///   
    /// </summary>
    /// <typeparam name="T">The type that was mapped</typeparam>
    /// <param name="self">Not used. Can be null</param>
    /// <returns>Instance of resolved type. If no mapped type, then T</returns>
    public static T Resolve<T>(this T self)
    {
      //Ignoring the "self" parameter. For simpler calling syntax.
      return MapScope<T>.Resolve();
    }

    class MapScope<T> : IDisposable
    {
      //todo: wip:
      //note, ThreadStatic only initialises on the main thread.
      //Subsequent threads will init to default values.
      [ThreadStatic]
      static bool IsMainThread = true;

      [ThreadStatic]
      static Func<T> Map;

      event Action OnDispose = delegate { };

      static MapScope()
      {
        //empty
      }
      
      private MapScope()
      {
        //empty
      }

      public MapScope(Func<T> resolver)
      {
        if (Map == null)
        {
          Map = resolver;
          OnDispose += () => Map = null;
        }
      }

      public static T Resolve()
      {
        if (Map != null)
        {
          return Map();
        }
        else
        {
          return Activator.CreateInstance<T>();
        }
      }      

      public void Dispose()
      {
        OnDispose();
      }
    }

  }
}
