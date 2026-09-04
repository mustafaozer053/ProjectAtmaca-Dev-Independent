using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Decisions
    .ApplyParticipationClassification;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Decisions;
using ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Decisions;

[Collection(SqlIntegrationCollection.Name)]
public sealed class
    ApplyParticipationClassificationConcurrencyTests
{
    [Fact]
    public async Task
    Handle_Should_TreatConcurrentExactDeliveryAsReplay_WithoutDuplicateProtectedWrites()
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
                30,
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

        // Replace only the operation-store registration
        // with a test-only synchronized decorator.
        ServiceDescriptor existingDescriptor =
            services.Single(
                descriptor =>
                    descriptor.ServiceType ==
                    typeof(IDecisionApplicationOperationStore));

        services.Remove(
            existingDescriptor);

        ConcurrentFirstLookupBarrier barrier =
            new(participantCount: 2);

        services.AddScoped<
            IDecisionApplicationOperationStore>(
            serviceProvider =>
            {
                ProjectAtmacaDbContext dbContext =
                    serviceProvider
                        .GetRequiredService<
                            ProjectAtmacaDbContext>();

                DecisionApplicationOperationStore inner =
                    new(dbContext);

                return new SynchronizedOperationStore(
                    inner,
                    barrier);
            });

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        var command =
            new ApplyParticipationClassificationCommand(
                operationId,
                decision.DecisionId,
                decision.Revision,
                appliedAtUtc);

        // Act — two genuinely concurrent deliveries,
        // each through its own production scope.
        Task<Result> firstTask =
            ExecuteAsync(
                serviceProvider,
                command);

        Task<Result> secondTask =
            ExecuteAsync(
                serviceProvider,
                command);

        Result[] results =
            await Task.WhenAll(
                firstTask,
                secondTask);

        // Assert — both logical deliveries succeed.
        results
            .Should()
            .OnlyContain(
                result =>
                    result.IsSuccess);

        barrier.ArrivalCount
            .Should()
            .Be(2);

        // Assert — durable SQL contains exactly
        // one protected application.
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

        using (new AssertionScope())
        {
            persistedParticipation.Status
                .Should()
                .Be(
                    ParticipationStatus.Present);

            applicationCount
                .Should()
                .Be(1);

            operationCount
                .Should()
                .Be(1);
        }
    }

    [Fact]
    public async Task
        Handle_Should_ReturnOperationConflict_WhenConcurrentDeliveryUsesSameOperationIdWithDifferentSemantics()
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

        DateTimeOffset firstAppliedAtUtc =
            new(
                2026,
                9,
                1,
                15,
                0,
                0,
                TimeSpan.Zero);

        DateTimeOffset secondAppliedAtUtc =
            firstAppliedAtUtc.AddMinutes(1);

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

        ServiceDescriptor existingDescriptor =
            services.Single(
                descriptor =>
                    descriptor.ServiceType ==
                    typeof(IDecisionApplicationOperationStore));

        services.Remove(
            existingDescriptor);

        ConcurrentFirstLookupBarrier barrier =
            new(participantCount: 2);

        services.AddScoped<
            IDecisionApplicationOperationStore>(
            serviceProvider =>
            {
                ProjectAtmacaDbContext dbContext =
                    serviceProvider
                        .GetRequiredService<
                            ProjectAtmacaDbContext>();

                DecisionApplicationOperationStore inner =
                    new(dbContext);

                return new SynchronizedOperationStore(
                    inner,
                    barrier);
            });

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        var firstCommand =
            new ApplyParticipationClassificationCommand(
                operationId,
                decision.DecisionId,
                decision.Revision,
                firstAppliedAtUtc);

        var conflictingCommand =
            new ApplyParticipationClassificationCommand(
                operationId,
                decision.DecisionId,
                decision.Revision,
                secondAppliedAtUtc);

        // Act — both deliveries must first observe
        // the OperationId as absent.
        Task<Result> firstTask =
            ExecuteAsync(
                serviceProvider,
                firstCommand);

        Task<Result> conflictingTask =
            ExecuteAsync(
                serviceProvider,
                conflictingCommand);

        Result[] results =
            await Task.WhenAll(
                firstTask,
                conflictingTask);

        // Assert — exactly one semantic operation wins.
        results.Count(
                result =>
                    result.IsSuccess)
            .Should()
            .Be(1);

        Result conflictResult =
            results.Single(
                result =>
                    result.IsFailure);

        conflictResult.Error
            .Should()
            .Be(
                ApplyParticipationClassificationErrors
                    .OperationConflict);

        barrier.ArrivalCount
            .Should()
            .Be(2);

        // Assert — inspect the single durable winner.
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

        DecisionApplication[] applications =
            await verificationContext
                .Set<DecisionApplication>()
                .AsNoTracking()
                .Where(
                    item =>
                        item.DecisionId ==
                        decision.DecisionId)
                .ToArrayAsync();

        DecisionApplicationOperation[] operations =
            await verificationContext
                .Set<DecisionApplicationOperation>()
                .AsNoTracking()
                .Where(
                    item =>
                        item.OperationId ==
                        operationId)
                .ToArrayAsync();

        applications
            .Should()
            .ContainSingle();

        operations
            .Should()
            .ContainSingle();

        DecisionApplication durableApplication =
            applications.Single();

        DecisionApplicationOperation durableOperation =
            operations.Single();

        using (new AssertionScope())
        {
            persistedParticipation.Status
                .Should()
                .Be(
                    ParticipationStatus.Present);

            durableOperation.DecisionId
                .Should()
                .Be(
                    decision.DecisionId);

            durableOperation.DecisionRevision
                .Should()
                .Be(
                    decision.Revision);

            durableOperation.AppliedAtUtc
                .Should()
                .BeOneOf(
                    firstAppliedAtUtc,
                    secondAppliedAtUtc);

            durableApplication.DecisionId
                .Should()
                .Be(
                    decision.DecisionId);

            durableApplication.AppliedDecisionRevision
                .Should()
                .Be(
                    decision.Revision);

            durableApplication.AppliedAtUtc
                .Should()
                .Be(
                    durableOperation.AppliedAtUtc,
                    "the single durable provenance must belong " +
                    "to the semantic operation that won the race");
        }
    }

    [Fact]
    public async Task
        Handle_Should_AllowIntentionalReapplication_WhenOperationIdsAreDifferent()
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

        DecisionApplicationOperationId firstOperationId =
            DecisionApplicationOperationId.New();

        DecisionApplicationOperationId secondOperationId =
            DecisionApplicationOperationId.New();

        DateTimeOffset appliedAtUtc =
            new(
                2026,
                9,
                1,
                15,
                30,
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

        var firstCommand =
            new ApplyParticipationClassificationCommand(
                firstOperationId,
                decision.DecisionId,
                decision.Revision,
                appliedAtUtc);

        var secondCommand =
            new ApplyParticipationClassificationCommand(
                secondOperationId,
                decision.DecisionId,
                decision.Revision,
                appliedAtUtc);

        // Act — two distinct logical operations intentionally
        // apply the same authoritative Decision revision.
        Result firstResult =
            await ExecuteAsync(
                serviceProvider,
                firstCommand);

        Result secondResult =
            await ExecuteAsync(
                serviceProvider,
                secondCommand);

        using (new AssertionScope())
        {
            firstResult.IsSuccess
                .Should()
                .BeTrue();

            secondResult.IsSuccess
                .Should()
                .BeTrue();
        }

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

        DecisionApplication[] applications =
            await verificationContext
                .Set<DecisionApplication>()
                .AsNoTracking()
                .Where(
                    item =>
                        item.DecisionId ==
                        decision.DecisionId)
                .ToArrayAsync();

        DecisionApplicationOperation[] operations =
            await verificationContext
                .Set<DecisionApplicationOperation>()
                .AsNoTracking()
                .Where(
                    item =>
                        item.DecisionId ==
                        decision.DecisionId)
                .ToArrayAsync();

        using (new AssertionScope())
        {
            persistedParticipation.Status
                .Should()
                .Be(
                    ParticipationStatus.Present);

            applications
                .Should()
                .HaveCount(
                    2,
                    "different OperationIds represent two " +
                    "intentional applications");

            operations
                .Should()
                .HaveCount(
                    2);

            operations
                .Select(
                    operation =>
                        operation.OperationId)
                .Should()
                .BeEquivalentTo(
                    new[]
                    {
                    firstOperationId,
                    secondOperationId
                    });

            applications
                .Should()
                .OnlyContain(
                    application =>
                        application.DecisionId ==
                            decision.DecisionId
                        &&
                        application.AppliedDecisionRevision ==
                            decision.Revision
                        &&
                        application.AppliedAtUtc ==
                            appliedAtUtc);

            operations
                .Should()
                .OnlyContain(
                    operation =>
                        operation.DecisionRevision ==
                            decision.Revision
                        &&
                        operation.AppliedAtUtc ==
                            appliedAtUtc);
        }
    }

    private static async Task<Result> ExecuteAsync(
        ServiceProvider serviceProvider,
        ApplyParticipationClassificationCommand command)
    {
        await using AsyncServiceScope scope =
            serviceProvider.CreateAsyncScope();

        ApplyParticipationClassificationCommandHandler handler =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplyParticipationClassificationCommandHandler>();

        return await handler.Handle(
            command,
            CancellationToken.None);
    }

    private sealed class SynchronizedOperationStore
        : IDecisionApplicationOperationStore
    {
        private readonly DecisionApplicationOperationStore
            _inner;

        private readonly ConcurrentFirstLookupBarrier
            _barrier;

        public SynchronizedOperationStore(
            DecisionApplicationOperationStore inner,
            ConcurrentFirstLookupBarrier barrier)
        {
            _inner = inner;
            _barrier = barrier;
        }

        public async Task<DecisionApplicationOperation?>
            GetByIdAsync(
                DecisionApplicationOperationId operationId,
                CancellationToken cancellationToken = default)
        {
            DecisionApplicationOperation? operation =
                await _inner.GetByIdAsync(
                    operationId,
                    cancellationToken);

            await _barrier.WaitAfterFirstLookupAsync(
                operation,
                cancellationToken);

            return operation;
        }

        public Task AddAsync(
            DecisionApplicationOperation operation,
            CancellationToken cancellationToken = default)
        {
            return _inner.AddAsync(
                operation,
                cancellationToken);
        }
    }

    private sealed class ConcurrentFirstLookupBarrier
    {
        private readonly int _participantCount;

        private readonly TaskCompletionSource<bool>
            _allArrived =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        private int _arrivalCount;

        public ConcurrentFirstLookupBarrier(
            int participantCount)
        {
            _participantCount =
                participantCount;
        }

        public int ArrivalCount =>
            Volatile.Read(
                ref _arrivalCount);

        public async Task WaitAfterFirstLookupAsync(
            DecisionApplicationOperation? operation,
            CancellationToken cancellationToken)
        {
            // Only the initial "operation absent" lookup
            // participates. The loser's later durable
            // reread must pass straight through.
            if (operation is not null)
            {
                return;
            }

            int arrival =
                Interlocked.Increment(
                    ref _arrivalCount);

            if (arrival == _participantCount)
            {
                _allArrived.TrySetResult(
                    true);
            }

            await _allArrived.Task.WaitAsync(
                cancellationToken);
        }
    }
}
