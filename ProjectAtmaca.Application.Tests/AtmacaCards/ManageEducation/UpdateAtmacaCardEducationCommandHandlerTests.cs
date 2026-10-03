using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.AtmacaCards.ManageEducation;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Application.Tests.AtmacaCards.ManageEducation;

public sealed class UpdateAtmacaCardEducationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUpdateEducationAndPersist()
    {
        var card = AtmacaCard.Issue(
            Guid.NewGuid(),
            AtmacaCardNumber.Create("ATM-000010").Value!,
            DateTime.UtcNow).Value!;
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateAtmacaCardEducationCommandHandler(
            new AllowingAuthorization(),
            new FakeRepository(card),
            unitOfWork);

        var result = await handler.Handle(
            new UpdateAtmacaCardEducationCommand(
                card.AtmacaCardId,
                true,
                "Atatürk Lisesi",
                "10. sınıf",
                "1234"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SchoolName.Should().Be("Atatürk Lisesi");
        unitOfWork.SaveCalls.Should().Be(1);
    }

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
