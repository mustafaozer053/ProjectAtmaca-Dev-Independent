using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Organizations.Catalog;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Organizations;

namespace ProjectAtmaca.Application.Tests.Organizations.Catalog;

public sealed class OrganizationCatalogServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateOrganizationUnderActiveParent()
    {
        Organization parent = Organization.Create("Kulüp", "CL").Value!;
        var repository = new FakeOrganizationRepository([parent]);
        var unitOfWork = new FakeUnitOfWork();
        var service = new OrganizationCatalogService(
            new AllowingAuthorization(),
            repository,
            unitOfWork);

        var result = await service.CreateAsync(
            " Futbol Akademisi ",
            " fa ",
            null,
            parent.Id,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Futbol Akademisi");
        result.Value.Code.Should().Be("FA");
        result.Value.ParentOrganizationId.Should().Be(parent.Id);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMissingParent()
    {
        var repository = new FakeOrganizationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new OrganizationCatalogService(
            new AllowingAuthorization(),
            repository,
            unitOfWork);

        var result = await service.CreateAsync(
            "Akademi",
            "AK",
            null,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ORGANIZATION_PARENT_NOT_FOUND");
        unitOfWork.SaveCalls.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectDuplicateCode()
    {
        var existing = Organization.Create("Kulüp", "CR").Value!;
        var repository = new FakeOrganizationRepository([existing]);
        var unitOfWork = new FakeUnitOfWork();
        var service = new OrganizationCatalogService(
            new AllowingAuthorization(),
            repository,
            unitOfWork);

        var result = await service.CreateAsync(
            "Başka kurum",
            " cr ",
            null,
            null,
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ORGANIZATION_CODE_DUPLICATE");
        unitOfWork.SaveCalls.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectMovingOrganizationUnderItsDescendant()
    {
        Organization parent = Organization.Create("Kulüp", "CR").Value!;
        Organization child = Organization.Create("Akademi", "AK", parent.Id).Value!;
        var repository = new FakeOrganizationRepository([parent, child]);
        var unitOfWork = new FakeUnitOfWork();
        var service = new OrganizationCatalogService(
            new AllowingAuthorization(),
            repository,
            unitOfWork);

        var result = await service.UpdateAsync(
            parent.Id,
            parent.Name,
            parent.Code,
            parent.Description,
            child.Id,
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ORGANIZATION_HIERARCHY_CYCLE");
        unitOfWork.SaveCalls.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateOrganizationDetailsAndParent()
    {
        Organization firstParent = Organization.Create("Kulüp", "CR").Value!;
        Organization secondParent = Organization.Create("Kulüp Akademi", "KA").Value!;
        Organization organization = Organization.Create(
            "Akademi",
            "AK",
            firstParent.Id,
            "Eski açıklama").Value!;
        var repository = new FakeOrganizationRepository(
            [firstParent, secondParent, organization]);
        var unitOfWork = new FakeUnitOfWork();
        var service = new OrganizationCatalogService(
            new AllowingAuthorization(),
            repository,
            unitOfWork);

        var result = await service.UpdateAsync(
            organization.Id,
            " Futbol Akademisi ",
            " fa ",
            " Yeni açıklama ",
            secondParent.Id,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Futbol Akademisi");
        result.Value.Code.Should().Be("FA");
        result.Value.Description.Should().Be("Yeni açıklama");
        result.Value.ParentOrganizationId.Should().Be(secondParent.Id);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldPreserveActiveChildren()
    {
        Organization parent = Organization.Create("Kulüp", "CR").Value!;
        Organization child = Organization.Create("Akademi", "AK", parent.Id).Value!;
        var repository = new FakeOrganizationRepository([parent, child]);
        var unitOfWork = new FakeUnitOfWork();
        var service = new OrganizationCatalogService(
            new AllowingAuthorization(),
            repository,
            unitOfWork);

        var result = await service.ChangeStatusAsync(
            parent.Id,
            false,
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ORGANIZATION_HAS_ACTIVE_CHILDREN");
        parent.IsActive.Should().BeTrue();
        unitOfWork.SaveCalls.Should().Be(0);
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldDeactivateOrganizationWithoutActiveChildren()
    {
        Organization organization = Organization.Create("Kulüp", "CR").Value!;
        var repository = new FakeOrganizationRepository([organization]);
        var unitOfWork = new FakeUnitOfWork();
        var service = new OrganizationCatalogService(
            new AllowingAuthorization(),
            repository,
            unitOfWork);

        var result = await service.ChangeStatusAsync(
            organization.Id,
            false,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        organization.IsActive.Should().BeFalse();
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldNotReactivateUnderInactiveParent()
    {
        Organization parent = Organization.Create("Kulüp", "CR").Value!;
        Organization child = Organization.Create("Akademi", "AK", parent.Id).Value!;
        parent.Deactivate();
        child.Deactivate();
        var repository = new FakeOrganizationRepository([parent, child]);
        var unitOfWork = new FakeUnitOfWork();
        var service = new OrganizationCatalogService(
            new AllowingAuthorization(),
            repository,
            unitOfWork);

        var result = await service.ChangeStatusAsync(
            child.Id,
            true,
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ORGANIZATION_PARENT_NOT_FOUND");
        child.IsActive.Should().BeFalse();
        unitOfWork.SaveCalls.Should().Be(0);
    }

    private sealed class FakeOrganizationRepository(
        IReadOnlyList<Organization>? initial = null) : IOrganizationCatalogRepository
    {
        private readonly List<Organization> _organizations = initial?.ToList() ?? [];

        public Task<Organization?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_organizations.SingleOrDefault(organization => organization.Id == id));

        public Task<IReadOnlyList<Organization>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Organization>>(_organizations.ToList());

        public Task<bool> CodeExistsAsync(
            string code,
            CancellationToken cancellationToken = default,
            Guid? excludingOrganizationId = null) =>
            Task.FromResult(_organizations.Any(organization =>
                organization.Id != excludingOrganizationId &&
                string.Equals(organization.Code, code, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> NameExistsUnderParentAsync(
            string name,
            Guid? parentOrganizationId,
            CancellationToken cancellationToken = default,
            Guid? excludingOrganizationId = null) =>
            Task.FromResult(_organizations.Any(organization =>
                organization.Id != excludingOrganizationId &&
                string.Equals(organization.Name, name, StringComparison.OrdinalIgnoreCase) &&
                organization.ParentOrganizationId == parentOrganizationId));

        public Task AddAsync(
            Organization organization,
            CancellationToken cancellationToken = default)
        {
            _organizations.Add(organization);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class AllowingAuthorization : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }
}
