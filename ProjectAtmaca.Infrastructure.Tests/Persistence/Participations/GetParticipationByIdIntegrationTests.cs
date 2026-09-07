using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Participations.GetById;
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
public sealed class GetParticipationByIdIntegrationTests
{
    [Fact]
    public async Task Handle_Should_ReturnParticipation_ThroughProductionComposition()
    {
        // Arrange
        Participation participation =
            Participation.Create(
                ActivityReference.ForTraining(
                    TrainingId.New()),
                AtmacaCardId.New())
            .Value!;

        await using (
            ProjectAtmacaDbContext seedContext =
                ParticipationPersistenceTestContextFactory
                    .CreateContext())
        {
            seedContext.Participations.Add(
                participation);

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

        await using AsyncServiceScope scope =
            serviceProvider.CreateAsyncScope();

        GetParticipationByIdQueryHandler handler =
            scope.ServiceProvider
                .GetRequiredService<
                    GetParticipationByIdQueryHandler>();

        GetParticipationByIdQuery query =
            new(
                participation.ParticipationId.Value);

        // Act
        Result<ParticipationDetails> result =
            await handler.Handle(
                query,
                CancellationToken.None);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        result.Value
            .Should()
            .NotBeNull();

        ParticipationDetails details =
            result.Value!;

        details.Id.Should()
            .Be(participation.ParticipationId.Value);

        details.AtmacaCardId.Should()
            .Be(participation.AtmacaCardId.Value);

        details.ActivityTypeCode.Should()
            .Be(ActivityTypeCode.TrainingCode);

        details.ActivityId.Should()
            .Be(participation.ActivityReference.ActivityId);

        details.Status.Should()
            .Be(ParticipationStatus.NotRecorded);

        details.ConditionCode.Should()
            .BeNull();

        details.JoinedAt.Should()
            .BeNull();

        details.LeftAt.Should()
            .BeNull();

        details.Note.Should()
            .BeNull();
    }
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenParticipationDoesNotExist_ThroughProductionComposition()
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

        await using AsyncServiceScope scope =
            serviceProvider.CreateAsyncScope();

        GetParticipationByIdQueryHandler handler =
            scope.ServiceProvider
                .GetRequiredService<
                    GetParticipationByIdQueryHandler>();

        Guid missingParticipationId =
            Guid.NewGuid();

        GetParticipationByIdQuery query =
            new(
                missingParticipationId);

        // Act
        Result<ParticipationDetails> result =
            await handler.Handle(
                query,
                CancellationToken.None);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .BeSameAs(
                GetParticipationByIdErrors.NotFound);
    }
}
