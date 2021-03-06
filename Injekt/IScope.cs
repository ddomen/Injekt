using System;

namespace Injekt {
    public interface IScope : IDisposable, IServiceProvider {
        bool IsRoot { get; }
        IScope Spawn();
        IScope Spawn(Action<IScopeConfiguration> configurator);
        IScope Configure(Action<IScopeConfiguration> configurator);
        object GetContextualService(Type type);
    }

    public static class IscopeBaseExtension {

        public static T GetContextualService<T>(this IScope self) => (T)self.GetContextualService(typeof(T));
        public static T GetService<T>(this IScope self) => (T)self.GetService(typeof(T));

    }
}
