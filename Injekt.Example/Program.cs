using System;
using Microsoft.EntityFrameworkCore;
using Injekt.EntityFramework;

namespace Injekt {
    
    public class ObjectTest : IDisposable {
        private static int s_ID = 0;
        private static int s_Count = 0;
        private int ID;
        public ObjectTest() {
            ID = ++s_ID;
            Console.WriteLine($"[{GetType().Name}] constructed ({ID} - {++s_Count})");
        }
        ~ObjectTest() {
            Console.WriteLine($"[{GetType().Name}] destructed ({ID} - {--s_Count})");
        }
        public void Dispose() {
            Console.WriteLine($"[{GetType().Name}] disposed ({ID})");
        }
    }

    public interface ISingletonTest { }
    public class SingletonTest : ObjectTest, ISingletonTest {
        public readonly int X;
        public SingletonTest(int x) { X = x; }
    }

    public class SingletonTestBis : ObjectTest, ISingletonTest {
        public SingletonTestBis() { }
    }

    public interface IScopedTest { }
    public class ScopedTest : ObjectTest, IScopedTest {
        private ISingletonTest singleton;
        public ScopedTest(ISingletonTest singleton) { this.singleton = singleton; }
    }

    public interface IContextualTest { }
    public class ContextualTest : ObjectTest, IContextualTest {
        private ISingletonTest singleton;
        public ContextualTest(ISingletonTest singleton) { this.singleton = singleton; }
    }

    public interface ITransientTest { }
    public class TransientTest : ObjectTest, ITransientTest {
        private ISingletonTest singleton;
        private IScopedTest scoped;
        public TransientTest(ISingletonTest singleton, IScopedTest scoped) {
            this.singleton = singleton;
            this.scoped = scoped;
        }
    }

    class TestDb : DbContext {
        public TestDb(DbContextOptions opt) : base(opt) { }
    }
    
    
    class Program {
        static void Main(string[] args) {

            ISingletonTest S0 = new SingletonTest(3);

            using IScope global = ScopeFactory.CreateRootScope(sc => {
                sc.AddSingleton(S0)
                    .AddScoped<IScopedTest, ScopedTest>()
                    .AddTransient<ITransientTest, TransientTest>()
                    .AddDbContext<TestDb>(Lifetime.Singleton)
                    .AddContextual<IContextualTest, ContextualTest>();
            });

            ISingletonTest S1 = global.GetService<ISingletonTest>();
            IScopedTest C1 = global.GetService<IScopedTest>();

            TestDb db1 = global.GetService<TestDb>();

            IContextualTest X1 = global.GetService<IContextualTest>();
            ITransientTest T1_1 = global.GetService<ITransientTest>();
            ITransientTest T1_2 = global.GetService<ITransientTest>();

            {
                using IScope child1 = global.Spawn(sc => sc.AddContextual<ISingletonTest, SingletonTestBis>());

                ISingletonTest S2 = child1.GetService<ISingletonTest>();
                IScopedTest C2 = child1.GetService<IScopedTest>();

                IContextualTest X2 = child1.GetService<IContextualTest>();
                ITransientTest T2_1 = child1.GetService<ITransientTest>();
                ITransientTest T2_2 = child1.GetService<ITransientTest>();

                TestDb db2 = child1.GetService<TestDb>();

                IScope child2 = child1.Spawn(sc => sc.AddSingleton<ISingletonTest, SingletonTestBis>());

                ISingletonTest S3 = child2.GetService<ISingletonTest>();
                IScopedTest C3 = child2.GetService<IScopedTest>();

                IContextualTest X3 = child2.GetService<IContextualTest>();
                ITransientTest T3_1 = child2.GetService<ITransientTest>();
                ITransientTest T3_2 = child2.GetService<ITransientTest>();
            }


        }
    }
}
