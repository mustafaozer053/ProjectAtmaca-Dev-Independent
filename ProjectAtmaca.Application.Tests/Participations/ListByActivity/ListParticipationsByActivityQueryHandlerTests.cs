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
    .Participations.ListByActivity;

public sealed class ListParticipationsByActivityQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnParticipations_WhenMatchesExist()
    {
        // Arrange
        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        IReadOnlyList<ParticipationListItem>
            expected =
            new[]
            {
                new ParticipationListItem(
                    Guid.NewGuid(),
                    AtmacaCardId.New().Value,
                    ParticipationStatus.Present,
                    ParticipationCondition.Late.Code,
                    new DateTimeOffset(
                        2026,
                        8,
                        20,
                        10,
                        0,
                        0,
                        TimeSpan.Zero),
                    null),

                new ParticipationListItem(
                    Guid.NewGuid(),
                    AtmacaCardId.New().Value,
                    ParticipationStatus.NotRecorded,
                    null,
                    null,
                    null)
            };

        var reader =
            new FakeParticipationReader(
                expected);

        var handler =
            new ListParticipationsByActivityQueryHandler(
                new GrantedActorAuthorizationService(),
                reader);

        var query =
            new ListParticipationsByActivityQuery(
                activityReference);

        // Act
        Result<IReadOnlyList<ParticipationListItem>>
            result =
            await handler.Handle(
                query,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        result.Value
            .Should()
            .Equal(expected);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptyCollection_WhenNoMatchesExist()
    {
        // Arrange
        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        IReadOnlyList<ParticipationListItem>
            expected =
            Array.Empty<ParticipationListItem>();

        var reader =
            new FakeParticipationReader(
                expected);

        var handler =
            new ListParticipationsByActivityQueryHandler(
                new GrantedActorAuthorizationService(),
                reader);

        var query =
            new ListParticipationsByActivityQuery(
                activityReference);

        // Act
        Result<IReadOnlyList<ParticipationListItem>>
            result =
            await handler.Handle(
                query,
                TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();

        result.Value
            .Should()
            .BeEmpty();
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
        private readonly IReadOnlyList<
            ParticipationListItem> _participations;

        public FakeParticipationReader(
            IReadOnlyList<ParticipationListItem>
                participations)
        {
            _participations =
                participations;
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
            return Task.FromResult(
                _participations);
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
            throw new NotSupportedException();
        }
    }
}
