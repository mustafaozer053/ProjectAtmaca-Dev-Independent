using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Trainings.Cancel;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Application.Tests.Trainings.Cancel;

public sealed class CancelTrainingCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_CancelTraining_AndPersist()
    {
        Training training = CreateTraining();
        var repository = new FakeTrainingRepository(training);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CancelTrainingCommandHandler(
            new GrantedAuthorizationService(), repository, unitOfWork);

        var result = await handler.Handle(
            new CancelTrainingCommand(training.TrainingId),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        training.Status.Should().Be(TrainingStatus.Cancelled);
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_AndNotPersist_WhenTrainingDoesNotExist()
    {
        var repository = new FakeTrainingRepository(null);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CancelTrainingCommandHandler(
            new GrantedAuthorizationService(), repository, unitOfWork);

        var result = await handler.Handle(
            new CancelTrainingCommand(TrainingId.From(Guid.NewGuid())),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(CancelTrainingErrors.NotFound);
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    private static Training CreateTraining()
    {
        var result = Training.Create(
            SeasonOrganization.Create(SeasonId.New(), OrganizationId.New()),
            TrainingTitle.Create("U15 Antrenmanı").Value!,
            TrainingDescription.Create("Taktik çalışma").Value!,
            TrainingLocation.Create("Ana saha").Value!,
            TrainingSchedule.Create(
                new DateOnly(2026, 9, 18),
                new TimeOnly(16, 0),
                new TimeOnly(17, 0)).Value!,
            new[]
            {
                TrainingTypeAssignment.Create(
                    TrainingTypeId.New(),
                    TrainingTypeDuration.Create(60).Value!)
            });

        return result.Value!;
    }

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
