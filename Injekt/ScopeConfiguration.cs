using System;
using System.Collections.Generic;

namespace Injekt {
    internal sealed class ScopeConfiguration : IScopeConfiguration {
        private bool Disposed = false;
        internal readonly Dictionary<Lifetime, Dictionary<Type, Type>> Configurations = new Dictionary<Lifetime, Dictionary<Type, Type>>();
        internal readonly Dictionary<Type, Func<IScope, object>> Constructors = new Dictionary<Type, Func<IScope, object>>();
        internal readonly Dictionary<Type, Action<IScope, object>> Destructors = new Dictionary<Type, Action<IScope, object>>();

        public IScopeConfiguration Clone() {
            if (Disposed) { throw new Exception("Disposed ScopeConfiguration"); }
            ScopeConfiguration result = new ScopeConfiguration();
            
            foreach (KeyValuePair<Lifetime, Dictionary<Type, Type>> config in Configurations) {
                Dictionary<Type, Type> mapper = new Dictionary<Type, Type>();
                result.Configurations.Add(config.Key, mapper);
                foreach (KeyValuePair<Type, Type> mapping in config.Value) { mapper.Add(mapping.Key, mapping.Value); }
            }

            foreach (KeyValuePair<Type, Func<IScope, object>> constructor in Constructors) { result.Constructors.Add(constructor.Key, constructor.Value); }
            foreach (KeyValuePair<Type, Action<IScope, object>> destructor in Destructors) { result.Destructors.Add(destructor.Key, destructor.Value); }

            return result;
        }

        internal Lifetime? GetServiceLifetime(Type serviceType) {
            if (Disposed) { throw new Exception("Disposed ScopeConfiguration"); }
            foreach (KeyValuePair<Lifetime, Dictionary<Type, Type>> configuration in Configurations) {
                if (configuration.Value.ContainsKey(serviceType)) { return configuration.Key; }
            }
            return null;
        }

        internal Func<IScope, object> GetConstructor(Type type) {
            if (Constructors.TryGetValue(type, out Func<IScope, object> constructor)) { return constructor; }
            return null;
        }

        internal Action<IScope, object> GetDestructor(Type type) {
            if (Destructors.TryGetValue(type, out Action<IScope, object> destructor)) { return destructor; }
            return null;
        }

        internal Type Resolve(Type serviceType, Lifetime lifetime) {
            if (Disposed) { throw new Exception("Disposed ScopeConfiguration"); }
            return Configurations.TryGetValue(lifetime, out Dictionary<Type, Type> target) && target.TryGetValue(serviceType, out Type result) ? result : null;
        }

        public IScopeConfiguration Add(Type type, Type target, Lifetime lifetime, Func<IScope, object> constructor, Action<IScope, object> destructor) {
            if (Disposed) { throw new Exception("Disposed ScopeConfiguration"); }
            if (!type.IsAssignableFrom(target)) { throw new Exception($"'{type.FullName}' is not a subtype of '{target.FullName}'!"); }
            if (Configurations.TryGetValue(lifetime, out Dictionary<Type, Type> container) && container.ContainsKey(type)) {
                throw new Exception($"A '{lifetime}' binding for the class '{type.FullName}' already exists!");
            }
            if (!Configurations.ContainsKey(lifetime)) {
                container = new Dictionary<Type, Type>();
                Configurations.Add(lifetime, container);
            }
            container.Add(type, target);
            if (constructor != null) { Constructors.Add(target, constructor); }
            if (destructor != null) { Destructors.Add(target, destructor); }
            return this;
        }

        public IScopeConfiguration Remove(Type type, Lifetime lifetime) {
            if (Disposed) { throw new Exception("Disposed ScopeConfiguration"); }
            if (!Configurations.TryGetValue(lifetime, out Dictionary<Type, Type> container) || !container.ContainsKey(type)) {
                throw new Exception($"A '{lifetime}' binding for the class '{type.FullName}' does not exists!");
            }
            container.Remove(type);
            return this;
        }

        public void Dispose() {
            if (!Disposed) {
                Configurations.Clear();
                Disposed = true;
            }
        }
    }
}

