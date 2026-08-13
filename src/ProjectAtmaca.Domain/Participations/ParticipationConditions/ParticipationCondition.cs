using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Participations;

public sealed class ParticipationCondition : ValueObject
{
    public const string LateCode = "LATE";

    public const string BtaCode = "BTA";

    public static ParticipationCondition Late { get; } =
        new(
            LateCode,
            ParticipationStatus.Present,
            AttendanceTreatment.Included);

    public static ParticipationCondition Bta { get; } =
        new(
            BtaCode,
            ParticipationStatus.Absent,
            AttendanceTreatment.Excluded);

    public string Code { get; }

    public ParticipationStatus ApplicableStatus { get; }

    public AttendanceTreatment AttendanceTreatment { get; }

    private ParticipationCondition()
    {
        Code = null!;
    }

    private ParticipationCondition(
        string code,
        ParticipationStatus applicableStatus,
        AttendanceTreatment attendanceTreatment)
    {
        Code = code;
        ApplicableStatus = applicableStatus;
        AttendanceTreatment = attendanceTreatment;
    }

    public static Result<ParticipationCondition> Create(
        string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result<ParticipationCondition>.Failure(
                ParticipationErrors
                    .UnsupportedParticipationCondition(
                        string.Empty));
        }

        string normalizedCode =
            code
                .Trim()
                .ToUpperInvariant();

        return normalizedCode switch
        {
            LateCode =>
                Result<ParticipationCondition>.Success(
                    Late),

            BtaCode =>
                Result<ParticipationCondition>.Success(
                    Bta),

            _ =>
                Result<ParticipationCondition>.Failure(
                    ParticipationErrors
                        .UnsupportedParticipationCondition(
                            normalizedCode))
        };
    }

    public bool IsApplicableTo(
        ParticipationStatus status)
    {
        return ApplicableStatus == status;
    }

    protected override IEnumerable<object?>
        GetEqualityComponents()
    {
        yield return Code;
    }

    public override string ToString()
    {
        return Code;
    }
}
