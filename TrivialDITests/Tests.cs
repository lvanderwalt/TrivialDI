using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TrivialDITests
{
  [TestFixture]
  public partial class Tests
  {
    public enum MapType
    {
      Global,
      ByOwner
    };

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

    [Test]
    public void ExampleUsage()
    {
      using (default(BaseClass).Map(() => new DerivedB()))
      {
        //The following code runs within the ambient context where BaseClass resolves to DerivedB.
        //It can be in another class, and has no dependency on the "using" clause. 
        //It also has no direct reference to the TrivialDI class.

        var instance = default(BaseClass).Resolve();
        Assert.AreEqual(typeof(DerivedB), instance.GetType());

      }

      //outside the "using" scope: resolves to BaseClass again
      var instance2 = default(BaseClass).Resolve();
      Assert.AreEqual(typeof(BaseClass), instance2.GetType());
    }
   
    [Test]
    public void ResolvingAnInstanceWithNoMaps()
    {
      Assert.AreEqual(typeof(BaseClass), default(BaseClass).Resolve().GetType());
    }

    [Test]
    public void ResolvingATypeByInterface()
    {
      using (default(INamedClass).Map(() => new BaseClass()))
      {
        Assert.IsTrue(default(INamedClass).Resolve() is BaseClass);
      }
    }

    [TestCase(MapType.Global, MapSource.Class)]
    [TestCase(MapType.ByOwner, MapSource.Class)]
    [TestCase(MapType.Global, MapSource.Interface)]
    [TestCase(MapType.ByOwner, MapSource.Interface)]
    public void OveriddenClassTypeMustResolve(MapType mapType, MapSource sourceType)
    {      
      using (GivenAMap(mapType, sourceType))
      {
        var owner = GivenAnOwner();
        var child = WhenOwnerIsAskedForNewChild(owner, sourceType);
        ThenTheInstanceMustBe(child);
      }
    }    

    [TestCase(MapType.Global, MapSource.Class)]
    [TestCase(MapType.ByOwner, MapSource.Class)]
    [TestCase(MapType.Global, MapSource.Interface)]
    [TestCase(MapType.ByOwner, MapSource.Interface)]
    public void IsolatedMapsMustNotAffectEachOther(MapType mapType, MapSource sourceType)
    {
      var owner = GivenAnOwner();
      INamedClass instance = null;
      using (GivenAMap(mapType, source: sourceType, target: MapWith.DerivedA))
      {
        instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
        ThenTheInstanceMustBe(instance, MapWith.DerivedA);
      }

      using (GivenAMap(mapType, source: sourceType, target: MapWith.DerivedB))
      {
        instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
        ThenTheInstanceMustBe(instance, MapWith.DerivedB);
      }
    }

    [Test]
    public void OwnerMapOnlyAppliesWhenResolvedByOwner()
    {
      using (GivenAMap(MapType.ByOwner, target: MapWith.DerivedA))
      {
        var owner = GivenAnOwner();
        var child = GivenChildCreatedByOwner(owner);
        ThenTheInstanceMustBe(child, MapWith.DerivedA);
        child = GivenAResolutionOutsideAnOwner();
        ThenInstanceMustBeTheBaseType(child);
      }
    }


    [TestCase(MapType.Global, MapSource.Class)]
    [TestCase(MapType.ByOwner, MapSource.Class)]
    [TestCase(MapType.Global, MapSource.Interface)]
    [TestCase(MapType.ByOwner, MapSource.Interface)]
    public void NestedResolveMustUseMapFromOuterScope(MapType mapType, MapSource sourceType)
    {
      using (GivenAMap(mapType, sourceType, MapWith.DerivedA))
      {
        var owner = GivenAnOwner();

        INamedClass instance = null;
        using (GivenAMap(mapType, sourceType, MapWith.DerivedB))
        {
          instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
          ThenTheInstanceMustBe(instance, MapWith.DerivedA);
        }

        instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
        ThenTheInstanceMustBe(instance, MapWith.DerivedA);
      }
    }


    [TestCase(MapType.Global)]
    [TestCase(MapType.ByOwner)]
    public void ThreadScopedMapsMustNotAffectEachOther(MapType mapType)
    {
      Task.WaitAll(
        Task.Run(() =>
          WhenThreadMapsToXThenThreadResolvesToX(mapType,
          MapSource.Class,
          MapWith.DerivedA)),

        Task.Run(() =>
          WhenThreadMapsToXThenThreadResolvesToX(mapType,
          MapSource.Class,
          MapWith.DerivedB))
      );
    }

    //todo: make thread-safe
    //[TestCase(MapType.Global)]
    //[TestCase(MapType.ByOwner)]
    //public void NewThreadsMustInheritAmbientMapping(MapType mapType)
    //{
    //  using (GivenAMap(mapType, MapSource.Class, MapWith.DerivedA))
    //  {
    //    Task.WaitAll(Task.Run(() =>
    //    {
    //      var owner = GivenAnOwner();
    //      var instance = WhenOwnerIsAskedForNewChild(owner, MapSource.Class);
    //      ThenTheInstanceMustBe(instance, MapWith.DerivedA);
    //    }));
    //  }
    //}

  }
}
