using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Participations.Create;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Application.Participations.MarkPresent;
using ProjectAtmaca.Application.Participations.RecordArrival;

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
    [Fact]
    public void AddApplication_Should_Register_GetParticipationByIdQueryHandler()
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
                    typeof(GetParticipationByIdQueryHandler));

        descriptor.Should()
            .NotBeNull();

        descriptor!.Lifetime
            .Should()
            .Be(ServiceLifetime.Scoped);
    }
    [Fact]
    public void AddApplication_Should_Register_MarkParticipationPresentCommandHandler()
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
                    typeof(
                        MarkParticipationPresentCommandHandler));

        descriptor.Should()
            .NotBeNull();

        descriptor!.Lifetime
            .Should()
            .Be(ServiceLifetime.Scoped);
    }
    [Fact]
    public void AddApplication_Should_Register_RecordParticipationArrivalCommandHandler()
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
                    typeof(
                        RecordParticipationArrivalCommandHandler));

        descriptor
            .Should()
            .NotBeNull();

        descriptor!.ImplementationType
            .Should()
            .Be(
                typeof(
                    RecordParticipationArrivalCommandHandler));

        descriptor.Lifetime
            .Should()
            .Be(ServiceLifetime.Scoped);
    }
}
