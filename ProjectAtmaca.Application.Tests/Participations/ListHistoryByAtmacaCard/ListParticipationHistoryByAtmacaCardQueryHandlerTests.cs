using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Security;
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
    .Participations.ListHistoryByAtmacaCard;

public sealed class ListParticipationHistoryByAtmacaCardQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnPage_WhenQueryIsValid()
    {
        // Arrange
        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        DateTime createdAtUtc =
            new(
                2026,
                8,
                24,
                10,
                30,
                0,
                DateTimeKind.Utc);

        ParticipationHistoryCursor nextCursor =
            new(
                atmacaCardId,
                createdAtUtc,
                Guid.NewGuid());

        IReadOnlyList<ParticipationHistoryItem> items =
            new[]
            {
                new ParticipationHistoryItem(
                    Guid.NewGuid(),
                    ActivityReference.ForTraining(
                        TrainingId.New()),
                    ParticipationStatus.Present,
                    ParticipationCondition.Late.Code,
                    new DateTimeOffset(
                        2026,
                        8,
                        24,
                        10,
                        0,
                        0,
                        TimeSpan.Zero),
                    null,
                    createdAtUtc)
            };

        ParticipationHistoryPage expected =
            new(
                items,
                nextCursor);

        var reader =
            new FakeParticipationReader(
                expected);

        var handler =
            new ListParticipationHistoryByAtmacaCardQueryHandler(
                new GrantedActorAuthorizationService(),
                reader);

        var query =
            new ListParticipationHistoryByAtmacaCardQuery(
                atmacaCardId,
                PageSize: 25,
                Cursor: null);

        // Act
        Result<ParticipationHistoryPage> result =
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
    public async Task Handle_Should_ReturnFailure_AndNotCallReader_WhenPageSizeIsZero()
    {
        // Arrange
        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        var reader =
            new CapturingParticipationReader();

        var handler =
            new ListParticipationHistoryByAtmacaCardQueryHandler(
                new GrantedActorAuthorizationService(),
                reader);

        var query =
            new ListParticipationHistoryByAtmacaCardQuery(
                atmacaCardId,
                PageSize: 0,
                Cursor: null);

        // Act
        Result<ParticipationHistoryPage> result =
            await handler.Handle(
                query,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        reader.WasCalled
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_AndNotCallReader_WhenPageSizeExceedsMaximum()
    {
        // Arrange
        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        var reader =
            new CapturingParticipationReader();

        var handler =
            new ListParticipationHistoryByAtmacaCardQueryHandler(
                new GrantedActorAuthorizationService(),
                reader);

        var query =
            new ListParticipationHistoryByAtmacaCardQuery(
                atmacaCardId,
                PageSize:
                    ListParticipationHistoryByAtmacaCardQueryHandler
                        .MaxPageSize + 1,
                Cursor: null);

        // Act
        Result<ParticipationHistoryPage> result =
            await handler.Handle(
                query,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        reader.WasCalled
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_AndNotCallReader_WhenCursorBelongsToDifferentAtmacaCard()
    {
        // Arrange
        AtmacaCardId requestedAtmacaCardId =
            AtmacaCardId.New();

        AtmacaCardId cursorAtmacaCardId =
            AtmacaCardId.New();

        ParticipationHistoryCursor cursor =
            new(
                cursorAtmacaCardId,
                new DateTime(
                    2026,
                    8,
                    25,
                    8,
                    0,
                    0,
                    DateTimeKind.Utc),
                Guid.NewGuid());

        var reader =
            new CapturingParticipationReader();

        var handler =
            new ListParticipationHistoryByAtmacaCardQueryHandler(
                new GrantedActorAuthorizationService(),
                reader);

        var query =
            new ListParticipationHistoryByAtmacaCardQuery(
                requestedAtmacaCardId,
                PageSize: 25,
                Cursor: cursor);

        // Act
        Result<ParticipationHistoryPage> result =
            await handler.Handle(
                query,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure
            .Should()
            .BeTrue();

        reader.WasCalled
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ForwardCursorUnchanged_WhenCursorBelongsToRequestedAtmacaCard()
    {
        // Arrange
        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        ParticipationHistoryCursor cursor =
            new(
                atmacaCardId,
                new DateTime(
                    2026,
                    8,
                    25,
                    9,
                    30,
                    0,
                    DateTimeKind.Utc),
                Guid.NewGuid());

        var reader =
            new CapturingParticipationReader(
                new ParticipationHistoryPage(
                    Array.Empty<ParticipationHistoryItem>(),
                    null));

        var handler =
            new ListParticipationHistoryByAtmacaCardQueryHandler(
                new GrantedActorAuthorizationService(),
                reader);

        var query =
            new ListParticipationHistoryByAtmacaCardQuery(
                atmacaCardId,
                PageSize: 25,
                Cursor: cursor);

        // Act
        Result<ParticipationHistoryPage> result =
            await handler.Handle(
                query,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        reader.WasCalled
            .Should()
            .BeTrue();

        reader.CapturedAtmacaCardId
            .Should()
            .Be(atmacaCardId);

        reader.CapturedPageSize
            .Should()
            .Be(25);

        reader.CapturedCursor
            .Should()
            .BeSameAs(cursor);
    }

    [Theory]
    [InlineData(
    ListParticipationHistoryByAtmacaCardQueryHandler.MinPageSize)]
    [InlineData(
    ListParticipationHistoryByAtmacaCardQueryHandler.MaxPageSize)]
    public async Task Handle_Should_CallReader_WithExactPageSize_WhenPageSizeIsAtBoundary(
    int pageSize)
    {
        // Arrange
        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        var reader =
            new CapturingParticipationReader(
                new ParticipationHistoryPage(
                    Array.Empty<ParticipationHistoryItem>(),
                    null));

        var handler =
            new ListParticipationHistoryByAtmacaCardQueryHandler(
                new GrantedActorAuthorizationService(),
                reader);

        var query =
            new ListParticipationHistoryByAtmacaCardQuery(
                atmacaCardId,
                pageSize,
                Cursor: null);

        // Act
        Result<ParticipationHistoryPage> result =
            await handler.Handle(
                query,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        reader.WasCalled
            .Should()
            .BeTrue();

        reader.CapturedPageSize
            .Should()
            .Be(pageSize);
    }

    private sealed class GrantedActorAuthorizationService
        : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Result.Success());
        }
    }

    private sealed class FakeParticipationReader
        : IParticipationReader
    {
        private readonly ParticipationHistoryPage
            _page;

        public FakeParticipationReader(
            ParticipationHistoryPage page)
        {
            _page =
                page;
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
            throw new NotSupportedException();
        }

        public Task<ParticipationHistoryPage>
            ListHistoryByAtmacaCardAsync(
                AtmacaCardId atmacaCardId,
                int pageSize,
                ParticipationHistoryCursor? cursor,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _page);
        }
    }

    private sealed class CapturingParticipationReader
    : IParticipationReader
    {
        private readonly ParticipationHistoryPage?
            _page;

        public CapturingParticipationReader(
            ParticipationHistoryPage? page = null)
        {
            _page =
                page;
        }

        public bool WasCalled
        {
            get;
            private set;
        }

        public AtmacaCardId? CapturedAtmacaCardId { get; private set; }

        public int CapturedPageSize { get; private set; }

        public ParticipationHistoryCursor? CapturedCursor { get; private set; }

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
            throw new NotSupportedException();
        }

        public Task<ParticipationHistoryPage>
            ListHistoryByAtmacaCardAsync(
                AtmacaCardId atmacaCardId,
                int pageSize,
                ParticipationHistoryCursor? cursor,
                CancellationToken cancellationToken = default)
        {
            WasCalled = true;

            CapturedAtmacaCardId =
                atmacaCardId;

            CapturedPageSize =
                pageSize;

            CapturedCursor =
                cursor;

            if (_page is null)
            {
                throw new InvalidOperationException(
                    "Reader should not be called.");
            }

            return Task.FromResult(
                _page);
        }
    }
}
