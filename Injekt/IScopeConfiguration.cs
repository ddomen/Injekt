using System;
using System.Collections;

namespace Injekt {
    public enum Lifetime {
        Singleton,
        Scoped,
        Contextual,
        Transient
    }

    public interface IScopeConfiguration : IDisposable {
        IScopeConfiguration Add(Type accessType, Type serviceType, Lifetime lifetime, Func<IScope, object> constructor, Action<IScope, object> destructor);
        IScopeConfiguration Remove(Type accessType, Lifetime lifetime);
        IScopeConfiguration Clone();
    }

    public static class IscopeConfigurationBaseExtension {

        private static Func<IScope, object> ConvertConstructor<T>(Func<IScope, T> constructor) {
            if (constructor == null) { return null; }
            return scope => constructor.Invoke(scope);
        }

        private static Action<IScope, object> ConvertDestructor<T>(Action<IScope, T> destructor) {
            if (destructor == null) { return null; }
            return (scope, target) => destructor.Invoke(scope, (T)target);
        }

        public static IScopeConfiguration AddSingleton(this IScopeConfiguration self, Type type, Type target, Func<IScope, object> constructor = null, Action<IScope, object> destructor = null)
            => self.Add(type, target, Lifetime.Singleton, constructor, destructor);
        public static IScopeConfiguration AddSingleton<I, T>(this IScopeConfiguration self, Func<IScope, T> constructor = null, Action<IScope, T> destructor = null) where T : I
            => self.Add(typeof(I), typeof(T), Lifetime.Singleton, ConvertConstructor(constructor), ConvertDestructor(destructor));
        public static IScopeConfiguration AddSingleton<T>(this IScopeConfiguration self, Func<IScope, T> constructor = null, Action<IScope, T> destructor = null)
            => self.Add(typeof(T), typeof(T), Lifetime.Singleton, ConvertConstructor(constructor), ConvertDestructor(destructor));

        public static IScopeConfiguration AddSingleton(this IScopeConfiguration self, Type type, object value, Action<IScope, object> destructor = null)
            => self.Add(type, type, Lifetime.Singleton, scope => value, destructor);
        public static IScopeConfiguration AddSingleton<T>(this IScopeConfiguration self, T value, Action<IScope, T> destructor = null)
            => self.Add(typeof(T), typeof(T), Lifetime.Singleton, scope => value, ConvertDestructor(destructor));
        public static IScopeConfiguration AddSingleton<I, T>(this IScopeConfiguration self, T value, Action<IScope, T> destructor = null) where T : I
            => self.Add(typeof(I), typeof(T), Lifetime.Singleton, scope => value, ConvertDestructor(destructor));

        public static IScopeConfiguration AddScoped(this IScopeConfiguration self, Type type, Type target, Func<IScope, object> constructor = null, Action<IScope, object> destructor = null)
            => self.Add(type, target, Lifetime.Scoped, constructor, destructor);
        public static IScopeConfiguration AddScoped<I, T>(this IScopeConfiguration self, Func<IScope, T> constructor = null, Action<IScope, T> destructor = null) where T : I
            => self.Add(typeof(I), typeof(T), Lifetime.Scoped, ConvertConstructor(constructor), ConvertDestructor(destructor));
        public static IScopeConfiguration AddScoped<T>(this IScopeConfiguration self, Func<IScope, T> constructor = null, Action<IScope, T> destructor = null)
            => self.Add(typeof(T), typeof(T), Lifetime.Scoped, ConvertConstructor(constructor), ConvertDestructor(destructor));

        public static IScopeConfiguration AddScoped(this IScopeConfiguration self, Type type, object value, Action<IScope, object> destructor = null)
            => self.Add(type, type, Lifetime.Scoped, scope => value, destructor);
        public static IScopeConfiguration AddScoped<T>(this IScopeConfiguration self, T value, Action<IScope, T> destructor = null)
            => self.Add(typeof(T), typeof(T), Lifetime.Scoped, scope => value, ConvertDestructor(destructor));
        public static IScopeConfiguration AddScoped<I, T>(this IScopeConfiguration self, T value, Action<IScope, T> destructor = null) where T : I
            => self.Add(typeof(I), typeof(T), Lifetime.Scoped, scope => value, ConvertDestructor(destructor));

        public static IScopeConfiguration AddContextual(this IScopeConfiguration self, Type type, Type target, Func<IScope, object> constructor = null, Action<IScope, object> destructor = null)
            => self.Add(type, target, Lifetime.Contextual, constructor, destructor);
        public static IScopeConfiguration AddContextual<I, T>(this IScopeConfiguration self, Func<IScope, T> constructor = null, Action<IScope, T> destructor = null) where T : I
            => self.Add(typeof(I), typeof(T), Lifetime.Contextual, ConvertConstructor(constructor), ConvertDestructor(destructor));
        public static IScopeConfiguration AddContextual<T>(this IScopeConfiguration self, Func<IScope, T> constructor = null, Action<IScope, T> destructor = null)
            => self.Add(typeof(T), typeof(T), Lifetime.Contextual, ConvertConstructor(constructor), ConvertDestructor(destructor));

        public static IScopeConfiguration AddContextual(this IScopeConfiguration self, Type type, object value, Action<IScope, object> destructor = null)
            => self.Add(type, type, Lifetime.Contextual, scope => value, destructor);
        public static IScopeConfiguration AddContextual<T>(this IScopeConfiguration self, T value, Action<IScope, T> destructor = null)
            => self.Add(typeof(T), typeof(T), Lifetime.Contextual, scope => value, ConvertDestructor(destructor));
        public static IScopeConfiguration AddContextual<I, T>(this IScopeConfiguration self, T value, Action<IScope, T> destructor = null) where T : I
            => self.Add(typeof(I), typeof(T), Lifetime.Contextual, scope => value, ConvertDestructor(destructor));

        public static IScopeConfiguration AddTransient(this IScopeConfiguration self, Type type, Type target, Func<IScope, object> constructor = null, Action<IScope, object> destructor = null)
            => self.Add(type, target, Lifetime.Transient, constructor, destructor);
        public static IScopeConfiguration AddTransient<I, T>(this IScopeConfiguration self, Func<IScope, T> constructor = null, Action<IScope, T> destructor = null) where T : I
            => self.Add(typeof(I), typeof(T), Lifetime.Transient, ConvertConstructor(constructor), ConvertDestructor(destructor));
        public static IScopeConfiguration AddTransient<T>(this IScopeConfiguration self, Func<IScope, T> constructor = null, Action<IScope, T> destructor = null)
            => self.Add(typeof(T), typeof(T), Lifetime.Transient, ConvertConstructor(constructor), ConvertDestructor(destructor));

        public static IScopeConfiguration AddTransient(this IScopeConfiguration self, Type type, object value, Action<IScope, object> destructor = null)
            => self.Add(type, type, Lifetime.Transient, scope => value, destructor);
        public static IScopeConfiguration AddTransient<T>(this IScopeConfiguration self, T value, Action<IScope, T> destructor = null)
            => self.Add(typeof(T), typeof(T), Lifetime.Transient, scope => value, ConvertDestructor(destructor));
        public static IScopeConfiguration AddTransient<I, T>(this IScopeConfiguration self, T value, Action<IScope, T> destructor = null) where T : I
            => self.Add(typeof(I), typeof(T), Lifetime.Transient, scope => value, ConvertDestructor(destructor));

        public static IScopeConfiguration RemoveSingleton(this IScopeConfiguration self, Type type) => self.Remove(type, Lifetime.Singleton);
        public static IScopeConfiguration RemoveSingleton<T>(this IScopeConfiguration self) => self.Remove(typeof(T), Lifetime.Singleton);

        public static IScopeConfiguration RemoveScoped(this IScopeConfiguration self, Type type) => self.Remove(type, Lifetime.Scoped);
        public static IScopeConfiguration RemoveScoped<T>(this IScopeConfiguration self) => self.Remove(typeof(T), Lifetime.Scoped);

        public static IScopeConfiguration RemoveContextual(this IScopeConfiguration self, Type type) => self.Remove(type, Lifetime.Contextual);
        public static IScopeConfiguration RemoveContextual<T>(this IScopeConfiguration self) => self.Remove(typeof(T), Lifetime.Contextual);

        public static IScopeConfiguration RemoveTransient(this IScopeConfiguration self, Type type) => self.Remove(type, Lifetime.Transient);
        public static IScopeConfiguration RemoveTransient<T>(this IScopeConfiguration self) => self.Remove(typeof(T), Lifetime.Transient);

    }
}
