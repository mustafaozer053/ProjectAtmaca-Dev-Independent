using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.AtmacaCards;
using ProjectAtmaca.Application.AtmacaCards.ChangeStatus;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Application.Tests.AtmacaCards.ChangeStatus;

public sealed class ChangeAtmacaCardStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldChangeStatusAndPersist()
    {
        var card = AtmacaCard.Issue(
            Guid.NewGuid(),
            AtmacaCardNumber.Create("ATM-000001").Value!,
            DateTime.UtcNow).Value!;
        var repository = new FakeRepository(card);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ChangeAtmacaCardStatusCommandHandler(
            new AllowingAuthorization(),
            repository,
            unitOfWork);

        var result = await handler.Handle(
            new ChangeAtmacaCardStatusCommand(card.AtmacaCardId, false),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        card.IsActive.Should().BeFalse();
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCardDoesNotExist()
    {
        var handler = new ChangeAtmacaCardStatusCommandHandler(
            new AllowingAuthorization(),
            new FakeRepository(null),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new ChangeAtmacaCardStatusCommand(AtmacaCardId.New(), false),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(AtmacaCardApplicationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldNotReadOrPersist_WhenPermissionIsDenied()
    {
        var card = AtmacaCard.Issue(
            Guid.NewGuid(),
            AtmacaCardNumber.Create("ATM-000002").Value!,
            DateTime.UtcNow).Value!;
        var repository = new FakeRepository(card);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ChangeAtmacaCardStatusCommandHandler(
            new DenyingAuthorization(),
            repository,
            unitOfWork);

        var result = await handler.Handle(
            new ChangeAtmacaCardStatusCommand(card.AtmacaCardId, false),
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(ActorAuthorizationErrors.Forbidden);
        repository.GetCalls.Should().Be(0);
        unitOfWork.SaveCalls.Should().Be(0);
        card.IsActive.Should().BeTrue();
    }

    private sealed class FakeRepository(AtmacaCard? card) : IAtmacaCardRepository
    {
        public int GetCalls { get; private set; }

        public Task<AtmacaCard?> GetByIdAsync(
            AtmacaCardId id,
            CancellationToken cancellationToken = default)
        {
            GetCalls++;
            return Task.FromResult(card?.AtmacaCardId == id ? card : null);
        }

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

    private sealed class DenyingAuthorization : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Result.Failure(ActorAuthorizationErrors.Forbidden));
    }
}
