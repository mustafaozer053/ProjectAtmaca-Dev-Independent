using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Infrastructure.Tests.Persistence.Participations;
using Xunit;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence;

public sealed class SqlIntegrationCollectionFixture
    : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await using var context =
            ParticipationPersistenceTestContextFactory.CreateContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
