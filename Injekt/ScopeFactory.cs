using System;

namespace Injekt {
    public static class ScopeFactory {
        public static IScope CreateRootScope() {
            return new Scope(new ScopeConfiguration(), null);
        }
        
        public static IScope CreateRootScope(Action<IScopeConfiguration> configurator) {
            ScopeConfiguration configuration = new ScopeConfiguration();
            configurator?.Invoke(configuration);
            return new Scope(configuration, null);
        }

        public static IScopeConfiguration CreateScopeConfiguration() {
            return new ScopeConfiguration();
        }
    }
}
