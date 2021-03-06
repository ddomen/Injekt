# **Injekt**
## **C# Scope Based Dependency Injection Library**

The aim of this library is to contextualize the dependency injection into different scopes allowing more control over the lifetime of an object during the construction of any level of an application.

## **Index**
- [Basic Usage](#Basic-Usage)
    - [IScopeConfiguration](#IScopeConfiguration)
    - [IScope](#IScope)
    - [Lifetime](#Lifetime)
    - [How to start](#How-to-start)
    - [Simple Example](#Simple-Example)
- [Advanced Usage](#Advanced-Usage)
    - [Context](#Context)
    - [Context Redefinition](#Context-Redefinition)
    - [Constructors and Destructors](#Constructors-and-Destructors)
    - [Reference Holding](#Reference-Holding)
- [Extensions List](#Extensions)


## **Basic Usage**

There are three principal concepts that let you interact with the dependency injection:
- `IScopeConfiguration`
- `IScope`
- `Lifetime`

## `IScopeConfiguration`
This interface allows to describe how to construct an object of a certain type *(and also how to destruct it)*. The behaviour is similar to a map that takes a ***key type*** and translate it to a ***value type*** definition (sort of `Dictionary`). This can be done during the creation of a scope or either during its configuration.

A declaration is composed by two components:
  1. The pair of `KeyType` and `ValueType`, both described as a `Type` variable or either a Generic
  2. The lifetime of the declaration, specified as a variable or in direct way

```c#
// Declaration with Type variables
myScopeConfiguration.Add(keyType, valueType, myLifetime);

// Declaration with Generics
myScopeConfiguration.Add<KeyType, ValueType>(myLifetime);

// Declaration with Type variables and direct lifetime
myScopeConfiguration.AddSingleton(keyType, valueType);
myScopeConfiguration.AddScoped(keyType, valueType);
myScopeConfiguration.AddTransient(keyType, valueType);

// Declaration with Generics and direct lifetime
myScopeConfiguration.AddSingleton<KeyType, ValueType>();
myScopeConfiguration.AddScoped<KeyType, ValueType>();
myScopeConfiguration.AddTransient<KeyType, ValueType>();
```
Note that in both cases it is mandatory that the `ValueType` extends or implements the `KeyType`. 

By default, if just one generic type is given, the `ValueType` will be used also as `KeyType`. 

It is also possible to remove at anytime the inserted definitions in the same way it was added, just by specifing the `KeyType`:

```c#
// Remove declaration with Type variables
myScopeConfiguration.Remove(keyType, myLifetime);

// Remove declaration with Generics
myScopeConfiguration.Remove<KeyType>(myLifetime);

// Remove declaration with Type variables and direct lifetime
myScopeConfiguration.RemoveSingleton(keyType);
myScopeConfiguration.RemoveScoped(keyType);
myScopeConfiguration.RemoveTransient(keyType);

// Remove declaration with Generics and direct lifetime
myScopeConfiguration.RemoveSingleton<KeyType>();
myScopeConfiguration.RemoveScoped<KeyType>();
myScopeConfiguration.RemoveTransient<KeyType>();
```


## `IScope`
A scope is a container that manages the creation and the disposition of the objects that are requested. This container is described by its configuration, which tell him how to resolve the dependencies. It is possible to retrieve a kind of an object just by using the following methods:

```c#
// Injection with Type variable
myScope.GetService(keyType);

// Injection with Generic
myScope.GetService<KeyType>();
```
*(Sied Note: The **Service** word is used to mantain `IServiceProvider` compatibility)*

The scope will try to resolve the given type with its own configuration, creating it if it is not available in the current context, or either returning the already created object.

## `Lifetime`

This parameter will describe the behaviour of the scope while it tries to resolve a request for a given type.

There are three basic behaviours:
- `Transient`: each request will spawn a new object.
- `Scoped`: the first request will spawn a new object, then any other request will refer to it.
- `Singleton`: spawn the object at configuration time, then any request will refer to it.

### **How to start**

In order to create a scope it is possible to use the `ScopeFactory`:
```c#
// Empty scope
ScopeFactory.CreateRootScope();

// Configurable scope
ScopeFactory.CreateRootScope(scopeConfig => {
    // Configurations...
});
```

### **Simple Example**
```c#
using Injekt;
using System.Diagnostics;

public interface ISingletonTest { }
public class SingletonTest : ISingletonTest { }

public interface IScopedTest {
    ISingletonTest MySingleton { get; }
}
public class ScopedTest : IScopedTest {
    public ISingletonTest MySingleton { get; }
    public ScopedTest(ISingletonTest singleton) { MySingleton = singleton; }
}

public class TransientTest {
    public ISingletonTest MySingleton;
    public IScopedTest MyScoped;
    public TransientTest(ISingletonTest singleton, IScopedTest scoped) {
        MySingleton = singleton;
        MyScoped = scoped;
    }
}

class Program {
    static void Main(string[] args) {
        // using keyword make sure the scope will
        // automatically dispose everything it manages
        using IScope global = ScopeFactory.CreateRootScope(sc => {
            sc.AddSingleton<ISingletonTest, SingletonTest>()
                .AddScoped<IScopedTest, ScopedTest>()
                // example of value mapped also as a key
                .AddTransient<TransientTest>();
            /* RESULTANT SCOPE IMAGE:
            {
                ISingletonTest var1 = new SingletonTest();
                IScopedTest var2;
            }
            */
        });

        // getting 2 times the same singleton
        ISingletonTest singleton1 = global.GetService<ISingletonTest>();
        ISingletonTest singleton2 = global.GetService<ISingletonTest>();
        Debug.Assert(singleton1 == singleton2, "Broken singleton creation");

        // getting 2 times the same scoped
        IScopedTest scoped1 = global.GetService<IScopedTest>();
        IScopedTest scoped2 = global.GetService<IScopedTest>();
        Debug.Assert(scoped1 == scoped2);
        // check that the dependency injected as singleton
        // is the same created above
        Debug.Assert(scoped1.MySingleton == singleton1, "Broken singleton injection");

        // getting 2 times differnt transients
        TransientTest transient1 = global.GetService<TransientTest>();
        TransientTest transient2 = global.GetService<TransientTest>();
        Debug.Assert(transient1 != transient2, "Broken transient lifetime");
        // check that the dependency injected as singleton
        // is the same created above
        Debug.Assert(transient1.MySingleton == singleton1 &&
                        transient2.MySingleton == singleton1,
                        "Broken singleton injection");
        // check that the dependency injected as scoped
        // is the same created above
        Debug.Assert(transient1.MyScoped == scoped1 &&
                        transient2.MyScoped == scoped1,
                        "Broken scoped injection");
    }
}

```

## **Advanced Usage**

### **Context**

Each scope generates a disposable context that manages the creation, the accesses and the disposition of each object that is attached to the scope. In this way it is possible to spawn a new subcontext that inherits from its parent all the configurations and instances. By doing this, it is necessary to instruct the configuration how to retrieve the instance in the correct context. Each `IScope` represents so a context, and each context can have multiple children. If you want to check if a scope is a root context (no parents), use the `IScope.IsRoot` property.

In this optic we have a new kind of configuration that is named *"Contextual"*. We can expand the lifetime definitions:
- `Transient`: each request will spawn a new object in the current context.
- `Contextual`: the first request will spawn a new object in the current context, then any other request will refer to it.
- `Scoped`: the first request will check if there is a parent scope with an instanced object, if it is present any other request will refer to it, otherwise creates an instance in the current context.
- `Singleton`: at configuration time it will check if there is a parent scope with an instanced object, if it is present any other request will refer to it, otherwise creates a Singleton instance in the current context.

In the same way as before we can configure a contextual declaration:
```c#
// Direct Add - Type variable
myScopeConfiguration.AddContextual(keyType, valueType);

// Direct Add - Generics
myScopeConfiguration.AddContextual<KeyType, ValueType>();

// Direct Remove - Type variable
myScopeConfiguration.RemoveContextual(keyType);

// Direct Remove - Generics
myScopeConfiguration.RemoveContextual<KeyType>();
```

To thread the current context as a root scope, it is possible to retrieve all the services as "Contextual", ignoring the parent inheritance:
```c#
// Type variable
myScope.GetContextualService(keyType);

// Generics
myScope.GetContextualService<KeyType>();
```
This method **IS NOT** a shortcut to retrieve an instance with contextual lifetime, but it is used to prevent inheritance in the current context, while accessing an instance.

### **Context Redefinition**

This kind of configuration allows more flexibility, for example it is possible to redefine different mappings in differents scopes, mantaining the instances of the parent:

```c#
public interface ISingletonTest { }
public class SingletonTest : ISingletonTest { }
public class AnotherSingletonTest : ISingletonTest { }
public class ScopedTest {}
public class InnerScopedTest {}

...

using IScope global = ScopeFactory.CreateRootScope(sc => {
    sc.Add<ScopedTest>()
        .Add<ISingletonTest, SingletonTest>();
    /* RESULTANT SCOPE IMAGE:
        global: {
            ISingletonTest var1 = new SingletonTest();
            ScopedTest var2;
        }
    */
});

using IScope child = global.Spawn(sc => {
    // REDEFINITION: we redefine the singleton mapping  in the
    // child scope, such that the resultant type is different,
    // using a scope shadowing
    sc.AddSingleton<ISingletonTest, AnotherSingletonTest>()
        .AddScoped<InnerScopedTest>();

     /* RESULTANT SCOPE IMAGE:
        global: {
            ISingletonTest var1 = new SingletonTest();
            ScopedTest var2;
            child: {
                ISingletonTest var1 = new AnotherSingletonTest();
                InnerScopedTest var3;
            }
        }

        Note how "var1" of the child scope
        shadows the "var1" of the global scope 
    */
});

var scoped = global.GetService<ScopedTest>();        // OK
try {
    global.GetService<InnerScopedTest>();           // ERROR
    Debug.Assert(false, "Broken scope inheritance (reversed)");
}
catch (Exception ex) { }

var singleton = global.GetService<ISingletonTest>();    // SingletonTest
Debug.Assert(singleton.GetType() == typeof(SingletonTest),
                "Broken scope inheritance (redefinition - parent)");

var anotherSingleton = child.GetService<ISingletonTest>();  // AnotherSingletonTest
Debug.Assert(anotherSingleton.GetType() == typeof(AnotherSingletonTest),
                "Broken scope inheritance (redefinition - child)");

var childScoped = child.GetService<ScopedTest>();    // OK
Debug.Assert(childScoped == scoped, "Broken scoped lifetime");
...

```

In such a way it is also possible to redefine also the new lifetime in the given context. Refer to this example in order to understand the results:
```c#
// ID Tracking
public class BaseTest {
    public static int S_ID = 0;
    public int ID;
    public BaseTest() { ID = S_ID++; }

    public override string ToString() => $"<{this.GetType().Name}#{ID}>";
}

public interface ISingletonTest { }
public interface IScopedTest { }
public interface ITransientTest { }
public interface IContextualTest { }

public class SingletonTest : BaseTest, ISingletonTest { }
public class ScopedTest : BaseTest, IScopedTest { }
public class TransientTest : BaseTest, ITransientTest { }
public class ContextualTest : BaseTest, IContextualTest { }

public class AnotherSingletonTest : BaseTest, ISingletonTest { }
public class AnotherScopedTest : BaseTest, IScopedTest { }
public class AnotherTransientTest : BaseTest, ITransientTest { }
public class AnotherContextualTest : BaseTest, IContextualTest { }

class Program {
    static void Main(string[] args) {

        using IScope global = ScopeFactory.CreateRootScope(sc => {
            // FIRST DEFINITIONS
            sc.AddSingleton<ISingletonTest, SingletonTest>()
                .AddScoped<IScopedTest, ScopedTest>()
                .AddTransient<ITransientTest, TransientTest>()
                .AddContextual<IContextualTest, ContextualTest>();
        });

        using IScope childc = global.Spawn(sc => {
            // REDEFINITION - same classes
            sc.AddSingleton<ISingletonTest, SingletonTest>()
                .AddScoped<IScopedTest, ScopedTest>()
                .AddTransient<ITransientTest, TransientTest>()
                .AddContextual<IContextualTest, ContextualTest>();
        });

        using IScope childr = global.Spawn(sc => {
            // REDEFINITION - new classes
            sc.AddSingleton<ISingletonTest, AnotherSingletonTest>()
                .AddScoped<IScopedTest, AnotherScopedTest>()
                .AddTransient<ITransientTest, AnotherTransientTest>()
                .AddContextual<IContextualTest, AnotherContextualTest>();
        });


        var sg = global.GetService<ISingletonTest>();   // <SingletonTest#0> (config time bound)
        var cg = global.GetService<IScopedTest>();      // <ScopedTest#2>
        var tg = global.GetService<ITransientTest>();   // <TransientTest#3>
        var xg = global.GetService<IContextualTest>();  // <ContextualTest#4>

        var sr = childc.GetService<ISingletonTest>();   // <SingletonTest#0> (same as global, config time bound)
        var cr = childc.GetService<IScopedTest>();      // <ScopedTest#2> (same as global)
        var tr = childc.GetService<ITransientTest>();   // <TransientTest#5>
        var xr = childc.GetService<IContextualTest>();  // <ContextualTest#6>

        var sc = childr.GetService<ISingletonTest>();   // <AnotherSingletonTest#1> (config time bound)
        var cc = childr.GetService<IScopedTest>();      // <AnotherscopedTest#7>
        var tc = childr.GetService<ITransientTest>();   // <AnotherTransientTest#8>
        var xc = childr.GetService<IContextualTest>();  // <AnotherContextualTest#9>

    }
}
```

### **Constructors and Destructors**
When configuring a scope, for any kind of lifetime, it is also possible to define a custom constructor or custom destructor by passing it to the declaration:

```c#
class ConstructorTest {
    public ConstructorTest(int x) { Console.WriteLine($"My number is: {x}"); }
}

...
scopeConfig.AddScoped<ConstructorTest>(scope => new ConstructorTest(5));
...

var t1 = scope.GetService<ISingletonTest>(); // My number is: 5
var t2 = scope.GetService<ISingletonTest>(); // no output
Debug.Assert(t1 == t2, "Broken scoped lifetime");
```

The destructor works slightly different. When a scope is disposed, every disposable reference it holds will be disposed too. A destructor will create a custom disposable function that will be called when the scope is disposed. Note that if a custom destructor is passed, the default destructor will not be called, so make sure to dispose your object in the custom destructor.

```c#
// By default the scope will call the Dispose function
// if the object implements the IDisposable interface 
class DisposableTest : IDisposable {
    void Dispose() { Console.WriteLine("Disposed!"); }
}
scopeConfig.AddScoped<DisposableTest>();
scope.Dispose(); // Disposed!


// Passing a custom destructor
class DestructorTest {
    void Destruct() { Console.WriteLine("Destructed!"); }
}
scopeConfig.AddScoped<DestructorTest>(null, (scope, instance) => {
    instance.Destruct();
});
scope.Dispose(); // Destructed!

class DestructorAndDisposableTest : IDisposable {
    void Destruct() { Console.WriteLine("Destructed!"); }
    void Dispose() { Console.WriteLine("Disposed!"); }
}
// Side effects of custom destructor: Dispose not called automatically
scopeConfig.AddScoped<DestructorAndDisposableTest>(null, (scope, instance) => {
    instance.Destruct();
});
scope.Dispose(); // Destructed!

// Side effects of custom destructor: Dispose not called automatically
scopeConfig.AddScoped<DestructorAndDisposableTest>(null, (scope, instance) => {
    instance.Destruct();
    instance.Dispose(); // necessary if the dispose is needed
});
scope.Dispose(); // Destructed! Disposed!
```

### **Reference Holding**

It is also possible to configure the scope to inject an external reference as owned by the scope itself:

```cs
var myInstance = new InstanceType(...);

scopeConfig.Add<KeyType, InstanceType>(myInstance, myLifetime);
scopeConfig.AddSingleton<KeyType, InstanceType>(myInstance);
scopeConfig.AddScoped<KeyType, InstanceType>(myInstance);
scopeConfig.AddContextual<KeyType, InstanceType>(myInstance);
scopeConfig.AddTransient<KeyType, InstanceType>(myInstance);

scope.GetService<InstanceType>(); // == myInstance
scope.Dispose(); // => myInstance.Dispose(); (if InstanceType is IDisposable)
```

This will make the scope manage the lifetime of the reference, destructing it as soon as the scope is disposed.
To prevent this behaviour, just pass a void action as a custom destructor:
```cs 
scopeConfig.Add<KeyType, InstanceType>(myInstance, myLifetime, (scope, instance) => {});
```


## **Extensions**
**Inject** supports extensions with c# extensions methods.

Here a list of available extensions:
- [EntityFramework](Injekt.EF/README.md)