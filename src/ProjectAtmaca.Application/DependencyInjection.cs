using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Security;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Application.Participations.Create;
using ProjectAtmaca.Application.Participations.CreateForTraining;
using ProjectAtmaca.Application.Participations.MarkPresent;
using ProjectAtmaca.Application.Participations.MarkAbsent;
using ProjectAtmaca.Application.Participations.CorrectClassification;
using ProjectAtmaca.Application.Participations.RecordArrival;
using ProjectAtmaca.Application.Participations.RecordDeparture;
using ProjectAtmaca.Application.Participations.ListByActivity;
using ProjectAtmaca.Application.Participations.GetSummaryByActivity;
using ProjectAtmaca.Application.Participations;
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
using ProjectAtmaca.Application.SeasonTeams.ChangeStatus;
using ProjectAtmaca.Application.SeasonTeams.List;
using ProjectAtmaca.Application.SeasonTeams.AddMembership;
using ProjectAtmaca.Application.SeasonTeams.EndMembership;
using ProjectAtmaca.Application.SeasonTeams.GetById;
using ProjectAtmaca.Application.SeasonTeams.AddMembershipAssignment;
using ProjectAtmaca.Application.SeasonTeams.EndMembershipAssignment;
using ProjectAtmaca.Application.Fixtures;
using ProjectAtmaca.Application.AtmacaCards.ChangeStatus;
using ProjectAtmaca.Application.AtmacaCards.ManageSportsProfile;
using ProjectAtmaca.Application.AtmacaCards.ManageMeasurements;
using ProjectAtmaca.Application.Positions;
using ProjectAtmaca.Application.AtmacaCards.ManageEducation;
using ProjectAtmaca.Application.AtmacaCards.ManageDocuments;
using ProjectAtmaca.Application.AtmacaCards.ManagePhoto;
using ProjectAtmaca.Application.Persons.ManageProfessionalTitles;
using ProjectAtmaca.Application.Organizations.Catalog;

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
        services.AddScoped<BtaEligibilityValidator>();

        services.AddScoped<
            GetParticipationByIdQueryHandler>();

        services.AddScoped<
            MarkParticipationPresentCommandHandler>();
        services.AddScoped<
            MarkParticipationAbsentCommandHandler>();
        services.AddScoped<CorrectParticipationClassificationCommandHandler>();

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
        services.AddScoped<AddSeasonTeamMembershipAssignmentCommandHandler>();
        services.AddScoped<EndSeasonTeamMembershipAssignmentCommandHandler>();
        services.AddScoped<GetSeasonTeamByIdQueryHandler>();
        services.AddScoped<ChangeSeasonTeamStatusCommandHandler>();
        services.AddScoped<ChangeAtmacaCardStatusCommandHandler>();
        services.AddScoped<UpsertAtmacaCardSportsProfileCommandHandler>();
        services.AddScoped<GetAtmacaCardSportsProfilesQueryHandler>();
        services.AddScoped<GetAtmacaCardMeasurementsQueryHandler>();
        services.AddScoped<RecordAtmacaCardMeasurementCommandHandler>();
        services.AddScoped<GetAtmacaCardEducationQueryHandler>();
        services.AddScoped<UpdateAtmacaCardEducationCommandHandler>();
        services.AddScoped<RecordAtmacaCardDocumentCommandHandler>();
        services.AddScoped<GetAtmacaCardDocumentsQueryHandler>();
        services.AddScoped<OpenAtmacaCardDocumentQueryHandler>();
        services.AddScoped<SaveAtmacaCardPhotoCommandHandler>();
        services.AddScoped<GetAtmacaCardPhotoQueryHandler>();
        services.AddScoped<GetProfessionalTitlesQueryHandler>();
        services.AddScoped<AddProfessionalTitleCommandHandler>();
        services.AddScoped<EndProfessionalTitleCommandHandler>();
        services.AddScoped<UpdateProfessionalTitleCommandHandler>();
        services.AddScoped<RemoveProfessionalTitleCommandHandler>();
        services.AddScoped<LinkProfessionalTitleDocumentCommandHandler>();
        services.AddScoped<ProjectAtmaca.Application.Organizations.Assignments.ListOrganizationDutyAssignmentsQueryHandler>();
        services.AddScoped<ProjectAtmaca.Application.Organizations.Assignments.AddOrganizationDutyAssignmentCommandHandler>();
        services.AddScoped<ProjectAtmaca.Application.Organizations.Assignments.EndOrganizationDutyAssignmentCommandHandler>();
        services.AddScoped<PositionCatalogService>();
        services.AddScoped<OrganizationCatalogService>();
        services.AddScoped<FixtureService>();

        return services;
    }
}
