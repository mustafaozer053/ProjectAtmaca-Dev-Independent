using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Organizations;

public sealed class Organization : AuditableAggregateRoot
{
    public OrganizationId OrganizationId =>
    OrganizationId.From(Id);

    public Guid? ParentOrganizationId { get; private set; }

    public string Name { get; private set; }

    public string Code { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    private Organization(
        Guid? parentOrganizationId,
        string name,
        string code,
        string? description)
    {
        ParentOrganizationId = parentOrganizationId;
        Name = name;
        Code = code;
        Description = description;
        IsActive = true;
    }

    public static Result<Organization> Create(
        string name,
        string code,
        Guid? parentOrganizationId = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Organization>.Failure(Error.Create("ORGANIZATION_NAME_REQUIRED", "Organization name is required."));

        if (string.IsNullOrWhiteSpace(code))
            return Result<Organization>.Failure(Error.Create("ORGANIZATION_CODE_REQUIRED", "Organization code is required."));

        var organization = new Organization(
            parentOrganizationId,
            name.Trim(),
            code.Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim());

        return Result<Organization>.Success(organization);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name cannot be empty.");

        Name = name.Trim();
    }

    public void ChangeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Organization code cannot be empty.");

        Code = code.Trim().ToUpperInvariant();
    }

    public void ChangeDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }

    public void ChangeParent(Guid? parentOrganizationId)
    {
        if (parentOrganizationId == Id)
            throw new ArgumentException("Organization cannot be parent of itself.");

        ParentOrganizationId = parentOrganizationId;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
