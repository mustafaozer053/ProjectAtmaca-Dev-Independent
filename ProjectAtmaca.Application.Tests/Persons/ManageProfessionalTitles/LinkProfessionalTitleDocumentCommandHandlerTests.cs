using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.AtmacaCards;
using ProjectAtmaca.Application.Persons.ManageProfessionalTitles;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Application.Tests.Persons.ManageProfessionalTitles;

public sealed class LinkProfessionalTitleDocumentCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldLinkDocumentOwnedByTheSamePerson()
    {
        var (person, card, title, documentId) = CreateScenario();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(person, card, unitOfWork);

        var result = await handler.Handle(
            new LinkProfessionalTitleDocumentCommand(person.Id, title.Id, documentId),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        title.EvidenceDocuments.Should().ContainSingle()
            .Which.AtmacaCardDocumentId.Should().Be(documentId);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldRejectDocumentFromAnotherPersonsCard()
    {
        var (person, card, title, _) = CreateScenario();
        var otherCard = AtmacaCard.Issue(
            Guid.NewGuid(),
            AtmacaCardNumber.Create("ATM-000093").Value!,
            DateTime.UtcNow).Value!;
        var otherDocumentId = AddDocument(otherCard);
        var handler = CreateHandler(person, card, new FakeUnitOfWork());

        var result = await handler.Handle(
            new LinkProfessionalTitleDocumentCommand(person.Id, title.Id, otherDocumentId),
            TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("ATMACA_CARD_DOCUMENT_NOT_FOUND");
        title.EvidenceDocuments.Should().BeEmpty();
    }

    private static (Person Person, AtmacaCard Card, PersonProfessionalTitle Title, Guid DocumentId)
        CreateScenario()
    {
        var person = Person.Create(
            PersonName.Create("Test Person").Value!,
            BirthDate.Create(new DateTime(1985, 1, 1)),
            Country.Create("TR", "Türkiye")).Value!;
        var card = AtmacaCard.Issue(
            person.Id,
            AtmacaCardNumber.Create("ATM-000092").Value!,
            DateTime.UtcNow).Value!;
        var title = person.AddProfessionalTitle(
            "Akademisyen",
            new DateOnly(2020, 1, 1)).Value!;
        return (person, card, title, AddDocument(card));
    }

    private static Guid AddDocument(AtmacaCard card)
    {
        var result = card.AddDocument(
            AtmacaCardDocumentType.Certificate,
            "Mesleki yeterlilik belgesi",
            null,
            new DateOnly(2020, 1, 1),
            "evidence.pdf",
            "application/pdf",
            12,
            $"atmaca-cards/{card.AtmacaCardId.Value:N}/{Guid.NewGuid():N}.pdf");
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error!.Message);
        return result.Value!.Id;
    }

    private static LinkProfessionalTitleDocumentCommandHandler CreateHandler(
        Person person,
        AtmacaCard card,
        FakeUnitOfWork unitOfWork) =>
        new(
            new AllowingAuthorization(),
            new FakePersonRepository(person),
            new FakeCardReader(card),
            new FakeAtmacaCardRepository(card),
            unitOfWork);

    private sealed class FakePersonRepository(Person person) : IPersonRepository
    {
        public Task<Person?> GetByIdAsync(Guid personId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Person?>(person.Id == personId ? person : null);
    }

    private sealed class FakeCardReader(AtmacaCard card) : IAtmacaCardReader
    {
        public Task<AtmacaCardSummary?> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default) =>
            Task.FromResult<AtmacaCardSummary?>(
                card.PersonId == personId
                    ? new AtmacaCardSummary(card.Id, personId, "Test Person", card.CardNumber.Value, true)
                    : null);

        public Task<IReadOnlyList<AtmacaCardSummary>> ListAsync(int limit = 100, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AtmacaCardSummary>>([]);

        public Task<IReadOnlyDictionary<Guid, AtmacaCardSummary>> GetSummariesAsync(
            IReadOnlyCollection<Guid> atmacaCardIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, AtmacaCardSummary>>(
                new Dictionary<Guid, AtmacaCardSummary>());

        public Task<IReadOnlyList<AtmacaCardSummary>> SearchAsync(
            string search,
            int limit = 20,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AtmacaCardSummary>>([]);
    }

    private sealed class FakeAtmacaCardRepository(AtmacaCard card) : IAtmacaCardRepository
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
