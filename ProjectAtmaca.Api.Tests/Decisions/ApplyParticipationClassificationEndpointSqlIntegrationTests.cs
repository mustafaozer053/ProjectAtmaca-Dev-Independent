using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Decisions.ApplyParticipationClassification;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Decisions;
using ProjectAtmaca.Domain.Decisions.Effects.Participations;
using ProjectAtmaca.Domain.Decisions.Snapshots.Participations;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Decisions;
using ProjectAtmaca.Infrastructure.Persistence.Security;

namespace ProjectAtmaca.Api.Tests.Decisions;

public sealed class ApplyParticipationClassificationEndpointSqlIntegrationTests
{
    [Theory]
    [InlineData("present", 204, null)]
    [InlineData("absent", 204, null)]
    [InlineData("denied", 403, "Security.Authorization.Forbidden")]
    [InlineData("domain-reject", 409, "Participation.Classification.CorrectionRequired")]
    [InlineData("revision-mismatch", 409, "Decision.RevisionMismatch")]
    [InlineData("decision-missing", 404, "Decision.NotFound")]
    [InlineData("target-missing", 404, "Participation.NotFound")]
    [InlineData("superseded", 409, "Decision.Superseded")]
    [InlineData("authority-revised", 409, "Decision.AuthorityLost")]
    [InlineData("authority-superseded", 409, "Decision.AuthorityLost")]
    public async Task Post_Should_PreserveProductionSqlApplicationContract(
        string scenario, int expectedStatus, string? expectedCode)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string databaseName = $"ProjectAtmaca_Api_ApplyDecision_{Guid.NewGuid():N}";
        string connection = $"Server=(localdb)\\mssqllocaldb;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";
        ActorId actor = ActorId.New();
        Participation participation = Participation.Create(
            ActivityReference.ForTraining(TrainingId.New()), AtmacaCardId.New()).Value!;
        if (scenario == "domain-reject") participation.MarkAbsent().IsSuccess.Should().BeTrue();
        ParticipationStatus initialStatus = participation.Status;
        var snapshot = ParticipationClassificationSnapshot.Create(
            participation.ActivityReference, participation.AtmacaCardId, participation.Status,
            participation.Condition, participation.JoinedAt, participation.LeftAt);
        Decision decision = Decision.CreateParticipationClassification(
            participation.ParticipationId, snapshot,
            scenario == "absent" ? ParticipationClassificationEffect.Absent() : ParticipationClassificationEffect.Present());
        if (scenario == "superseded") decision.SupersedeBy(DecisionId.New());
        Guid operationId = Guid.NewGuid();
        DateTimeOffset appliedAt = new DateTimeOffset(2026, 9, 13, 10, 0, 0, TimeSpan.Zero).AddTicks(1234567);
        using var root = new ProjectAtmacaApiFactory();
        using var factory = root.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.ReplaceProjectAtmacaDatabase(connection);
            DecisionHistoryTestAuthentication.AddTo(services);
            if (scenario.StartsWith("authority-", StringComparison.Ordinal))
            {
                // Interleave a durable authority update after handler preflight,
                // then execute the real SQL committer on the request's DbContext.
                services.RemoveAll<IDecisionAuthorityCommitter>();
                services.AddScoped<IDecisionAuthorityCommitter>(provider =>
                    new AuthorityChangingCommitter(
                        new DecisionAuthorityCommitter(provider.GetRequiredService<ProjectAtmacaDbContext>()),
                        connection, scenario));
            }
        }));
        try
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ProjectAtmacaDbContext>();
                context.Database.GetDbConnection().Database.Should().Be(databaseName);
                context.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.SqlServer");
                await context.Database.MigrateAsync(token);
            }
            // Seed data outside a request; request writes retain the production audit interceptor.
            await using (var seed = new ProjectAtmacaDbContext(
                new DbContextOptionsBuilder<ProjectAtmacaDbContext>().UseSqlServer(connection).Options))
            {
                seed.Set<ActorIdentityMapping>().Add(ActorIdentityMapping.Create(
                    ExternalIdentity.Create(ProjectAtmacaApiFactory.AuthenticationAuthority,
                        DecisionHistoryTestAuthentication.Subject), actor));
                if (scenario != "denied") seed.Set<ActorPermissionGrant>().Add(
                    ActorPermissionGrant.Create(actor, Permissions.Decisions.ApplyParticipationClassification));
                if (scenario != "target-missing") seed.Participations.Add(participation);
                if (scenario != "decision-missing") seed.Set<Decision>().Add(decision);
                await seed.SaveChangesAsync(token);
            }

            using var client = factory.CreateClient();
            string uri = $"/api/decisions/{decision.DecisionId.Value:D}/apply-participation-classification";
            var request = new
            {
                operationId,
                decisionRevision = scenario == "revision-mismatch" ? decision.Revision.Value + 1 : decision.Revision.Value,
                appliedAtUtc = appliedAt
            };
            using var response = await client.PostAsJsonAsync(uri, request, token);
            string body = await response.Content.ReadAsStringAsync(token);
            ((int)response.StatusCode).Should().Be(expectedStatus, "response: {0}", body);
            if (expectedCode is not null)
            {
                response.Content.Headers.ContentType.Should().NotBeNull();
                response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
                using var json = JsonDocument.Parse(body);
                json.RootElement.GetProperty("code").GetString().Should().Be(expectedCode);
                json.RootElement.GetProperty("status").GetInt32().Should().Be(expectedStatus);
            }
            else
            {
                body.Should().BeEmpty();
                using var replay = await client.PostAsJsonAsync(uri, request, token);
                replay.StatusCode.Should().Be(HttpStatusCode.NoContent);
                (await replay.Content.ReadAsStringAsync(token)).Should().BeEmpty();
                using var conflict = await client.PostAsJsonAsync(uri, new
                {
                    operationId, decisionRevision = decision.Revision.Value, appliedAtUtc = appliedAt.AddSeconds(1)
                }, token);
                conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
                using var conflictJson = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync(token));
                conflictJson.RootElement.GetProperty("code").GetString()
                    .Should().Be("DecisionApplication.OperationConflict");
            }

            await using var verificationScope = factory.Services.CreateAsyncScope();
            var verification = verificationScope.ServiceProvider.GetRequiredService<ProjectAtmacaDbContext>();
            var applications = await verification.Set<DecisionApplication>().AsNoTracking().ToListAsync(token);
            var operations = await verification.Set<DecisionApplicationOperation>().AsNoTracking().ToListAsync(token);
            applications.Should().HaveCount(expectedCode is null ? 1 : 0);
            operations.Should().HaveCount(expectedCode is null ? 1 : 0);
            if (scenario.StartsWith("authority-", StringComparison.Ordinal))
            {
                var authoritative = await verification.Set<Decision>().AsNoTracking().SingleAsync(token);
                if (scenario == "authority-revised")
                    authoritative.Revision.Value.Should().Be(decision.Revision.Value + 1);
                else
                    authoritative.SupersededByDecisionId.Should().NotBeNull();
            }
            if (scenario != "target-missing")
            {
                var persisted = await verification.Participations.AsNoTracking().SingleAsync(token);
                persisted.Status.Should().Be(expectedCode is not null ? initialStatus :
                    scenario == "absent" ? ParticipationStatus.Absent : ParticipationStatus.Present);
                if (expectedCode is null) persisted.LastModifiedByActorId.Should().Be(actor);
                else persisted.LastModifiedByActorId.Should().BeNull();
            }
            if (expectedCode is null)
            {
                applications[0].DecisionId.Should().Be(decision.DecisionId);
                applications[0].Target.Should().Be(decision.Target);
                applications[0].AppliedDecisionRevision.Should().Be(decision.Revision);
                applications[0].AppliedAtUtc.Should().Be(appliedAt);
                operations[0].OperationId.Value.Should().Be(operationId);
                operations[0].DecisionId.Should().Be(decision.DecisionId);
                operations[0].DecisionRevision.Should().Be(decision.Revision);
                operations[0].AppliedAtUtc.Should().Be(appliedAt);
            }
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ProjectAtmacaDbContext>();
            context.Database.GetDbConnection().Database.Should().Be(databaseName);
            await context.Database.EnsureDeletedAsync(CancellationToken.None);
        }
    }

    private sealed class AuthorityChangingCommitter(
        DecisionAuthorityCommitter inner, string connection, string scenario) : IDecisionAuthorityCommitter
    {
        public async Task<DecisionAuthorityCommitOutcome> CommitAsync(
            DecisionApplicationOperationId operationId, DecisionId decisionId,
            DecisionRevision expectedRevision, CancellationToken cancellationToken = default)
        {
            await using (var competingContext = new ProjectAtmacaDbContext(
                new DbContextOptionsBuilder<ProjectAtmacaDbContext>().UseSqlServer(connection).Options))
            {
                var authoritative = await competingContext.Set<Decision>()
                    .SingleAsync(item => item.Id == decisionId.Value, cancellationToken);
                authoritative.Revision.Should().Be(expectedRevision);
                if (scenario == "authority-revised")
                    authoritative.ReEvaluate(authoritative.Snapshot, ParticipationClassificationEffect.Absent());
                else
                    authoritative.SupersedeBy(DecisionId.New());
                await competingContext.SaveChangesAsync(cancellationToken);
            }

            return await inner.CommitAsync(operationId, decisionId, expectedRevision, cancellationToken);
        }
    }
}
