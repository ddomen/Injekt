using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Injekt {
    internal sealed class Scope : IScope {
        private bool Disposed = false;
        private readonly Scope Parent;
        private readonly List<Scope> Children = new List<Scope>();
        private readonly ScopeConfiguration Configuration;
        private readonly Dictionary<Type, object> Instances = new Dictionary<Type, object>();
        private readonly List<IDisposable> Disposables = new List<IDisposable>();
        public bool IsRoot => this.Parent == null;

        private sealed class Disposer : IDisposable {
            private readonly Action<IScope, object> OnDispose;
            private readonly object Target;
            private readonly Scope Scope;
            private bool Disposed = false;
            public Disposer(Action<IScope, object> disposer, Scope scope, object target) {
                OnDispose = disposer;
                Target = target;
                Scope = scope;
            }
            public void Dispose() {
                if (!Disposed) {
                    OnDispose(Scope, Target);
                    Disposed = true;
                }
            }
        }

        internal Scope(ScopeConfiguration configuration, Scope parent) {
            Parent = parent;
            Configuration = configuration;
        
            if (Configuration.Configurations.TryGetValue(Lifetime.Singleton, out Dictionary<Type, Type> singletons)) {
                foreach (KeyValuePair<Type, Type> singleton in singletons) {
                    InstantiateSingleton(singleton.Key, singleton.Value);
                }
            }
        }

        internal void OnAttachedConfigurationChange(Type type, Type target, Lifetime lifetime) {
            if (lifetime == Lifetime.Singleton) { InstantiateSingleton(type, target); }
        }

        private object AddInstance(Type type, object result) {
            Instances.Add(type, result);
            Action<IScope, object> disposer = Configuration.GetDestructor(result.GetType());
            if (disposer != null) { Disposables.Add(new Disposer(disposer, this, result)); }
            else if (result is IDisposable disposable) { Disposables.Add(disposable); }
            return result;
        }
        private object AddInstance(Type type, Lifetime lifetime) => AddInstance(type, Instantiate(type, lifetime));

        private bool TryGetBuilder(Type type, out Func<IScope, object> builder) {
            Func<IScope, object> constructor = Configuration.GetConstructor(type);
            if (constructor == null && !IsRoot) { Parent.TryGetBuilder(type, out constructor); }
            builder = constructor;
            return constructor != null;
        }

        private object Instantiate(Type type) {
            if (Disposed) { throw new Exception("Disposed scope"); }
            if (TryGetBuilder(type, out Func<IScope, object> builder)) { return builder.Invoke(this); }

            // Constructor recovery
            ConstructorInfo ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
            if (ctor == null) { throw new Exception($"The class '{type.FullName}' has no available public constructor!"); }
            
            // Parametere recovery
            ParameterInfo[] parameterInfos = ctor.GetParameters();
            object[] parameters = new object[parameterInfos.Length];
            for (int i = 0; i < parameterInfos.Length; ++i) {
                parameters[i] = GetService(parameterInfos[i].ParameterType);
            }

            // Constructor invocation
            return ctor.Invoke(parameters);
        }

        private object Instantiate(Type serviceType, Lifetime lifetime) {
            Type resolved = GetServiceType(serviceType, lifetime);
            if (resolved == null) { throw new Exception($"No binding found for the class '{serviceType.FullName}'!"); }
            return Instantiate(resolved);
        }

        private object InstantiateTransient(Type type, Lifetime lifetime) {
            object result = Instantiate(type, lifetime);
            if (result is IDisposable disposable) { Disposables.Add(disposable); }
            return result;
        }

        private object InstantiateSingleton(Type serviceType, Type target) {
            object singleton = GetService(serviceType, Lifetime.Singleton, false);
            if (singleton != null && singleton.GetType().IsAssignableFrom(target)) { return singleton; }
            return AddInstance(serviceType, Instantiate(target));
        }

        private Lifetime? GetServiceLifetime(Type type) {
            Lifetime? lifetime = Configuration.GetServiceLifetime(type);
            if (lifetime.HasValue || IsRoot) { return lifetime; }
            return Parent.GetServiceLifetime(type);
        }

        private Type GetServiceType(Type serviceType, Lifetime lifetime) {
            Type resolved = Configuration.Resolve(serviceType, lifetime);
            if (resolved != null || IsRoot) { return resolved; }
            return Parent.GetServiceType(serviceType, lifetime);
        }


        public object GetContextualService(Type type) {
            if (Disposed) { throw new Exception("Disposed scope"); }

            Lifetime? lifetime = Configuration.GetServiceLifetime(type);
            if (lifetime == Lifetime.Transient) { return InstantiateTransient(type, lifetime.Value); }

            if (!Instances.ContainsKey(type)) {
                if (lifetime == Lifetime.Scoped || lifetime == Lifetime.Contextual) { AddInstance(type, lifetime); }
                else { throw new Exception($"No binding found for '{type.FullName}' in the current context"); }
            }
            return Instances[type];
        }

        private object GetService(Type type, Lifetime lifetime, bool insert) {
            if (lifetime == Lifetime.Transient) { return InstantiateTransient(type, lifetime); }
            
            if (Instances.ContainsKey(type)) { return Instances[type]; }
            else if (!IsRoot && lifetime != Lifetime.Contextual) { 
                object result = Parent.GetService(type, lifetime, false);
                Type expected = result == null ? null : Configuration.Resolve(type, lifetime);
                if (insert && (result == null || result.GetType() != expected)) { result = AddInstance(type, lifetime); }
                return result;
            }
            else if (insert && lifetime == Lifetime.Scoped || lifetime == Lifetime.Contextual) { return AddInstance(type, lifetime); }
            return null;
        }

        public object GetService(Type type) {
            if (Disposed) { throw new Exception("Disposed scope"); }
            if (type == typeof(IScope)) { return this; }
            else if (type == typeof(IScopeConfiguration)) { return Configuration; }
            Lifetime lifetime = GetServiceLifetime(type) ?? throw new Exception($"No binding found for '{type.FullName}' in the current context tree");
            object result = GetService(type, lifetime, true);
            if (result == null) { throw new Exception($"No binding found for '{type.FullName}' in the current context tree"); }
            return result;
        }

        public IScope Spawn() => Spawn(new ScopeConfiguration());
        public IScope Spawn(ScopeConfiguration configuration) {
            if (Disposed) { throw new Exception("Disposed scope"); }
            Scope child = new Scope(configuration, this);
            this.Children.Add(child);
            return child;
        }

        public IScope Spawn(Action<IScopeConfiguration> configurator) {
            if (Disposed) { throw new Exception("Disposed scope"); }
            ScopeConfiguration configuration = new ScopeConfiguration();
            configurator?.Invoke(configuration);
            return Spawn(configuration);
        }

        public IScope Configure(Action<IScopeConfiguration> configurator) { configurator?.Invoke(Configuration); return this; }

        public void Dispose() {
            if (!Disposed) {
                for (int i = 0; i < Children.Count; ++i) {
                    Children[i].Dispose();
                }
                Children.Clear();

                foreach (IDisposable disposable in Disposables) {
                    disposable.Dispose();
                }
                Disposables.Clear();
                Instances.Clear();

                Configuration.Dispose();

                if (Parent != null) { Parent.Children.Remove(this); }

                Disposed = true;
            }
        }
    }

    
}
