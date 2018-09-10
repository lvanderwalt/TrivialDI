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
    [SetUp]
    public void SetupForEachTest()
    {
      //initialize container to indicate outer thread
      default(object).InitializeDI();
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

    [TestCase(MapSource.Class)]
    [TestCase(MapSource.Interface)]
    public void OveriddenClassTypeMustResolve(MapSource sourceType)
    {      
      using (GivenAMap(sourceType))
      {
        var instance = WhenInstanceIsResolved(sourceType);
        ThenTheInstanceMustBe(instance);
      }
    }    

    [TestCase(MapSource.Class)]
    [TestCase(MapSource.Interface)]
    public void IsolatedMapsMustNotAffectEachOther(MapSource sourceType)
    {
      INamedClass instance = null;
      using (GivenAMap(source: sourceType, target: MapWith.DerivedA))
      {
        instance = WhenInstanceIsResolved(sourceType);
        ThenTheInstanceMustBe(instance, MapWith.DerivedA);
      }

      using (GivenAMap(source: sourceType, target: MapWith.DerivedB))
      {
        instance = WhenInstanceIsResolved(sourceType);
        ThenTheInstanceMustBe(instance, MapWith.DerivedB);
      }
    }

    [TestCase(MapSource.Class)]
    [TestCase(MapSource.Interface)]
    public void NestedResolveMustUseMapFromOuterScope(MapSource sourceType)
    {
      using (GivenAMap(sourceType, MapWith.DerivedA))
      {
        INamedClass instance = null;
        using (GivenAMap(sourceType, MapWith.DerivedB))
        {
          instance = WhenInstanceIsResolved(sourceType);
          ThenTheInstanceMustBe(instance, MapWith.DerivedA);
        }

        instance = WhenInstanceIsResolved(sourceType);
        ThenTheInstanceMustBe(instance, MapWith.DerivedA);
      }
    }


    [Test]
    public void ThreadScopedMapsMustNotAffectEachOther()
    {
      Task.WaitAll(
        Task.Run(() =>
          WhenThreadMapsToXThenThreadResolvesToX(
          MapSource.Class,
          MapWith.DerivedA)),

        Task.Run(() =>
          WhenThreadMapsToXThenThreadResolvesToX(
          MapSource.Class,
          MapWith.DerivedB))
      );
    }

    [Test]
    public void NewThreadsMustInheritAmbientMapping()
    {
      using (GivenAMap(MapSource.Class, MapWith.DerivedA))
      {
        Task.WaitAll(Task.Run(() =>
        {
          var instance = WhenInstanceIsResolved(MapSource.Class);
          ThenTheInstanceMustBe(instance, MapWith.DerivedA);
        }));
      }
    }

    [Test]
    public void MappingInSubThreadsMustInheritAmbientMapping()
    {
      using (GivenAMap(MapSource.Class, MapWith.DerivedA))
      {
        Task.WaitAll(Task.Run(() =>
        {

          using (GivenAMap(MapSource.Class, MapWith.DerivedB))
          {
            var instance = WhenInstanceIsResolved(MapSource.Class);
            ThenTheInstanceMustBe(instance, MapWith.DerivedA);
          }
          
        }));
      }
    }

  }
}
