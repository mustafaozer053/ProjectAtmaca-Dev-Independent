using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.TrainingTypes;

public sealed class TrainingType : AuditableAggregateRoot
{
    public TrainingTypeId TrainingTypeId =>
        TrainingTypeId.From(Id);

    public TrainingTypeCode Code { get; private set; }

    public TrainingTypeName Name { get; private set; }

    public TrainingTypeDescription Description { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    private TrainingType()
    {
        Code = null!;
        Name = null!;
        Description = null!;
    }

    private TrainingType(
        TrainingTypeId id,
        TrainingTypeCode code,
        TrainingTypeName name,
        TrainingTypeDescription description,
        int displayOrder)
        : base(id.Value)
    {
        Code = code;
        Name = name;
        Description = description;
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public static Result<TrainingType> Create(
        TrainingTypeCode code,
        TrainingTypeName name,
        TrainingTypeDescription description,
        int displayOrder)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(description);

        if (displayOrder < 0)
        {
            return Result<TrainingType>.Failure(
                TrainingTypeErrors.DisplayOrderInvalid);
        }

        return Result<TrainingType>.Success(
            new TrainingType(
                TrainingTypeId.New(),
                code,
                name,
                description,
                displayOrder));
    }

    public void ChangeCode(TrainingTypeCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        Code = code;
    }

    public void Rename(TrainingTypeName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
    }

    public void ChangeDescription(
        TrainingTypeDescription description)
    {
        ArgumentNullException.ThrowIfNull(description);

        Description = description;
    }

    public Result ChangeDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
        {
            return Result.Failure(
                TrainingTypeErrors.DisplayOrderInvalid);
        }

        DisplayOrder = displayOrder;

        return Result.Success();
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
