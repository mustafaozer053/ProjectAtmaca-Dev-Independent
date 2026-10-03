using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.AtmacaCards;
using ProjectAtmaca.Application.AtmacaCards.ManageSportsProfile;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Positions;

namespace ProjectAtmaca.Application.Tests.AtmacaCards.ManageSportsProfile;

public sealed class UpsertAtmacaCardSportsProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSaveProfileForActiveCard()
    {
        var card = CreateCard();
        var repository = new FakeRepository(card);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpsertAtmacaCardSportsProfileCommandHandler(
            new AllowingAuthorization(),
            repository,
            new FakePositionRepository(),
            unitOfWork);

        var result = await handler.Handle(
            new UpsertAtmacaCardSportsProfileCommand(
                card.AtmacaCardId,
                "Futbol",
                "LIC-001",
                new DateOnly(2018, 7, 1),
                new DateOnly(2020, 8, 15),
                AthleteCompetitionLevel.Amateur,
                false),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LicenseNumber!.Value.Should().Be("LIC-001");
        result.Value.IsNationalAthlete.Should().BeFalse();
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldAllowProfileUpdates_WhenCardIsInactive()
    {
        var card = CreateCard();
        card.Deactivate();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpsertAtmacaCardSportsProfileCommandHandler(
            new AllowingAuthorization(),
            new FakeRepository(card),
            new FakePositionRepository(),
            unitOfWork);

        var result = await handler.Handle(
            new UpsertAtmacaCardSportsProfileCommand(
                card.AtmacaCardId, "Futbol", null, null, null, null, null),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldSavePositionsOnlyFromSelectedSport()
    {
        var card = CreateCard();
        var position = Position.Create("Futbol", "ST", "Stoper").Value!;
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpsertAtmacaCardSportsProfileCommandHandler(
            new AllowingAuthorization(),
            new FakeRepository(card),
            new FakePositionRepository([position]),
            unitOfWork);

        var result = await handler.Handle(
            new UpsertAtmacaCardSportsProfileCommand(
                card.AtmacaCardId,
                "Futbol",
                null,
                null,
                null,
                null,
                null,
                [position.Id]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Positions.Should().ContainSingle().Which.Id.Should().Be(position.Id);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    private static AtmacaCard CreateCard() =>
        AtmacaCard.Issue(
            Guid.NewGuid(),
            AtmacaCardNumber.Create("ATM-000010").Value!,
            DateTime.UtcNow).Value!;

    private sealed class FakeRepository(AtmacaCard card) : IAtmacaCardRepository
    {
        public Task<AtmacaCard?> GetByIdAsync(
            AtmacaCardId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AtmacaCard?>(card.AtmacaCardId == id ? card : null);

    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }

    }

    private sealed class FakePositionRepository(IReadOnlyList<Position>? initial = null) : IPositionRepository
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

    private sealed class AllowingAuthorization : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }
}
