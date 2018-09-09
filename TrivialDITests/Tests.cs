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
    public enum OverrideTypeEnum
    {
      Global,
      ByOwner
    };

    public enum OverrideSourceEnum
    {
      Class,
      Interface
    }

    public enum OverrideWithEnum
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
    public void LazyLoadedChildPropertyMustPersist()
    {
      var owner = GivenAnOwner();
      WhenANameAssignedToChild(owner.Child, "test");
      ThenInstanceMustBeTheBaseType(owner.Child);
      ThenTheNameMustBe("test", owner.Child.Name);
    }

    [Test]
    public void ResolvingAnInstanceWithNoOverrides()
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

    [TestCase(OverrideTypeEnum.Global, OverrideSourceEnum.Class)]
    [TestCase(OverrideTypeEnum.ByOwner, OverrideSourceEnum.Class)]
    [TestCase(OverrideTypeEnum.Global, OverrideSourceEnum.Interface)]
    [TestCase(OverrideTypeEnum.ByOwner, OverrideSourceEnum.Interface)]
    public void OveriddenClassTypeMustResolve(OverrideTypeEnum overrideType, OverrideSourceEnum sourceType)
    {      
      using (GivenAnOverride(overrideType, sourceType))
      {
        var owner = GivenAnOwner();
        var child = WhenOwnerIsAskedForNewChild(owner, sourceType);
        ThenTheInstanceMustBe(child);
      }
    }


    [TestCase(OverrideTypeEnum.Global)]
    [TestCase(OverrideTypeEnum.ByOwner)]
    public void OveriddenChildPropertyMustPersist(OverrideTypeEnum overrideType)
    { 
      using (GivenAnOverride(overrideType))
      {
        var owner = GivenAnOwner();
        WhenANameAssignedToChild(owner.Child, "test");
        ThenTheNameMustBe("test", owner.Child.Name);
        ThenTheInstanceMustBe(owner.Child);
      }
    }

    [TestCase(OverrideTypeEnum.Global, OverrideSourceEnum.Class)]
    [TestCase(OverrideTypeEnum.ByOwner, OverrideSourceEnum.Class)]
    [TestCase(OverrideTypeEnum.Global, OverrideSourceEnum.Interface)]
    [TestCase(OverrideTypeEnum.ByOwner, OverrideSourceEnum.Interface)]
    public void IsolatedOverridesMustNotAffectEachOther(OverrideTypeEnum overrideType, OverrideSourceEnum sourceType)
    {
      var owner = GivenAnOwner();
      INamedClass instance = null;
      using (GivenAnOverride(overrideType, source: sourceType, target: OverrideWithEnum.DerivedA))
      {
        instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
        ThenTheInstanceMustBe(instance, OverrideWithEnum.DerivedA);
      }

      using (GivenAnOverride(overrideType, source: sourceType, target: OverrideWithEnum.DerivedB))
      {
        instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
        ThenTheInstanceMustBe(instance, OverrideWithEnum.DerivedB);
      }
    }

    [Test]
    public void OwnerOverrideOnlyAppliesWhenResolvedByOwner()
    {
      using (GivenAnOverride(OverrideTypeEnum.ByOwner, target: OverrideWithEnum.DerivedA))
      {
        var owner = GivenAnOwner();
        var child = GivenChildCreatedByOwner(owner);
        ThenTheInstanceMustBe(child, OverrideWithEnum.DerivedA);
        child = GivenAResolutionOutsideAnOwner();
        ThenInstanceMustBeTheBaseType(child);
      }
    }


    [TestCase(OverrideTypeEnum.Global, OverrideSourceEnum.Class)]
    [TestCase(OverrideTypeEnum.ByOwner, OverrideSourceEnum.Class)]
    [TestCase(OverrideTypeEnum.Global, OverrideSourceEnum.Interface)]
    [TestCase(OverrideTypeEnum.ByOwner, OverrideSourceEnum.Interface)]
    public void NestedResolveMustUseOverrideFromOuterScope(OverrideTypeEnum overrideType, OverrideSourceEnum sourceType)
    {
      using (GivenAnOverride(overrideType, sourceType, OverrideWithEnum.DerivedA))
      {
        var owner = GivenAnOwner();

        INamedClass instance = null;
        using (GivenAnOverride(overrideType, sourceType, OverrideWithEnum.DerivedB))
        {
          instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
          ThenTheInstanceMustBe(instance, OverrideWithEnum.DerivedA);
        }

        instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
        ThenTheInstanceMustBe(instance, OverrideWithEnum.DerivedA);
      }
    }


    //todo: make static maps thread-safe:
    //[TestCase(OverrideTypeEnum.Global)]
    //[TestCase(OverrideTypeEnum.ByOwner)]
    //public void ThreadScopedOverridesMustNotAffectEachOther(OverrideTypeEnum overrideType)
    //{
    //  Task.WaitAll(
    //    Task.Run(() =>
    //      WhenThreadMapsToXThenThreadResolvesToX(overrideType, 
    //      OverrideSourceEnum.Class, 
    //      OverrideWithEnum.DerivedA)),

    //    Task.Run(() =>
    //      WhenThreadMapsToXThenThreadResolvesToX(overrideType, 
    //      OverrideSourceEnum.Class, 
    //      OverrideWithEnum.DerivedB))
    //  );
    //}

    [TestCase(OverrideTypeEnum.Global)]
    [TestCase(OverrideTypeEnum.ByOwner)]
    public void NewThreadsMustInheritAmbientMapping(OverrideTypeEnum overrideType)
    {
      using (GivenAnOverride(overrideType, OverrideSourceEnum.Class, OverrideWithEnum.DerivedA))
      {
        Task.WaitAll(Task.Run(() =>
        {
          var owner = GivenAnOwner();
          var instance = WhenOwnerIsAskedForNewChild(owner, OverrideSourceEnum.Class);
          ThenTheInstanceMustBe(instance, OverrideWithEnum.DerivedA);          
        }));        
      }
    }

  }
}
