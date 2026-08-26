using FluentAssertions;
using ProjectAtmaca.Application.Participations;
using ProjectAtmaca.Application.Participations.GetById;
using ProjectAtmaca.Application.Participations.GetSummaryByActivity;
using ProjectAtmaca.Application.Participations.ListByActivity;
using ProjectAtmaca.Application.Participations.ListHistoryByAtmacaCard;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;

namespace ProjectAtmaca.Application.Tests
    .Participations.GetSummaryByActivity;

public sealed class GetParticipationSummaryByActivityQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnSummary_WhenReaderReturnsSummary()
    {
        // Arrange
        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        ParticipationActivitySummary expected =
            new(
                Total: 6,
                NotRecorded: 1,
                Present: 4,
                Absent: 1);

        var reader =
            new FakeParticipationReader(
                expected);

        var handler =
            new GetParticipationSummaryByActivityQueryHandler(
                reader);

        var query =
            new GetParticipationSummaryByActivityQuery(
                activityReference);

        // Act
        Result<ParticipationActivitySummary> result =
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
    public async Task Handle_Should_ReturnZeroSummary_WhenReaderReturnsZeroSummary()
    {
        // Arrange
        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        ParticipationActivitySummary zeroSummary =
            new(
                Total: 0,
                NotRecorded: 0,
                Present: 0,
                Absent: 0);

        var reader =
            new FakeParticipationReader(
                zeroSummary);

        var handler =
            new GetParticipationSummaryByActivityQueryHandler(
                reader);

        var query =
            new GetParticipationSummaryByActivityQuery(
                activityReference);

        // Act
        Result<ParticipationActivitySummary> result =
            await handler.Handle(
                query,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        result.Value
            .Should()
            .Be(zeroSummary);

        result.Value.Total
            .Should()
            .Be(0);

        result.Value.NotRecorded
            .Should()
            .Be(0);

        result.Value.Present
            .Should()
            .Be(0);

        result.Value.Absent
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task Handle_Should_PropagateCancellationToken_ToReader()
    {
        // Arrange
        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        ParticipationActivitySummary summary =
            new(
                Total: 0,
                NotRecorded: 0,
                Present: 0,
                Absent: 0);

        var reader =
            new CapturingParticipationReader(
                summary);

        var handler =
            new GetParticipationSummaryByActivityQueryHandler(
                reader);

        var query =
            new GetParticipationSummaryByActivityQuery(
                activityReference);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        CancellationToken cancellationToken =
            cancellationTokenSource.Token;

        // Act
        Result<ParticipationActivitySummary> result =
            await handler.Handle(
                query,
                cancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        reader.CapturedCancellationToken
            .Should()
            .Be(cancellationToken);
    }

    private sealed class FakeParticipationReader
        : IParticipationReader
    {
        private readonly ParticipationActivitySummary
            _summary;

        public FakeParticipationReader(
            ParticipationActivitySummary summary)
        {
            _summary =
                summary;
        }

        public Task<ParticipationDetails?> GetByIdAsync(
            Guid participationId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<ParticipationListItem>>
            ListByActivityAsync(
                ActivityReference activityReference,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ParticipationActivitySummary>
            GetSummaryByActivityAsync(
                ActivityReference activityReference,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _summary);
        }

        public Task<ParticipationHistoryPage>
            ListHistoryByAtmacaCardAsync(
                AtmacaCardId atmacaCardId,
                int pageSize,
                ParticipationHistoryCursor? cursor,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class CapturingParticipationReader
    : IParticipationReader
    {
        private readonly ParticipationActivitySummary
            _summary;

        public CapturingParticipationReader(
            ParticipationActivitySummary summary)
        {
            _summary =
                summary;
        }

        public CancellationToken CapturedCancellationToken
        {
            get;
            private set;
        }

        public Task<ParticipationDetails?> GetByIdAsync(
            Guid participationId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<ParticipationListItem>>
            ListByActivityAsync(
                ActivityReference activityReference,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ParticipationActivitySummary>
            GetSummaryByActivityAsync(
                ActivityReference activityReference,
                CancellationToken cancellationToken = default)
        {
            CapturedCancellationToken =
                cancellationToken;

            return Task.FromResult(
                _summary);
        }

        public Task<ParticipationHistoryPage>
            ListHistoryByAtmacaCardAsync(
                AtmacaCardId atmacaCardId,
                int pageSize,
                ParticipationHistoryCursor? cursor,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
