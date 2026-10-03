namespace ProjectAtmaca.Web.Services;

public sealed record CatalogItemResponse(
    Guid Id,
    string Name,
    Guid? ParentOrganizationId = null,
    string? HierarchyPath = null);

// Client-side mirrors of ProjectAtmaca.Api.SeasonTeams response contracts.
// Kept separate from the API project so the Web app only depends on the
// wire shape (JSON) rather than referencing the Api assembly directly.

public sealed record SeasonTeamResponse(
    Guid Id,
    Guid SeasonId,
    Guid OrganizationId,
    Guid AgeGroupId,
    string Name,
    int MembershipCount,
    bool IsActive);

public sealed record SeasonTeamDetailsResponse(
    Guid Id,
    Guid SeasonId,
    Guid OrganizationId,
    Guid AgeGroupId,
    string Name,
    bool IsActive,
    DateTime SeasonStartDate,
    DateTime SeasonEndDate,
    IReadOnlyList<SeasonTeamMembershipResponse> Memberships);

public sealed record AtmacaCardSummary(
    Guid AtmacaCardId,
    Guid PersonId,
    string FullName,
    string CardNumber,
    bool IsActive = true);

public sealed record AtmacaCardSportsProfileResponse(
    Guid Id,
    string SportName,
    string? LicenseNumber,
    DateOnly? StartedSportOn,
    DateOnly? ClubRegisteredOn,
    int? CompetitionLevel,
    bool? IsNationalAthlete,
    IReadOnlyList<AtmacaCardPositionResponse> Positions);

public sealed record AtmacaCardPositionResponse(Guid Id, string Code, string Name);

public sealed record PositionCatalogItem(
    Guid Id,
    string SportName,
    string Code,
    string Name,
    bool IsActive);

public sealed record AtmacaCardMeasurementResponse(
    Guid Id,
    DateOnly MeasuredOn,
    decimal? HeightCentimeters,
    decimal? WeightKilograms);

public sealed record AtmacaCardEducationResponse(
    bool IsCurrentlyStudying,
    string? SchoolName,
    string? SchoolGrade,
    string? SchoolNumber);

public sealed record AtmacaCardDocumentResponse(
    Guid Id,
    int DocumentType,
    string Title,
    string? Issuer,
    DateOnly? IssuedOn,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes);

public sealed record ProfessionalTitleResponse(
    Guid Id,
    string Title,
    DateOnly StartedOn,
    DateOnly? EndedOn,
    IReadOnlyList<Guid> EvidenceDocumentIds);

public sealed record SeasonTeamRosterViewResponse(
    Guid SeasonTeamId,
    int TotalMembershipCount,
    int ActiveMembershipCount,
    int InactiveMembershipCount,
    int UnclassifiedMembershipCount,
    IReadOnlyList<SeasonTeamRosterPrimarySectionResponse> PrimaryRosterSections,
    IReadOnlyList<SeasonTeamRosterGroupResponse> Groups,
    IReadOnlyList<SeasonTeamRosterClassificationCountResponse> ClassificationCounts,
    IReadOnlyList<SeasonTeamMembershipResponse> UnclassifiedMemberships);

public sealed record SeasonTeamRosterGroupResponse(
    string Classification,
    int MembershipCount,
    IReadOnlyList<SeasonTeamMembershipResponse> Memberships);

public sealed record SeasonTeamRosterClassificationCountResponse(
    string Classification,
    int MembershipCount);

public sealed record SeasonTeamRosterPrimarySectionResponse(
    string SectionKey,
    string Label,
    int MembershipCount,
    bool HasMembers);

public sealed record SeasonTeamMembershipResponse(
    Guid Id,
    Guid AtmacaCardId,
    string? AtmacaCardDisplayName,
    string? AtmacaCardNumber,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive,
    IReadOnlyList<SeasonTeamMembershipAssignmentResponse> Assignments);

public sealed record SeasonTeamMembershipAssignmentResponse(
    Guid Id,
    string Kind,
    Guid DefinitionId,
    string DisplayNameSnapshot,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive);

public sealed record OrganizationDutyAssignmentResponse(
    Guid Id,
    Guid AtmacaCardId,
    Guid OrganizationId,
    string Title,
    DateTime StartDate,
    DateTime? EndDate);
