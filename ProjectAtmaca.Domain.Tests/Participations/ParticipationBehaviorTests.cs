using FluentAssertions;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Participations;
using ProjectAtmaca.Domain.Trainings;
using Xunit;

namespace ProjectAtmaca.Domain.Tests.Participations;

public sealed class ParticipationBehaviorTests
{
    [Fact]
    public void MarkPresent_Should_ChangeStatus_From_NotRecorded_To_Present()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        // Act
        Result result =
            participation.MarkPresent();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        participation.Status.Should()
            .Be(ParticipationStatus.Present);

        participation.Condition.Should()
            .BeNull();
    }

    [Fact]
    public void MarkPresent_Should_SetLateCondition_When_ConditionIsLate()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        // Act
        Result result =
            participation.MarkPresent(
                ParticipationCondition.Late);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        participation.Status.Should()
            .Be(ParticipationStatus.Present);

        participation.Condition.Should()
            .Be(ParticipationCondition.Late);

        participation.Condition!
            .AttendanceTreatment.Should()
            .Be(AttendanceTreatment.Included);
    }

    [Fact]
    public void MarkPresent_Should_Fail_And_PreserveState_When_ConditionIsBta()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        ParticipationStatus originalStatus =
            participation.Status;

        ParticipationCondition? originalCondition =
            participation.Condition;

        // Act
        Result result =
            participation.MarkPresent(
                ParticipationCondition.Bta);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors.InvalidConditionForStatus);

        participation.Status.Should()
            .Be(originalStatus);

        participation.Condition.Should()
            .Be(originalCondition);
    }

    [Fact]
    public void MarkPresent_Should_Fail_When_StatusIsAbsent()
    {
        // Arrange
        Participation participation =
            CreateAbsentBtaParticipation();

        ParticipationStatus originalStatus =
            participation.Status;

        ParticipationCondition? originalCondition =
            participation.Condition;

        // Act
        Result result =
            participation.MarkPresent();

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors
                    .ClassificationCorrectionRequired);

        participation.Status.Should()
            .Be(originalStatus);

        participation.Condition.Should()
            .Be(originalCondition);
    }

    [Fact]
    public void MarkPresent_Should_Be_Idempotent_When_SemanticStateIsUnchanged()
    {
        // Arrange
        Participation participation =
            CreatePresentParticipation();

        // Act
        Result result =
            participation.MarkPresent();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        participation.Status.Should()
            .Be(ParticipationStatus.Present);

        participation.Condition.Should()
            .BeNull();
    }

    [Fact]
    public void MarkAbsent_Should_ChangeStatus_From_NotRecorded_To_Absent()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        // Act
        Result result =
            participation.MarkAbsent(
                ParticipationCondition.Bta);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        participation.Status.Should()
            .Be(ParticipationStatus.Absent);

        participation.Condition.Should()
            .Be(ParticipationCondition.Bta);

        participation.Condition!
            .AttendanceTreatment.Should()
            .Be(AttendanceTreatment.Excluded);

        participation.JoinedAt.Should()
            .BeNull();

        participation.LeftAt.Should()
            .BeNull();
    }

    [Fact]
    public void MarkAbsent_Should_Succeed_Without_Condition()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        // Act
        Result result =
            participation.MarkAbsent();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        participation.Status.Should()
            .Be(ParticipationStatus.Absent);

        participation.Condition.Should()
            .BeNull();
    }

    [Fact]
    public void MarkAbsent_Should_Fail_And_PreserveState_When_ConditionIsLate()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        ParticipationStatus originalStatus =
            participation.Status;

        ParticipationCondition? originalCondition =
            participation.Condition;

        // Act
        Result result =
            participation.MarkAbsent(
                ParticipationCondition.Late);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors.InvalidConditionForStatus);

        participation.Status.Should()
            .Be(originalStatus);

        participation.Condition.Should()
            .Be(originalCondition);
    }

    [Fact]
    public void MarkAbsent_Should_Fail_When_StatusIsPresent()
    {
        // Arrange
        Participation participation =
            CreatePresentParticipation();

        ParticipationStatus originalStatus =
            participation.Status;

        ParticipationCondition? originalCondition =
            participation.Condition;

        // Act
        Result result =
            participation.MarkAbsent(
                ParticipationCondition.Bta);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors
                    .ClassificationCorrectionRequired);

        participation.Status.Should()
            .Be(originalStatus);

        participation.Condition.Should()
            .Be(originalCondition);
    }

    [Fact]
    public void MarkAbsent_Should_Fail_When_ArrivalWasAlreadyRecorded()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        participation.RecordArrival(joinedAt);

        // Act
        Result result =
            participation.MarkAbsent(
                ParticipationCondition.Bta);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors
                    .ClassificationCorrectionRequired);

        participation.Status.Should()
            .Be(ParticipationStatus.NotRecorded);

        participation.JoinedAt.Should()
            .Be(joinedAt);

        participation.LeftAt.Should()
            .BeNull();
    }

    [Fact]
    public void RecordArrival_Should_SetJoinedAt_When_StatusIsNotRecorded()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        // Act
        Result result =
            participation.RecordArrival(
                joinedAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        participation.Status.Should()
            .Be(ParticipationStatus.NotRecorded);

        participation.JoinedAt.Should()
            .Be(joinedAt);
    }

    [Fact]
    public void RecordArrival_Should_SetJoinedAt_When_StatusIsPresent()
    {
        // Arrange
        Participation participation =
            CreatePresentParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        // Act
        Result result =
            participation.RecordArrival(
                joinedAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        participation.Status.Should()
            .Be(ParticipationStatus.Present);

        participation.JoinedAt.Should()
            .Be(joinedAt);
    }

    [Fact]
    public void RecordArrival_Should_Fail_And_PreserveState_When_StatusIsAbsent()
    {
        // Arrange
        Participation participation =
            CreateAbsentBtaParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        // Act
        Result result =
            participation.RecordArrival(
                joinedAt);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors
                    .ArrivalCannotBeRecordedWhenAbsent);

        participation.Status.Should()
            .Be(ParticipationStatus.Absent);

        participation.JoinedAt.Should()
            .BeNull();
    }

    [Fact]
    public void RecordArrival_Should_Be_Idempotent_When_SameValueIsRepeated()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        participation.RecordArrival(joinedAt);

        // Act
        Result result =
            participation.RecordArrival(joinedAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        participation.JoinedAt.Should()
            .Be(joinedAt);
    }

    [Fact]
    public void RecordArrival_Should_RequireCorrection_When_DifferentArrivalWasAlreadyRecorded()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        DateTimeOffset differentJoinedAt =
            joinedAt.AddMinutes(10);

        participation.RecordArrival(joinedAt);

        // Act
        Result result =
            participation.RecordArrival(
                differentJoinedAt);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors
                    .ArrivalCorrectionRequired);

        participation.JoinedAt.Should()
            .Be(joinedAt);
    }

    [Fact]
    public void RecordDeparture_Should_SetLeftAt_When_ArrivalWasRecorded()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        DateTimeOffset leftAt =
            joinedAt.AddHours(1);

        participation.RecordArrival(joinedAt);

        // Act
        Result result =
            participation.RecordDeparture(leftAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        participation.Status.Should()
            .Be(ParticipationStatus.NotRecorded);

        participation.JoinedAt.Should()
            .Be(joinedAt);

        participation.LeftAt.Should()
            .Be(leftAt);
    }

    [Fact]
    public void RecordDeparture_Should_Fail_When_ArrivalWasNotRecorded()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        DateTimeOffset leftAt =
            CreateJoinedAt().AddHours(1);

        // Act
        Result result =
            participation.RecordDeparture(leftAt);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors
                    .ArrivalRequiredBeforeDeparture);

        participation.LeftAt.Should()
            .BeNull();
    }

    [Fact]
    public void RecordDeparture_Should_Fail_And_PreserveState_When_LeftAtIsBeforeJoinedAt()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        DateTimeOffset invalidLeftAt =
            joinedAt.AddMinutes(-15);

        participation.RecordArrival(joinedAt);

        // Act
        Result result =
            participation.RecordDeparture(
                invalidLeftAt);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors
                    .DepartureCannotBeBeforeArrival);

        participation.JoinedAt.Should()
            .Be(joinedAt);

        participation.LeftAt.Should()
            .BeNull();
    }

    [Fact]
    public void RecordDeparture_Should_Fail_When_StatusIsAbsent()
    {
        // Arrange
        Participation participation =
            CreateAbsentBtaParticipation();

        DateTimeOffset leftAt =
            CreateJoinedAt().AddHours(1);

        // Act
        Result result =
            participation.RecordDeparture(leftAt);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors
                    .DepartureCannotBeRecordedWhenAbsent);

        participation.LeftAt.Should()
            .BeNull();
    }

    [Fact]
    public void RecordDeparture_Should_Be_Idempotent_When_SameValueIsRepeated()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        DateTimeOffset leftAt =
            joinedAt.AddHours(1);

        participation.RecordArrival(joinedAt);
        participation.RecordDeparture(leftAt);

        // Act
        Result result =
            participation.RecordDeparture(leftAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        participation.LeftAt.Should()
            .Be(leftAt);
    }

    [Fact]
    public void RecordDeparture_Should_RequireCorrection_When_DifferentDepartureWasAlreadyRecorded()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        DateTimeOffset joinedAt =
            CreateJoinedAt();

        DateTimeOffset leftAt =
            joinedAt.AddHours(1);

        DateTimeOffset differentLeftAt =
            leftAt.AddMinutes(15);

        participation.RecordArrival(joinedAt);
        participation.RecordDeparture(leftAt);

        // Act
        Result result =
            participation.RecordDeparture(
                differentLeftAt);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should()
            .BeSameAs(
                ParticipationErrors
                    .DepartureCorrectionRequired);

        participation.LeftAt.Should()
            .Be(leftAt);
    }

    [Fact]
    public void UpdateNote_Should_SetNote_When_NoteIsValid()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        ParticipationNote note =
            ParticipationNote.Create(
                "Bugün bireysel çalıştı.").Value!;

        // Act
        participation.UpdateNote(note);

        // Assert
        participation.Note.Should()
            .Be(note);

        participation.Note!.Value.Should()
            .Be("Bugün bireysel çalıştı.");
    }

    [Fact]
    public void UpdateNote_Should_ClearNote_When_NoteIsNull()
    {
        // Arrange
        Participation participation =
            CreateNotRecordedParticipation();

        ParticipationNote note =
            ParticipationNote.Create(
                "Doktor kontrolü nedeniyle erken ayrıldı.").Value!;

        participation.UpdateNote(note);

        // Act
        participation.UpdateNote(null);

        // Assert
        participation.Note.Should()
            .BeNull();
    }

    private static Participation CreateNotRecordedParticipation()
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

    private static Participation CreatePresentParticipation()
    {
        Participation participation =
            CreateNotRecordedParticipation();

        Result result =
            participation.MarkPresent();

        result.IsSuccess.Should().BeTrue();

        return participation;
    }

    private static Participation CreateAbsentBtaParticipation()
    {
        Participation participation =
            CreateNotRecordedParticipation();

        Result result =
            participation.MarkAbsent(
                ParticipationCondition.Bta);

        result.IsSuccess.Should().BeTrue();

        return participation;
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