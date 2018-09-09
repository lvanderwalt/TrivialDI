using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace TrivialDITests
{
  [TestFixture]
  public class Tests
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
      ChildA,
      ChildB
    }

    [Test]
    public void ExampleUsage()
    {
      using (default(ChildBase).Map(() => new ChildB()))
      {
        //The following code runs within the ambient context where ChildBase resolves to ChildB.
        //It can be in another class, and has no dependency on the "using" clause. 
        //It also has no direct reference to the TrivialDI class.

        var instance = default(ChildBase).Resolve();
        Assert.AreEqual(typeof(ChildB), instance.GetType());

      }

      //outside the "using" scope: resolves to ChildBase again
      var instance2 = default(ChildBase).Resolve();
      Assert.AreEqual(typeof(ChildBase), instance2.GetType());
    }
    
    [Test]
    public void LazyLoadedChildPropertyMustPersist()
    {
      var owner = GivenAnOwner();
      WhenANameAssignedToChild(owner.Child, "test");
      ThenTheChildMustBeTheBaseType(owner.Child);
      ThenTheNameMustBe("test", owner.Child.Name);
    }

    [Test]
    public void ResolvingAnInstanceWithNoOverrides()
    {
      Assert.AreEqual(typeof(ChildBase), default(ChildBase).Resolve().GetType());
    }

    [Test]
    public void ResolvingATypeByInterface()
    {
      using (default(IChild).Map(() => new ChildBase()))
      {
        Assert.IsTrue(default(IChild).Resolve() is ChildBase);
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
        ThenTheChildMustBe(child);
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
        ThenTheChildMustBe(owner.Child);
      }
    }

    [TestCase(OverrideTypeEnum.Global, OverrideSourceEnum.Class)]
    [TestCase(OverrideTypeEnum.ByOwner, OverrideSourceEnum.Class)]
    [TestCase(OverrideTypeEnum.Global, OverrideSourceEnum.Interface)]
    [TestCase(OverrideTypeEnum.ByOwner, OverrideSourceEnum.Interface)]
    public void IsolatedOverridesMustNotAffectEachOther(OverrideTypeEnum overrideType, OverrideSourceEnum sourceType)
    {
      var owner = GivenAnOwner();
      IChild child = null;
      using (GivenAnOverride(overrideType, source: sourceType, target: OverrideWithEnum.ChildA))
      {
        child = WhenOwnerIsAskedForNewChild(owner, sourceType);
        ThenTheChildMustBe(child, OverrideWithEnum.ChildA);
      }

      using (GivenAnOverride(overrideType, source: sourceType, target: OverrideWithEnum.ChildB))
      {
        child = WhenOwnerIsAskedForNewChild(owner, sourceType);
        ThenTheChildMustBe(child, OverrideWithEnum.ChildB);
      }
    }

    [Test]
    public void OwnerOverrideOnlyAppliesWhenResolvedByOwner()
    {
      using (GivenAnOverride(OverrideTypeEnum.ByOwner, target: OverrideWithEnum.ChildA))
      {
        var owner = GivenAnOwner();
        var child = GivenChildCreatedByOwner(owner);
        ThenTheChildMustBe(child, OverrideWithEnum.ChildA);
        child = GivenAResolutionOutsideAnOwner();
        ThenTheChildMustBeTheBaseType(child);
      }
    }


    [TestCase(OverrideTypeEnum.Global, OverrideSourceEnum.Class)]
    [TestCase(OverrideTypeEnum.ByOwner, OverrideSourceEnum.Class)]
    [TestCase(OverrideTypeEnum.Global, OverrideSourceEnum.Interface)]
    [TestCase(OverrideTypeEnum.ByOwner, OverrideSourceEnum.Interface)]
    public void NestedResolveMustUseOverrideFromOuterScope(OverrideTypeEnum overrideType, OverrideSourceEnum sourceType)
    {
      using (GivenAnOverride(overrideType, sourceType, OverrideWithEnum.ChildA))
      {
        var owner = GivenAnOwner();

        IChild child = null;
        using (GivenAnOverride(overrideType, sourceType, OverrideWithEnum.ChildB))
        {
          child = WhenOwnerIsAskedForNewChild(owner, sourceType);
          ThenTheChildMustBe(child, OverrideWithEnum.ChildA);
        }

        child = WhenOwnerIsAskedForNewChild(owner, sourceType);
        ThenTheChildMustBe(child, OverrideWithEnum.ChildA);
      }
    }

    //**helpers**//

    private IDisposable GivenAnOverride(OverrideTypeEnum overrideType,
        OverrideSourceEnum source = OverrideSourceEnum.Class,
        OverrideWithEnum target = OverrideWithEnum.ChildA)
    {
      switch (source)
      {
        case OverrideSourceEnum.Class:
          switch (target)
          {
            case OverrideWithEnum.ChildA:
              return GivenAnOverride<ChildBase, ChildA>(overrideType);
            case OverrideWithEnum.ChildB:
              return GivenAnOverride<ChildBase, ChildB>(overrideType);
          }
          break;
        case OverrideSourceEnum.Interface:
          switch (target)
          {
            case OverrideWithEnum.ChildA:
              return GivenAnOverride<IChild, ChildA>(overrideType);
            case OverrideWithEnum.ChildB:
              return GivenAnOverride<IChild, ChildB>(overrideType);
          }
          break;
      }
      return null;
    }

    private IDisposable GivenAnOverride<TSource, TTarget>(OverrideTypeEnum overrideType)
        where TTarget : ChildBase, TSource, new()
    {
      Func<TTarget> resolver = () => new TTarget();
      switch (overrideType)
      {
        case OverrideTypeEnum.Global:
          return default(TSource).Map(resolver);
        case OverrideTypeEnum.ByOwner:
          return default(TSource).Map<OwnerWithChild, TSource>(resolver);
      }
      return null;
    }


    private ChildBase GivenChildCreatedByOwner(OwnerWithChild owner)
    {
      return owner.NewChild();
    }


    private void ThenTheChildMustBeTheBaseType(ChildBase child)
    {
      Assert.IsTrue(child.GetType() == typeof(ChildBase));
    }

    private ChildBase GivenAResolutionOutsideAnOwner()
    {
      return default(ChildBase).Resolve();
    }

    private IChild WhenOwnerIsAskedForNewChild(OwnerWithChild owner, OverrideSourceEnum sourceType = OverrideSourceEnum.Class)
    {
      switch (sourceType)
      {
        case OverrideSourceEnum.Class:
          return owner.NewChild();
        case OverrideSourceEnum.Interface:
          return owner.NewChildInterface();
        default:
          throw new NotImplementedException();
      }
    }

    private void ThenTheChildMustBe(IChild child, OverrideWithEnum expected = OverrideWithEnum.ChildA)
    {
      Assert.IsNotNull(child);
      Type expectedType = expected == OverrideWithEnum.ChildA ? typeof(ChildA) : typeof(ChildB);
      switch (expected)
      {
        case OverrideWithEnum.ChildA:
          Assert.IsTrue(child.GetType() == expectedType);
          break;
        case OverrideWithEnum.ChildB:
          Assert.IsTrue(child.GetType() == expectedType);
          break;
        default:
          throw new NotImplementedException();
      }
    }

    private void ThenTheNameMustBe(string expected, string name)
    {
      Assert.AreEqual(expected, name);
    }

    private void WhenANameAssignedToChild(ChildBase child, string name)
    {
      child.Name = name;
    }

    private OwnerWithChild GivenAnOwner()
    {
      return new OwnerWithChild();
    }


    class OwnerWithChild
    {
      public ChildBase Child { get; private set; }

      public OwnerWithChild()
      {
        Child = default(ChildBase).Resolve(this);
      }

      internal ChildBase NewChild()
      {
        return default(ChildBase).Resolve(this);
      }

      internal IChild NewChildInterface()
      {
        return default(IChild).Resolve(this);
      }
    }

    interface IChild
    {
      string Name { get; set; }
    }

    class ChildBase : IChild
    {
      public string Name { get; set; }
    }

    class ChildA : ChildBase
    {
    }

    class ChildB : ChildBase
    {
    }
  }
}
