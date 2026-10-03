using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Persons;

public sealed class PersonProfessionalTitle : Entity
{
    private readonly List<PersonProfessionalTitleEvidenceDocument> _evidenceDocuments = [];

    public Guid PersonId { get; private set; }
    public string Title { get; private set; }
    public DateOnly StartedOn { get; private set; }
    public DateOnly? EndedOn { get; private set; }
    public IReadOnlyCollection<PersonProfessionalTitleEvidenceDocument> EvidenceDocuments =>
        _evidenceDocuments.AsReadOnly();

    private PersonProfessionalTitle()
    {
        Title = null!;
    }

    private PersonProfessionalTitle(
        Guid personId,
        string title,
        DateOnly startedOn)
    {
        PersonId = personId;
        Title = title;
        StartedOn = startedOn;
    }

    internal static Result<PersonProfessionalTitle> Create(
        Guid personId,
        string title,
        DateOnly startedOn)
    {
        string normalizedTitle = title?.Trim() ?? string.Empty;
        if (personId == Guid.Empty)
            return Result<PersonProfessionalTitle>.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_PERSON_REQUIRED",
                "Person id is required."));
        if (normalizedTitle.Length is < 2 or > 150)
            return Result<PersonProfessionalTitle>.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_INVALID",
                "Professional title must be between 2 and 150 characters."));
        if (startedOn == default)
            return Result<PersonProfessionalTitle>.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_START_DATE_INVALID",
                "Professional title start date is required."));

        return Result<PersonProfessionalTitle>.Success(
            new PersonProfessionalTitle(personId, normalizedTitle, startedOn));
    }

    public Result End(DateOnly endedOn)
    {
        if (EndedOn.HasValue)
            return Result.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_ALREADY_ENDED",
                "Professional title has already ended."));
        if (endedOn == default || endedOn < StartedOn)
            return Result.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_PERIOD_INVALID",
                "End date cannot be earlier than the start date."));

        EndedOn = endedOn;
        return Result.Success();
    }

    public Result Update(string title, DateOnly startedOn)
    {
        if (EndedOn.HasValue)
            return Result.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_ALREADY_ENDED",
                "A professional title in history cannot be edited."));

        var updatedTitle = Create(PersonId, title, startedOn);
        if (updatedTitle.IsFailure)
            return Result.Failure(updatedTitle.Error!);

        Title = updatedTitle.Value!.Title;
        StartedOn = updatedTitle.Value.StartedOn;
        return Result.Success();
    }

    public Result LinkEvidenceDocument(Guid documentId)
    {
        if (documentId == Guid.Empty)
            return Result.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_DOCUMENT_REQUIRED",
                "Document id is required."));
        if (_evidenceDocuments.Any(link => link.AtmacaCardDocumentId == documentId))
            return Result.Failure(Error.Create(
                "PERSON_PROFESSIONAL_TITLE_DOCUMENT_DUPLICATE",
                "This document is already linked to the professional title."));

        _evidenceDocuments.Add(new PersonProfessionalTitleEvidenceDocument(Id, documentId));
        return Result.Success();
    }
}
