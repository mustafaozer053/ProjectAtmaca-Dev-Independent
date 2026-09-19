using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.SeasonTeams;

public static class SeasonTeamErrors
{
    public static readonly Error NameRequired =
        Error.Create(
            "SEASON_TEAM_NAME_REQUIRED",
            "Season team name is required.");

    public static readonly Error SeasonRequired =
        Error.Create(
            "SEASON_TEAM_SEASON_REQUIRED",
            "Season id is required.");

    public static readonly Error OrganizationRequired =
        Error.Create(
            "SEASON_TEAM_ORGANIZATION_REQUIRED",
            "Organization id is required.");

    public static readonly Error AgeGroupRequired =
        Error.Create(
            "SEASON_TEAM_AGE_GROUP_REQUIRED",
            "Age group id is required.");

    public static readonly Error MembershipRequired =
        Error.Create(
            "SEASON_TEAM_MEMBERSHIP_REQUIRED",
            "Season team membership is required.");

    public static readonly Error DuplicateMembership =
        Error.Create(
            "SEASON_TEAM_DUPLICATE_MEMBERSHIP",
            "The Atmaca card has an overlapping membership in this season team.");

    public static readonly Error MembershipNotFound =
        Error.Create(
            "SEASON_TEAM_MEMBERSHIP_NOT_FOUND",
            "Season team membership was not found.");
}
