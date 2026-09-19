using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.SeasonTeams;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Application.Participations.Create;

namespace ProjectAtmaca.Application.Participations.CreateForTraining;

public sealed class CreateTrainingParticipationCommandHandler
{
    private readonly IActorAuthorizationService _authorizationService;
    private readonly IParticipationRepository _participationRepository;
    private readonly ITrainingRepository _trainingRepository;
    private readonly ISeasonTeamRepository _seasonTeamRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTrainingParticipationCommandHandler(
        IActorAuthorizationService authorizationService,
        IParticipationRepository participationRepository,
        ITrainingRepository trainingRepository,
        ISeasonTeamRepository seasonTeamRepository,
        IUnitOfWork unitOfWork)
    {
        _authorizationService = authorizationService;
        _participationRepository = participationRepository;
        _trainingRepository = trainingRepository;
        _seasonTeamRepository = seasonTeamRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ParticipationId>> Handle(
        CreateTrainingParticipationCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _authorizationService.AuthorizeAsync(
            Permissions.Participations.Create,
            cancellationToken);
        if (authorization.IsFailure)
            return Result<ParticipationId>.Failure(authorization.Error!);

        Training? training = await _trainingRepository.GetByIdAsync(
            command.TrainingId,
            cancellationToken);
        if (training is null)
            return Result<ParticipationId>.Failure(
                CreateTrainingParticipationErrors.TrainingNotFound);

        if (training.SeasonTeamId is null)
            return Result<ParticipationId>.Failure(
                CreateTrainingParticipationErrors.SeasonTeamRequired);

        SeasonTeam? seasonTeam = await _seasonTeamRepository.GetByIdAsync(
            training.SeasonTeamId.Value,
            cancellationToken);
        if (seasonTeam is null)
            return Result<ParticipationId>.Failure(
                CreateTrainingParticipationErrors.MembershipNotActive);

        if (seasonTeam.Status != SeasonTeamStatus.Active)
            return Result<ParticipationId>.Failure(
                CreateTrainingParticipationErrors.SeasonTeamInactive);

        if (!seasonTeam.HasActiveMembership(
                command.AtmacaCardId,
                training.Schedule.Date.ToDateTime(TimeOnly.MinValue)))
        {
            return Result<ParticipationId>.Failure(
                CreateTrainingParticipationErrors.MembershipNotActive);
        }

        var activityReference = ActivityReference.ForTraining(
            command.TrainingId);
        if (await _participationRepository.ExistsAsync(
                command.AtmacaCardId,
                activityReference,
                cancellationToken))
        {
            return Result<ParticipationId>.Failure(
                CreateParticipationErrors.AlreadyExists);
        }

        var creation = Participation.Create(
            activityReference,
            command.AtmacaCardId);
        if (creation.IsFailure)
            return Result<ParticipationId>.Failure(creation.Error!);

        await _participationRepository.AddAsync(
            creation.Value!,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ParticipationId>.Success(
            creation.Value!.ParticipationId);
    }
}
