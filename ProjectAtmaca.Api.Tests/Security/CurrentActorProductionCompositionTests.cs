using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Api.Security;
using ProjectAtmaca.Api.Tests.Infrastructure;
using ProjectAtmaca.Application.Abstractions.Security;

namespace ProjectAtmaca.Api.Tests.Security;

public sealed class CurrentActorProductionCompositionTests
{
    [Fact]
    public void ProductionHost_Should_ComposeScopedActorSecurityServices()
    {
        // Arrange
        using ProjectAtmacaApiFactory factory =
            new();

        using IServiceScope firstScope =
            factory.Services.CreateScope();

        using IServiceScope secondScope =
            factory.Services.CreateScope();

        // Act
        IClaimsTransformation? firstTransformation =
            firstScope.ServiceProvider
                .GetService<IClaimsTransformation>();

        IClaimsTransformation? repeatedTransformation =
            firstScope.ServiceProvider
                .GetService<IClaimsTransformation>();

        IClaimsTransformation? secondTransformation =
            secondScope.ServiceProvider
                .GetService<IClaimsTransformation>();

        ICurrentActor? firstCurrentActor =
            firstScope.ServiceProvider
                .GetService<ICurrentActor>();

        ICurrentActor? repeatedCurrentActor =
            firstScope.ServiceProvider
                .GetService<ICurrentActor>();

        ICurrentActor? secondCurrentActor =
            secondScope.ServiceProvider
                .GetService<ICurrentActor>();

        IHttpContextAccessor? httpContextAccessor =
            firstScope.ServiceProvider
                .GetService<IHttpContextAccessor>();

        // Assert
        firstTransformation
            .Should()
            .BeOfType<ActorClaimsTransformation>();

        firstTransformation
            .Should()
            .BeSameAs(
                repeatedTransformation);

        firstTransformation
            .Should()
            .NotBeSameAs(
                secondTransformation);

        firstCurrentActor
            .Should()
            .BeOfType<HttpContextCurrentActor>();

        firstCurrentActor
            .Should()
            .BeSameAs(
                repeatedCurrentActor);

        firstCurrentActor
            .Should()
            .NotBeSameAs(
                secondCurrentActor);

        httpContextAccessor
            .Should()
            .NotBeNull();
    }
}