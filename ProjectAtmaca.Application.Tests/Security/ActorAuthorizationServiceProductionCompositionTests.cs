using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Security;

namespace ProjectAtmaca.Application.Tests.Security;

public sealed class ActorAuthorizationServiceProductionCompositionTests
{
    [Fact]
    public void AddApplication_Should_RegisterActorAuthorizationServiceAsScopedService()
    {
        IServiceCollection services =
            new ServiceCollection();

        services.AddApplication();

        ServiceDescriptor[] descriptors =
            services
                .Where(
                    descriptor =>
                        descriptor.ServiceType ==
                        typeof(IActorAuthorizationService))
                .ToArray();

        descriptors
            .Should()
            .ContainSingle(
                "production application composition must register " +
                "IActorAuthorizationService exactly once");

        ServiceDescriptor descriptor =
            descriptors.Single();

        descriptor.Lifetime
            .Should()
            .Be(
                ServiceLifetime.Scoped);

        descriptor.ImplementationType
            .Should()
            .Be(
                typeof(ActorAuthorizationService));

        descriptor.ImplementationFactory
            .Should()
            .BeNull();

        descriptor.ImplementationInstance
            .Should()
            .BeNull();
    }
}
