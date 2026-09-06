using System.Security.Claims;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

using ProjectAtmaca.Api.Security;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Api.Tests.Security;

public sealed class ActorClaimsTransformationTests
{
    private const string IssuerClaimType =
        "iss";

    private const string SubjectClaimType =
        "sub";

    [Fact]
    public async Task TransformAsync_Should_ResolveExactIdentity_RemoveSpoofedClaim_AndRemainIdempotent()
    {
        // Arrange
        const string issuer =
            "https://Identity.ProjectAtmaca.test ";

        const string subject =
            "Subject-Exact ";

        ActorId actorId =
            ActorId.New();

        RecordingActorIdentityResolver resolver =
            new(
                Result<ActorId>.Success(
                    actorId));

        ClaimsIdentity bearerIdentity =
            new(
                [
                    new Claim(
                        IssuerClaimType,
                        issuer),
                    new Claim(
                        SubjectClaimType,
                        subject),
                    new Claim(
                        ActorClaimTypes.ActorId,
                        ActorId.New()
                            .Value
                            .ToString("D"))
                ],
                authenticationType:
                    "Bearer");

        ClaimsPrincipal principal =
            new(
                bearerIdentity);

        using CancellationTokenSource cancellationSource =
            new();

        HttpContextAccessor accessor =
            CreateAccessor(
                principal,
                cancellationSource.Token);

        ActorClaimsTransformation transformation =
            new(
                resolver,
                accessor);

        // Act
        ClaimsPrincipal firstResult =
            await transformation.TransformAsync(
                principal);

        ClaimsPrincipal secondResult =
            await transformation.TransformAsync(
                principal);

        // Assert
        firstResult
            .Should()
            .BeSameAs(principal);

        secondResult
            .Should()
            .BeSameAs(principal);

        resolver.CallCount
            .Should()
            .Be(1);

        resolver.LastIdentity
            .Should()
            .NotBeNull();

        resolver.LastIdentity!.Issuer
            .Should()
            .Be(issuer);

        resolver.LastIdentity.Subject
            .Should()
            .Be(subject);

        resolver.LastCancellationToken
            .Should()
            .Be(
                cancellationSource.Token);

        bearerIdentity
            .FindAll(
                ActorClaimTypes.ActorId)
            .Should()
            .BeEmpty();

        ClaimsIdentity resolutionIdentity =
            principal
                .Identities
                .Single(
                    identity =>
                        identity.AuthenticationType ==
                            ActorClaimTypes
                                .ResolutionAuthenticationType);

        resolutionIdentity
            .FindAll(
                ActorClaimTypes.ActorId)
            .Select(
                claim =>
                    claim.Value)
            .Should()
            .Equal(
                actorId.Value.ToString("D"));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(1, 2)]
    public async Task TransformAsync_Should_FailClosed_WhenExternalIdentityCardinalityIsNotExact(
        int issuerCount,
        int subjectCount)
    {
        // Arrange
        RecordingActorIdentityResolver resolver =
            new(
                Result<ActorId>.Success(
                    ActorId.New()));

        List<Claim> claims =
            [];

        for (int index = 0; index < issuerCount; index++)
        {
            claims.Add(
                new Claim(
                    IssuerClaimType,
                    $"issuer-{index}"));
        }

        for (int index = 0; index < subjectCount; index++)
        {
            claims.Add(
                new Claim(
                    SubjectClaimType,
                    $"subject-{index}"));
        }

        ClaimsPrincipal principal =
            new(
                new ClaimsIdentity(
                    claims,
                    authenticationType:
                        "Bearer"));

        ActorClaimsTransformation transformation =
            new(
                resolver,
                CreateAccessor(
                    principal,
                    TestContext.Current.CancellationToken));

        // Act
        ClaimsPrincipal result =
            await transformation.TransformAsync(
                principal);

        // Assert
        result
            .Should()
            .BeSameAs(principal);

        resolver.CallCount
            .Should()
            .Be(0);

        principal
            .Identities
            .Should()
            .NotContain(
                identity =>
                    identity.AuthenticationType ==
                        ActorClaimTypes
                            .ResolutionAuthenticationType);
    }

    [Theory]
    [InlineData("", "subject")]
    [InlineData("issuer", " ")]
    public async Task TransformAsync_Should_FailClosed_WhenExternalIdentityComponentIsBlank(
        string issuer,
        string subject)
    {
        // Arrange
        RecordingActorIdentityResolver resolver =
            new(
                Result<ActorId>.Success(
                    ActorId.New()));

        ClaimsPrincipal principal =
            new(
                new ClaimsIdentity(
                    [
                        new Claim(
                            IssuerClaimType,
                            issuer),
                        new Claim(
                            SubjectClaimType,
                            subject)
                    ],
                    authenticationType:
                        "Bearer"));

        ActorClaimsTransformation transformation =
            new(
                resolver,
                CreateAccessor(
                    principal,
                    TestContext.Current.CancellationToken));

        // Act
        await transformation.TransformAsync(
            principal);

        // Assert
        resolver.CallCount
            .Should()
            .Be(0);

        principal
            .FindAll(
                ActorClaimTypes.ActorId)
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task TransformAsync_Should_RemoveSpoofedClaim_WithoutTrustingAnUnmappedIdentity()
    {
        // Arrange
        RecordingActorIdentityResolver resolver =
            new(
                Result<ActorId>.Failure(
                    ActorIdentityResolutionErrors.NotMapped));

        ClaimsPrincipal principal =
            new(
                new ClaimsIdentity(
                    [
                        new Claim(
                            IssuerClaimType,
                            "https://identity.projectatmaca.test"),
                        new Claim(
                            SubjectClaimType,
                            "unmapped-subject"),
                        new Claim(
                            ActorClaimTypes.ActorId,
                            ActorId.New()
                                .Value
                                .ToString("D"))
                    ],
                    authenticationType:
                        "Bearer"));

        ActorClaimsTransformation transformation =
            new(
                resolver,
                CreateAccessor(
                    principal,
                    TestContext.Current.CancellationToken));

        // Act
        ClaimsPrincipal result =
            await transformation.TransformAsync(
                principal);

        // Assert
        result
            .Should()
            .BeSameAs(principal);

        resolver.CallCount
            .Should()
            .Be(1);

        principal
            .FindAll(
                ActorClaimTypes.ActorId)
            .Should()
            .BeEmpty();

        principal
            .Identities
            .Should()
            .NotContain(
                identity =>
                    identity.AuthenticationType ==
                        ActorClaimTypes
                            .ResolutionAuthenticationType);
    }

    [Fact]
    public async Task TransformAsync_Should_NotResolveAnUnauthenticatedPrincipal()
    {
        // Arrange
        RecordingActorIdentityResolver resolver =
            new(
                Result<ActorId>.Success(
                    ActorId.New()));

        ClaimsIdentity unauthenticatedIdentity =
            new(
                [
                    new Claim(
                        IssuerClaimType,
                        "https://identity.projectatmaca.test"),
                    new Claim(
                        SubjectClaimType,
                        "subject"),
                    new Claim(
                        ActorClaimTypes.ActorId,
                        ActorId.New()
                            .Value
                            .ToString("D"))
                ]);

        ClaimsPrincipal principal =
            new(
                unauthenticatedIdentity);

        ActorClaimsTransformation transformation =
            new(
                resolver,
                CreateAccessor(
                    principal,
                    TestContext.Current.CancellationToken));

        // Act
        await transformation.TransformAsync(
            principal);

        // Assert
        resolver.CallCount
            .Should()
            .Be(0);

        principal
            .FindAll(
                ActorClaimTypes.ActorId)
            .Should()
            .BeEmpty();
    }

    private static HttpContextAccessor CreateAccessor(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        DefaultHttpContext httpContext =
            new()
            {
                User =
                    principal,
                RequestAborted =
                    cancellationToken
            };

        return new HttpContextAccessor
        {
            HttpContext =
                httpContext
        };
    }

    private sealed class RecordingActorIdentityResolver
        : IActorIdentityResolver
    {
        private readonly Result<ActorId> _result;

        public int CallCount { get; private set; }

        public ExternalIdentity? LastIdentity { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public RecordingActorIdentityResolver(
            Result<ActorId> result)
        {
            _result = result;
        }

        public Task<Result<ActorId>> ResolveAsync(
            ExternalIdentity externalIdentity,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            LastIdentity =
                externalIdentity;

            LastCancellationToken =
                cancellationToken;

            return Task.FromResult(
                _result);
        }
    }
}