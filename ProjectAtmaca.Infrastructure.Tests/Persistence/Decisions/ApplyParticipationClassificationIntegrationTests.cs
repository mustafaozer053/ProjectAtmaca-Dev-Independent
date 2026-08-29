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
}
