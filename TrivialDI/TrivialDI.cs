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
    /// Map type T to be resolved as another type T, when resolved by a TOwner.
    /// 
    /// Example:
    /// 
    ///   default(Person).Map&lt;Department, Person&gt;(() => new Employee())
    ///   
    /// </summary>
    /// <typeparam name="TOwner"></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <param name="self">Not used. Can be null</param>
    /// <param name="resolver">Function returning resolved type</param>
    /// <returns>Instance to an ambient context. To be used in a using clause</returns>
    public static IDisposable Map<TOwner, T>(this T self, Func<T> resolver)
    {
      //Ignoring the "self" parameter. For simpler calling syntax.
      return MapScope<T>.New<TOwner>(resolver);
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
      return MapScope<T>.Resolve<object>(null);
    }

    /// <summary>
    /// Create a new instance of a type previously mapped for a specific owner class
    /// 
    /// Example:
    /// 
    ///   var instance = default(Person).Resolve(departmentInstance);
    ///   
    /// </summary>
    /// <typeparam name="TOwner">The class that owns the instance of T</typeparam>
    /// <typeparam name="T">The type that was mapped</typeparam>
    /// <param name="self">Not used. Can be null</param>
    /// <param name="owner">Instance of owner class. If null, resolution will default to the previously mapped type, or T</param>
    /// <returns>Instance of resolved type</returns>
    public static T Resolve<TOwner, T>(this T self, TOwner owner)
    {
      //Ignoring the "self" parameter. For simpler calling syntax.
      return MapScope<T>.Resolve<TOwner>(owner);
    }

    class MapScope<T> : IDisposable
    {
      //Holds a static variable for every return type T
      static Func<T> GlobalOverride = null;

      event Action OnDispose = delegate { };

      class OwnerOverride<TOwner>
      {
        //Holds a static variable for every TOwner, for every return type T
        public static Func<T> Override = null;
      }

      public static T Resolve<TOwner>(TOwner owner)
      {
        if (owner != null && OwnerOverride<TOwner>.Override != null)
        {
          return OwnerOverride<TOwner>.Override();
        }
        else if (GlobalOverride != null)
        {
          return GlobalOverride();
        }
        else
        {
          return Activator.CreateInstance<T>();
        }
      }

      public MapScope(Func<T> resolver)
      {
        if (GlobalOverride == null)
        {
          GlobalOverride = resolver;
          OnDispose += () => GlobalOverride = null;
        }
      }

      private MapScope()
      {
        //empty
      }

      public static MapScope<T> New<TOwner>(Func<T> resolver)
      {
        var scope = new MapScope<T>();
        if (OwnerOverride<TOwner>.Override == null)
        {
          OwnerOverride<TOwner>.Override = resolver;
          scope.OnDispose += () => OwnerOverride<TOwner>.Override = null;
        }
        return scope;
      }

      public void Dispose()
      {
        OnDispose();
      }
    }

  }
}
