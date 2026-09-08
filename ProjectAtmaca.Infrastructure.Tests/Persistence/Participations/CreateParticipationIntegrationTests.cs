using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations.Create;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Security;
using ProjectAtmaca.Infrastructure.Tests.Persistence;
using Xunit;
using Xunit.Sdk;

using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests
    .Persistence.Participations;

[Collection(SqlIntegrationCollection.Name)]
public sealed class CreateParticipationIntegrationTests
{
    [Fact]
    public async Task Handle_Should_PersistParticipation_ThroughProductionComposition()
    {
        // Arrange
        Dictionary<string, string?> configurationValues =
            new()
            {
                ["ConnectionStrings:ProjectAtmacaDatabase"] =
                    ParticipationPersistenceTestContextFactory
                        .ConnectionString
            };

        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(configurationValues)
                .Build();

        ServiceCollection services =
            new();

        services.AddApplication();

        services.AddTestCurrentActor();

        services.AddInfrastructure(
            configuration);

        await using ServiceProvider serviceProvider =
            services.BuildServiceProvider();

        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        CreateParticipationCommand command =
            new(
                activityReference,
                atmacaCardId);

        ParticipationId participationId;

        ActorId currentActorId;

        await using (
            AsyncServiceScope scope =
                serviceProvider.CreateAsyncScope())
        {
            ICurrentActor currentActor =
                scope.ServiceProvider
                    .GetRequiredService<ICurrentActor>();

            currentActorId =
                currentActor.ActorId;

            ProjectAtmacaDbContext authorizationContext =
                scope.ServiceProvider
                    .GetRequiredService<
                        ProjectAtmacaDbContext>();

            using CancellationTokenSource authorizationCancellationSource =
                new();

            CancellationToken authorizationCancellationToken =
                authorizationCancellationSource.Token;

            bool createPermissionExists =
                await authorizationContext
                    .Set<ActorPermissionGrant>()
                    .AnyAsync(
                        grant =>
                            grant.ActorId ==
                                currentActorId &&
                            grant.PermissionCode ==
                                Permissions.Participations.Create.Code,
                        authorizationCancellationToken);

            if (!createPermissionExists)
            {
                authorizationContext
                    .Set<ActorPermissionGrant>()
                    .Add(
                        ActorPermissionGrant.Create(
                            currentActorId,
                            Permissions.Participations.Create));

                await authorizationContext.SaveChangesAsync(
                    authorizationCancellationToken);
            }

            CreateParticipationCommandHandler handler =
                scope.ServiceProvider
                    .GetRequiredService<
                        CreateParticipationCommandHandler>();

            // Act
            var result =
                await handler.Handle(
                    command,
                    CancellationToken.None);

            // Assert — Application outcome
            result.IsSuccess
                .Should()
                .BeTrue();

            result.Value
                .Should()
                .NotBeNull();

            participationId =
                result.Value!;
        }

        // Assert — fresh DbContext proves real SQL persistence,
        // not DI scope state or EF change tracking.
        await using ProjectAtmacaDbContext verificationContext =
            ParticipationPersistenceTestContextFactory
                .CreateContext();

        Participation? persistedParticipation =
            await verificationContext.Participations
                .SingleOrDefaultAsync(
                    participation =>
                        participation.Id ==
                        participationId.Value,
                    CancellationToken.None);

        persistedParticipation
            .Should()
            .NotBeNull();

        persistedParticipation!.ParticipationId
            .Should()
            .Be(participationId);

        persistedParticipation.ActivityReference
            .Should()
            .Be(activityReference);

        persistedParticipation.AtmacaCardId
            .Should()
            .Be(atmacaCardId);

        persistedParticipation.Status
            .Should()
            .Be(ParticipationStatus.NotRecorded);

        persistedParticipation.CreatedByActorId
            .Should()
            .Be(currentActorId);

        persistedParticipation.LastModifiedByActorId
            .Should()
            .BeNull();
    }
}
