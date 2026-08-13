using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.Enums;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Domain.Scouting.ValueObjects;

public sealed class InitialScoutingSource : ValueObject
{
    public InitialScoutingSourceType SourceType { get; }

    public PersonName? ReferrerName { get; }

    public string? SourceDescription { get; }

    private InitialScoutingSource(
        InitialScoutingSourceType sourceType,
        PersonName? referrerName,
        string? sourceDescription)
    {
        SourceType = sourceType;
        ReferrerName = referrerName;
        SourceDescription = sourceDescription;
    }

    public static Result<InitialScoutingSource> Create(
        InitialScoutingSourceType sourceType,
        PersonName? referrerName = null,
        string? sourceDescription = null)
    {
        if (!Enum.IsDefined(sourceType))
        {
            return Result<InitialScoutingSource>.Failure(
                Error.Create(
                    "INITIAL_SCOUTING_SOURCE_TYPE_INVALID",
                    "Initial scouting source type is invalid."));
        }

        if (sourceType == InitialScoutingSourceType.Reference &&
            referrerName is null)
        {
            return Result<InitialScoutingSource>.Failure(
                Error.Create(
                    "INITIAL_SCOUTING_SOURCE_REFERRER_REQUIRED",
                    "Referrer name is required for a reference source."));
        }

        if (sourceType == InitialScoutingSourceType.Other &&
            string.IsNullOrWhiteSpace(sourceDescription))
        {
            return Result<InitialScoutingSource>.Failure(
                Error.Create(
                    "INITIAL_SCOUTING_SOURCE_DESCRIPTION_REQUIRED",
                    "Source description is required for an other source."));
        }

        if (sourceType != InitialScoutingSourceType.Reference && 
            referrerName is not null)
        {
            return Result<InitialScoutingSource>.Failure(
                Error.Create(
                    "INITIAL_SCOUTING_SOURCE_REFERRER_NOT_ALLOWED",
                    "Referrer name is only allowed for a reference source."));
        }

        if (sourceType != InitialScoutingSourceType.Other &&
            !string.IsNullOrWhiteSpace(sourceDescription))
        {
            return Result<InitialScoutingSource>.Failure(
                Error.Create(
                    "INITIAL_SCOUTING_SOURCE_DESCRIPTION_NOT_ALLOWED",
                    "Source description is only allowed for an other source."));
        }

        var normalizedDescription =
            string.IsNullOrWhiteSpace(sourceDescription)
                ? null
                : sourceDescription.Trim();

        return Result<InitialScoutingSource>.Success(
            new InitialScoutingSource(
                sourceType,
                referrerName,
                normalizedDescription));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SourceType;
        yield return ReferrerName;
        yield return SourceDescription;
    }

    public override string ToString()
    {
        return SourceType switch
        {
            InitialScoutingSourceType.Reference =>
                $"{SourceType} - {ReferrerName}",

            InitialScoutingSourceType.Other =>
                $"{SourceType} - {SourceDescription}",

            _ => SourceType.ToString()
        };
    }
}
