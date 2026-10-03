using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Organizations;

namespace ProjectAtmaca.Application.Organizations.Catalog;

public sealed class OrganizationCatalogService(
    IActorAuthorizationService authorizationService,
    IOrganizationCatalogRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<IReadOnlyList<Organization>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Organizations.Manage,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<IReadOnlyList<Organization>>.Failure(authorization.Error!);

        return Result<IReadOnlyList<Organization>>.Success(
            await repository.ListAsync(cancellationToken));
    }

    public async Task<Result<Organization>> CreateAsync(
        string name,
        string code,
        string? description,
        Guid? parentOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Organizations.Manage,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<Organization>.Failure(authorization.Error!);

        Error? validationError = ValidateDetails(name, code, description, parentOrganizationId);
        if (validationError is not null)
            return Result<Organization>.Failure(validationError);

        if (parentOrganizationId.HasValue)
        {
            Organization? parent = await repository.GetByIdAsync(
                parentOrganizationId.Value,
                cancellationToken);
            if (parent is null || !parent.IsActive)
                return Result<Organization>.Failure(Error.Create(
                    "ORGANIZATION_PARENT_NOT_FOUND",
                    "An active parent organization was not found."));
        }

        var organizationResult = Organization.Create(
            name,
            code,
            parentOrganizationId,
            description);
        if (organizationResult.IsFailure)
            return organizationResult;

        Organization organization = organizationResult.Value!;
        if (await repository.CodeExistsAsync(organization.Code, cancellationToken))
            return Result<Organization>.Failure(Error.Create(
                "ORGANIZATION_CODE_DUPLICATE",
                "An organization with this code already exists."));
        if (await repository.NameExistsUnderParentAsync(
                organization.Name,
                parentOrganizationId,
                cancellationToken))
            return Result<Organization>.Failure(Error.Create(
                "ORGANIZATION_NAME_DUPLICATE",
                "An organization with this name already exists under the selected parent."));

        await repository.AddAsync(organization, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Organization>.Success(organization);
    }

    public async Task<Result<Organization>> UpdateAsync(
        Guid organizationId,
        string name,
        string code,
        string? description,
        Guid? parentOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Organizations.Manage,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<Organization>.Failure(authorization.Error!);
        if (organizationId == Guid.Empty)
            return Result<Organization>.Failure(Error.Create(
                "ORGANIZATION_ID_INVALID",
                "Organization id is invalid."));

        Error? validationError = ValidateDetails(name, code, description, parentOrganizationId);
        if (validationError is not null)
            return Result<Organization>.Failure(validationError);

        Organization? organization = await repository.GetByIdAsync(
            organizationId,
            cancellationToken);
        if (organization is null)
            return Result<Organization>.Failure(Error.Create(
                "ORGANIZATION_NOT_FOUND",
                "Organization was not found."));

        IReadOnlyList<Organization> allOrganizations = await repository.ListAsync(cancellationToken);
        if (parentOrganizationId.HasValue)
        {
            Organization? parent = allOrganizations.SingleOrDefault(
                item => item.Id == parentOrganizationId.Value);
            if (parent is null ||
                !parent.IsActive &&
                (organization.IsActive || organization.ParentOrganizationId != parentOrganizationId))
                return Result<Organization>.Failure(Error.Create(
                    "ORGANIZATION_PARENT_NOT_FOUND",
                    "An active parent organization was not found."));

            var visited = new HashSet<Guid>();
            Guid? ancestorId = parentOrganizationId;
            while (ancestorId.HasValue)
            {
                if (ancestorId.Value == organizationId || !visited.Add(ancestorId.Value))
                    return Result<Organization>.Failure(Error.Create(
                        "ORGANIZATION_HIERARCHY_CYCLE",
                        "The selected parent would create a cycle in the organization hierarchy."));

                Organization? ancestor = allOrganizations.SingleOrDefault(
                    item => item.Id == ancestorId.Value);
                ancestorId = ancestor?.ParentOrganizationId;
            }
        }

        string normalizedName = name.Trim();
        string normalizedCode = code.Trim().ToUpperInvariant();
        if (await repository.CodeExistsAsync(
                normalizedCode,
                cancellationToken,
                organizationId))
            return Result<Organization>.Failure(Error.Create(
                "ORGANIZATION_CODE_DUPLICATE",
                "An organization with this code already exists."));
        if (await repository.NameExistsUnderParentAsync(
                normalizedName,
                parentOrganizationId,
                cancellationToken,
                organizationId))
            return Result<Organization>.Failure(Error.Create(
                "ORGANIZATION_NAME_DUPLICATE",
                "An organization with this name already exists under the selected parent."));

        organization.Rename(normalizedName);
        organization.ChangeCode(normalizedCode);
        organization.ChangeDescription(description);
        organization.ChangeParent(parentOrganizationId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Organization>.Success(organization);
    }

    public async Task<Result> ChangeStatusAsync(
        Guid organizationId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            Permissions.Organizations.Manage,
            cancellationToken);
        if (authorization.IsFailure)
            return authorization;
        if (organizationId == Guid.Empty)
            return Result.Failure(Error.Create(
                "ORGANIZATION_ID_INVALID",
                "Organization id is invalid."));

        Organization? organization = await repository.GetByIdAsync(
            organizationId,
            cancellationToken);
        if (organization is null)
            return Result.Failure(Error.Create(
                "ORGANIZATION_NOT_FOUND",
                "Organization was not found."));
        if (organization.IsActive == isActive)
            return Result.Success();

        IReadOnlyList<Organization> allOrganizations = await repository.ListAsync(cancellationToken);
        if (isActive && organization.ParentOrganizationId is Guid parentId)
        {
            Organization? parent = allOrganizations.SingleOrDefault(item => item.Id == parentId);
            if (parent is null || !parent.IsActive)
                return Result.Failure(Error.Create(
                    "ORGANIZATION_PARENT_NOT_FOUND",
                    "Activate the parent organization before activating this organization."));
        }

        if (!isActive && HasActiveDescendant(organizationId, allOrganizations))
            return Result.Failure(Error.Create(
                "ORGANIZATION_HAS_ACTIVE_CHILDREN",
                "An organization with active child units cannot be deactivated."));

        if (isActive)
            organization.Activate();
        else
            organization.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static bool HasActiveDescendant(
        Guid organizationId,
        IReadOnlyList<Organization> organizations)
    {
        var visited = new HashSet<Guid> { organizationId };
        var pending = new Stack<Guid>();
        pending.Push(organizationId);

        while (pending.TryPop(out Guid parentId))
        {
            foreach (Organization child in organizations.Where(
                         item => item.ParentOrganizationId == parentId))
            {
                if (!visited.Add(child.Id))
                    continue;
                if (child.IsActive)
                    return true;
                pending.Push(child.Id);
            }
        }

        return false;
    }

    private static Error? ValidateDetails(
        string name,
        string code,
        string? description,
        Guid? parentOrganizationId)
    {
        if (parentOrganizationId == Guid.Empty)
            return Error.Create("ORGANIZATION_PARENT_INVALID", "Parent organization id is invalid.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            return Error.Create("ORGANIZATION_NAME_INVALID", "Organization name must be between 1 and 200 characters.");
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 50)
            return Error.Create("ORGANIZATION_CODE_INVALID", "Organization code must be between 1 and 50 characters.");
        if (description?.Trim().Length > 500)
            return Error.Create("ORGANIZATION_DESCRIPTION_INVALID", "Organization description cannot exceed 500 characters.");

        return null;
    }
}
