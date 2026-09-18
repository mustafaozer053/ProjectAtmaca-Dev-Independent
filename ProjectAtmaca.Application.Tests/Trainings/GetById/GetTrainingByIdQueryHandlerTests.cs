using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Trainings.GetById;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Organizations;
using ProjectAtmaca.Domain.Seasons;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Application.Tests.Trainings.GetById;

public sealed class GetTrainingByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnTrainingDetails()
    {
        Training training = CreateTraining();
        var handler = new GetTrainingByIdQueryHandler(
            new GrantedAuthorizationService(),
            new FakeTrainingRepository(training));

        var result = await handler.Handle(
            new GetTrainingByIdQuery(training.TrainingId),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(training.TrainingId.Value);
        result.Value.Status.Should().Be(TrainingStatus.Planned);
        result.Value.Title.Should().Be("U15 Antrenmanı");
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTrainingDoesNotExist()
    {
        var handler = new GetTrainingByIdQueryHandler(
            new GrantedAuthorizationService(),
            new FakeTrainingRepository(null));

        var result = await handler.Handle(
            new GetTrainingByIdQuery(TrainingId.From(Guid.NewGuid())),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(GetTrainingByIdErrors.NotFound);
    }

    private static Training CreateTraining() =>
        Training.Create(
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
        public Task<Training?> GetByIdAsync(
            TrainingId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(training);
    }
}
