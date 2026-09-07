using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Participations.ListHistoryByAtmacaCard;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Tests.Persistence;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

[Collection(SqlIntegrationCollection.Name)]
public sealed class ListParticipationHistoryByAtmacaCardIntegrationTests
{
    [Fact]
    public async Task Handle_Should_ReturnOnlyRequestedAtmacaCardHistory_InDescendingCreationOrder()
    {
        // Arrange
        AtmacaCardId targetAtmacaCardId =
            AtmacaCardId.New();

        AtmacaCardId otherAtmacaCardId =
            AtmacaCardId.New();

        Participation oldestParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                targetAtmacaCardId)
            .Value!;

        Participation newestParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                targetAtmacaCardId)
            .Value!;

        Participation middleParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                targetAtmacaCardId)
            .Value!;

        Participation otherCardParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                otherAtmacaCardId)
            .Value!;

        DateTime oldestCreatedAtUtc =
            new(
                2026,
                8,
                25,
                8,
                0,
                0,
                DateTimeKind.Utc);

        DateTime newestCreatedAtUtc =
            new(
                2026,
                8,
                25,
                10,
                0,
                0,
                DateTimeKind.Utc);

        DateTime middleCreatedAtUtc =
            new(
                2026,
                8,
                25,
                9,
                0,
                0,
                DateTimeKind.Utc);

        DateTime otherCardCreatedAtUtc =
            new(
                2026,
                8,
                25,
                11,
                0,
                0,
                DateTimeKind.Utc);

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.AddRange(
                oldestParticipation,
                newestParticipation,
                middleParticipation,
                otherCardParticipation);

            seedContext.Entry(oldestParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    oldestCreatedAtUtc;

            seedContext.Entry(newestParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    newestCreatedAtUtc;

            seedContext.Entry(middleParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    middleCreatedAtUtc;

            seedContext.Entry(otherCardParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    otherCardCreatedAtUtc;

            await seedContext.SaveChangesAsync();
        }

        Dictionary<string, string?> configurationValues =
            new()
            {
                ["ConnectionStrings:ProjectAtmacaDatabase"] =
                    ParticipationPersistenceTestContextFactory
                        .ConnectionString
            };

        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    configurationValues)
                .Build();

        ServiceCollection services =
            new();

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        var query =
            new ListParticipationHistoryByAtmacaCardQuery(
                targetAtmacaCardId,
                10,
                null);

        Result<ParticipationHistoryPage>
            result;

        await using (
            AsyncServiceScope scope =
                serviceProvider.CreateAsyncScope())
        {
            ListParticipationHistoryByAtmacaCardQueryHandler
                handler =
                scope.ServiceProvider
                    .GetRequiredService<
                        ListParticipationHistoryByAtmacaCardQueryHandler>();

            // Act
            result =
                await handler.Handle(
                    query,
                    CancellationToken.None);
        }

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        result.Value
            .Should()
            .NotBeNull();

        result.Value!.Items
            .Should()
            .HaveCount(3);

        result.Value.Items
            .Select(
                item =>
                    item.ParticipationId)
            .Should()
            .Equal(
                newestParticipation
                    .ParticipationId.Value,
                middleParticipation
                    .ParticipationId.Value,
                oldestParticipation
                    .ParticipationId.Value);

        result.Value.Items
            .Should()
            .NotContain(
                item =>
                    item.ParticipationId ==
                    otherCardParticipation
                        .ParticipationId.Value);
    }

    [Fact]
    public async Task Handle_Should_ReturnPageSizeItems_AndNextCursor_WhenMoreHistoryExists()
    {
        // Arrange
        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        Participation firstParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation secondParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation thirdParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation fourthParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        DateTime firstCreatedAtUtc =
            new(
                2026,
                8,
                25,
                12,
                0,
                0,
                DateTimeKind.Utc);

        DateTime secondCreatedAtUtc =
            new(
                2026,
                8,
                25,
                11,
                0,
                0,
                DateTimeKind.Utc);

        DateTime thirdCreatedAtUtc =
            new(
                2026,
                8,
                25,
                10,
                0,
                0,
                DateTimeKind.Utc);

        DateTime fourthCreatedAtUtc =
            new(
                2026,
                8,
                25,
                9,
                0,
                0,
                DateTimeKind.Utc);

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.AddRange(
                firstParticipation,
                secondParticipation,
                thirdParticipation,
                fourthParticipation);

            seedContext.Entry(firstParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    firstCreatedAtUtc;

            seedContext.Entry(secondParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    secondCreatedAtUtc;

            seedContext.Entry(thirdParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    thirdCreatedAtUtc;

            seedContext.Entry(fourthParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    fourthCreatedAtUtc;

            await seedContext.SaveChangesAsync();
        }

        Dictionary<string, string?> configurationValues =
            new()
            {
                ["ConnectionStrings:ProjectAtmacaDatabase"] =
                    ParticipationPersistenceTestContextFactory
                        .ConnectionString
            };

        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    configurationValues)
                .Build();

        ServiceCollection services =
            new();

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        var query =
            new ListParticipationHistoryByAtmacaCardQuery(
                atmacaCardId,
                PageSize: 3,
                Cursor: null);

        Result<ParticipationHistoryPage>
            result;

        await using (
            AsyncServiceScope scope =
                serviceProvider.CreateAsyncScope())
        {
            ListParticipationHistoryByAtmacaCardQueryHandler handler =
                scope.ServiceProvider
                    .GetRequiredService<
                        ListParticipationHistoryByAtmacaCardQueryHandler>();

            // Act
            result =
                await handler.Handle(
                    query,
                    CancellationToken.None);
        }

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        result.Value
            .Should()
            .NotBeNull();

        result.Value!.Items
            .Should()
            .HaveCount(3);

        result.Value.Items
            .Select(
                item =>
                    item.ParticipationId)
            .Should()
            .Equal(
                firstParticipation
                    .ParticipationId.Value,
                secondParticipation
                    .ParticipationId.Value,
                thirdParticipation
                    .ParticipationId.Value);

        result.Value.NextCursor
            .Should()
            .NotBeNull();

        result.Value.NextCursor!.AtmacaCardId
            .Should()
            .Be(atmacaCardId);

        result.Value.NextCursor.CreatedAtUtc
            .Should()
            .Be(thirdCreatedAtUtc);

        result.Value.NextCursor.ParticipationId
            .Should()
            .Be(
                thirdParticipation
                    .ParticipationId.Value);
    }

    [Fact]
    public async Task Handle_Should_ReturnNullNextCursor_WhenHistoryCountEqualsPageSize()
    {
        // Arrange
        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        Participation newestParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation middleParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation oldestParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        DateTime newestCreatedAtUtc =
            new(
                2026,
                8,
                25,
                12,
                0,
                0,
                DateTimeKind.Utc);

        DateTime middleCreatedAtUtc =
            new(
                2026,
                8,
                25,
                11,
                0,
                0,
                DateTimeKind.Utc);

        DateTime oldestCreatedAtUtc =
            new(
                2026,
                8,
                25,
                10,
                0,
                0,
                DateTimeKind.Utc);

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.AddRange(
                newestParticipation,
                middleParticipation,
                oldestParticipation);

            seedContext.Entry(newestParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    newestCreatedAtUtc;

            seedContext.Entry(middleParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    middleCreatedAtUtc;

            seedContext.Entry(oldestParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    oldestCreatedAtUtc;

            await seedContext.SaveChangesAsync();
        }

        Dictionary<string, string?> configurationValues =
            new()
            {
                ["ConnectionStrings:ProjectAtmacaDatabase"] =
                    ParticipationPersistenceTestContextFactory
                        .ConnectionString
            };

        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    configurationValues)
                .Build();

        ServiceCollection services =
            new();

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        var query =
            new ListParticipationHistoryByAtmacaCardQuery(
                atmacaCardId,
                PageSize: 3,
                Cursor: null);

        Result<ParticipationHistoryPage>
            result;

        await using (
            AsyncServiceScope scope =
                serviceProvider.CreateAsyncScope())
        {
            ListParticipationHistoryByAtmacaCardQueryHandler handler =
                scope.ServiceProvider
                    .GetRequiredService<
                        ListParticipationHistoryByAtmacaCardQueryHandler>();

            // Act
            result =
                await handler.Handle(
                    query,
                    CancellationToken.None);
        }

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        result.Value
            .Should()
            .NotBeNull();

        result.Value!.Items
            .Should()
            .HaveCount(3);

        result.Value.Items
            .Select(
                item =>
                    item.ParticipationId)
            .Should()
            .Equal(
                newestParticipation
                    .ParticipationId.Value,
                middleParticipation
                    .ParticipationId.Value,
                oldestParticipation
                    .ParticipationId.Value);

        result.Value.NextCursor
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task Handle_Should_ContinueAfterCursor_WithoutDuplicatesOrGaps()
    {
        // Arrange
        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        Participation newestParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation secondParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation cursorParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation fourthParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation oldestParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        DateTime newestCreatedAtUtc =
            new(
                2026,
                8,
                25,
                12,
                0,
                0,
                DateTimeKind.Utc);

        DateTime secondCreatedAtUtc =
            new(
                2026,
                8,
                25,
                11,
                0,
                0,
                DateTimeKind.Utc);

        DateTime cursorCreatedAtUtc =
            new(
                2026,
                8,
                25,
                10,
                0,
                0,
                DateTimeKind.Utc);

        DateTime fourthCreatedAtUtc =
            new(
                2026,
                8,
                25,
                9,
                0,
                0,
                DateTimeKind.Utc);

        DateTime oldestCreatedAtUtc =
            new(
                2026,
                8,
                25,
                8,
                0,
                0,
                DateTimeKind.Utc);

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.AddRange(
                newestParticipation,
                secondParticipation,
                cursorParticipation,
                fourthParticipation,
                oldestParticipation);

            seedContext.Entry(newestParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    newestCreatedAtUtc;

            seedContext.Entry(secondParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    secondCreatedAtUtc;

            seedContext.Entry(cursorParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    cursorCreatedAtUtc;

            seedContext.Entry(fourthParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    fourthCreatedAtUtc;

            seedContext.Entry(oldestParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    oldestCreatedAtUtc;

            await seedContext.SaveChangesAsync();
        }

        Dictionary<string, string?> configurationValues =
            new()
            {
                ["ConnectionStrings:ProjectAtmacaDatabase"] =
                    ParticipationPersistenceTestContextFactory
                        .ConnectionString
            };

        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    configurationValues)
                .Build();

        ServiceCollection services =
            new();

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        Result<ParticipationHistoryPage>
            firstPageResult;

        await using (
            AsyncServiceScope firstScope =
                serviceProvider.CreateAsyncScope())
        {
            ListParticipationHistoryByAtmacaCardQueryHandler handler =
                firstScope.ServiceProvider
                    .GetRequiredService<
                        ListParticipationHistoryByAtmacaCardQueryHandler>();

            var firstQuery =
                new ListParticipationHistoryByAtmacaCardQuery(
                    atmacaCardId,
                    PageSize: 3,
                    Cursor: null);

            // Act — Page 1
            firstPageResult =
                await handler.Handle(
                    firstQuery,
                    CancellationToken.None);
        }

        // Assert — Page 1
        firstPageResult.IsSuccess
            .Should()
            .BeTrue();

        firstPageResult.Value
            .Should()
            .NotBeNull();

        firstPageResult.Value!.Items
            .Select(
                item =>
                    item.ParticipationId)
            .Should()
            .Equal(
                newestParticipation
                    .ParticipationId.Value,
                secondParticipation
                    .ParticipationId.Value,
                cursorParticipation
                    .ParticipationId.Value);

        firstPageResult.Value.NextCursor
            .Should()
            .NotBeNull();

        ParticipationHistoryCursor cursor =
            firstPageResult.Value.NextCursor!;

        Result<ParticipationHistoryPage>
            secondPageResult;

        await using (
            AsyncServiceScope secondScope =
                serviceProvider.CreateAsyncScope())
        {
            ListParticipationHistoryByAtmacaCardQueryHandler handler =
                secondScope.ServiceProvider
                    .GetRequiredService<
                        ListParticipationHistoryByAtmacaCardQueryHandler>();

            var secondQuery =
                new ListParticipationHistoryByAtmacaCardQuery(
                    atmacaCardId,
                    PageSize: 3,
                    Cursor: cursor);

            // Act — Page 2
            secondPageResult =
                await handler.Handle(
                    secondQuery,
                    CancellationToken.None);
        }

        // Assert — Page 2
        secondPageResult.IsSuccess
            .Should()
            .BeTrue();

        secondPageResult.Value
            .Should()
            .NotBeNull();

        secondPageResult.Value!.Items
            .Select(
                item =>
                    item.ParticipationId)
            .Should()
            .Equal(
                fourthParticipation
                    .ParticipationId.Value,
                oldestParticipation
                    .ParticipationId.Value);

        secondPageResult.Value.NextCursor
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task Handle_Should_NotLoseItems_WhenCursorTimestampMatchesRemainingItems()
    {
        // Arrange
        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        Participation firstSameTimestampParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation secondSameTimestampParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation thirdSameTimestampParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation olderParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        DateTime sharedCreatedAtUtc =
            new(
                2026,
                8,
                26,
                12,
                0,
                0,
                DateTimeKind.Utc);

        DateTime olderCreatedAtUtc =
            new(
                2026,
                8,
                26,
                11,
                0,
                0,
                DateTimeKind.Utc);

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.AddRange(
                firstSameTimestampParticipation,
                secondSameTimestampParticipation,
                thirdSameTimestampParticipation,
                olderParticipation);

            seedContext.Entry(firstSameTimestampParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    sharedCreatedAtUtc;

            seedContext.Entry(secondSameTimestampParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    sharedCreatedAtUtc;

            seedContext.Entry(thirdSameTimestampParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    sharedCreatedAtUtc;

            seedContext.Entry(olderParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    olderCreatedAtUtc;

            await seedContext.SaveChangesAsync();
        }

        Dictionary<string, string?> configurationValues =
            new()
            {
                ["ConnectionStrings:ProjectAtmacaDatabase"] =
                    ParticipationPersistenceTestContextFactory
                        .ConnectionString
            };

        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    configurationValues)
                .Build();

        ServiceCollection services =
            new();

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        Result<ParticipationHistoryPage>
            firstPageResult;

        await using (
            AsyncServiceScope firstScope =
                serviceProvider.CreateAsyncScope())
        {
            ListParticipationHistoryByAtmacaCardQueryHandler handler =
                firstScope.ServiceProvider
                    .GetRequiredService<
                        ListParticipationHistoryByAtmacaCardQueryHandler>();

            var firstQuery =
                new ListParticipationHistoryByAtmacaCardQuery(
                    atmacaCardId,
                    PageSize: 2,
                    Cursor: null);

            // Act — Page 1
            firstPageResult =
                await handler.Handle(
                    firstQuery,
                    CancellationToken.None);
        }

        // Assert — Page 1
        firstPageResult.IsSuccess
            .Should()
            .BeTrue();

        firstPageResult.Value
            .Should()
            .NotBeNull();

        firstPageResult.Value!.Items
            .Should()
            .HaveCount(2);

        firstPageResult.Value.Items
            .Should()
            .OnlyContain(
                item =>
                    item.CreatedAtUtc ==
                    sharedCreatedAtUtc);

        firstPageResult.Value.NextCursor
            .Should()
            .NotBeNull();

        ParticipationHistoryCursor cursor =
            firstPageResult.Value.NextCursor!;

        cursor.CreatedAtUtc
            .Should()
            .Be(sharedCreatedAtUtc);

        Result<ParticipationHistoryPage>
            secondPageResult;

        await using (
            AsyncServiceScope secondScope =
                serviceProvider.CreateAsyncScope())
        {
            ListParticipationHistoryByAtmacaCardQueryHandler handler =
                secondScope.ServiceProvider
                    .GetRequiredService<
                        ListParticipationHistoryByAtmacaCardQueryHandler>();

            var secondQuery =
                new ListParticipationHistoryByAtmacaCardQuery(
                    atmacaCardId,
                    PageSize: 2,
                    Cursor: cursor);

            // Act — Page 2
            secondPageResult =
                await handler.Handle(
                    secondQuery,
                    CancellationToken.None);
        }

        // Assert — Page 2
        secondPageResult.IsSuccess
            .Should()
            .BeTrue();

        secondPageResult.Value
            .Should()
            .NotBeNull();

        Guid[] allParticipationIds =
        [
            firstSameTimestampParticipation
            .ParticipationId.Value,

        secondSameTimestampParticipation
            .ParticipationId.Value,

        thirdSameTimestampParticipation
            .ParticipationId.Value,

        olderParticipation
            .ParticipationId.Value
        ];

        Guid[] firstPageIds =
            firstPageResult.Value.Items
                .Select(
                    item =>
                        item.ParticipationId)
                .ToArray();

        Guid[] expectedSecondPageIds =
            allParticipationIds
                .Except(
                    firstPageIds)
                .ToArray();

        secondPageResult.Value!.Items
            .Should()
            .HaveCount(2);

        secondPageResult.Value.Items
            .Select(
                item =>
                    item.ParticipationId)
            .Should()
            .BeEquivalentTo(
                expectedSecondPageIds);

        firstPageIds
            .Intersect(
                secondPageResult.Value.Items
                    .Select(
                        item =>
                            item.ParticipationId))
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptyPage_WhenNoHistoryRemainsAfterCursor()
    {
        // Arrange
        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        Participation newestParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation oldestParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        DateTime newestCreatedAtUtc =
            new(
                2026,
                8,
                26,
                12,
                0,
                0,
                DateTimeKind.Utc);

        DateTime oldestCreatedAtUtc =
            new(
                2026,
                8,
                26,
                11,
                0,
                0,
                DateTimeKind.Utc);

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.AddRange(
                newestParticipation,
                oldestParticipation);

            seedContext.Entry(newestParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    newestCreatedAtUtc;

            seedContext.Entry(oldestParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    oldestCreatedAtUtc;

            await seedContext.SaveChangesAsync();
        }

        Dictionary<string, string?> configurationValues =
            new()
            {
                ["ConnectionStrings:ProjectAtmacaDatabase"] =
                    ParticipationPersistenceTestContextFactory
                        .ConnectionString
            };

        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    configurationValues)
                .Build();

        ServiceCollection services =
            new();

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        ParticipationHistoryCursor cursor =
            new(
                atmacaCardId,
                oldestCreatedAtUtc,
                oldestParticipation
                    .ParticipationId.Value);

        var query =
            new ListParticipationHistoryByAtmacaCardQuery(
                atmacaCardId,
                PageSize: 10,
                Cursor: cursor);

        Result<ParticipationHistoryPage>
            result;

        await using (
            AsyncServiceScope scope =
                serviceProvider.CreateAsyncScope())
        {
            ListParticipationHistoryByAtmacaCardQueryHandler handler =
                scope.ServiceProvider
                    .GetRequiredService<
                        ListParticipationHistoryByAtmacaCardQueryHandler>();

            // Act
            result =
                await handler.Handle(
                    query,
                    CancellationToken.None);
        }

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        result.Value
            .Should()
            .NotBeNull();

        result.Value!.Items
            .Should()
            .BeEmpty();

        result.Value.NextCursor
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnSamePage_WhenSameCursorIsReused()
    {
        // Arrange
        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        Participation firstParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation secondParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation thirdParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        Participation fourthParticipation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                atmacaCardId)
            .Value!;

        DateTime firstCreatedAtUtc =
            new(
                2026,
                8,
                26,
                12,
                0,
                0,
                DateTimeKind.Utc);

        DateTime secondCreatedAtUtc =
            new(
                2026,
                8,
                26,
                11,
                0,
                0,
                DateTimeKind.Utc);

        DateTime thirdCreatedAtUtc =
            new(
                2026,
                8,
                26,
                10,
                0,
                0,
                DateTimeKind.Utc);

        DateTime fourthCreatedAtUtc =
            new(
                2026,
                8,
                26,
                9,
                0,
                0,
                DateTimeKind.Utc);

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.AddRange(
                firstParticipation,
                secondParticipation,
                thirdParticipation,
                fourthParticipation);

            seedContext.Entry(firstParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    firstCreatedAtUtc;

            seedContext.Entry(secondParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    secondCreatedAtUtc;

            seedContext.Entry(thirdParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    thirdCreatedAtUtc;

            seedContext.Entry(fourthParticipation)
                .Property(
                    nameof(
                        AuditableAggregateRoot
                            .CreatedAtUtc))
                .CurrentValue =
                    fourthCreatedAtUtc;

            await seedContext.SaveChangesAsync();
        }

        Dictionary<string, string?> configurationValues =
            new()
            {
                ["ConnectionStrings:ProjectAtmacaDatabase"] =
                    ParticipationPersistenceTestContextFactory
                        .ConnectionString
            };

        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    configurationValues)
                .Build();

        ServiceCollection services =
            new();

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        Result<ParticipationHistoryPage>
            firstPageResult;

        await using (
            AsyncServiceScope firstScope =
                serviceProvider.CreateAsyncScope())
        {
            ListParticipationHistoryByAtmacaCardQueryHandler handler =
                firstScope.ServiceProvider
                    .GetRequiredService<
                        ListParticipationHistoryByAtmacaCardQueryHandler>();

            var firstQuery =
                new ListParticipationHistoryByAtmacaCardQuery(
                    atmacaCardId,
                    PageSize: 2,
                    Cursor: null);

            firstPageResult =
                await handler.Handle(
                    firstQuery,
                    CancellationToken.None);
        }

        firstPageResult.IsSuccess
            .Should()
            .BeTrue();

        firstPageResult.Value!.NextCursor
            .Should()
            .NotBeNull();

        ParticipationHistoryCursor cursor =
            firstPageResult.Value.NextCursor!;

        var continuationQuery =
            new ListParticipationHistoryByAtmacaCardQuery(
                atmacaCardId,
                PageSize: 2,
                Cursor: cursor);

        Result<ParticipationHistoryPage>
            firstContinuationResult;

        Result<ParticipationHistoryPage>
            repeatedContinuationResult;

        await using (
            AsyncServiceScope secondScope =
                serviceProvider.CreateAsyncScope())
        {
            ListParticipationHistoryByAtmacaCardQueryHandler handler =
                secondScope.ServiceProvider
                    .GetRequiredService<
                        ListParticipationHistoryByAtmacaCardQueryHandler>();

            // Act — First use of cursor
            firstContinuationResult =
                await handler.Handle(
                    continuationQuery,
                    CancellationToken.None);
        }

        await using (
            AsyncServiceScope thirdScope =
                serviceProvider.CreateAsyncScope())
        {
            ListParticipationHistoryByAtmacaCardQueryHandler handler =
                thirdScope.ServiceProvider
                    .GetRequiredService<
                        ListParticipationHistoryByAtmacaCardQueryHandler>();

            // Act — Reuse same cursor
            repeatedContinuationResult =
                await handler.Handle(
                    continuationQuery,
                    CancellationToken.None);
        }

        // Assert
        firstContinuationResult.IsSuccess
            .Should()
            .BeTrue();

        repeatedContinuationResult.IsSuccess
            .Should()
            .BeTrue();

        firstContinuationResult.Value!.Items
            .Select(
                item =>
                    item.ParticipationId)
            .Should()
            .Equal(
                repeatedContinuationResult.Value!.Items
                    .Select(
                        item =>
                            item.ParticipationId));

        firstContinuationResult.Value.NextCursor
            .Should()
            .Be(
                repeatedContinuationResult.Value.NextCursor);
    }
}
