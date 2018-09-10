using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TrivialDITests
{
  public partial class Tests
  {
    public enum MapSource
    {
      Class,
      Interface
    }

    public enum MapWith
    {
      DerivedA,
      DerivedB
    }


    private void WhenThreadMapsToXThenThreadResolvesToX(MapSource sourceType, MapWith target)
    {
      var instance = WhenInstanceIsResolved( sourceType);
      ThenInstanceMustBeTheBaseType(instance);

      Console.WriteLine($"mapping to {target}");
      using (GivenAMap(source: sourceType, target: target))
      {
        Thread.Sleep(100);
        Console.WriteLine($"resolving for {target}");
        instance = WhenInstanceIsResolved( sourceType);
        ThenTheInstanceMustBe(instance, target);
      }
    }

    private IDisposable GivenAMap(
        MapSource source = MapSource.Class,
        MapWith target = MapWith.DerivedA)
    {
      switch (source)
      {
        case MapSource.Class:
          switch (target)
          {
            case MapWith.DerivedA:
              return GivenAMapping<BaseClass, DerivedA>();
            case MapWith.DerivedB:
              return GivenAMapping<BaseClass, DerivedB>();
          }
          break;
        case MapSource.Interface:
          switch (target)
          {
            case MapWith.DerivedA:
              return GivenAMapping<INamedClass, DerivedA>();
            case MapWith.DerivedB:
              return GivenAMapping<INamedClass, DerivedB>();
          }
          break;
      }
      return null;
    }

    private IDisposable GivenAMapping<TSource, TTarget>()
        where TTarget : BaseClass, TSource, new()
    {
      Func<TTarget> resolver = () => new TTarget();
      return default(TSource).Map(resolver);
    }


    private BaseClass GivenInstanceIsResolved()
    {
      return default(BaseClass).Resolve();
    }


    private void ThenInstanceMustBeTheBaseType(object instance)
    {
      Assert.IsTrue(instance.GetType() == typeof(BaseClass));
    }
    
    private INamedClass WhenInstanceIsResolved(MapSource sourceType = MapSource.Class)
    {
      switch (sourceType)
      {
        case MapSource.Class:
          return default(BaseClass).Resolve();
        case MapSource.Interface:
          return default(INamedClass).Resolve();
        default:
          throw new NotImplementedException();
      }
    }

    private void ThenTheInstanceMustBe(INamedClass instance, MapWith expected = MapWith.DerivedA)
    {
      Assert.IsNotNull(instance);
      switch (expected)
      {
        case MapWith.DerivedA:
          Assert.IsTrue(instance.GetType() == typeof(DerivedA));
          break;
        case MapWith.DerivedB:
          Assert.IsTrue(instance.GetType() == typeof(DerivedB));
          break;
        default:
          throw new NotImplementedException();
      }
    }   

    interface INamedClass
    {
      string Name { get; set; }
    }

    class BaseClass : INamedClass
    {
      public string Name { get; set; }
    }

    class DerivedA : BaseClass
    {
    }

    class DerivedB : BaseClass
    {
    }


  }
}
