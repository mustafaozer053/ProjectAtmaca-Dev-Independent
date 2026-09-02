using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Decisions;
using ProjectAtmaca.Infrastructure.Persistence.Readers;
using ProjectAtmaca.Infrastructure.Persistence.Repositories;
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

        services.AddDbContext<ProjectAtmacaDbContext>(
            options =>
                options.UseSqlServer(
                    connectionString));

        services.AddScoped<
            IParticipationRepository,
            ParticipationRepository>();

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

        return services;
    }
}
