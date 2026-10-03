using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using ProjectAtmaca.Api.Tests.Decisions;
using ProjectAtmaca.Api.Tests.Infrastructure;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Positions;

namespace ProjectAtmaca.Api.Tests.AtmacaCards;

public sealed class AtmacaCardStatusEndpointTests
{
    [Fact]
    public async Task Patch_ShouldChangeCardStatus_AndReturnNoContent()
    {
        var card = CreateCard();
        var repository = new InMemoryCardRepository(card);
        using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();

        using var response = await client.PatchAsJsonAsync(
            $"/api/atmaca-cards/{card.AtmacaCardId.Value:D}/status",
            new { isActive = false },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        card.IsActive.Should().BeFalse();
        repository.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Patch_ShouldReturnForbidden_WhenPermissionIsDenied()
    {
        var card = CreateCard();
        var repository = new InMemoryCardRepository(card);
        using var factory = CreateFactory(repository, new DenyingAuthorization());
        using var client = factory.CreateClient();

        using var response = await client.PatchAsJsonAsync(
            $"/api/atmaca-cards/{card.AtmacaCardId.Value:D}/status",
            new { isActive = false },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        card.IsActive.Should().BeTrue();
        repository.GetCalls.Should().Be(0);
    }

    [Fact]
    public async Task Patch_ShouldReturnUnauthorized_WhenRequestIsUnauthenticated()
    {
        using var factory = new ProjectAtmacaApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PatchAsJsonAsync(
            $"/api/atmaca-cards/{Guid.NewGuid():D}/status",
            new { isActive = false },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutSportsProfile_ShouldCreateProfile()
    {
        var card = CreateCard();
        var repository = new InMemoryCardRepository(card);
        using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/atmaca-cards/{card.AtmacaCardId.Value:D}/sports-profiles",
            new
            {
                sportName = "Futbol",
                licenseNumber = "LIC-001",
                startedSportOn = new DateOnly(2018, 7, 1),
                clubRegisteredOn = new DateOnly(2020, 8, 15),
                competitionLevel = AthleteCompetitionLevel.Amateur,
                isNationalAthlete = false
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        card.SportsProfiles.Should().ContainSingle()
            .Which.LicenseNumber!.Value.Should().Be("LIC-001");
        repository.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task PutEducation_ShouldSaveSchoolDetails_AndGetEducation()
    {
        var card = CreateCard();
        var repository = new InMemoryCardRepository(card);
        using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();

        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/atmaca-cards/{card.AtmacaCardId.Value:D}/education",
            new
            {
                isCurrentlyStudying = true,
                schoolName = "Atatürk Lisesi",
                schoolGrade = "10. sınıf",
                schoolNumber = "1234"
            },
            TestContext.Current.CancellationToken);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var getResponse = await client.GetAsync(
            $"/api/atmaca-cards/{card.AtmacaCardId.Value:D}/education",
            TestContext.Current.CancellationToken);
        var education = await getResponse.Content.ReadFromJsonAsync<EducationResponseForTest>(
            TestContext.Current.CancellationToken);

        education.Should().NotBeNull();
        education!.IsCurrentlyStudying.Should().BeTrue();
        education.SchoolName.Should().Be("Atatürk Lisesi");
        education.SchoolGrade.Should().Be("10. sınıf");
        education.SchoolNumber.Should().Be("1234");
        repository.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task PutSportsProfile_ShouldPersistSelectedPositions()
    {
        var card = CreateCard();
        var position = Position.Create("Futbol", "ST", "Stoper").Value!;
        var repository = new InMemoryCardRepository(card);
        using var factory = CreateFactory(
            repository,
            positions: new InMemoryPositionRepository([position]));
        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/atmaca-cards/{card.AtmacaCardId.Value:D}/sports-profiles",
            new
            {
                sportName = "Futbol",
                positionIds = new[] { position.Id }
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        card.SportsProfiles.Single().Positions.Should().ContainSingle()
            .Which.Id.Should().Be(position.Id);
    }

    [Fact]
    public async Task PositionCatalog_ShouldCreateListAndDeactivatePositionsBySport()
    {
        var cardRepository = new InMemoryCardRepository(null);
        var positionRepository = new InMemoryPositionRepository();
        using var factory = CreateFactory(cardRepository, positions: positionRepository);
        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            "/api/positions",
            new { sportName = "Futbol", code = "GK", name = "Kaleci" },
            TestContext.Current.CancellationToken);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<PositionResponseForTest>(
            TestContext.Current.CancellationToken);
        created.Should().NotBeNull();
        created!.IsActive.Should().BeTrue();

        using var listResponse = await client.GetAsync(
            "/api/positions?sportName=Futbol",
            TestContext.Current.CancellationToken);
        var positions = await listResponse.Content.ReadFromJsonAsync<List<PositionResponseForTest>>(
            TestContext.Current.CancellationToken);
        positions.Should().ContainSingle().Which.Name.Should().Be("Kaleci");

        using var deactivateResponse = await client.PatchAsJsonAsync(
            $"/api/positions/{created.Id:D}/status",
            new { isActive = false },
            TestContext.Current.CancellationToken);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var activeListResponse = await client.GetAsync(
            "/api/positions?sportName=Futbol&activeOnly=true",
            TestContext.Current.CancellationToken);
        var activePositions = await activeListResponse.Content.ReadFromJsonAsync<List<PositionResponseForTest>>(
            TestContext.Current.CancellationToken);
        activePositions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSportsProfiles_ShouldReturnForbidden_WhenPermissionIsDenied()
    {
        var card = CreateCard();
        var repository = new InMemoryCardRepository(card);
        using var factory = CreateFactory(repository, new DenyingAuthorization());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/atmaca-cards/{card.AtmacaCardId.Value:D}/sports-profiles",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        repository.GetCalls.Should().Be(0);
    }

    [Fact]
    public async Task PostMeasurement_ShouldAppendToCardHistory()
    {
        var card = CreateCard();
        var repository = new InMemoryCardRepository(card);
        using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/atmaca-cards/{card.AtmacaCardId.Value:D}/measurements",
            new
            {
                measuredOn = new DateOnly(2026, 9, 30),
                heightCentimeters = 160.25m,
                weightKilograms = 50.5m
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        card.Measurements.Should().ContainSingle()
            .Which.HeightCentimeters.Should().Be(160.25m);
        repository.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetMeasurements_ShouldReturnForbidden_WhenPermissionIsDenied()
    {
        var card = CreateCard();
        var repository = new InMemoryCardRepository(card);
        using var factory = CreateFactory(repository, new DenyingAuthorization());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/atmaca-cards/{card.AtmacaCardId.Value:D}/measurements",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        repository.GetCalls.Should().Be(0);
    }

    private static WebApplicationFactory<global::Program> CreateFactory(
        InMemoryCardRepository repository,
        IActorAuthorizationService? authorization = null,
        IPositionRepository? positions = null)
    {
        var root = new ProjectAtmacaApiFactory();
        return root.WithWebHostBuilder(
            builder => builder.ConfigureServices(
                services =>
                {
                    DecisionHistoryTestAuthentication.AddTo(services);
                    services.RemoveAll<IActorIdentityResolver>();
                    services.AddSingleton<IActorIdentityResolver>(
                        new Resolver());
                    services.RemoveAll<IActorAuthorizationService>();
                    services.AddSingleton(
                        authorization ?? new AllowingAuthorization());
                    services.RemoveAll<IAtmacaCardRepository>();
                    services.AddSingleton<IAtmacaCardRepository>(repository);
                    services.RemoveAll<IPositionRepository>();
                    services.AddSingleton<IPositionRepository>(
                        positions ?? new InMemoryPositionRepository());
                    services.RemoveAll<IUnitOfWork>();
                    services.AddSingleton<IUnitOfWork>(repository);
                }));
    }

    private static AtmacaCard CreateCard() =>
        AtmacaCard.Issue(
            Guid.NewGuid(),
            AtmacaCardNumber.Create("ATM-000001").Value!,
            DateTime.UtcNow).Value!;

    private sealed record PositionResponseForTest(
        Guid Id,
        string SportName,
        string Code,
        string Name,
        bool IsActive);

    private sealed record EducationResponseForTest(
        bool IsCurrentlyStudying,
        string? SchoolName,
        string? SchoolGrade,
        string? SchoolNumber);

    private sealed class Resolver : IActorIdentityResolver
    {
        public Task<Result<ActorId>> ResolveAsync(
            ExternalIdentity externalIdentity,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<ActorId>.Success(ActorId.New()));
    }

    private sealed class AllowingAuthorization : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class DenyingAuthorization : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Failure(ActorAuthorizationErrors.Forbidden));
    }

    private sealed class InMemoryCardRepository(AtmacaCard? card)
        : IAtmacaCardRepository, IUnitOfWork
    {
        public int GetCalls { get; private set; }
        public int SaveCalls { get; private set; }

        public Task<AtmacaCard?> GetByIdAsync(
            AtmacaCardId id,
            CancellationToken cancellationToken = default)
        {
            GetCalls++;
            return Task.FromResult(card?.AtmacaCardId == id ? card : null);
        }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }

    }

    private sealed class InMemoryPositionRepository(IReadOnlyList<Position>? initial = null)
        : IPositionRepository
    {
        private readonly List<Position> _positions = initial?.ToList() ?? [];

        public Task<Position?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_positions.SingleOrDefault(position => position.Id == id));

        public Task<IReadOnlyList<Position>> ListBySportAsync(
            string sportName,
            bool activeOnly,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Position>>(_positions
                .Where(position => string.Equals(
                    position.SportName, sportName, StringComparison.OrdinalIgnoreCase))
                .Where(position => !activeOnly || position.IsActive)
                .ToList());

        public Task<bool> ExistsBySportAndCodeAsync(
            string sportName,
            string code,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_positions.Any(position =>
                string.Equals(position.SportName, sportName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(position.Code, code, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Position position, CancellationToken cancellationToken = default) =>
            AddAsyncCore(position);

        private Task AddAsyncCore(Position position)
        {
            _positions.Add(position);
            return Task.CompletedTask;
        }
    }
}
