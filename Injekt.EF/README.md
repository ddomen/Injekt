# **Injekt**
## **C# Scope Based Dependency Injection Library**
## Extension - Entity Framework

**Injekt** supports the `IServiceProvider` interface in order to mantain compatibility with `EntityFramework` dependency injection.

You can configure a scope to include a `DbContext` in this way:
```c#
myScopeConfiguration.AddDbContext<MyDbContext>(); // Lifetime.Scoped is default
myScopeConfiguration.AddDbContext<MyDbContext>(Lifetime.Singleton);
```

The `AddDbContext` method also supports the usual constructor and destructor arguments.

For more details see the whole [**Injekt** documentation](../README.md).