using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Persons.ManageProfessionalTitles;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Application.Tests.Persons.ManageProfessionalTitles;

public sealed class ProfessionalTitleCommandHandlerTests
{
    [Fact]
    public async Task AddAndEnd_ShouldPersistTitleAndPreserveItsHistory()
    {
        var person = CreatePerson();
        var unitOfWork = new FakeUnitOfWork();
        var repository = new FakeRepository(person);
        var addHandler = new AddProfessionalTitleCommandHandler(
            new AllowingAuthorization(), repository, unitOfWork);
        var endHandler = new EndProfessionalTitleCommandHandler(
            new AllowingAuthorization(), repository, unitOfWork);

        var added = await addHandler.Handle(
            new AddProfessionalTitleCommand(
                person.Id,
                "Spor Hukuku Uzmanı",
                new DateOnly(2020, 1, 1)),
            TestContext.Current.CancellationToken);
        var ended = await endHandler.Handle(
            new EndProfessionalTitleCommand(
                person.Id,
                added.Value!.Id,
                new DateOnly(2025, 4, 30)),
            TestContext.Current.CancellationToken);

        added.IsSuccess.Should().BeTrue();
        ended.IsSuccess.Should().BeTrue();
        person.ProfessionalTitles.Should().ContainSingle()
            .Which.EndedOn.Should().Be(new DateOnly(2025, 4, 30));
        unitOfWork.SaveCalls.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAndRemove_ShouldPersistActiveTitleChanges()
    {
        var person = CreatePerson();
        var unitOfWork = new FakeUnitOfWork();
        var repository = new FakeRepository(person);
        var added = person.AddProfessionalTitle("Antrenör", new DateOnly(2020, 1, 1)).Value!;
        var updateHandler = new UpdateProfessionalTitleCommandHandler(
            new AllowingAuthorization(), repository, unitOfWork);
        var removeHandler = new RemoveProfessionalTitleCommandHandler(
            new AllowingAuthorization(), repository, unitOfWork);

        var updated = await updateHandler.Handle(
            new UpdateProfessionalTitleCommand(
                person.Id,
                added.Id,
                "UEFA C Lisanslı Antrenör",
                new DateOnly(2021, 1, 1)),
            TestContext.Current.CancellationToken);
        var removed = await removeHandler.Handle(
            new RemoveProfessionalTitleCommand(person.Id, added.Id),
            TestContext.Current.CancellationToken);

        updated.IsSuccess.Should().BeTrue();
        removed.IsSuccess.Should().BeTrue();
        person.ProfessionalTitles.Should().BeEmpty();
        unitOfWork.SaveCalls.Should().Be(2);
    }

    private static Person CreatePerson() =>
        Person.Create(
            PersonName.Create("Test Person").Value!,
            BirthDate.Create(new DateTime(1985, 1, 1)),
            Country.Create("TR", "Türkiye")).Value!;

    private sealed class FakeRepository(Person person) : IPersonRepository
    {
        public Task<Person?> GetByIdAsync(Guid personId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Person?>(person.Id == personId ? person : null);
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
