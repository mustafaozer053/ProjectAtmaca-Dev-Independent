using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Domain.TrainingTypes;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Auditing;
using ProjectAtmaca.Infrastructure.Persistence.Decisions;
using ProjectAtmaca.Infrastructure.Persistence.Readers;
using ProjectAtmaca.Infrastructure.Persistence.Repositories;
using ProjectAtmaca.Infrastructure.Persistence.Security;
using ProjectAtmaca.Application.Decisions;

namespace ProjectAtmaca.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString =
            configuration.GetConnectionString(
                "ProjectAtmacaDatabase")
            ?? throw new InvalidOperationException(
                "Connection string " +
                "'ProjectAtmacaDatabase' was not found.");

        services.AddScoped<
            AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<ProjectAtmacaDbContext>(
            (serviceProvider, options) =>
                options
                    .UseSqlServer(
                        connectionString)
                    .AddInterceptors(
                        serviceProvider
                            .GetRequiredService<
                                AuditableEntitySaveChangesInterceptor>()));

        services.AddHealthChecks()
            .AddDbContextCheck<ProjectAtmacaDbContext>(
                name: "projectatmaca-database",
                tags:
                    new[]
                    {
                        "ready"
                    });

        services.AddScoped<
            IParticipationRepository,
            ParticipationRepository>();

        services.AddScoped<
            ITrainingRepository,
            TrainingRepository>();

        services.AddScoped<
            ITrainingTypeRepository,
            TrainingTypeRepository>();

        services.AddScoped<
            IDecisionRepository,
            DecisionRepository>();

        services.AddScoped<
            IDecisionApplicationRepository,
            DecisionApplicationRepository>();

        services.AddScoped<
            IParticipationReader,
            ParticipationReader>();

        services.AddScoped<
            IDecisionApplicationReader,
            DecisionApplicationReader>();

        services.AddScoped<
            IUnitOfWork,
            UnitOfWork>();

        services.AddScoped<
            IDecisionAuthorityCommitter,
            DecisionAuthorityCommitter>();

        services.AddScoped<
            IDecisionApplicationOperationStore,
            DecisionApplicationOperationStore>();

        services.AddScoped<
            IActorIdentityResolver,
            ActorIdentityResolver>();

        services.AddScoped<
            IActorPermissionEvaluator,
            ActorPermissionEvaluator>();

        services.AddScoped<ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard.IPersonRegistrationStore,
            ProjectAtmaca.Infrastructure.Persistence.Persons.SqlPersonRegistrationStore>();
        services.AddScoped<ProjectAtmaca.Domain.Services.IAtmacaCardNumberGenerator,
            ProjectAtmaca.Infrastructure.Persistence.Persons.SqlAtmacaCardNumberGenerator>();
        services.AddScoped<ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard.PersonRegistrationAuthorization>();
        services.AddScoped<ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard.PreparePersonRegistration>();
        services.AddScoped<ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard.RegisterPersonWithAtmacaCard>();
        Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton<TimeProvider>(services, TimeProvider.System);

        return services;
    }
}
