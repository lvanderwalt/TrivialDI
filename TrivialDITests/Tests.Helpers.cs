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


    private void WhenThreadMapsToXThenThreadResolvesToX(MapType overrideType, MapSource sourceType, MapWith target)
    {
      var owner = GivenAnOwner();
      var instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
      ThenInstanceMustBeTheBaseType(instance);

      Console.WriteLine($"mapping to {target}");
      using (GivenAMap(overrideType, source: sourceType, target: target))
      {
        Thread.Sleep(100);
        Console.WriteLine($"resolving for {target}");
        instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
        ThenTheInstanceMustBe(instance, target);
      }
    }

    private IDisposable GivenAMap(MapType overrideType,
        MapSource source = MapSource.Class,
        MapWith target = MapWith.DerivedA)
    {
      switch (source)
      {
        case MapSource.Class:
          switch (target)
          {
            case MapWith.DerivedA:
              return GivenAnOverride<BaseClass, DerivedA>(overrideType);
            case MapWith.DerivedB:
              return GivenAnOverride<BaseClass, DerivedB>(overrideType);
          }
          break;
        case MapSource.Interface:
          switch (target)
          {
            case MapWith.DerivedA:
              return GivenAnOverride<INamedClass, DerivedA>(overrideType);
            case MapWith.DerivedB:
              return GivenAnOverride<INamedClass, DerivedB>(overrideType);
          }
          break;
      }
      return null;
    }

    private IDisposable GivenAnOverride<TSource, TTarget>(MapType overrideType)
        where TTarget : BaseClass, TSource, new()
    {
      Func<TTarget> resolver = () => new TTarget();
      switch (overrideType)
      {
        case MapType.Global:
          return default(TSource).Map(resolver);
        case MapType.ByOwner:
          return default(TSource).Map<OwnerWithChild, TSource>(resolver);
      }
      return null;
    }


    private BaseClass GivenChildCreatedByOwner(OwnerWithChild owner)
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

    private INamedClass WhenOwnerIsAskedForNewChild(OwnerWithChild owner, MapSource sourceType = MapSource.Class)
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
      Type expectedType = expected == MapWith.DerivedA ? typeof(DerivedA) : typeof(DerivedB);
      switch (expected)
      {
        case MapWith.DerivedA:
          Assert.IsTrue(instance.GetType() == expectedType);
          break;
        case MapWith.DerivedB:
          Assert.IsTrue(instance.GetType() == expectedType);
          break;
        default:
          throw new NotImplementedException();
      }
    }

    private void ThenTheNameMustBe(string expected, string name)
    {
      Assert.AreEqual(expected, name);
    }

    private void WhenANameAssignedToChild(BaseClass child, string name)
    {
      child.Name = name;
    }

    private OwnerWithChild GivenAnOwner()
    {
      return new OwnerWithChild();
    }


    class OwnerWithChild
    {
      public BaseClass Child { get; private set; }

      public OwnerWithChild()
      {
        Child = default(BaseClass).Resolve(this);
      }

      internal BaseClass NewChild()
      {
        return default(BaseClass).Resolve(this);
      }

      internal INamedClass NewChildInterface()
      {
        return default(INamedClass).Resolve(this);
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
