using ProjectAtmaca.Domain.SeasonTeams;

namespace ProjectAtmaca.Application.SeasonTeams.GetById;

public sealed record SeasonTeamDetails(
    Guid Id,
    Guid SeasonId,
    Guid OrganizationId,
    Guid AgeGroupId,
    string Name,
    bool IsActive,
    IReadOnlyList<SeasonTeamMembershipDetails> Memberships);

public sealed record SeasonTeamMembershipDetails(
    Guid Id,
    Guid AtmacaCardId,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive,
    IReadOnlyList<SeasonTeamMembershipAssignmentDetails> Assignments);

public sealed record SeasonTeamMembershipAssignmentDetails(
    Guid Id,
    SeasonTeamAssignmentKind Kind,
    Guid DefinitionId,
    string DisplayNameSnapshot,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive);
