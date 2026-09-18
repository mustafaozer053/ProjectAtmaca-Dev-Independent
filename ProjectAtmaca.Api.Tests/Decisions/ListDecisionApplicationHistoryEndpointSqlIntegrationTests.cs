using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectAtmaca.Api.Decisions.ListApplicationHistory;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Decisions;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Readers;
using ProjectAtmaca.Infrastructure.Persistence.Security;

namespace ProjectAtmaca.Api.Tests.Decisions;

public sealed class ListDecisionApplicationHistoryEndpointSqlIntegrationTests
{
    [Fact]
    public async Task Get_Should_FilterAndOrderProvenanceThroughProductionSqlComposition()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string databaseName = $"ProjectAtmaca_Api_DecisionHistory_{Guid.NewGuid():N}";
        string connection = $"Server=(localdb)\\mssqllocaldb;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";
        ActorId actor = ActorId.New();
        DecisionId decision = DecisionId.New();
        var target = DecisionTargetReference.ForParticipation(ParticipationId.New());
        var time = new DateTimeOffset(2026, 9, 13, 10, 0, 0, TimeSpan.Zero).AddTicks(1234567);
        var oldest = DecisionApplication.Create(decision, target, DecisionRevision.Initial, time.AddDays(-1));
        var first = DecisionApplication.Create(decision, target, DecisionRevision.From(3), time);
        var second = DecisionApplication.Create(decision, target, DecisionRevision.From(2), time);
        var excluded = DecisionApplication.Create(DecisionId.New(), target, DecisionRevision.Initial, time.AddDays(1));
        using var root = new ProjectAtmacaApiFactory();
        using var factory = root.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.ReplaceProjectAtmacaDatabase(connection);
            DecisionHistoryTestAuthentication.AddTo(services);
        }));
        try
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ProjectAtmacaDbContext>();
                context.Database.GetDbConnection().Database.Should().Be(databaseName);
                context.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.SqlServer");
                scope.ServiceProvider.GetRequiredService<IDecisionApplicationReader>()
                    .Should().BeOfType<DecisionApplicationReader>();
                await context.Database.MigrateAsync(token);
                context.Set<ActorIdentityMapping>().Add(ActorIdentityMapping.Create(
                    ExternalIdentity.Create(ProjectAtmacaApiFactory.AuthenticationAuthority,
                        DecisionHistoryTestAuthentication.Subject), actor));
                context.Set<ActorPermissionGrant>().Add(ActorPermissionGrant.Create(
                    actor, Permissions.Decisions.ListApplicationHistory));
                // Fixed IDs distinguish SQL Server ordering from in-memory Guid ordering.
                context.Entry(first).Property(item => item.Id).CurrentValue =
                    Guid.Parse("00000000-0000-0000-0000-000000000002");
                context.Entry(second).Property(item => item.Id).CurrentValue =
                    Guid.Parse("ffffffff-ffff-ffff-ffff-000000000001");
                context.Set<DecisionApplication>().AddRange(oldest, second, excluded, first);
                await context.SaveChangesAsync(token);
            }

            using var client = factory.CreateClient();
            using var response = await client.GetAsync($"/api/decisions/{decision.Value:D}/applications", token);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var rows = (await response.Content.ReadFromJsonAsync<DecisionApplicationHistoryItemResponse[]>(token))!;
            rows.Select(row => row.DecisionApplicationId).Should().Equal(
                first.DecisionApplicationId.Value, second.DecisionApplicationId.Value, oldest.DecisionApplicationId.Value);
            rows.Should().OnlyContain(row => row.DecisionId == decision.Value &&
                row.TargetId == target.TargetId && row.TargetTypeCode == "PARTICIPATION");
            rows.Select(row => row.AppliedDecisionRevision).Should().Equal(3, 2, 1);
            rows.Select(row => row.AppliedAtUtc).Should().Equal(time, time, time.AddDays(-1));

            using var emptyResponse = await client.GetAsync($"/api/decisions/{Guid.NewGuid():D}/applications", token);
            emptyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            (await emptyResponse.Content.ReadFromJsonAsync<DecisionApplicationHistoryItemResponse[]>(token))!
                .Should().BeEmpty();

            await using var verificationScope = factory.Services.CreateAsyncScope();
            var verification = verificationScope.ServiceProvider.GetRequiredService<ProjectAtmacaDbContext>();
            (await verification.Set<DecisionApplication>().CountAsync(token)).Should().Be(4);
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ProjectAtmacaDbContext>();
            context.Database.GetDbConnection().Database.Should().Be(databaseName);
            await context.Database.EnsureDeletedAsync(CancellationToken.None);
        }
    }
}
