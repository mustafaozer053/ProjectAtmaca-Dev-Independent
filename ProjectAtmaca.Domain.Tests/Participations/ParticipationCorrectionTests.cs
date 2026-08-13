using FluentAssertions;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Participations.DomainEvents;
using ProjectAtmaca.Domain.Trainings;
using Xunit;

namespace ProjectAtmaca.Domain.Tests.Participations;

public sealed class ParticipationCorrectionTests
{
    [Fact]
    public void CorrectArrival_Should_Fail_When_ArrivalWasNotRecorded()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        ParticipationCorrectionReason reason =
            CreateCorrectionReason();

        // Act
        Result result =
            participation.CorrectArrival(
                CreateJoinedAt(),
                reason);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors.ArrivalNotRecorded);

        participation.JoinedAt.Should()
            .BeNull();
    }

    [Fact]
    public void CorrectArrival_Should_Be_Idempotent_When_ValueIsUnchanged()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        participation.RecordArrival(joinedAt);

        ParticipationCorrectionReason reason =
            CreateCorrectionReason();

        int eventCountBefore =
            participation.DomainEvents.Count;

        // Act
        Result result =
            participation.CorrectArrival(
                joinedAt,
                reason);

        // Assert
        result.IsSuccess.Should().BeTrue();

        participation.JoinedAt.Should()
            .Be(joinedAt);

        participation.DomainEvents.Count.Should()
            .Be(eventCountBefore);

        participation.DomainEvents
            .Should()
            .NotContain(e =>
                e is ParticipationArrivalCorrectedDomainEvent);
    }

    [Fact]
    public void CorrectArrival_Should_UpdateArrival_AndRaiseDomainEvent()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        DateTimeOffset originalJoinedAt =
            CreateJoinedAt();

        DateTimeOffset correctedJoinedAt =
            originalJoinedAt.AddMinutes(7);

        participation.RecordArrival(
            originalJoinedAt);

        ParticipationCorrectionReason reason =
            CreateCorrectionReason();

        int eventCountBefore =
            participation.DomainEvents.Count;

        // Act
        Result result =
            participation.CorrectArrival(
                correctedJoinedAt,
                reason);

        // Assert
        result.IsSuccess.Should().BeTrue();

        participation.JoinedAt.Should()
            .Be(correctedJoinedAt);

        participation.DomainEvents.Count.Should()
            .Be(eventCountBefore + 1);

        ParticipationArrivalCorrectedDomainEvent domainEvent =
            participation.DomainEvents
                .Should()
                .ContainSingle(e =>
                    e is ParticipationArrivalCorrectedDomainEvent)
                .Which
                .Should()
                .BeOfType<ParticipationArrivalCorrectedDomainEvent>()
                .Which;

        domainEvent.ParticipationId.Should()
            .Be(participation.ParticipationId);

        domainEvent.PreviousJoinedAt.Should()
            .Be(originalJoinedAt);

        domainEvent.CorrectedJoinedAt.Should()
            .Be(correctedJoinedAt);

        domainEvent.Reason.Should()
            .Be(reason);
    }

    [Fact]
    public void CorrectArrival_Should_Fail_AndPreserveState_When_AfterDeparture()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        DateTimeOffset leftAt =
            joinedAt.AddHours(1);

        participation.RecordArrival(joinedAt);
        participation.RecordDeparture(leftAt);

        ParticipationCorrectionReason reason =
            CreateCorrectionReason();

        DateTimeOffset invalidCorrectedJoinedAt =
            leftAt.AddMinutes(1);

        int eventCountBefore =
            participation.DomainEvents.Count;

        // Act
        Result result =
            participation.CorrectArrival(
                invalidCorrectedJoinedAt,
                reason);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors
                    .ArrivalCannotBeAfterDeparture);

        participation.JoinedAt.Should()
            .Be(joinedAt);

        participation.LeftAt.Should()
            .Be(leftAt);

        participation.DomainEvents.Count.Should()
            .Be(eventCountBefore);
    }

    [Fact]
    public void CorrectDeparture_Should_Fail_When_DepartureWasNotRecorded()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        participation.RecordArrival(joinedAt);

        ParticipationCorrectionReason reason =
            CreateCorrectionReason();

        // Act
        Result result =
            participation.CorrectDeparture(
                joinedAt.AddHours(1),
                reason);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors.DepartureNotRecorded);

        participation.LeftAt.Should()
            .BeNull();
    }

    [Fact]
    public void CorrectDeparture_Should_Be_Idempotent_When_ValueIsUnchanged()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        DateTimeOffset leftAt =
            joinedAt.AddHours(1);

        participation.RecordArrival(joinedAt);
        participation.RecordDeparture(leftAt);

        ParticipationCorrectionReason reason =
            CreateCorrectionReason();

        int eventCountBefore =
            participation.DomainEvents.Count;

        // Act
        Result result =
            participation.CorrectDeparture(
                leftAt,
                reason);

        // Assert
        result.IsSuccess.Should().BeTrue();

        participation.LeftAt.Should()
            .Be(leftAt);

        participation.DomainEvents.Count.Should()
            .Be(eventCountBefore);

        participation.DomainEvents
            .Should()
            .NotContain(e =>
                e is ParticipationDepartureCorrectedDomainEvent);
    }

    [Fact]
    public void CorrectDeparture_Should_UpdateDeparture_AndRaiseDomainEvent()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        DateTimeOffset originalLeftAt =
            joinedAt.AddHours(1);

        DateTimeOffset correctedLeftAt =
            originalLeftAt.AddMinutes(15);

        participation.RecordArrival(joinedAt);
        participation.RecordDeparture(
            originalLeftAt);

        ParticipationCorrectionReason reason =
            CreateCorrectionReason();

        int eventCountBefore =
            participation.DomainEvents.Count;

        // Act
        Result result =
            participation.CorrectDeparture(
                correctedLeftAt,
                reason);

        // Assert
        result.IsSuccess.Should().BeTrue();

        participation.LeftAt.Should()
            .Be(correctedLeftAt);

        participation.DomainEvents.Count.Should()
            .Be(eventCountBefore + 1);

        ParticipationDepartureCorrectedDomainEvent domainEvent =
            participation.DomainEvents
                .Should()
                .ContainSingle(e =>
                    e is ParticipationDepartureCorrectedDomainEvent)
                .Which
                .Should()
                .BeOfType<ParticipationDepartureCorrectedDomainEvent>()
                .Which;

        domainEvent.ParticipationId.Should()
            .Be(participation.ParticipationId);

        domainEvent.PreviousLeftAt.Should()
            .Be(originalLeftAt);

        domainEvent.CorrectedLeftAt.Should()
            .Be(correctedLeftAt);

        domainEvent.Reason.Should()
            .Be(reason);
    }

    [Fact]
    public void CorrectDeparture_Should_Fail_AndPreserveState_When_BeforeArrival()
    {
        // Arrange
        Participation participation =
            CreateParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        DateTimeOffset leftAt =
            joinedAt.AddHours(1);

        participation.RecordArrival(joinedAt);
        participation.RecordDeparture(leftAt);

        ParticipationCorrectionReason reason =
            CreateCorrectionReason();

        DateTimeOffset invalidCorrectedLeftAt =
            joinedAt.AddMinutes(-1);

        int eventCountBefore =
            participation.DomainEvents.Count;

        // Act
        Result result =
            participation.CorrectDeparture(
                invalidCorrectedLeftAt,
                reason);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors
                    .DepartureCannotBeBeforeArrival);

        participation.JoinedAt.Should()
            .Be(joinedAt);

        participation.LeftAt.Should()
            .Be(leftAt);

        participation.DomainEvents.Count.Should()
            .Be(eventCountBefore);
    }

    private static Participation CreateParticipation()
    {
        ActivityReference activityReference =
            ActivityReference.ForTraining(
                TrainingId.New());

        AtmacaCardId atmacaCardId =
            AtmacaCardId.New();

        return Participation.Create(
            activityReference,
            atmacaCardId).Value!;
    }

    private static ParticipationCorrectionReason
        CreateCorrectionReason()
    {
        return ParticipationCorrectionReason.Create(
            "Yanlış giriş düzeltildi.").Value!;
    }

    private static DateTimeOffset CreateJoinedAt()
    {
        return new DateTimeOffset(
            2026,
            8,
            4,
            17,
            55,
            0,
            TimeSpan.FromHours(3));
    }
}
