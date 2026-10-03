using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Organizations.Assignments;
using ProjectAtmaca.Domain.Assignments;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Application.Tests.Organizations.Assignments;

public sealed class OrganizationDutyAssignmentCommandHandlerTests
{
    [Fact]
    public async Task Add_ShouldPersistSeasonIndependentDuty()
    {
        Guid organizationId = Guid.NewGuid();
        Guid cardId = Guid.NewGuid();
        var repository = new FakeRepository(organizationId, cardId);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AddOrganizationDutyAssignmentCommandHandler(
            new AllowingAuthorization(),
            repository,
            unitOfWork);

        var result = await handler.Handle(
            new AddOrganizationDutyAssignmentCommand(
                organizationId,
                cardId,
                "Başkan",
                new DateTime(2024, 6, 1),
                null),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Başkan");
        result.Value.EndDate.Should().BeNull();
        repository.Values.Should().ContainSingle();
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Add_ShouldRejectOverlappingDuplicateDuty()
    {
        Guid organizationId = Guid.NewGuid();
        Guid cardId = Guid.NewGuid();
        var repository = new FakeRepository(organizationId, cardId);
        repository.Values.Add(OrganizationDutyAssignment.Create(
            cardId,
            organizationId,
            AssignmentTitle.Create("Başkan").Value!,
            new DateTime(2024, 1, 1),
            null).Value!);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AddOrganizationDutyAssignmentCommandHandler(
            new AllowingAuthorization(),
            repository,
            unitOfWork);

        var result = await handler.Handle(
            new AddOrganizationDutyAssignmentCommand(
                organizationId,
                cardId,
                " başkan ",
                new DateTime(2025, 1, 1),
                null),
            TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("ORGANIZATION_DUTY_DUPLICATE");
        repository.Values.Should().ContainSingle();
        unitOfWork.SaveCalls.Should().Be(0);
    }

    [Fact]
    public async Task Add_ShouldRejectDutyForInactiveOrganization()
    {
        Guid organizationId = Guid.NewGuid();
        Guid cardId = Guid.NewGuid();
        var repository = new FakeRepository(organizationId, cardId)
        {
            OrganizationActive = false
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AddOrganizationDutyAssignmentCommandHandler(
            new AllowingAuthorization(),
            repository,
            unitOfWork);

        var result = await handler.Handle(
            new AddOrganizationDutyAssignmentCommand(
                organizationId,
                cardId,
                "Başkan",
                new DateTime(2024, 6, 1),
                null),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ORGANIZATION_INACTIVE");
        repository.Values.Should().BeEmpty();
        unitOfWork.SaveCalls.Should().Be(0);
    }

    private sealed class FakeRepository(Guid organizationId, Guid cardId)
        : IOrganizationDutyAssignmentRepository
    {
        public List<OrganizationDutyAssignment> Values { get; } = [];
        public bool OrganizationActive { get; set; } = true;

        public Task<bool> OrganizationExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == organizationId);

        public Task<bool> IsOrganizationActiveAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == organizationId && OrganizationActive);

        public Task<bool> AtmacaCardExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == cardId);

        public Task<IReadOnlyList<OrganizationDutyAssignment>> ListAsync(
            Guid orgId,
            Guid atmacaCardId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationDutyAssignment>>(
                Values.Where(value =>
                    value.OrganizationId == orgId &&
                    value.AtmacaCardId == atmacaCardId).ToList());

        public Task<OrganizationDutyAssignment?> GetByIdAsync(
            Guid orgId,
            Guid atmacaCardId,
            Guid assignmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Values.SingleOrDefault(value =>
                value.OrganizationId == orgId &&
                value.AtmacaCardId == atmacaCardId &&
                value.Id == assignmentId));

        public Task AddAsync(
            OrganizationDutyAssignment assignment,
            CancellationToken cancellationToken = default)
        {
            Values.Add(assignment);
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
