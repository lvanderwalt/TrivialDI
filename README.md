# TrivialDI

## A light-weight, zero-config, dependency injection container.

Uses an ambient context pattern, with the "using" clause. Useful when constructor-injection is not practical.


### Example

```cs
using (default(Person).Map(() => new Employee()))
{

  //The following code runs within the ambient context where Person resolves to Employee.
  //It can be in another class, and has no dependency on the "using" clause. 
  //It also has no direct reference to the TrivialDI class.

  var instance = default(Person).Resolve();
  //instance is now an Employee

}

//outside the "using" scope: resolves to Person again
var instance2 = default(Person).Resolve();
```

**NOTE:** See the _TrivialDITests_ project for Nunit tests
