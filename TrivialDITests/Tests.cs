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

    [TestCase(OverrideTypeEnum.Global, OverrideSourceEnum.Class)]
    [TestCase(OverrideTypeEnum.ByOwner, OverrideSourceEnum.Class)]
    [TestCase(OverrideTypeEnum.Global, OverrideSourceEnum.Interface)]
    [TestCase(OverrideTypeEnum.ByOwner, OverrideSourceEnum.Interface)]
    public async Task ThreadScopedOverridesMustNotAffectEachOther(OverrideTypeEnum overrideType, OverrideSourceEnum sourceType)
    {
      await WhenThreadMapsToXThenThreadResolvesToX(overrideType, sourceType, OverrideWithEnum.DerivedA);
      await WhenThreadMapsToXThenThreadResolvesToX(overrideType, sourceType, OverrideWithEnum.DerivedB);
    }

    private async Task WhenThreadMapsToXThenThreadResolvesToX(OverrideTypeEnum overrideType, OverrideSourceEnum sourceType, OverrideWithEnum target)
    {
      await Task.Run(() => {

        var owner = GivenAnOwner();
        var instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
        ThenInstanceMustBeTheBaseType(instance);

        Console.WriteLine($"mapping to {target}");
        using (GivenAnOverride(overrideType, source: sourceType, target: target))
        {
          Thread.Sleep(500);
          Console.WriteLine($"resolving for {target}");
          instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
          ThenTheInstanceMustBe(instance, target);
        }

      });      
    }

    //**helpers**//

    private IDisposable GivenAnOverride(OverrideTypeEnum overrideType,
        OverrideSourceEnum source = OverrideSourceEnum.Class,
        OverrideWithEnum target = OverrideWithEnum.DerivedA)
    {
      switch (source)
      {
        case OverrideSourceEnum.Class:
          switch (target)
          {
            case OverrideWithEnum.DerivedA:
              return GivenAnOverride<BaseClass, DerivedA>(overrideType);
            case OverrideWithEnum.DerivedB:
              return GivenAnOverride<BaseClass, DerivedB>(overrideType);
          }
          break;
        case OverrideSourceEnum.Interface:
          switch (target)
          {
            case OverrideWithEnum.DerivedA:
              return GivenAnOverride<INamedClass, DerivedA>(overrideType);
            case OverrideWithEnum.DerivedB:
              return GivenAnOverride<INamedClass, DerivedB>(overrideType);
          }
          break;
      }
      return null;
    }

    private IDisposable GivenAnOverride<TSource, TTarget>(OverrideTypeEnum overrideType)
        where TTarget : BaseClass, TSource, new()
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

    private INamedClass WhenOwnerIsAskedForNewChild(OwnerWithChild owner, OverrideSourceEnum sourceType = OverrideSourceEnum.Class)
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

    private void ThenTheInstanceMustBe(INamedClass instance, OverrideWithEnum expected = OverrideWithEnum.DerivedA)
    {
      Assert.IsNotNull(instance);
      Type expectedType = expected == OverrideWithEnum.DerivedA ? typeof(DerivedA) : typeof(DerivedB);
      switch (expected)
      {
        case OverrideWithEnum.DerivedA:
          Assert.IsTrue(instance.GetType() == expectedType);
          break;
        case OverrideWithEnum.DerivedB:
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
