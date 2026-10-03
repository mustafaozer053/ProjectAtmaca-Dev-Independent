using FluentAssertions;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons;

namespace ProjectAtmaca.Domain.Tests.Persons;

public sealed class PersonProfessionalTitleTests
{
    [Fact]
    public void AddProfessionalTitle_ShouldAllowMultipleDifferentTitles()
    {
        var person = CreatePerson();
        var startedOn = new DateOnly(2020, 1, 1);

        var first = person.AddProfessionalTitle("Akademisyen", startedOn);
        var second = person.AddProfessionalTitle("Spor Hukuku Uzmanı", startedOn);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        person.ProfessionalTitles.Should().HaveCount(2);
    }

    [Fact]
    public void AddProfessionalTitle_ShouldRejectSameTitleForOverlappingPeriod()
    {
        var person = CreatePerson();
        var title = person.AddProfessionalTitle("Akademisyen", new DateOnly(2020, 1, 1)).Value!;

        var duplicate = person.AddProfessionalTitle(" akademisyen ", new DateOnly(2024, 1, 1));

        duplicate.Error!.Code.Should().Be("PERSON_PROFESSIONAL_TITLE_DUPLICATE");
        title.End(new DateOnly(2024, 12, 31)).IsSuccess.Should().BeTrue();
        var renewed = person.AddProfessionalTitle("Akademisyen", new DateOnly(2025, 1, 1));
        renewed.IsSuccess.Should().BeTrue();
        person.ProfessionalTitles.Should().HaveCount(2);
    }

    [Fact]
    public void EndProfessionalTitle_ShouldKeepHistoricalRecord()
    {
        var person = CreatePerson();
        var title = person.AddProfessionalTitle("Akademisyen", new DateOnly(2020, 1, 1)).Value!;

        var result = person.EndProfessionalTitle(title.Id, new DateOnly(2025, 4, 30));

        result.IsSuccess.Should().BeTrue();
        person.ProfessionalTitles.Should().ContainSingle();
        person.ProfessionalTitles.Single().EndedOn.Should().Be(new DateOnly(2025, 4, 30));
    }

    [Fact]
    public void UpdateProfessionalTitle_ShouldChangeActiveTitleDetails()
    {
        var person = CreatePerson();
        var title = person.AddProfessionalTitle("Akademisyen", new DateOnly(2020, 1, 1)).Value!;

        var result = person.UpdateProfessionalTitle(
            title.Id,
            "Spor Hukuku Uzmanı",
            new DateOnly(2021, 1, 1));

        result.IsSuccess.Should().BeTrue();
        title.Title.Should().Be("Spor Hukuku Uzmanı");
        title.StartedOn.Should().Be(new DateOnly(2021, 1, 1));
    }

    [Fact]
    public void UpdateProfessionalTitle_ShouldRejectDuplicatesAndHistoricalTitles()
    {
        var person = CreatePerson();
        var first = person.AddProfessionalTitle("Akademisyen", new DateOnly(2020, 1, 1)).Value!;
        var second = person.AddProfessionalTitle("Antrenör", new DateOnly(2020, 1, 1)).Value!;
        person.EndProfessionalTitle(first.Id, new DateOnly(2024, 12, 31));

        var duplicate = person.UpdateProfessionalTitle(
            second.Id,
            "Akademisyen",
            new DateOnly(2024, 1, 1));
        var ended = person.UpdateProfessionalTitle(
            first.Id,
            "Akademisyen",
            new DateOnly(2020, 1, 1));

        duplicate.Error!.Code.Should().Be("PERSON_PROFESSIONAL_TITLE_DUPLICATE");
        ended.Error!.Code.Should().Be("PERSON_PROFESSIONAL_TITLE_ALREADY_ENDED");
        second.Title.Should().Be("Antrenör");
    }

    [Fact]
    public void RemoveProfessionalTitle_ShouldOnlyRemoveActiveTitlesWithoutEvidence()
    {
        var person = CreatePerson();
        var removable = person.AddProfessionalTitle("Akademisyen", new DateOnly(2020, 1, 1)).Value!;
        var linked = person.AddProfessionalTitle("Antrenör", new DateOnly(2020, 1, 1)).Value!;
        linked.LinkEvidenceDocument(Guid.NewGuid());
        var historical = person.AddProfessionalTitle("Spor Hukuku Uzmanı", new DateOnly(2020, 1, 1)).Value!;
        person.EndProfessionalTitle(historical.Id, new DateOnly(2024, 12, 31));

        var blockedByEvidence = person.RemoveProfessionalTitle(linked.Id);
        var blockedByHistory = person.RemoveProfessionalTitle(historical.Id);
        var removed = person.RemoveProfessionalTitle(removable.Id);

        blockedByEvidence.Error!.Code.Should().Be("PERSON_PROFESSIONAL_TITLE_HAS_EVIDENCE");
        blockedByHistory.Error!.Code.Should().Be("PERSON_PROFESSIONAL_TITLE_ALREADY_ENDED");
        removed.IsSuccess.Should().BeTrue();
        person.ProfessionalTitles.Select(title => title.Id)
            .Should().BeEquivalentTo([linked.Id, historical.Id]);
    }

    [Fact]
    public void LinkEvidenceDocument_ShouldAllowMultipleDocumentsAndRejectDuplicates()
    {
        var title = CreatePerson()
            .AddProfessionalTitle("Akademisyen", new DateOnly(2020, 1, 1))
            .Value!;

        var first = title.LinkEvidenceDocument(Guid.NewGuid());
        Guid secondDocumentId = Guid.NewGuid();
        var second = title.LinkEvidenceDocument(secondDocumentId);
        var duplicate = title.LinkEvidenceDocument(secondDocumentId);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        duplicate.Error!.Code.Should().Be("PERSON_PROFESSIONAL_TITLE_DOCUMENT_DUPLICATE");
        title.EvidenceDocuments.Should().HaveCount(2);
    }

    private static Person CreatePerson() =>
        Person.Create(
            PersonName.Create("Test Person").Value!,
            BirthDate.Create(new DateTime(1985, 1, 1)),
            Country.Create("TR", "Türkiye")).Value!;
}
