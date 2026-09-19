using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Trainings.Confirm;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.TrainingTypes;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Tests.Trainings.Confirm;

public sealed class ConfirmTrainingCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_ConfirmTraining_AndPersist()
    {
        Training training = CreateTraining();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ConfirmTrainingCommandHandler(
            new GrantedAuthorizationService(),
            new FakeTrainingRepository(training),
            unitOfWork);

        var result = await handler.Handle(
            new ConfirmTrainingCommand(training.TrainingId),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        training.Status.Should().Be(TrainingStatus.Confirmed);
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_NotPersist_WhenTrainingDoesNotExist()
    {
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ConfirmTrainingCommandHandler(
            new GrantedAuthorizationService(),
            new FakeTrainingRepository(null),
            unitOfWork);

        var result = await handler.Handle(
            new ConfirmTrainingCommand(TrainingId.From(Guid.NewGuid())),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(ConfirmTrainingErrors.NotFound);
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    private static Training CreateTraining() =>
        Training.Create(
            SeasonOrganization.Create(
                SeasonId.New(),
                OrganizationId.New()),
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

        public Task<IReadOnlyList<Training>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Training>>(
                Array.Empty<Training>());
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
