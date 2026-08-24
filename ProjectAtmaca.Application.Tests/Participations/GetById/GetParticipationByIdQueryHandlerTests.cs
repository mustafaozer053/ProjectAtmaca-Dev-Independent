using FluentAssertions;
using ProjectAtmaca.Application.Participations.ListByActivity;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Domain.Common;
using Xunit;
using ProjectAtmaca.Application.Participations;

namespace ProjectAtmaca.Application.Tests
    .Participations.GetById;

public sealed class GetParticipationByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenParticipationDoesNotExist()
    {
        // Arrange
        var reader =
            new FakeParticipationReader();

        var handler =
            new GetParticipationByIdQueryHandler(
                reader);

        Guid participationId =
            Guid.NewGuid();

        var query =
            new GetParticipationByIdQuery(
                participationId);

        // Act
        Result<ParticipationDetails> result =
            await handler.Handle(
                query,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        result.Error
            .Should()
            .BeSameAs(
                GetParticipationByIdErrors.NotFound);
    }
    [Fact]
    public async Task Handle_Should_ReturnParticipation_WhenParticipationExists()
    {
        // Arrange
        Guid participationId =
            Guid.NewGuid();

        Guid atmacaCardId =
            Guid.NewGuid();

        Guid activityId =
            Guid.NewGuid();

        ParticipationDetails expected =
            new(
                participationId,
                atmacaCardId,
                ActivityTypeCode.TrainingCode,
                activityId,
                ParticipationStatus.Present,
                "LATE",
                DateTimeOffset.UtcNow.AddMinutes(-30),
                null,
                "Arrived late.");

        var reader =
            new FakeParticipationReader(
                expected);

        var handler =
            new GetParticipationByIdQueryHandler(
                reader);

        var query =
            new GetParticipationByIdQuery(
                participationId);

        // Act
        Result<ParticipationDetails> result =
            await handler.Handle(
                query,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        result.Value
            .Should()
            .Be(expected);
    }
    [Fact]
    public async Task Handle_Should_PropagateCancellationToken_ToReader()
    {
        // Arrange
        using var cancellationTokenSource =
            new CancellationTokenSource();

        CancellationToken cancellationToken =
            cancellationTokenSource.Token;

        var reader =
            new CapturingParticipationReader();

        var handler =
            new GetParticipationByIdQueryHandler(
                reader);

        var query =
            new GetParticipationByIdQuery(
                Guid.NewGuid());

        // Act
        await handler.Handle(
            query,
            cancellationToken);

        // Assert
        reader.CapturedCancellationToken
            .Should()
            .Be(cancellationToken);
    }
    private sealed class FakeParticipationReader
    : IParticipationReader
    {
        private readonly ParticipationDetails? _participation;

        public FakeParticipationReader(
            ParticipationDetails? participation = null)
        {
            _participation = participation;
        }

        public Task<ParticipationDetails?> GetByIdAsync(
            Guid participationId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _participation);
        }
        public Task<IReadOnlyList<ParticipationListItem>>
            ListByActivityAsync(
                ActivityReference activityReference,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
    private sealed class CapturingParticipationReader
    : IParticipationReader
    {
        public CancellationToken CapturedCancellationToken
        {
            get;
            private set;
        }

        public Task<ParticipationDetails?> GetByIdAsync(
            Guid participationId,
            CancellationToken cancellationToken = default)
        {
            CapturedCancellationToken =
                cancellationToken;

            return Task.FromResult<ParticipationDetails?>(
                null);
        }
        public Task<IReadOnlyList<ParticipationListItem>>
            ListByActivityAsync(
                ActivityReference activityReference,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}