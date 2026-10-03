using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.AtmacaCards.ManageSportsProfile;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Application.Tests.AtmacaCards.ManageSportsProfile;

public sealed class GetAtmacaCardSportsProfilesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnProfilesForAuthorizedReader()
    {
        var card = AtmacaCard.Issue(
            Guid.NewGuid(),
            AtmacaCardNumber.Create("ATM-000020").Value!,
            DateTime.UtcNow).Value!;
        card.UpsertSportsProfile(
            "Futbol", "LIC-20", null, null,
            AthleteCompetitionLevel.Amateur, true);
        var handler = new GetAtmacaCardSportsProfilesQueryHandler(
            new AllowingAuthorization(),
            new FakeRepository(card));

        var result = await handler.Handle(
            card.AtmacaCardId,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.LicenseNumber.Should().Be("LIC-20");
    }

    [Fact]
    public async Task Handle_ShouldNotReadCard_WhenPermissionIsDenied()
    {
        var card = AtmacaCard.Issue(
            Guid.NewGuid(),
            AtmacaCardNumber.Create("ATM-000021").Value!,
            DateTime.UtcNow).Value!;
        var repository = new FakeRepository(card);
        var handler = new GetAtmacaCardSportsProfilesQueryHandler(
            new DenyingAuthorization(),
            repository);

        var result = await handler.Handle(
            card.AtmacaCardId,
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(ActorAuthorizationErrors.Forbidden);
        repository.GetCalls.Should().Be(0);
    }

    private sealed class FakeRepository(AtmacaCard card) : IAtmacaCardRepository
    {
        public int GetCalls { get; private set; }

        public Task<AtmacaCard?> GetByIdAsync(
            AtmacaCardId id,
            CancellationToken cancellationToken = default)
        {
            GetCalls++;
            return Task.FromResult<AtmacaCard?>(
                card.AtmacaCardId == id ? card : null);
        }

    }

    private sealed class AllowingAuthorization : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class DenyingAuthorization : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Result.Failure(ActorAuthorizationErrors.Forbidden));
    }
}
