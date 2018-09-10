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
      var owner = GivenAnOwner();
      var instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
      ThenInstanceMustBeTheBaseType(instance);

      Console.WriteLine($"mapping to {target}");
      using (GivenAMap(source: sourceType, target: target))
      {
        Thread.Sleep(100);
        Console.WriteLine($"resolving for {target}");
        instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
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
              return GivenAnOverride<BaseClass, DerivedA>();
            case MapWith.DerivedB:
              return GivenAnOverride<BaseClass, DerivedB>();
          }
          break;
        case MapSource.Interface:
          switch (target)
          {
            case MapWith.DerivedA:
              return GivenAnOverride<INamedClass, DerivedA>();
            case MapWith.DerivedB:
              return GivenAnOverride<INamedClass, DerivedB>();
          }
          break;
      }
      return null;
    }

    private IDisposable GivenAnOverride<TSource, TTarget>()
        where TTarget : BaseClass, TSource, new()
    {
      Func<TTarget> resolver = () => new TTarget();
      return default(TSource).Map(resolver);
    }


    private BaseClass GivenChildCreatedByOwner(Owner owner)
    {
      return owner.NewChild();
    }


    private void ThenInstanceMustBeTheBaseType(object instance)
    {
      Assert.IsTrue(instance.GetType() == typeof(BaseClass));
    }

    private BaseClass GivenAResolutionOutsideAnOwner()
    {
      return default(BaseClass).Resolve();
    }

    private INamedClass WhenOwnerIsAskedForNewChild(Owner owner, MapSource sourceType = MapSource.Class)
    {
      switch (sourceType)
      {
        case MapSource.Class:
          return owner.NewChild();
        case MapSource.Interface:
          return owner.NewChildInterface();
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
    
    private Owner GivenAnOwner()
    {
      return new Owner();
    }


    //todo: remove owner class. No longer needed since owner-mapping removed.
    class Owner
    {
      public BaseClass Child { get; private set; }

      public Owner()
      {
        Child = default(BaseClass).Resolve();
      }

      internal BaseClass NewChild()
      {
        return default(BaseClass).Resolve();
      }

      internal INamedClass NewChildInterface()
      {
        return default(INamedClass).Resolve();
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
