using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Persons;

public sealed class PersonProfessionalTitleEvidenceDocument : Entity
{
    public Guid PersonProfessionalTitleId { get; private set; }
    public Guid AtmacaCardDocumentId { get; private set; }

    private PersonProfessionalTitleEvidenceDocument()
    {
    }

    internal PersonProfessionalTitleEvidenceDocument(Guid titleId, Guid documentId)
    {
        PersonProfessionalTitleId = titleId;
        AtmacaCardDocumentId = documentId;
    }
}
