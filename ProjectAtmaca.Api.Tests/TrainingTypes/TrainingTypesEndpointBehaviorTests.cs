using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using ProjectAtmaca.Api.Tests.Decisions;
using ProjectAtmaca.Api.Tests.Infrastructure;
using ProjectAtmaca.Application;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.TrainingTypes;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Api.Tests.TrainingTypes;

public sealed class TrainingTypesEndpointBehaviorTests
{
    [Fact]
    public async Task Post_Should_CreateTrainingType_AndReturnCreatedLocation()
    {
        InMemoryTrainingTypeRepository repository = new();
        using var factory = CreateFactory(repository);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/training-types",
            new
            {
                code = "tactic",
                name = "Taktik",
                description = "Topla çalışma",
                displayOrder = 2
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid id = await response.Content.ReadFromJsonAsync<Guid>(
            TestContext.Current.CancellationToken);
        response.Headers.Location!.ToString()
            .Should().Be($"/api/training-types/{id:D}");
        repository.Values.Should().ContainSingle(x =>
            x.TrainingTypeId.Value == id &&
            x.Code.Value == "TACTIC" &&
            x.DisplayOrder == 2);
    }

    [Fact]
    public async Task Get_Should_ApplyActiveOnlyFilter_AndProjectItems()
    {
        InMemoryTrainingTypeRepository repository = new();
        TrainingType active = CreateTrainingType("TECHNICAL", "Teknik", 1);
        TrainingType inactive = CreateTrainingType("RECOVERY", "Toparlanma", 2);
        inactive.Deactivate();
        repository.Seed(active, inactive);
        using var factory = CreateFactory(repository);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage activeResponse = await client.GetAsync(
            "/api/training-types?activeOnly=true",
            TestContext.Current.CancellationToken);
        using HttpResponseMessage allResponse = await client.GetAsync(
            "/api/training-types?activeOnly=false",
            TestContext.Current.CancellationToken);

        activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        allResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<TrainingTypeResponse> activeItems =
            await activeResponse.Content.ReadFromJsonAsync<
                List<TrainingTypeResponse>>(
                TestContext.Current.CancellationToken) ?? [];
        List<TrainingTypeResponse> allItems =
            await allResponse.Content.ReadFromJsonAsync<
                List<TrainingTypeResponse>>(
                TestContext.Current.CancellationToken) ?? [];
        activeItems.Should().ContainSingle(x => x.Code == "TECHNICAL");
        allItems.Should().HaveCount(2);
        repository.ObservedActiveOnly.Should().Equal(true, false);
    }

    [Fact]
    public async Task Patch_Should_ChangeStatus_AndReturnNoContent()
    {
        InMemoryTrainingTypeRepository repository = new();
        TrainingType trainingType = CreateTrainingType("TACTIC", "Taktik", 1);
        repository.Seed(trainingType);
        using var factory = CreateFactory(repository);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/training-types/{trainingType.TrainingTypeId.Value:D}/status",
            new { isActive = false },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        trainingType.IsActive.Should().BeFalse();
        repository.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Patch_Should_ReturnNotFound_WhenTrainingTypeDoesNotExist()
    {
        InMemoryTrainingTypeRepository repository = new();
        using var factory = CreateFactory(repository);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/training-types/{Guid.NewGuid():D}/status",
            new { isActive = false },
            TestContext.Current.CancellationToken);

        await AssertProblem(
            response,
            HttpStatusCode.NotFound,
            TrainingTypeApplicationErrors.NotFound.Code);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Patch_Should_ReturnBadRequest_WhenIdIsInvalid(
        string trainingTypeId)
    {
        using var factory =
            CreateFactory(new InMemoryTrainingTypeRepository());
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/training-types/{trainingTypeId}/status",
            new { isActive = false },
            TestContext.Current.CancellationToken);

        await AssertProblem(response, HttpStatusCode.BadRequest, "TrainingType.Id.Invalid");
    }

    [Fact]
    public async Task Post_Should_ReturnBadRequest_WhenDisplayOrderIsNegative()
    {
        using var factory =
            CreateFactory(new InMemoryTrainingTypeRepository());
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/training-types",
            new
            {
                code = "TACTIC",
                name = "Taktik",
                description = "Topla çalışma",
                displayOrder = -1
            },
            TestContext.Current.CancellationToken);

        await AssertProblem(
            response,
            HttpStatusCode.BadRequest,
            "TrainingType.DisplayOrder.Invalid");
    }

    private static WebApplicationFactory<global::Program> CreateFactory(
        InMemoryTrainingTypeRepository repository)
    {
        ProjectAtmacaApiFactory root = new();
        return root.WithWebHostBuilder(
            builder => builder.ConfigureServices(
                services =>
                {
                    DecisionHistoryTestAuthentication.AddTo(services);
                    services.RemoveAll<IActorIdentityResolver>();
                    services.AddSingleton<IActorIdentityResolver>(
                        new Resolver());
                    services.RemoveAll<IActorAuthorizationService>();
                    services.AddSingleton<IActorAuthorizationService>(
                        new AllowingAuthorization());
                    services.RemoveAll<ITrainingTypeRepository>();
                    services.AddSingleton<ITrainingTypeRepository>(repository);
                    services.RemoveAll<IUnitOfWork>();
                    services.AddSingleton<IUnitOfWork>(repository);
                }));
    }

    private static TrainingType CreateTrainingType(
        string code,
        string name,
        int displayOrder)
    {
        return TrainingType.Create(
            TrainingTypeCode.Create(code).Value!,
            TrainingTypeName.Create(name).Value!,
            TrainingTypeDescription.Create("Açıklama").Value!,
            displayOrder).Value!;
    }

    private static async Task AssertProblem(
        HttpResponseMessage response,
        HttpStatusCode status,
        string code)
    {
        string body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(status, body);
        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/problem+json");
        using JsonDocument problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("status").GetInt32()
            .Should().Be((int)status);
        problem.RootElement.GetProperty("code").GetString().Should().Be(code);
    }

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

    private sealed class InMemoryTrainingTypeRepository : ITrainingTypeRepository, IUnitOfWork
    {
        private readonly List<TrainingType> _values = [];

        public IReadOnlyList<TrainingType> Values => _values;

        public List<bool> ObservedActiveOnly { get; } = [];

        public int SaveChangesCalls { get; private set; }

        public Task AddAsync(
            TrainingType trainingType,
            CancellationToken cancellationToken = default)
        {
            _values.Add(trainingType);
            return Task.CompletedTask;
        }

        public Task<TrainingType?> GetByIdAsync(
            TrainingTypeId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.SingleOrDefault(x =>
                x.TrainingTypeId == id));

        public Task<IReadOnlyList<TrainingType>> ListAsync(
            bool activeOnly,
            CancellationToken cancellationToken = default)
        {
            ObservedActiveOnly.Add(activeOnly);
            IReadOnlyList<TrainingType> result = activeOnly
                ? _values.Where(x => x.IsActive).ToList()
                : _values.ToList();
            return Task.FromResult(result);
        }

        public void Seed(params TrainingType[] values)
        {
            _values.AddRange(values);
        }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed record TrainingTypeResponse(
        Guid Id,
        string Code,
        string Name,
        string Description,
        int DisplayOrder,
        bool IsActive);
}
