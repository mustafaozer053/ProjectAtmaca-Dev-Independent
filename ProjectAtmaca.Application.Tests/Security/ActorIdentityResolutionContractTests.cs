using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Tests.Security;

public sealed class ActorIdentityResolutionContractTests
{
    [Fact]
    public void ExternalIdentity_Should_PreserveIssuerAndSubjectExactly()
    {
        // Arrange
        const string issuer =
            "urn:project-atmaca:test-issuer";

        const string subject =
            "Actor|Case-Sensitive-01";

        // Act
        ExternalIdentity externalIdentity =
            ExternalIdentity.Create(
                issuer,
                subject);

        // Assert
        externalIdentity.Issuer
            .Should()
            .Be(issuer);

        externalIdentity.Subject
            .Should()
            .Be(subject);
    }

    [Theory]
    [InlineData("", "subject")]
    [InlineData("   ", "subject")]
    [InlineData("issuer", "")]
    [InlineData("issuer", "   ")]
    public void ExternalIdentity_Should_RejectBlankComponents(
        string issuer,
        string subject)
    {
        // Act
        Action action =
            () => ExternalIdentity.Create(
                issuer,
                subject);

        // Assert
        action
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public async Task Resolver_Should_ReturnCanonicalActorId_AsExplicitResult()
    {
        // Arrange
        ExternalIdentity externalIdentity =
            ExternalIdentity.Create(
                "urn:project-atmaca:test-issuer",
                "external-subject-001");

        ActorId expectedActorId =
            ActorId.New();

        IActorIdentityResolver resolver =
            new StubActorIdentityResolver(
                Result<ActorId>.Success(
                    expectedActorId));

        // Act
        Result<ActorId> result =
            await resolver.ResolveAsync(
                externalIdentity,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .BeNull();

        result.Value
            .Should()
            .Be(expectedActorId);
    }

    [Fact]
    public async Task Resolver_Should_RepresentUnmappedIdentity_AsExplicitFailure()
    {
        // Arrange
        ExternalIdentity externalIdentity =
            ExternalIdentity.Create(
                "urn:project-atmaca:test-issuer",
                "unmapped-subject");

        IActorIdentityResolver resolver =
            new StubActorIdentityResolver(
                Result<ActorId>.Failure(
                    ActorIdentityResolutionErrors.NotMapped));

        // Act
        Result<ActorId> result =
            await resolver.ResolveAsync(
                externalIdentity,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .BeSameAs(
                ActorIdentityResolutionErrors.NotMapped);

        result.Error!
            .Code
            .Should()
            .Be(
                "Security.ActorIdentity.NotMapped");
    }

    private sealed class StubActorIdentityResolver
        : IActorIdentityResolver
    {
        private readonly Result<ActorId> _resolution;

        public StubActorIdentityResolver(
            Result<ActorId> resolution)
        {
            _resolution = resolution;
        }

        public Task<Result<ActorId>> ResolveAsync(
            ExternalIdentity externalIdentity,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                externalIdentity);

            cancellationToken
                .ThrowIfCancellationRequested();

            return Task.FromResult(
                _resolution);
        }
    }
}