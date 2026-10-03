namespace ProjectAtmaca.Application.Persons.ManageProfessionalTitles;

public sealed record ProfessionalTitleDetails(
    Guid Id,
    string Title,
    DateOnly StartedOn,
    DateOnly? EndedOn,
    IReadOnlyList<Guid> EvidenceDocumentIds);
