using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Decisions.Snapshots;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Decisions;

[Collection(SqlIntegrationCollection.Name)]
public sealed class ApplyParticipationClassificationIntegrationTests
{
    [Fact]
    public async Task Handle_Should_ApplyDecisionAndPersistExactProvenance_ThroughProductionComposition()
    {
        // Arrange
        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        Participation participation =
            Participation.Create(
                activityReference,
                atmacaCardId)
            .Value!;

        ParticipationId participationId =
            participation.ParticipationId;

        ParticipationClassificationSnapshot snapshot =
            ParticipationClassificationSnapshot.Create(
                activityReference,
                atmacaCardId,
                ParticipationStatus.NotRecorded,
                null,
                null,
                null);

        Decision decision =
            Decision.CreateParticipationClassification(
                participationId,
                snapshot,
                ParticipationClassificationEffect.Present());

        DecisionId decisionId =
            decision.DecisionId;

        DecisionRevision decisionRevision =
            decision.Revision;

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                8,
                28,
                13,
                0,
                0,
                TimeSpan.Zero);

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.Add(
                participation);

            seedContext
                .Set<Decision>()
                .Add(decision);

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

        services.AddLogging();

        services.AddApplication();

        services.AddInfrastructure(
            configuration);

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        await using (
            AsyncServiceScope scope =
                serviceProvider.CreateAsyncScope())
        {
            ApplyParticipationClassificationCommandHandler handler =
                scope.ServiceProvider
                    .GetRequiredService<
                        ApplyParticipationClassificationCommandHandler>();

            var command =
                new ApplyParticipationClassificationCommand(
                    DecisionApplicationOperationId.New(),
                    decisionId,
                    decisionRevision,
                    appliedAtUtc);

            // Act
            Result result =
                await handler.Handle(
                    command,
                    CancellationToken.None);

            // Assert — Application outcome
            result.IsSuccess
                .Should()
                .BeTrue();
        }

        // Assert — fresh DbContext proves durable SQL state.
        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation? persistedParticipation =
            await verificationContext.Participations
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                        participationId.Value);

        DecisionApplication? persistedApplication =
            await verificationContext
                .Set<DecisionApplication>()
                .SingleOrDefaultAsync(
                    item =>
                        item.DecisionId ==
                            decisionId);

        persistedParticipation
            .Should()
            .NotBeNull();

        persistedParticipation!.Status
            .Should()
            .Be(ParticipationStatus.Present);

        persistedApplication
            .Should()
            .NotBeNull();

        persistedApplication!.DecisionId
            .Should()
            .Be(decisionId);

        persistedApplication.Target
            .Should()
            .Be(decision.Target);

        persistedApplication.AppliedDecisionRevision
            .Should()
            .Be(decisionRevision);

        persistedApplication.AppliedAtUtc
            .Should()
            .Be(appliedAtUtc);
    }

    [Fact]
    public async Task Handle_Should_TreatExactRedeliveryAsReplay_WithoutDuplicateProvenance()
    {
        // Arrange
        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        Participation participation =
            Participation.Create(
                activityReference,
                atmacaCardId)
            .Value!;

        Decision decision =
            Decision.CreateParticipationClassification(
                participation.ParticipationId,
                ParticipationClassificationSnapshot.Create(
                    activityReference,
                    atmacaCardId,
                    ParticipationStatus.NotRecorded,
                    null,
                    null,
                    null),
                ParticipationClassificationEffect.Present());

        DecisionApplicationOperationId operationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                9,
                1,
                14,
                0,
                0,
                TimeSpan.Zero);

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.Add(
                participation);

            seedContext
                .Set<Decision>()
                .Add(decision);

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

        services.AddLogging();

        services.AddApplication();

        services.AddInfrastructure(
            configuration);

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        var command =
            new ApplyParticipationClassificationCommand(
                operationId,
                decision.DecisionId,
                decision.Revision,
                appliedAtUtc);

        Result firstResult;

        await using (
            AsyncServiceScope firstScope =
                serviceProvider.CreateAsyncScope())
        {
            ApplyParticipationClassificationCommandHandler handler =
                firstScope.ServiceProvider
                    .GetRequiredService<
                        ApplyParticipationClassificationCommandHandler>();

            firstResult =
                await handler.Handle(
                    command,
                    CancellationToken.None);
        }

        // Act — simulate redelivery through a fresh
        // production scope.
        Result replayResult;

        await using (
            AsyncServiceScope replayScope =
                serviceProvider.CreateAsyncScope())
        {
            ApplyParticipationClassificationCommandHandler handler =
                replayScope.ServiceProvider
                    .GetRequiredService<
                        ApplyParticipationClassificationCommandHandler>();

            replayResult =
                await handler.Handle(
                    command,
                    CancellationToken.None);
        }

        // Assert
        firstResult.IsSuccess
            .Should()
            .BeTrue();

        replayResult.IsSuccess
            .Should()
            .BeTrue();

        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation persistedParticipation =
            await verificationContext.Participations
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id ==
                        participation.Id);

        int applicationCount =
            await verificationContext
                .Set<DecisionApplication>()
                .AsNoTracking()
                .CountAsync(
                    item =>
                        item.DecisionId ==
                        decision.DecisionId);

        int operationCount =
            await verificationContext
                .Set<DecisionApplicationOperation>()
                .AsNoTracking()
                .CountAsync(
                    item =>
                        item.OperationId ==
                        operationId);

        persistedParticipation.Status
            .Should()
            .Be(ParticipationStatus.Present);

        applicationCount
            .Should()
            .Be(1);

        operationCount
            .Should()
            .Be(1);
    }
}
