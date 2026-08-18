using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Infrastructure.Persistence.Readers;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Repositories;
using ProjectAtmaca.Application.Abstractions.Persistence;

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
            IParticipationReader,
            ParticipationReader>();

        services.AddScoped<
            IUnitOfWork,
            UnitOfWork>();

        return services;
    }
}
