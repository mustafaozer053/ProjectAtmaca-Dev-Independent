using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Security;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Application.Participations.Create;
using ProjectAtmaca.Application.Participations.CreateForTraining;
using ProjectAtmaca.Application.Participations.MarkPresent;
using ProjectAtmaca.Application.Participations.RecordArrival;
using ProjectAtmaca.Application.Participations.RecordDeparture;
using ProjectAtmaca.Application.Participations.ListByActivity;
using ProjectAtmaca.Application.Participations.GetSummaryByActivity;
using ProjectAtmaca.Application.Participations.ListHistoryByAtmacaCard;
using ProjectAtmaca.Application.Decisions.ApplyParticipationClassification;
using ProjectAtmaca.Application.Decisions.ListApplicationHistory;
using ProjectAtmaca.Application.Trainings.Cancel;
using ProjectAtmaca.Application.Trainings.GetById;
using ProjectAtmaca.Application.Trainings.Create;
using ProjectAtmaca.Application.Trainings.Confirm;
using ProjectAtmaca.Application.Trainings.Reschedule;
using ProjectAtmaca.Application.TrainingTypes.Create;
using ProjectAtmaca.Application.TrainingTypes.List;
using ProjectAtmaca.Application.TrainingTypes.ChangeStatus;
using ProjectAtmaca.Application.Trainings.List;
using ProjectAtmaca.Application.SeasonTeams.Create;
using ProjectAtmaca.Application.SeasonTeams.List;
using ProjectAtmaca.Application.SeasonTeams.AddMembership;
using ProjectAtmaca.Application.SeasonTeams.EndMembership;
using ProjectAtmaca.Application.SeasonTeams.GetById;

namespace ProjectAtmaca.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddMetrics();

        services.AddScoped<
            IActorAuthorizationService,
            ActorAuthorizationService>();

        services.AddSingleton<
            IDecisionApplicationMetrics,
            DecisionApplicationMetrics>();

        services.AddScoped<
            CreateParticipationCommandHandler>();
        services.AddScoped<
            CreateTrainingParticipationCommandHandler>();

        services.AddScoped<
            GetParticipationByIdQueryHandler>();

        services.AddScoped<
            MarkParticipationPresentCommandHandler>();

        services.AddScoped<
            RecordParticipationArrivalCommandHandler>();

        services.AddScoped<
            RecordParticipationDepartureCommandHandler>();

        services.AddScoped<
            ListParticipationsByActivityQueryHandler>();

        services.AddScoped<
            GetParticipationSummaryByActivityQueryHandler>();

        services.AddScoped<
            ListParticipationHistoryByAtmacaCardQueryHandler>();

        services.AddScoped<
            ApplyParticipationClassificationCommandHandler>();

        services.AddScoped<
            ListDecisionApplicationHistoryQueryHandler>();

        services.AddScoped<
            CancelTrainingCommandHandler>();

        services.AddScoped<
            GetTrainingByIdQueryHandler>();

        services.AddScoped<
            CreateTrainingCommandHandler>();

        services.AddScoped<
            ConfirmTrainingCommandHandler>();

        services.AddScoped<
            RescheduleTrainingCommandHandler>();
        services.AddScoped<ListTrainingsQueryHandler>();
        services.AddScoped<CreateTrainingTypeCommandHandler>();
        services.AddScoped<ListTrainingTypesQueryHandler>();
        services.AddScoped<ChangeTrainingTypeStatusCommandHandler>();
        services.AddScoped<CreateSeasonTeamCommandHandler>();
        services.AddScoped<ListSeasonTeamsQueryHandler>();
        services.AddScoped<AddSeasonTeamMembershipCommandHandler>();
        services.AddScoped<EndSeasonTeamMembershipCommandHandler>();
        services.AddScoped<GetSeasonTeamByIdQueryHandler>();

        return services;
    }
}
