using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Trainings.Create;
using ProjectAtmaca.Application.Trainings.Reschedule;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.TrainingTypes;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Tests.Trainings.Reschedule;

public sealed class RescheduleTrainingCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_RescheduleAndPersist()
    {
        Training training = CreateTraining();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RescheduleTrainingCommandHandler(
            new GrantedAuthorizationService(),
            new FakeTrainingRepository(training),
            unitOfWork);

        var result = await handler.Handle(
            new RescheduleTrainingCommand(
                training.TrainingId,
                new DateOnly(2026, 9, 20),
                new TimeOnly(17, 0),
                new TimeOnly(18, 0),
                new[]
                {
                    new TrainingTypeAssignmentInput(Guid.NewGuid(), 60)
                }),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        training.Schedule.Date.Should().Be(new DateOnly(2026, 9, 20));
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_NotPersist_WhenTrainingDoesNotExist()
    {
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RescheduleTrainingCommandHandler(
            new GrantedAuthorizationService(),
            new FakeTrainingRepository(null),
            unitOfWork);

        var result = await handler.Handle(
            new RescheduleTrainingCommand(
                TrainingId.From(Guid.NewGuid()),
                new DateOnly(2026, 9, 20),
                new TimeOnly(17, 0),
                new TimeOnly(18, 0),
                Array.Empty<TrainingTypeAssignmentInput>()),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(RescheduleTrainingErrors.NotFound);
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    private static Training CreateTraining() =>
        Training.Create(
            SeasonOrganization.Create(SeasonId.New(), OrganizationId.New()),
            TrainingTitle.Create("U15 Antrenmanı").Value!,
            TrainingDescription.Create("Taktik çalışma").Value!,
            TrainingLocation.Create("Ana saha").Value!,
            TrainingSchedule.Create(
                new DateOnly(2026, 9, 19),
                new TimeOnly(16, 0),
                new TimeOnly(17, 0)).Value!,
            new[]
            {
                TrainingTypeAssignment.Create(
                    TrainingTypeId.New(),
                    TrainingTypeDuration.Create(60).Value!)
            }).Value!;

    private sealed class GrantedAuthorizationService : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class FakeTrainingRepository(Training? training)
        : ITrainingRepository
    {
        public Task AddAsync(
            Training training,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Training?> GetByIdAsync(
            TrainingId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(training);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }
}
