using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application.Participations.Create;
using ProjectAtmaca.Application;

namespace ProjectAtmaca.Application.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_Should_Register_CreateParticipationCommandHandler()
    {
        // Arrange
        ServiceCollection services =
            new();

        // Act
        services.AddApplication();

        // Assert
        ServiceDescriptor? descriptor =
            services.SingleOrDefault(
                service =>
                    service.ServiceType ==
                    typeof(CreateParticipationCommandHandler));

        descriptor.Should()
            .NotBeNull();

        descriptor!.Lifetime
            .Should()
            .Be(ServiceLifetime.Scoped);
    }
}
