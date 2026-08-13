using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.AgeGroups;

public sealed class AgeGroup : AuditableAggregateRoot
{
    public AgeGroupCode Code { get; private set; }

    public bool IsActive { get; private set; }

    private AgeGroup()
    {
        Code = null!;
    }

    private AgeGroup(
        Guid id,
        AgeGroupCode code)
        : base(id)
    {
        Code = code;
        IsActive = true;
    }

    public static Result<AgeGroup> Create(
        AgeGroupCode code)
    {
        if (code is null)
        {
            return Result<AgeGroup>.Failure(
                Error.Create(
                    "AGE_GROUP_CODE_REQUIRED",
                    "Age group code is required."));
        }

        return Result<AgeGroup>.Success(
            new AgeGroup(
                Guid.NewGuid(),
                code));
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
