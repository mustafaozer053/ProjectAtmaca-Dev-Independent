using FluentAssertions;
using Microsoft.EntityFrameworkCore;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Security;
using ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Security;

[Collection(SqlIntegrationCollection.Name)]
public sealed class ActorPermissionGrantSqlContractTests
{
    [Fact]
    public async Task Database_Should_DistinguishPermissionCodeByCase()
    {
        ActorId actorId =
            ActorId.New();

        string suffix =
            Guid.NewGuid()
                .ToString("N");

        string upperCaseCode =
            $"Security.Case.{suffix}";

        string lowerCaseCode =
            $"security.case.{suffix}";

        ActorPermissionGrant upperCaseGrant =
            ActorPermissionGrant.Create(
                actorId,
                Permission.Create(
                    upperCaseCode));

        ActorPermissionGrant lowerCaseGrant =
            ActorPermissionGrant.Create(
                actorId,
                Permission.Create(
                    lowerCaseCode));

        await using (
            ProjectAtmacaDbContext writeContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            writeContext
                .Set<ActorPermissionGrant>()
                .AddRange(
                    upperCaseGrant,
                    lowerCaseGrant);

            await writeContext
                .SaveChangesAsync();
        }

        string[] storedCodes;

        await using (
            ProjectAtmacaDbContext readContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            storedCodes =
                await readContext
                    .Set<ActorPermissionGrant>()
                    .AsNoTracking()
                    .Where(
                        grant =>
                            grant.ActorId == actorId)
                    .Select(
                        grant =>
                            grant.PermissionCode)
                    .ToArrayAsync();
        }

        storedCodes.Should()
            .HaveCount(2);

        storedCodes.Should()
            .Contain(upperCaseCode);

        storedCodes.Should()
            .Contain(lowerCaseCode);

        upperCaseCode.Should()
            .NotBe(
                lowerCaseCode);
    }

    [Fact]
    public async Task Database_Should_PreserveTrailingSpaceAsDistinctPermissionIdentity()
    {
        ActorId actorId =
            ActorId.New();

        string permissionCode =
            $"Security.Trailing.{Guid.NewGuid():N}";

        string permissionCodeWithTrailingSpace =
            permissionCode + " ";

        ActorPermissionGrant exactGrant =
            ActorPermissionGrant.Create(
                actorId,
                Permission.Create(
                    permissionCode));

        await using (
            ProjectAtmacaDbContext writeContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            writeContext
                .Set<ActorPermissionGrant>()
                .Add(
                    exactGrant);

            await writeContext
                .SaveChangesAsync();

            int insertedRows =
                await writeContext.Database
                    .ExecuteSqlInterpolatedAsync(
                        $"""
                        INSERT INTO [ActorPermissionGrants]
                            ([ActorId], [PermissionCode])
                        VALUES
                            ({actorId.Value}, {permissionCodeWithTrailingSpace});
                        """);

            insertedRows.Should()
                .Be(1);
        }

        await using (
            ProjectAtmacaDbContext readContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            var rows =
                await readContext
                    .Set<ActorPermissionGrant>()
                    .AsNoTracking()
                    .Where(
                        grant =>
                            grant.ActorId == actorId)
                    .Select(
                        grant =>
                            new
                            {
                                grant.PermissionCode,
                                PermissionCodeByteLength =
                                    EF.Property<int>(
                                        grant,
                                        "PermissionCodeByteLength")
                            })
                    .ToArrayAsync();

            rows.Should()
                .HaveCount(2);

            var exactStoredGrant =
                rows.Single(
                    grant =>
                        string.Equals(
                            grant.PermissionCode,
                            permissionCode,
                            StringComparison.Ordinal));

            var trailingStoredGrant =
                rows.Single(
                    grant =>
                        string.Equals(
                            grant.PermissionCode,
                            permissionCodeWithTrailingSpace,
                            StringComparison.Ordinal));

            exactStoredGrant
                .PermissionCodeByteLength
                .Should()
                .Be(
                    permissionCode.Length *
                    sizeof(char));

            trailingStoredGrant
                .PermissionCodeByteLength
                .Should()
                .Be(
                    permissionCodeWithTrailingSpace.Length *
                    sizeof(char));

            trailingStoredGrant
                .PermissionCodeByteLength
                .Should()
                .Be(
                    exactStoredGrant.PermissionCodeByteLength +
                    sizeof(char));
        }
    }

    [Fact]
    public async Task Database_Should_RejectExactDuplicateActorPermissionGrant()
    {
        ActorId actorId =
            ActorId.New();

        Permission permission =
            Permission.Create(
                $"Security.Duplicate.{Guid.NewGuid():N}");

        ActorPermissionGrant originalGrant =
            ActorPermissionGrant.Create(
                actorId,
                permission);

        await using (
            ProjectAtmacaDbContext originalContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            originalContext
                .Set<ActorPermissionGrant>()
                .Add(
                    originalGrant);

            await originalContext
                .SaveChangesAsync();
        }

        Func<Task> saveDuplicate =
            async () =>
            {
                await using ProjectAtmacaDbContext
                    conflictingContext =
                        ParticipationPersistenceTestContextFactory
                            .CreateContext();

                ActorPermissionGrant conflictingGrant =
                    ActorPermissionGrant.Create(
                        actorId,
                        permission);

                conflictingContext
                    .Set<ActorPermissionGrant>()
                    .Add(
                        conflictingGrant);

                await conflictingContext
                    .SaveChangesAsync();
            };

        await saveDuplicate.Should()
            .ThrowAsync<DbUpdateException>();

        await using (
            ProjectAtmacaDbContext verificationContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            int durableGrantCount =
                await verificationContext
                    .Set<ActorPermissionGrant>()
                    .AsNoTracking()
                    .CountAsync(
                        grant =>
                            grant.ActorId == actorId);

            durableGrantCount.Should()
                .Be(1);
        }
    }
}