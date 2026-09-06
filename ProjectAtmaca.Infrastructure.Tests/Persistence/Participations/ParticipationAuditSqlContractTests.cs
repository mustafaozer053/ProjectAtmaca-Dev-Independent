using System.Data.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Tests.Persistence;
using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

[Collection(SqlIntegrationCollection.Name)]
public sealed class ParticipationAuditSqlContractTests
{
    [Fact]
    public async Task
        CanonicalActorAuditValues_Should_RoundTripThroughSqlServer()
    {
        // Arrange
        CancellationToken cancellationToken =
            CancellationToken.None;

        Participation participation =
            CreateParticipation();

        ActorId createdByActorId =
            ActorId.New();

        ActorId lastModifiedByActorId =
            ActorId.New();

        participation.SetCreatedBy(
            createdByActorId);

        participation.MarkAsModified(
            lastModifiedByActorId);

        Guid participationId =
            participation.ParticipationId.Value;

        await using (
            ProjectAtmacaDbContext writeContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            writeContext
                .Set<Participation>()
                .Add(
                    participation);

            await writeContext.SaveChangesAsync(
                cancellationToken);
        }

        // Act
        await using ProjectAtmacaDbContext readContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation? reloaded =
            await readContext
                .Set<Participation>()
                .FindAsync(
                    [participationId],
                    cancellationToken);

        // Assert
        reloaded
            .Should()
            .NotBeNull();

        reloaded!.CreatedByActorId
            .Should()
            .Be(
                createdByActorId);

        reloaded.LastModifiedByActorId
            .Should()
            .Be(
                lastModifiedByActorId);

        reloaded.LastModifiedAtUtc
            .Should()
            .NotBeNull();

        readContext
            .Entry(
                reloaded)
            .Property<string?>(
                "CreatedBy")
            .CurrentValue
            .Should()
            .BeNull();

        readContext
            .Entry(
                reloaded)
            .Property<string?>(
                "LastModifiedBy")
            .CurrentValue
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task
        LegacyTextAuditValues_Should_RemainReadableWithoutCanonicalActorIds()
    {
        // Arrange
        CancellationToken cancellationToken =
            CancellationToken.None;

        const string legacyCreatedBy =
            "legacy-created-by";

        const string legacyLastModifiedBy =
            "legacy-last-modified-by";

        Participation participation =
            CreateParticipation();

        Guid participationId =
            participation.ParticipationId.Value;

        await using (
            ProjectAtmacaDbContext writeContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            writeContext
                .Set<Participation>()
                .Add(
                    participation);

            writeContext
                .Entry(
                    participation)
                .Property<string?>(
                    "CreatedBy")
                .CurrentValue =
                    legacyCreatedBy;

            writeContext
                .Entry(
                    participation)
                .Property<string?>(
                    "LastModifiedBy")
                .CurrentValue =
                    legacyLastModifiedBy;

            await writeContext.SaveChangesAsync(
                cancellationToken);
        }

        // Act
        await using ProjectAtmacaDbContext readContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation? reloaded =
            await readContext
                .Set<Participation>()
                .FindAsync(
                    [participationId],
                    cancellationToken);

        // Assert
        reloaded
            .Should()
            .NotBeNull();

        reloaded!.CreatedByActorId
            .Should()
            .BeNull();

        reloaded.LastModifiedByActorId
            .Should()
            .BeNull();

        readContext
            .Entry(
                reloaded)
            .Property<string?>(
                "CreatedBy")
            .CurrentValue
            .Should()
            .Be(
                legacyCreatedBy);

        readContext
            .Entry(
                reloaded)
            .Property<string?>(
                "LastModifiedBy")
            .CurrentValue
            .Should()
            .Be(
                legacyLastModifiedBy);
    }

    [Fact]
    public async Task
        Database_Should_ExposeCanonicalAndLegacyAuditColumns()
    {
        // Arrange
        CancellationToken cancellationToken =
            CancellationToken.None;

        await using ProjectAtmacaDbContext context =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        await context.Database.OpenConnectionAsync(
            cancellationToken);

        DbConnection connection =
            context.Database.GetDbConnection();

        await using DbCommand command =
            connection.CreateCommand();

        command.CommandText =
            """
            SELECT
                [name],
                TYPE_NAME([user_type_id]) AS [StoreType],
                [is_nullable],
                [max_length]
            FROM [sys].[columns]
            WHERE [object_id] =
                OBJECT_ID(N'[dbo].[Participations]')
              AND [name] IN
              (
                  N'CreatedBy',
                  N'LastModifiedBy',
                  N'CreatedByActorId',
                  N'LastModifiedByActorId'
              )
            ORDER BY [name];
            """;

        var columns =
            new Dictionary<string, ColumnShape>(
                StringComparer.Ordinal);

        // Act
        await using DbDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        while (
            await reader.ReadAsync(
                cancellationToken))
        {
            columns.Add(
                reader.GetString(0),
                new ColumnShape(
                    reader.GetString(1),
                    reader.GetBoolean(2),
                    reader.GetInt16(3)));
        }

        // Assert
        columns
            .Should()
            .HaveCount(4);

        columns["CreatedByActorId"]
            .Should()
            .Be(
                new ColumnShape(
                    "uniqueidentifier",
                    IsNullable: true,
                    MaxLength: 16));

        columns["LastModifiedByActorId"]
            .Should()
            .Be(
                new ColumnShape(
                    "uniqueidentifier",
                    IsNullable: true,
                    MaxLength: 16));

        columns["CreatedBy"]
            .Should()
            .Be(
                new ColumnShape(
                    "nvarchar",
                    IsNullable: true,
                    MaxLength: 400));

        columns["LastModifiedBy"]
            .Should()
            .Be(
                new ColumnShape(
                    "nvarchar",
                    IsNullable: true,
                    MaxLength: 400));
    }

    private static Participation CreateParticipation()
    {
        var creationResult =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                AtmacaCardId.New());

        creationResult.IsSuccess
            .Should()
            .BeTrue();

        return creationResult.Value!;
    }

    private sealed record ColumnShape(
        string StoreType,
        bool IsNullable,
        short MaxLength);
}
