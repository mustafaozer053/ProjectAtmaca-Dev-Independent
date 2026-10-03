using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.AtmacaCards.ManageMeasurements;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Application.Tests.AtmacaCards.ManageMeasurements;

public sealed class RecordAtmacaCardMeasurementCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldRecordMeasurementAndSave()
    {
        var card = CreateCard();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RecordAtmacaCardMeasurementCommandHandler(
            new AllowingAuthorization(),
            new FakeRepository(card),
            unitOfWork,
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero)));

        var result = await handler.Handle(
            new RecordAtmacaCardMeasurementCommand(
                card.AtmacaCardId, new DateOnly(2026, 9, 30), 160m, 50.5m),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WeightKilograms.Should().Be(50.5m);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldRejectFutureMeasurementDate()
    {
        var card = CreateCard();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RecordAtmacaCardMeasurementCommandHandler(
            new AllowingAuthorization(),
            new FakeRepository(card),
            unitOfWork,
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero)));

        var result = await handler.Handle(
            new RecordAtmacaCardMeasurementCommand(
                card.AtmacaCardId, new DateOnly(2026, 10, 2), 160m, 50m),
            TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("ATMACA_CARD_MEASUREMENT_DATE_FUTURE");
        unitOfWork.SaveCalls.Should().Be(0);
    }

    private static AtmacaCard CreateCard() =>
        AtmacaCard.Issue(
            Guid.NewGuid(),
            AtmacaCardNumber.Create("ATM-000030").Value!,
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

    private sealed class AllowingAuthorization : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
