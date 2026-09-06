using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Security;
using ProjectAtmaca.Infrastructure.Tests.Persistence.Participations;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Security;

[Collection(SqlIntegrationCollection.Name)]
public sealed class ActorIdentityResolverTests
{
    [Fact]
    public async Task ResolveAsync_Should_ResolveExactIssuerAndSubjectWithoutTrackingMappings()
    {
        // Arrange
        string discriminator =
            Guid.NewGuid()
                .ToString("N");

        string issuer =
            $"https://identity-{discriminator}.projectatmaca.test";

        string issuerWithDifferentCase =
            $"https://Identity-{discriminator}.projectatmaca.test";

        string subject =
            $"subject-{discriminator}";

        string subjectWithDifferentCase =
            $"Subject-{discriminator}";

        (ExternalIdentity Identity, ActorId ActorId)[] expectations =
        [
            (
                ExternalIdentity.Create(
                    issuer,
                    subject),
                ActorId.New()),
            (
                ExternalIdentity.Create(
                    issuerWithDifferentCase,
                    subject),
                ActorId.New()),
            (
                ExternalIdentity.Create(
                    issuer,
                    subjectWithDifferentCase),
                ActorId.New()),
            (
                ExternalIdentity.Create(
                    issuer + " ",
                    subject),
                ActorId.New()),
            (
                ExternalIdentity.Create(
                    issuer,
                    subject + " "),
                ActorId.New())
        ];

        ActorIdentityMapping[] mappings =
            expectations
                .Select(
                    expectation =>
                        ActorIdentityMapping.Create(
                            expectation.Identity,
                            expectation.ActorId))
                .ToArray();

        await PersistMappingsAsync(
            mappings);

        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ActorIdentityResolver resolver =
            new(context);

        // Act and Assert
        foreach (
            (ExternalIdentity identity, ActorId expectedActorId)
            in expectations)
        {
            Result<ActorId> result =
                await resolver.ResolveAsync(
                    identity,
                    CancellationToken.None);

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

        context.ChangeTracker
            .Entries<ActorIdentityMapping>()
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_Should_ReturnNotMapped_WhenExactIdentityDoesNotExist()
    {
        // Arrange
        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ActorIdentityResolver resolver =
            new(context);

        ExternalIdentity unmappedIdentity =
            ExternalIdentity.Create(
                $"https://identity-{Guid.NewGuid():N}.projectatmaca.test",
                $"subject-{Guid.NewGuid():N}");

        // Act
        Result<ActorId> result =
            await resolver.ResolveAsync(
                unmappedIdentity,
                CancellationToken.None);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .BeSameAs(
                ActorIdentityResolutionErrors.NotMapped);
    }

    [Fact]
    public async Task ResolveAsync_Should_PropagateCancellation()
    {
        // Arrange
        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ActorIdentityResolver resolver =
            new(context);

        ExternalIdentity externalIdentity =
            ExternalIdentity.Create(
                $"https://identity-{Guid.NewGuid():N}.projectatmaca.test",
                $"subject-{Guid.NewGuid():N}");

        using CancellationTokenSource cancellationSource =
            new();

        cancellationSource.Cancel();

        // Act
        Func<Task> resolve =
            async () =>
            {
                await resolver.ResolveAsync(
                    externalIdentity,
                    cancellationSource.Token);
            };

        // Assert
        await resolve
            .Should()
            .ThrowAsync<OperationCanceledException>();
    }

    private static async Task PersistMappingsAsync(
        params ActorIdentityMapping[] mappings)
    {
        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        context
            .Set<ActorIdentityMapping>()
            .AddRange(
                mappings);

        await context.SaveChangesAsync(
            CancellationToken.None);
    }
}