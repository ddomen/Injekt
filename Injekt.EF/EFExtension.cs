using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;

namespace Injekt.EntityFramework {

    public static class EFExtension {
        private static Action<DbContextOptionsBuilder> ConvertBuilder<TContext>(Action<DbContextOptionsBuilder<TContext>> builder) where TContext : DbContext {
            if (builder == null) { return null; }
            return b => builder.Invoke((DbContextOptionsBuilder<TContext>)b);
        } 

        public static IScopeConfiguration AddDbContext(this IScopeConfiguration self, Type target, Action<DbContextOptionsBuilder> builder = null, Lifetime lifetime = Lifetime.Scoped, Lifetime optionsLifetime = Lifetime.Scoped) {
            if (!typeof(DbContext).IsAssignableFrom(target)) { throw new Exception($"The class '{target.FullName}' does not extend the class DbContext!"); }

            ConstructorInfo ctor = target.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).FirstOrDefault(c => {
                ParameterInfo[] parameters = c.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(DbContextOptions);
            });
            if (ctor == null) { throw new Exception($"The class '{target.FullName}' must have at least one constructor accepting DbContextOptions"); }

            Type optionsType = typeof(DbContextOptions<>).MakeGenericType(new Type[] { target });
            ConstructorInfo optionsBuilder = typeof(DbContextOptionsBuilder<>).MakeGenericType(new Type[] { target }).GetConstructor(new Type[0]);

            self.AddSingleton<ISingletonOptionsInitializer, SingletonOptionsInitializer>();
            self.AddSingleton<IDbSetFinder, DbSetFinder>();
            self.AddSingleton<IDbSetSource, DbSetSource>();
            self.AddSingleton<IDbSetInitializer, DbSetInitializer>();

            self.Add(optionsType, optionsType, optionsLifetime, scope => {
                DbContextOptionsBuilder currentOptionsBuilder = (DbContextOptionsBuilder)optionsBuilder.Invoke(new object[0]);
                currentOptionsBuilder.UseInternalServiceProvider(scope);
                builder?.Invoke(currentOptionsBuilder);
                return currentOptionsBuilder.Options;
            }, null);
            return self.Add(target, target, lifetime, scope => ctor.Invoke(new object[] { scope.GetService(optionsType) }), null);
        }
        public static IScopeConfiguration AddDbContext(this IScopeConfiguration self, Type target, Lifetime lifetime = Lifetime.Scoped, Lifetime optionsLifetime = Lifetime.Scoped)
            => self.AddDbContext(target, null, lifetime, optionsLifetime);
        public static IScopeConfiguration AddDbContext<TContext>(this IScopeConfiguration self, Action<DbContextOptionsBuilder<TContext>> builder = null, Lifetime lifetime = Lifetime.Scoped, Lifetime optionsLifetime = Lifetime.Scoped)
            where TContext : DbContext => self.AddDbContext(typeof(TContext), ConvertBuilder(builder), lifetime, optionsLifetime);
        public static IScopeConfiguration AddDbContext<TContext>(this IScopeConfiguration self, Lifetime lifetime, Lifetime optionsLifetime = Lifetime.Scoped)
            where TContext : DbContext => self.AddDbContext(typeof(TContext), null, lifetime, optionsLifetime);
    }
}