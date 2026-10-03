using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Positions;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Positions;

namespace ProjectAtmaca.Application.Tests.Positions;

public sealed class PositionCatalogServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateSportSpecificPosition()
    {
        var repository = new FakePositionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new PositionCatalogService(
            new AllowingAuthorization(),
            repository,
            unitOfWork);

        var result = await service.CreateAsync(
            " Futbol ",
            " st ",
            " Stoper ",
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SportName.Should().Be("Futbol");
        result.Value.Code.Should().Be("ST");
        result.Value.Name.Should().Be("Stoper");
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectDuplicateCodeWithinSport()
    {
        var existing = Position.Create("Futbol", "ST", "Stoper").Value!;
        var repository = new FakePositionRepository([existing]);
        var unitOfWork = new FakeUnitOfWork();
        var service = new PositionCatalogService(
            new AllowingAuthorization(),
            repository,
            unitOfWork);

        var result = await service.CreateAsync(
            "Futbol",
            "st",
            "Yeni stoper",
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("POSITION_CODE_DUPLICATE");
        unitOfWork.SaveCalls.Should().Be(0);
    }

    private sealed class FakePositionRepository(IReadOnlyList<Position>? initial = null)
        : IPositionRepository
    {
        private readonly List<Position> _positions = initial?.ToList() ?? [];

        public Task<Position?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_positions.SingleOrDefault(position => position.Id == id));

        public Task<IReadOnlyList<Position>> ListBySportAsync(
            string sportName,
            bool activeOnly,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Position>>(_positions
                .Where(position => string.Equals(
                    position.SportName, sportName, StringComparison.OrdinalIgnoreCase))
                .Where(position => !activeOnly || position.IsActive)
                .ToList());

        public Task<bool> ExistsBySportAndCodeAsync(
            string sportName,
            string code,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_positions.Any(position =>
                string.Equals(position.SportName, sportName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(position.Code, code, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Position position, CancellationToken cancellationToken = default)
        {
            _positions.Add(position);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class AllowingAuthorization : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }
}
