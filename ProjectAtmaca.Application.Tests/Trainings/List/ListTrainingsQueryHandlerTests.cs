using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Trainings.List;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.TrainingTypes;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Tests.Trainings.List;

public sealed class ListTrainingsQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_FilterByDateAndStatus()
    {
        Training matching = CreateTraining(
            new DateOnly(2026, 9, 20),
            TrainingStatus.Confirmed);
        Training outsideRange = CreateTraining(
            new DateOnly(2026, 9, 21),
            TrainingStatus.Planned);

        var handler = new ListTrainingsQueryHandler(
            new GrantedAuthorizationService(),
            new FakeTrainingRepository(matching, outsideRange));

        var result = await handler.Handle(
            new ListTrainingsQuery(
                new DateOnly(2026, 9, 20),
                new DateOnly(2026, 9, 20),
                Status: TrainingStatus.Confirmed),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.Id.Should().Be(matching.TrainingId.Value);
    }

    [Fact]
    public async Task Handle_Should_RejectInvalidDateRange()
    {
        var handler = new ListTrainingsQueryHandler(
            new GrantedAuthorizationService(),
            new FakeTrainingRepository());

        var result = await handler.Handle(
            new ListTrainingsQuery(
                new DateOnly(2026, 9, 21),
                new DateOnly(2026, 9, 20)),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(TrainingListErrors.InvalidDateRange);
    }

    private static Training CreateTraining(
        DateOnly date,
        TrainingStatus status)
    {
        Training training = Training.Create(
            SeasonOrganization.Create(
                SeasonId.New(),
                OrganizationId.New()),
            TrainingTitle.Create("U15").Value!,
            TrainingDescription.Create(null).Value!,
            TrainingLocation.Create("Saha").Value!,
            TrainingSchedule.Create(
                date,
                new TimeOnly(16, 0),
                new TimeOnly(17, 0)).Value!,
            new[]
            {
                TrainingTypeAssignment.Create(
                    TrainingTypeId.New(),
                    TrainingTypeDuration.Create(60).Value!)
            }).Value!;

        if (status == TrainingStatus.Confirmed)
            training.Confirm();
        else if (status == TrainingStatus.Cancelled)
            training.Cancel();

        return training;
    }

    private sealed class GrantedAuthorizationService : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class FakeTrainingRepository(
        params Training[] trainings) : ITrainingRepository
    {
        public Task AddAsync(
            Training training,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Training?> GetByIdAsync(
            TrainingId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Training?>(null);

        public Task<IReadOnlyList<Training>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Training>>(trainings);
    }
}
