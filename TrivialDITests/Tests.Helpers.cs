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


    private void WhenThreadMapsToXThenThreadResolvesToX(OverrideTypeEnum overrideType, OverrideSourceEnum sourceType, OverrideWithEnum target)
    {
      var owner = GivenAnOwner();
      var instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
      ThenInstanceMustBeTheBaseType(instance);

      Console.WriteLine($"mapping to {target}");
      using (GivenAnOverride(overrideType, source: sourceType, target: target))
      {
        Thread.Sleep(100);
        Console.WriteLine($"resolving for {target}");
        instance = WhenOwnerIsAskedForNewChild(owner, sourceType);
        ThenTheInstanceMustBe(instance, target);
      }
    }

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
