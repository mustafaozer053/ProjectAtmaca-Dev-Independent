using FluentAssertions;

using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.TrainingTypes.List;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Application.Tests.TrainingTypes.List;

public sealed class ListTrainingTypesQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_MapRepositoryValues_AndForwardActiveOnly()
    {
        TrainingType active = CreateTrainingType(
            "TACTIC",
            "Taktik",
            2);
        TrainingType inactive = CreateTrainingType(
            "OLD",
            "Eski çalışma",
            4);
        inactive.Deactivate();

        FakeTrainingTypeRepository repository = new(
            active,
            inactive);
        var handler = new ListTrainingTypesQueryHandler(
            new GrantedAuthorizationService(),
            repository);

        Result<IReadOnlyList<TrainingTypeListItem>> result =
            await handler.Handle(
                new ListTrainingTypesQuery(ActiveOnly: true),
                TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        repository.ObservedActiveOnly.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Id.Should().Be(active.TrainingTypeId.Value);
        result.Value[0].Code.Should().Be("TACTIC");
        result.Value[0].Name.Should().Be("Taktik");
        result.Value[0].DisplayOrder.Should().Be(2);
        result.Value[0].IsActive.Should().BeTrue();
        result.Value[1].IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ForwardRequestForAllTrainingTypes()
    {
        FakeTrainingTypeRepository repository = new();
        var handler = new ListTrainingTypesQueryHandler(
            new GrantedAuthorizationService(),
            repository);

        Result<IReadOnlyList<TrainingTypeListItem>> result =
            await handler.Handle(
                new ListTrainingTypesQuery(ActiveOnly: false),
                TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        repository.ObservedActiveOnly.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnForbiddenWithoutRepositoryAccess_WhenPermissionIsDenied()
    {
        FakeTrainingTypeRepository repository = new();
        var handler = new ListTrainingTypesQueryHandler(
            new DenyingAuthorizationService(),
            repository);

        Result<IReadOnlyList<TrainingTypeListItem>> result =
            await handler.Handle(
                new ListTrainingTypesQuery(ActiveOnly: true),
                TestContext.Current.CancellationToken);

        result.Error.Should().Be(ActorAuthorizationErrors.Forbidden);
        repository.ListCallCount.Should().Be(0);
    }

    private static TrainingType CreateTrainingType(
        string code,
        string name,
        int displayOrder) =>
        TrainingType.Create(
            TrainingTypeCode.Create(code).Value!,
            TrainingTypeName.Create(name).Value!,
            TrainingTypeDescription.Create("Açıklama").Value!,
            displayOrder)
        .Value!;

    private sealed class GrantedAuthorizationService
        : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class DenyingAuthorizationService
        : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Result.Failure(
                    ActorAuthorizationErrors.Forbidden));
    }

    private sealed class FakeTrainingTypeRepository(
        params TrainingType[] values)
        : ITrainingTypeRepository
    {
        private readonly IReadOnlyList<TrainingType> _values = values;

        public int ListCallCount { get; private set; }

        public bool? ObservedActiveOnly { get; private set; }

        public Task<IReadOnlyList<TrainingType>> ListAsync(
            bool activeOnly,
            CancellationToken cancellationToken = default)
        {
            ListCallCount++;
            ObservedActiveOnly = activeOnly;
            return Task.FromResult(_values);
        }

        public Task AddAsync(
            TrainingType trainingType,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<TrainingType?> GetByIdAsync(
            TrainingTypeId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<TrainingType?>(null);
    }
}
