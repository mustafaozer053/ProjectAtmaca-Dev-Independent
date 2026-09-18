using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons.Registration;
using ProjectAtmaca.Domain.Services;

namespace ProjectAtmaca.Application.Tests.Persons.RegisterWithAtmacaCard;

public sealed class RegisterPersonWithAtmacaCardTests
{
    [Fact]
    public async Task Handle_Should_ReturnAuthorizationFailure_WithoutAccessingStoreOrAllocatingNumber()
    {
        var fixture = new Fixture { Allowed = false };
        var result = await fixture.Run(Guid.NewGuid(), Input());
        result.Error.Should().BeSameAs(ActorAuthorizationErrors.Forbidden);
        fixture.Reads.Should().Be(0);
        fixture.Allocations.Should().Be(0);
        fixture.Commits.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_ReplayNormalizedInput_WithoutPreparingOrCommittingAgain()
    {
        var fixture = new Fixture();
        var operation = Guid.NewGuid();
        var first = await fixture.Run(operation, Input());
        var replay = await fixture.Run(operation, Input() with { NationalIdentityNumber = " 12345678901 " });
        first.IsSuccess.Should().BeTrue();
        replay.Value.Should().BeSameAs(first.Value);
        fixture.Authorizations.Should().Be(2);
        fixture.Allocations.Should().Be(1);
        fixture.Commits.Should().Be(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_Should_RejectChangedIdentityOrOptionalField(bool identity)
    {
        var fixture = new Fixture();
        var operation = Guid.NewGuid();
        await fixture.Run(operation, Input());
        var changed = identity ? Input() with { NationalIdentityNumber = "98765432109" }
            : Input() with { MotherName = PersonName.Create("Another Mother").Value! };
        var result = await fixture.Run(operation, changed);
        result.Error.Should().BeSameAs(PersonRegistrationOperationErrors.OperationConflict);
        fixture.Allocations.Should().Be(1);
        fixture.Commits.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_IsolateOperationByActor()
    {
        var fixture = new Fixture();
        var operation = Guid.NewGuid();
        var first = await fixture.Run(operation, Input());
        fixture.ActorId = ActorId.New();
        var second = await fixture.Run(operation, Input());
        second.Value!.PersonId.Should().NotBe(first.Value!.PersonId);
        fixture.Commits.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Should_NotTreatFailedCommitAsSuccessOrCompletedReplay()
    {
        var fixture = new Fixture { FailCommit = true };
        var operation = Guid.NewGuid();
        var failed = await fixture.Run(operation, Input());
        failed.Error.Should().BeSameAs(Fixture.CommitFailure);
        failed.Value.Should().BeNull();
        fixture.FailCommit = false;
        var retry = await fixture.Run(operation, Input());
        retry.IsSuccess.Should().BeTrue();
        fixture.Commits.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Should_RejectEmptyOperationBeforeStoreAccess()
    {
        var fixture = new Fixture();
        var result = await fixture.Run(Guid.Empty, Input());
        result.Error.Should().BeSameAs(PersonRegistrationOperationErrors.OperationRequired);
        fixture.Reads.Should().Be(0);
        fixture.Allocations.Should().Be(0);
    }

    private static PersonRegistrationInput Input() => new(PersonName.Create("Test Person").Value!,
        BirthDate.Create(new DateTime(2010, 1, 1)), Country.Create("TR", "Türkiye"),
        TurkishCitizenshipStatus.ByBirth, "12345678901");

    // Sequential in-memory contract double; no SQL atomicity or concurrency claim.
    private sealed class Fixture : ICurrentActor, IActorAuthorizationService, IPersonRegistrationStore, IAtmacaCardNumberGenerator
    {
        public static readonly Error CommitFailure = Error.Create("Test.CommitFailure", "Commit failed.");
        private readonly Dictionary<(ActorId, Guid), CompletedPersonRegistration> _completed = new();
        public ActorId ActorId { get; set; } = ActorId.New();
        public bool Allowed { get; init; } = true;
        public bool FailCommit { get; set; }
        public int Authorizations, Reads, Allocations, Commits;

        public Task<Result<RegistrationReceipt>> Run(Guid operation, PersonRegistrationInput input)
        {
            var gate = new PersonRegistrationAuthorization(this);
            return new RegisterPersonWithAtmacaCard(gate, this, this,
                new PreparePersonRegistration(gate, this, TimeProvider.System))
                .Handle(operation, input, TestContext.Current.CancellationToken);
        }
        public Task<Result> AuthorizeAsync(Permission permission, CancellationToken cancellationToken = default)
        {
            Authorizations++;
            permission.Should().BeSameAs(Permissions.Persons.RegisterWithAtmacaCard);
            return Task.FromResult(Allowed ? Result.Success() : Result.Failure(ActorAuthorizationErrors.Forbidden));
        }
        public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
        {
            Allocations++;
            return Task.FromResult($"ATM-{Allocations:D6}");
        }
        public Task<CompletedPersonRegistration?> FindAsync(ActorId actorId, Guid operationId, CancellationToken cancellationToken)
        {
            Reads++;
            cancellationToken.Should().Be(TestContext.Current.CancellationToken);
            return Task.FromResult(_completed.GetValueOrDefault((actorId, operationId)));
        }
        public Task<Result<RegistrationReceipt>> CommitAsync(ActorId actorId, Guid operationId,
            PersonRegistrationInput input, PreparedPersonRegistration draft, CancellationToken cancellationToken)
        {
            Commits++;
            cancellationToken.Should().Be(TestContext.Current.CancellationToken);
            if (FailCommit) return Task.FromResult(Result<RegistrationReceipt>.Failure(CommitFailure));
            var receipt = new RegistrationReceipt(draft.Person.Id, draft.Card.Id,
                draft.Card.CardNumber.Value, draft.Card.IssuedAtUtc);
            _completed.Add((actorId, operationId), new CompletedPersonRegistration(input, receipt));
            return Task.FromResult(Result<RegistrationReceipt>.Success(receipt));
        }
    }
}
