using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Security;
using ProjectAtmaca.Infrastructure.Tests.Persistence.Participations;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Security;

[Collection(SqlIntegrationCollection.Name)]
public sealed class ActorIdentityMappingSqlContractTests
{
    [Fact]
    public async Task Database_Should_PreserveCaseSensitiveExternalIdentities_AndAllowMultipleMappingsPerActor()
    {
        // Arrange
        string discriminator =
            Guid.NewGuid()
                .ToString("N");

        string upperCaseIssuer =
            $"https://Identity-{discriminator}.projectatmaca.test";

        string lowerCaseIssuer =
            $"https://identity-{discriminator}.projectatmaca.test";

        const string lowerCaseSubject =
            "subject-a";

        const string upperCaseSubject =
            "Subject-A";

        ActorId actorId =
            ActorId.New();

        ActorIdentityMapping issuerCaseMapping =
            ActorIdentityMapping.Create(
                ExternalIdentity.Create(
                    upperCaseIssuer,
                    lowerCaseSubject),
                actorId);

        ActorIdentityMapping canonicalMapping =
            ActorIdentityMapping.Create(
                ExternalIdentity.Create(
                    lowerCaseIssuer,
                    lowerCaseSubject),
                actorId);

        ActorIdentityMapping subjectCaseMapping =
            ActorIdentityMapping.Create(
                ExternalIdentity.Create(
                    lowerCaseIssuer,
                    upperCaseSubject),
                actorId);

        await using (
            ProjectAtmacaDbContext writeContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            writeContext
                .Set<ActorIdentityMapping>()
                .AddRange(
                    issuerCaseMapping,
                    canonicalMapping,
                    subjectCaseMapping);

            await writeContext.SaveChangesAsync(
                CancellationToken.None);
        }

        // Act
        await using ProjectAtmacaDbContext readContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        var storedMappings =
            await readContext
                .Set<ActorIdentityMapping>()
                .AsNoTracking()
                .Where(
                    mapping =>
                        mapping.ActorId == actorId)
                .Select(
                    mapping =>
                        new
                        {
                            mapping.Issuer,
                            mapping.Subject
                        })
                .ToListAsync(
                    CancellationToken.None);

        // Assert
        storedMappings
            .Should()
            .HaveCount(3);

        storedMappings
            .Should()
            .ContainSingle(
                mapping =>
                    mapping.Issuer == upperCaseIssuer &&
                    mapping.Subject == lowerCaseSubject);

        storedMappings
            .Should()
            .ContainSingle(
                mapping =>
                    mapping.Issuer == lowerCaseIssuer &&
                    mapping.Subject == lowerCaseSubject);

        storedMappings
            .Should()
            .ContainSingle(
                mapping =>
                    mapping.Issuer == lowerCaseIssuer &&
                    mapping.Subject == upperCaseSubject);
    }

    [Fact]
    public async Task Database_Should_PreserveTrailingSpaceIdentity_AndComputedByteLengths()
    {
        // Arrange
        string issuer =
            $"https://identity-{Guid.NewGuid():N}.projectatmaca.test";

        string subjectWithoutTrailingSpace =
            $"subject-{Guid.NewGuid():N}";

        string subjectWithTrailingSpace =
            subjectWithoutTrailingSpace + " ";

        ActorId actorId =
            ActorId.New();

        ActorIdentityMapping exactMapping =
            ActorIdentityMapping.Create(
                ExternalIdentity.Create(
                    issuer,
                    subjectWithoutTrailingSpace),
                actorId);

        ActorIdentityMapping trailingSpaceMapping =
            ActorIdentityMapping.Create(
                ExternalIdentity.Create(
                    issuer,
                    subjectWithTrailingSpace),
                actorId);

        await using (
            ProjectAtmacaDbContext writeContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            writeContext
                .Set<ActorIdentityMapping>()
                .AddRange(
                    exactMapping,
                    trailingSpaceMapping);

            await writeContext.SaveChangesAsync(
                CancellationToken.None);
        }

        // Act
        await using ProjectAtmacaDbContext readContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        var storedMappings =
            await readContext
                .Set<ActorIdentityMapping>()
                .AsNoTracking()
                .Where(
                    mapping =>
                        mapping.ActorId == actorId)
                .Select(
                    mapping =>
                        new
                        {
                            mapping.Subject,
                            SubjectByteLength =
                                EF.Property<int>(
                                    mapping,
                                    "SubjectByteLength")
                        })
                .ToListAsync(
                    CancellationToken.None);

        // Assert
        storedMappings
            .Should()
            .HaveCount(2);

        var exactStoredMapping =
            storedMappings.Single(
                mapping =>
                    mapping.Subject ==
                        subjectWithoutTrailingSpace);

        var trailingStoredMapping =
            storedMappings.Single(
                mapping =>
                    mapping.Subject ==
                        subjectWithTrailingSpace);

        exactStoredMapping.SubjectByteLength
            .Should()
            .Be(
                subjectWithoutTrailingSpace.Length *
                sizeof(char));

        trailingStoredMapping.SubjectByteLength
            .Should()
            .Be(
                subjectWithTrailingSpace.Length *
                sizeof(char));

        trailingStoredMapping.SubjectByteLength
            .Should()
            .Be(
                exactStoredMapping.SubjectByteLength +
                sizeof(char));
    }

    [Fact]
    public async Task Database_Should_RejectRemappingAnExactExternalIdentity()
    {
        // Arrange
        ExternalIdentity externalIdentity =
            ExternalIdentity.Create(
                $"https://identity-{Guid.NewGuid():N}.projectatmaca.test",
                $"subject-{Guid.NewGuid():N}");

        ActorIdentityMapping originalMapping =
            ActorIdentityMapping.Create(
                externalIdentity,
                ActorId.New());

        await using (
            ProjectAtmacaDbContext originalContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            originalContext
                .Set<ActorIdentityMapping>()
                .Add(originalMapping);

            await originalContext.SaveChangesAsync(
                CancellationToken.None);
        }

        await using ProjectAtmacaDbContext conflictingContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        ActorIdentityMapping conflictingMapping =
            ActorIdentityMapping.Create(
                externalIdentity,
                ActorId.New());

        conflictingContext
            .Set<ActorIdentityMapping>()
            .Add(conflictingMapping);

        // Act
        Func<Task> saveConflict =
            async () =>
            {
                await conflictingContext.SaveChangesAsync(
                    CancellationToken.None);
            };

        // Assert
        await saveConflict
            .Should()
            .ThrowAsync<DbUpdateException>();
    }
}