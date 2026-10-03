using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ProjectAtmaca.Application.AtmacaCards;
using ProjectAtmaca.Application.AtmacaCards.ChangeStatus;
using ProjectAtmaca.Application.AtmacaCards.ManageSportsProfile;
using ProjectAtmaca.Application.AtmacaCards.ManageMeasurements;
using ProjectAtmaca.Application.AtmacaCards.ManageEducation;
using ProjectAtmaca.Application.AtmacaCards.ManageDocuments;
using ProjectAtmaca.Application.AtmacaCards.ManagePhoto;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Api.AtmacaCards;

[ApiController]
[Route("api/atmaca-cards")]
public sealed class AtmacaCardsController(
    IAtmacaCardReader reader,
    ChangeAtmacaCardStatusCommandHandler statusHandler,
    UpsertAtmacaCardSportsProfileCommandHandler sportsProfileHandler,
    GetAtmacaCardSportsProfilesQueryHandler getSportsProfilesHandler,
    GetAtmacaCardMeasurementsQueryHandler getMeasurementsHandler,
    RecordAtmacaCardMeasurementCommandHandler recordMeasurementHandler,
    GetAtmacaCardEducationQueryHandler getEducationHandler,
    UpdateAtmacaCardEducationCommandHandler updateEducationHandler,
    RecordAtmacaCardDocumentCommandHandler recordDocumentHandler,
    GetAtmacaCardDocumentsQueryHandler getDocumentsHandler,
    OpenAtmacaCardDocumentQueryHandler openDocumentHandler,
    SaveAtmacaCardPhotoCommandHandler savePhotoHandler,
    GetAtmacaCardPhotoQueryHandler getPhotoHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AtmacaCardSummary>>> Search(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            var all = await reader.ListAsync(cancellationToken: cancellationToken);
            return Ok(all);
        }

        if (search.Trim().Length < 2)
            return Ok(Array.Empty<AtmacaCardSummary>());

        var results = await reader.SearchAsync(
            search,
            cancellationToken: cancellationToken);

        return Ok(results);
    }

    [HttpGet("{atmacaCardId:guid}/sports-profiles")]
    public async Task<ActionResult<IReadOnlyList<AtmacaCardSportsProfileResponse>>> GetSportsProfiles(
        Guid atmacaCardId,
        CancellationToken cancellationToken)
    {
        if (atmacaCardId == Guid.Empty)
            return ToProblem(AtmacaCardApplicationErrors.InvalidId);

        var result = await getSportsProfilesHandler.Handle(
            AtmacaCardId.From(atmacaCardId),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Ok(result.Value!.Select(ToResponse).ToList());
    }

    [HttpPut("{atmacaCardId:guid}/sports-profiles")]
    public async Task<ActionResult<AtmacaCardSportsProfileResponse>> UpsertSportsProfile(
        Guid atmacaCardId,
        [FromBody] UpsertAtmacaCardSportsProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (atmacaCardId == Guid.Empty)
            return ToProblem(AtmacaCardApplicationErrors.InvalidId);

        var result = await sportsProfileHandler.Handle(
            new UpsertAtmacaCardSportsProfileCommand(
                AtmacaCardId.From(atmacaCardId),
                request.SportName ?? string.Empty,
                request.LicenseNumber,
                request.StartedSportOn,
                request.ClubRegisteredOn,
                request.CompetitionLevel,
                request.IsNationalAthlete,
                request.PositionIds),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Ok(ToResponse(result.Value!));
    }

    [HttpGet("{atmacaCardId:guid}/measurements")]
    public async Task<ActionResult<IReadOnlyList<AtmacaCardMeasurementResponse>>> GetMeasurements(
        Guid atmacaCardId,
        CancellationToken cancellationToken)
    {
        if (atmacaCardId == Guid.Empty)
            return ToProblem(AtmacaCardApplicationErrors.InvalidId);

        var result = await getMeasurementsHandler.Handle(
            AtmacaCardId.From(atmacaCardId),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Ok(result.Value!.Select(ToResponse).ToList());
    }

    [HttpGet("{atmacaCardId:guid}/education")]
    public async Task<ActionResult<AtmacaCardEducationResponse>> GetEducation(
        Guid atmacaCardId,
        CancellationToken cancellationToken)
    {
        if (atmacaCardId == Guid.Empty)
            return ToProblem(AtmacaCardApplicationErrors.InvalidId);

        var result = await getEducationHandler.Handle(
            AtmacaCardId.From(atmacaCardId),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Ok(ToResponse(result.Value!));
    }

    [HttpPut("{atmacaCardId:guid}/education")]
    public async Task<ActionResult<AtmacaCardEducationResponse>> UpdateEducation(
        Guid atmacaCardId,
        [FromBody] UpdateAtmacaCardEducationRequest request,
        CancellationToken cancellationToken)
    {
        if (atmacaCardId == Guid.Empty)
            return ToProblem(AtmacaCardApplicationErrors.InvalidId);

        var result = await updateEducationHandler.Handle(
            new UpdateAtmacaCardEducationCommand(
                AtmacaCardId.From(atmacaCardId),
                request.IsCurrentlyStudying,
                request.SchoolName,
                request.SchoolGrade,
                request.SchoolNumber),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Ok(ToResponse(result.Value!));
    }

    [HttpGet("{atmacaCardId:guid}/documents")]
    public async Task<ActionResult<IReadOnlyList<AtmacaCardDocumentResponse>>> GetDocuments(
        Guid atmacaCardId,
        CancellationToken cancellationToken)
    {
        if (atmacaCardId == Guid.Empty)
            return ToProblem(AtmacaCardApplicationErrors.InvalidId);

        var result = await getDocumentsHandler.Handle(
            AtmacaCardId.From(atmacaCardId),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Ok(result.Value!.Select(ToResponse).ToList());
    }

    [HttpPost("{atmacaCardId:guid}/documents")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11_000_000)]
    public async Task<ActionResult<AtmacaCardDocumentResponse>> RecordDocument(
        Guid atmacaCardId,
        [FromForm] RecordAtmacaCardDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (atmacaCardId == Guid.Empty)
            return ToProblem(AtmacaCardApplicationErrors.InvalidId);

        await using Stream content = request.File.OpenReadStream();
        var result = await recordDocumentHandler.Handle(
            new RecordAtmacaCardDocumentCommand(
                AtmacaCardId.From(atmacaCardId),
                request.DocumentType,
                request.Title ?? string.Empty,
                request.Issuer,
                request.IssuedOn,
                request.File.FileName,
                request.File.ContentType,
                request.File.Length,
                content),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Ok(ToResponse(result.Value!));
    }

    [HttpGet("{atmacaCardId:guid}/documents/{documentId:guid}/file")]
    public async Task<IActionResult> DownloadDocument(
        Guid atmacaCardId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await openDocumentHandler.Handle(
            AtmacaCardId.From(atmacaCardId),
            documentId,
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        var document = result.Value!;
        return File(document.Content, document.ContentType, document.FileName);
    }

    [HttpPut("{atmacaCardId:guid}/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6_000_000)]
    public async Task<IActionResult> SavePhoto(
        Guid atmacaCardId,
        [FromForm] AtmacaCardPhotoUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (atmacaCardId == Guid.Empty)
            return ToProblem(AtmacaCardApplicationErrors.InvalidId);

        await using Stream content = request.File.OpenReadStream();
        var result = await savePhotoHandler.Handle(
            new SaveAtmacaCardPhotoCommand(
                AtmacaCardId.From(atmacaCardId),
                request.File.FileName,
                request.File.ContentType,
                request.File.Length,
                content),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return NoContent();
    }

    [HttpGet("{atmacaCardId:guid}/photo")]
    public async Task<IActionResult> GetPhoto(
        Guid atmacaCardId,
        CancellationToken cancellationToken)
    {
        var result = await getPhotoHandler.Handle(
            AtmacaCardId.From(atmacaCardId),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return File(result.Value!.Content, result.Value.ContentType);
    }

    [HttpPost("{atmacaCardId:guid}/measurements")]
    public async Task<ActionResult<AtmacaCardMeasurementResponse>> RecordMeasurement(
        Guid atmacaCardId,
        [FromBody] RecordAtmacaCardMeasurementRequest request,
        CancellationToken cancellationToken)
    {
        if (atmacaCardId == Guid.Empty)
            return ToProblem(AtmacaCardApplicationErrors.InvalidId);

        var result = await recordMeasurementHandler.Handle(
            new RecordAtmacaCardMeasurementCommand(
                AtmacaCardId.From(atmacaCardId),
                request.MeasuredOn,
                request.HeightCentimeters,
                request.WeightKilograms),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return Ok(ToResponse(result.Value!));
    }

    [HttpPatch("{atmacaCardId:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid atmacaCardId,
        [FromBody] ChangeAtmacaCardStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (atmacaCardId == Guid.Empty)
            return ToProblem(AtmacaCardApplicationErrors.InvalidId);

        var result = await statusHandler.Handle(
            new ChangeAtmacaCardStatusCommand(
                AtmacaCardId.From(atmacaCardId),
                request.IsActive),
            cancellationToken);
        if (result.IsFailure)
            return ToProblem(result.Error!);

        return NoContent();
    }

    private ObjectResult ToProblem(Error error)
    {
        var statusCode = error.Code == ActorAuthorizationErrors.Forbidden.Code
            ? StatusCodes.Status403Forbidden
            : error.Code == AtmacaCardApplicationErrors.NotFound.Code
                || error.Code == "ATMACA_CARD_DOCUMENT_NOT_FOUND"
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = error.Message
        };
        problem.Extensions["code"] = error.Code;
        var response = StatusCode(statusCode, problem);
        response.ContentTypes.Add("application/problem+json");
        return response;
    }

    private static AtmacaCardSportsProfileResponse ToResponse(
        AtmacaCardSportsProfile profile) =>
        new(
            profile.Id,
            profile.SportName,
            profile.LicenseNumber?.Value,
            profile.StartedSportOn,
            profile.ClubRegisteredOn,
            profile.CompetitionLevel,
            profile.IsNationalAthlete,
            profile.Positions
                .Select(position => new AtmacaCardPositionResponse(
                    position.Id,
                    position.Code,
                    position.Name))
                .ToList());

    private static AtmacaCardSportsProfileResponse ToResponse(
        AtmacaCardSportsProfileDetails profile) =>
        new(
            profile.Id,
            profile.SportName,
            profile.LicenseNumber,
            profile.StartedSportOn,
            profile.ClubRegisteredOn,
            profile.CompetitionLevel,
            profile.IsNationalAthlete,
            profile.Positions
                .Select(position => new AtmacaCardPositionResponse(
                    position.Id,
                    position.Code,
                    position.Name))
                .ToList());

    private static AtmacaCardMeasurementResponse ToResponse(
        AtmacaCardMeasurementDetails measurement) =>
        new(
            measurement.Id,
            measurement.MeasuredOn,
            measurement.HeightCentimeters,
            measurement.WeightKilograms);

    private static AtmacaCardEducationResponse ToResponse(
        AtmacaCardEducationDetails education) =>
        new(
            education.IsCurrentlyStudying,
            education.SchoolName,
            education.SchoolGrade,
            education.SchoolNumber);

    private static AtmacaCardDocumentResponse ToResponse(
        AtmacaCardDocumentDetails document) =>
        new(
            document.Id,
            document.DocumentType,
            document.Title,
            document.Issuer,
            document.IssuedOn,
            document.OriginalFileName,
            document.ContentType,
            document.FileSizeBytes);
}

public sealed record ChangeAtmacaCardStatusRequest(bool IsActive);

public sealed record UpsertAtmacaCardSportsProfileRequest(
    string? SportName,
    string? LicenseNumber,
    DateOnly? StartedSportOn,
    DateOnly? ClubRegisteredOn,
    AthleteCompetitionLevel? CompetitionLevel,
    bool? IsNationalAthlete,
    IReadOnlyCollection<Guid>? PositionIds = null);

public sealed record AtmacaCardSportsProfileResponse(
    Guid Id,
    string SportName,
    string? LicenseNumber,
    DateOnly? StartedSportOn,
    DateOnly? ClubRegisteredOn,
    AthleteCompetitionLevel? CompetitionLevel,
    bool? IsNationalAthlete,
    IReadOnlyList<AtmacaCardPositionResponse> Positions);

public sealed record AtmacaCardPositionResponse(Guid Id, string Code, string Name);

public sealed record RecordAtmacaCardMeasurementRequest(
    DateOnly MeasuredOn,
    decimal? HeightCentimeters,
    decimal? WeightKilograms);

public sealed record AtmacaCardMeasurementResponse(
    Guid Id,
    DateOnly MeasuredOn,
    decimal? HeightCentimeters,
    decimal? WeightKilograms);

public sealed record UpdateAtmacaCardEducationRequest(
    bool IsCurrentlyStudying,
    string? SchoolName,
    string? SchoolGrade,
    string? SchoolNumber);

public sealed record AtmacaCardEducationResponse(
    bool IsCurrentlyStudying,
    string? SchoolName,
    string? SchoolGrade,
    string? SchoolNumber);

public sealed class RecordAtmacaCardDocumentRequest
{
    public AtmacaCardDocumentType DocumentType { get; init; }
    public string? Title { get; init; }
    public string? Issuer { get; init; }
    public DateOnly? IssuedOn { get; init; }
    public IFormFile File { get; init; } = null!;
}

public sealed class AtmacaCardPhotoUploadRequest
{
    public IFormFile File { get; init; } = null!;
}

public sealed record AtmacaCardDocumentResponse(
    Guid Id,
    AtmacaCardDocumentType DocumentType,
    string Title,
    string? Issuer,
    DateOnly? IssuedOn,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes);
