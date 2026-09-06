using System.Security.Claims;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

using ProjectAtmaca.Api.Security;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;

namespace ProjectAtmaca.Api.Tests.Security;

public sealed class HttpContextCurrentActorTests
{
    [Fact]
    public void ActorId_Should_ReturnCanonicalActorFromTrustedResolutionIdentity()
    {
        // Arrange
        ActorId expectedActorId =
            ActorId.New();

        ClaimsIdentity resolutionIdentity =
            CreateResolutionIdentity(
                expectedActorId.Value.ToString("D"));

        HttpContextAccessor accessor =
            CreateAccessor(
                resolutionIdentity);

        ICurrentActor currentActor =
            new HttpContextCurrentActor(
                accessor);

        // Act
        ActorId actorId =
            currentActor.ActorId;

        // Assert
        actorId
            .Should()
            .Be(expectedActorId);
    }

    [Fact]
    public void ActorId_Should_IgnoreSpoofedActorClaimFromBearerIdentity()
    {
        // Arrange
        ClaimsIdentity bearerIdentity =
            new(
                [
                    new Claim(
                        ActorClaimTypes.ActorId,
                        ActorId.New()
                            .Value
                            .ToString("D"))
                ],
                authenticationType:
                    "Bearer");

        HttpContextAccessor accessor =
            CreateAccessor(
                bearerIdentity);

        ICurrentActor currentActor =
            new HttpContextCurrentActor(
                accessor);

        // Act
        Func<ActorId> readActorId =
            () =>
                currentActor.ActorId;

        // Assert
        readActorId
            .Should()
            .Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-actor-id")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void ActorId_Should_RejectInvalidTrustedActorClaim(
        string actorClaimValue)
    {
        // Arrange
        ClaimsIdentity resolutionIdentity =
            CreateResolutionIdentity(
                actorClaimValue);

        HttpContextAccessor accessor =
            CreateAccessor(
                resolutionIdentity);

        ICurrentActor currentActor =
            new HttpContextCurrentActor(
                accessor);

        // Act
        Func<ActorId> readActorId =
            () =>
                currentActor.ActorId;

        // Assert
        readActorId
            .Should()
            .Throw<InvalidOperationException>();
    }

    [Fact]
    public void ActorId_Should_RejectAmbiguousTrustedActorClaims()
    {
        // Arrange
        ClaimsIdentity resolutionIdentity =
            new(
                [
                    new Claim(
                        ActorClaimTypes.ActorId,
                        ActorId.New()
                            .Value
                            .ToString("D")),
                    new Claim(
                        ActorClaimTypes.ActorId,
                        ActorId.New()
                            .Value
                            .ToString("D"))
                ],
                ActorClaimTypes
                    .ResolutionAuthenticationType);

        HttpContextAccessor accessor =
            CreateAccessor(
                resolutionIdentity);

        ICurrentActor currentActor =
            new HttpContextCurrentActor(
                accessor);

        // Act
        Func<ActorId> readActorId =
            () =>
                currentActor.ActorId;

        // Assert
        readActorId
            .Should()
            .Throw<InvalidOperationException>();
    }

    [Fact]
    public void ActorId_Should_FailClosedOutsideAnHttpRequest()
    {
        // Arrange
        HttpContextAccessor accessor =
            new();

        ICurrentActor currentActor =
            new HttpContextCurrentActor(
                accessor);

        // Act
        Func<ActorId> readActorId =
            () =>
                currentActor.ActorId;

        // Assert
        readActorId
            .Should()
            .Throw<InvalidOperationException>();
    }

    private static ClaimsIdentity CreateResolutionIdentity(
        string actorClaimValue)
    {
        return new ClaimsIdentity(
            [
                new Claim(
                    ActorClaimTypes.ActorId,
                    actorClaimValue)
            ],
            ActorClaimTypes
                .ResolutionAuthenticationType);
    }

    private static HttpContextAccessor CreateAccessor(
        params ClaimsIdentity[] identities)
    {
        DefaultHttpContext httpContext =
            new()
            {
                User =
                    new ClaimsPrincipal(
                        identities)
            };

        return new HttpContextAccessor
        {
            HttpContext =
                httpContext
        };
    }
}