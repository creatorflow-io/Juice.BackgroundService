using FluentAssertions;
using Juice.BgService.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Juice.BgService.Tests.XUnit
{
    /// <summary>
    /// Tests for IServiceFactory&lt;T&gt; typed factory priority inside ServiceFactory.
    /// US1: Typed factory is invoked when registered, reflection path bypassed.
    /// US2: No typed factory registered → identical to pre-feature behaviour.
    /// US3: Multiple typed factories do not cross-invoke.
    /// </summary>
    public class TypedFactoryTests
    {
        #region Stub types

        private class ServiceModelStub : IServiceModel
        {
            public Guid? Id { get; set; }
            public string Name { get; set; } = "";
            public Dictionary<string, object?> Options { get; set; } = new();
            public string AssemblyQualifiedName { get; set; } = "";
        }

        private class StubServiceA : ManagedService
        {
            public override Task<(bool Healthy, string Message)> HealthCheckAsync()
                => Task.FromResult((true, string.Empty));
            protected override Task ExecuteAsync() => Task.CompletedTask;
        }

        private class StubServiceB : ManagedService
        {
            public override Task<(bool Healthy, string Message)> HealthCheckAsync()
                => Task.FromResult((true, string.Empty));
            protected override Task ExecuteAsync() => Task.CompletedTask;
        }

        private class StubServiceAFactory : IServiceFactory<StubServiceA>
        {
            public int CallCount { get; private set; }
            public IManagedService? ReturnValue { get; set; } = new StubServiceA();

            public IManagedService? CreateService<TModel>()
                where TModel : class, IServiceModel
            {
                CallCount++;
                return ReturnValue;
            }
        }

        private class StubServiceBFactory : IServiceFactory<StubServiceB>
        {
            public int CallCount { get; private set; }
            public IManagedService? ReturnValue { get; set; } = new StubServiceB();

            public IManagedService? CreateService<TModel>()
                where TModel : class, IServiceModel
            {
                CallCount++;
                return ReturnValue;
            }
        }

        private class ThrowingFactory : IServiceFactory<StubServiceA>
        {
            public IManagedService? CreateService<TModel>()
                where TModel : class, IServiceModel
                => throw new InvalidOperationException("Factory intentionally threw");
        }

        private static ServiceFactory BuildServiceFactory(IServiceCollection services)
        {
            services.AddLogging();
            services.AddSingleton<ServiceFactory>();
            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<ServiceFactory>();
        }

        #endregion

        // ── US1 ──────────────────────────────────────────────────────────────

        [Fact(DisplayName = "US1: Typed factory is invoked and its result returned")]
        public void Typed_factory_is_invoked_and_result_returned()
        {
            var stubFactory = new StubServiceAFactory();
            var services = new ServiceCollection();
            services.AddSingleton<IServiceFactory<StubServiceA>>(stubFactory);
            var factory = BuildServiceFactory(services);

            var result = factory.CreateService<ServiceModelStub>(
                new ServiceModelStub { AssemblyQualifiedName = typeof(StubServiceA).AssemblyQualifiedName! });

            result.Should().NotBeNull();
            result.Should().BeOfType<StubServiceA>();
            stubFactory.CallCount.Should().Be(1);
        }

        [Fact(DisplayName = "US1: Typed factory returning null falls back to reflection path")]
        public void Typed_factory_null_fallback_creates_via_reflection()
        {
            var stubFactory = new StubServiceAFactory { ReturnValue = null };
            var services = new ServiceCollection();
            services.AddSingleton<IServiceFactory<StubServiceA>>(stubFactory);
            var factory = BuildServiceFactory(services);

            var result = factory.CreateService<ServiceModelStub>(
                new ServiceModelStub { AssemblyQualifiedName = typeof(StubServiceA).AssemblyQualifiedName! });

            // Factory was called (opt-out), fallback reflection path created the service
            stubFactory.CallCount.Should().Be(1);
            result.Should().NotBeNull();
            result.Should().BeOfType<StubServiceA>();
        }

        [Fact(DisplayName = "US1: Typed factory exception falls back to reflection path")]
        public void Typed_factory_exception_fallback_creates_via_reflection()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IServiceFactory<StubServiceA>, ThrowingFactory>();
            var factory = BuildServiceFactory(services);

            var result = factory.CreateService<ServiceModelStub>(
                new ServiceModelStub { AssemblyQualifiedName = typeof(StubServiceA).AssemblyQualifiedName! });

            // Despite exception in typed factory, reflection path produces the service
            result.Should().NotBeNull();
            result.Should().BeOfType<StubServiceA>();
        }

        [Fact(DisplayName = "US1: Typed factory for ServiceA is NOT invoked when creating ServiceB")]
        public void Typed_factory_not_invoked_for_unrelated_type()
        {
            var stubFactoryA = new StubServiceAFactory();
            var services = new ServiceCollection();
            services.AddSingleton<IServiceFactory<StubServiceA>>(stubFactoryA);
            var factory = BuildServiceFactory(services);

            var result = factory.CreateService<ServiceModelStub>(
                new ServiceModelStub { AssemblyQualifiedName = typeof(StubServiceB).AssemblyQualifiedName! });

            result.Should().NotBeNull();
            result.Should().BeOfType<StubServiceB>();
            stubFactoryA.CallCount.Should().Be(0, "factory for ServiceA must not be consulted for ServiceB");
        }

        [Fact(DisplayName = "US1: IsServiceExists returns true for type with registered typed factory")]
        public void IsServiceExists_true_when_typed_factory_registered()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IServiceFactory<StubServiceA>, StubServiceAFactory>();
            var factory = BuildServiceFactory(services);

            factory.IsServiceExists(new ServiceModelStub { AssemblyQualifiedName = typeof(StubServiceA).AssemblyQualifiedName! })
                   .Should().BeTrue();
        }

        // ── US2 ──────────────────────────────────────────────────────────────

        [Fact(DisplayName = "US2: No typed factory registered → reflection path creates service")]
        public void No_typed_factory_falls_through_to_reflection()
        {
            var factory = BuildServiceFactory(new ServiceCollection());

            var result = factory.CreateService<ServiceModelStub>(
                new ServiceModelStub { AssemblyQualifiedName = typeof(StubServiceA).AssemblyQualifiedName! });

            result.Should().NotBeNull();
            result.Should().BeOfType<StubServiceA>();
        }

        [Fact(DisplayName = "US2: No typed factory registered → IsServiceExists returns true for IManagedService")]
        public void No_typed_factory_IsServiceExists_unchanged()
        {
            var factory = BuildServiceFactory(new ServiceCollection());

            factory.IsServiceExists(new ServiceModelStub { AssemblyQualifiedName = typeof(StubServiceA).AssemblyQualifiedName! })
                   .Should().BeTrue();
        }

        [Fact(DisplayName = "US2: Unknown type returns null from CreateService")]
        public void Unknown_type_returns_null()
        {
            var factory = BuildServiceFactory(new ServiceCollection());

            var result = factory.CreateService<ServiceModelStub>(
                new ServiceModelStub { AssemblyQualifiedName = "Some.NonExistent.Type, NonExistentAssembly" });

            result.Should().BeNull();
        }

        // ── US3 ──────────────────────────────────────────────────────────────

        [Fact(DisplayName = "US3: Two typed factories — each called exactly for its own type, zero cross-invocations")]
        public void Multiple_typed_factories_no_cross_invocation()
        {
            var factoryA = new StubServiceAFactory();
            var factoryB = new StubServiceBFactory();

            var services = new ServiceCollection();
            services.AddSingleton<IServiceFactory<StubServiceA>>(factoryA);
            services.AddSingleton<IServiceFactory<StubServiceB>>(factoryB);
            var factory = BuildServiceFactory(services);

            var resultA = factory.CreateService<ServiceModelStub>(
                new ServiceModelStub { AssemblyQualifiedName = typeof(StubServiceA).AssemblyQualifiedName! });
            var resultB = factory.CreateService<ServiceModelStub>(
                new ServiceModelStub { AssemblyQualifiedName = typeof(StubServiceB).AssemblyQualifiedName! });

            resultA.Should().BeOfType<StubServiceA>();
            resultB.Should().BeOfType<StubServiceB>();

            factoryA.CallCount.Should().Be(1, "factory A must be called exactly once for ServiceA");
            factoryB.CallCount.Should().Be(1, "factory B must be called exactly once for ServiceB");
        }
    }
}
