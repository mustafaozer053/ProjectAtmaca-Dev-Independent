using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ProjectAtmaca.Api.Participations.ListHistoryByAtmacaCard;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using ProjectAtmaca.Infrastructure.Persistence;
using ProjectAtmaca.Infrastructure.Persistence.Readers;
using ProjectAtmaca.Infrastructure.Persistence.Security;

namespace ProjectAtmaca.Api.Tests.Participations;

public sealed class ListParticipationHistoryByAtmacaCardEndpointProductionCompositionTests
{
    private const string TestAuthenticationScheme = "ListHistorySqlRoundTrip";
    internal const string ExternalSubject = "list-history-sql-round-trip-subject";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Get_Should_ContinueSqlBackedHistory_WhenReturnedNextCursorIsRoundTripped(bool sameTimestamp)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databaseName = $"ProjectAtmaca_Api_ListHistory_{Guid.NewGuid():N}";
        string connectionString =
            "Server=(localdb)\\mssqllocaldb;" +
            $"Database={databaseName};" +
            "Trusted_Connection=True;TrustServerCertificate=True";

        ActorId actorId = ActorId.New();
        AtmacaCardId targetCardId = AtmacaCardId.New();
        Participation newest = CreateParticipation(targetCardId);
        Participation oldest = CreateParticipation(targetCardId);
        Participation excluded = CreateParticipation(AtmacaCardId.New());
        DateTime newestCreatedAtUtc =
            new DateTime(2026, 9, 13, 10, 0, 0, DateTimeKind.Utc).AddTicks(1234567);

        using ProjectAtmacaApiFactory rootFactory = new();
        using WebApplicationFactory<global::Program> factory =
            rootFactory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.ReplaceProjectAtmacaDatabase(connectionString);
                    services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = TestAuthenticationScheme;
                            options.DefaultChallengeScheme = TestAuthenticationScheme;
                            options.DefaultForbidScheme = TestAuthenticationScheme;
                        })
                        .AddScheme<AuthenticationSchemeOptions,
                            ListHistorySqlRoundTripAuthenticationHandler>(
                            TestAuthenticationScheme, _ => { });
                }));

        try
        {
            await using (AsyncServiceScope setupScope = factory.Services.CreateAsyncScope())
            {
                ProjectAtmacaDbContext context = setupScope.ServiceProvider
                    .GetRequiredService<ProjectAtmacaDbContext>();
                context.Database.GetDbConnection().Database.Should().Be(databaseName);
                context.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.SqlServer");
                setupScope.ServiceProvider.GetRequiredService<IParticipationReader>()
                    .Should().BeOfType<ParticipationReader>();
                await context.Database.MigrateAsync(cancellationToken);
            }

            // Seed fixed historical timestamps without the write-time audit interceptor.
            // HTTP requests still use the production DbContext registration and reader.
            await using (ProjectAtmacaDbContext seedContext = new(
                new DbContextOptionsBuilder<ProjectAtmacaDbContext>()
                    .UseSqlServer(connectionString).Options))
            {
                seedContext.Set<ActorIdentityMapping>().Add(
                    ActorIdentityMapping.Create(
                        ExternalIdentity.Create(
                            ProjectAtmacaApiFactory.AuthenticationAuthority, ExternalSubject),
                        actorId));
                seedContext.Set<ActorPermissionGrant>().Add(
                    ActorPermissionGrant.Create(
                        actorId, Permissions.Participations.ListHistoryByAtmacaCard));
                if (sameTimestamp)
                {
                    // These IDs sort oppositely under SQL Server and Guid.CompareTo.
                    // The final six bytes take precedence in SQL Server ordering.
                    seedContext.Entry(newest).Property(item => item.Id).CurrentValue =
                        Guid.Parse("00000000-0000-0000-0000-000000000002");
                    seedContext.Entry(oldest).Property(item => item.Id).CurrentValue =
                        Guid.Parse("ffffffff-ffff-ffff-ffff-000000000001");
                }
                seedContext.Participations.AddRange(oldest, excluded, newest);
                seedContext.Entry(newest).Property(item => item.CreatedAtUtc)
                    .CurrentValue = newestCreatedAtUtc;
                seedContext.Entry(oldest).Property(item => item.CreatedAtUtc)
                    .CurrentValue = sameTimestamp
                        ? newestCreatedAtUtc
                        : newestCreatedAtUtc.AddHours(-1);
                seedContext.Entry(excluded).Property(item => item.CreatedAtUtc)
                    .CurrentValue = newestCreatedAtUtc.AddHours(1);
                await seedContext.SaveChangesAsync(cancellationToken);
            }

            using HttpClient client = factory.CreateClient(
                new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            string firstUri =
                $"/api/participations/history?atmacaCardId={targetCardId.Value:D}&pageSize=1";
            using HttpResponseMessage firstResponse =
                await client.GetAsync(firstUri, cancellationToken);
            firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            firstResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
            ParticipationHistoryPageResponse firstPage =
                (await firstResponse.Content.ReadFromJsonAsync<ParticipationHistoryPageResponse>(
                    cancellationToken))!;
            firstPage.Should().NotBeNull();
            firstPage.Items.Select(item => item.ParticipationId)
                .Should().Equal(newest.ParticipationId.Value);
            firstPage.NextCursor.Should().NotBeNull();
            ParticipationHistoryCursorResponse cursor = firstPage.NextCursor!;
            cursor.AtmacaCardId.Should().Be(targetCardId.Value);
            cursor.ParticipationId.Should().Be(newest.ParticipationId.Value);
            cursor.CreatedAtUtc.Should().Be(newestCreatedAtUtc);

            // Use the documented O query format, preserving the returned Kind and ticks.
            // Do not repair the response with SpecifyKind, ToUniversalTime or a Z suffix.
            string encodedCreatedAtUtc = Uri.EscapeDataString(
                cursor.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
            string continuationUri = firstUri +
                $"&cursorAtmacaCardId={cursor.AtmacaCardId:D}" +
                $"&cursorCreatedAtUtc={encodedCreatedAtUtc}" +
                $"&cursorParticipationId={cursor.ParticipationId:D}";
            using HttpResponseMessage continuationResponse =
                await client.GetAsync(continuationUri, cancellationToken);
            string continuationBody =
                await continuationResponse.Content.ReadAsStringAsync(cancellationToken);

            continuationResponse.StatusCode.Should().Be(HttpStatusCode.OK,
                "the SQL-backed response cursor must be reusable; returned timestamp {0:O}, Kind {1}; response {2}",
                cursor.CreatedAtUtc, cursor.CreatedAtUtc.Kind, continuationBody);
            cursor.CreatedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
            ParticipationHistoryPageResponse secondPage =
                (await continuationResponse.Content.ReadFromJsonAsync<ParticipationHistoryPageResponse>(
                    cancellationToken))!;
            secondPage.Should().NotBeNull();
            secondPage.Items.Select(item => item.ParticipationId)
                .Should().Equal(oldest.ParticipationId.Value);
            secondPage.NextCursor.Should().BeNull();
            firstPage.Items.Concat(secondPage.Items).Select(item => item.ParticipationId)
                .Should().Equal(newest.ParticipationId.Value, oldest.ParticipationId.Value);
        }
        finally
        {
            await using AsyncServiceScope cleanupScope = factory.Services.CreateAsyncScope();
            ProjectAtmacaDbContext context = cleanupScope.ServiceProvider
                .GetRequiredService<ProjectAtmacaDbContext>();
            context.Database.GetDbConnection().Database.Should().Be(databaseName);
            await context.Database.EnsureDeletedAsync(CancellationToken.None);
        }
    }

    private static Participation CreateParticipation(AtmacaCardId cardId) =>
        Participation.Create(
            ActivityReference.ForTraining(TrainingId.New()), cardId).Value!;
}

public sealed class ListHistorySqlRoundTripAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public ListHistorySqlRoundTripAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        ClaimsIdentity identity = new(
            [
                new Claim("iss", ProjectAtmacaApiFactory.AuthenticationAuthority),
                new Claim("sub",
                    ListParticipationHistoryByAtmacaCardEndpointProductionCompositionTests.ExternalSubject)
            ], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
