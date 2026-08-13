using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Participations;

public static class ParticipationErrors
{
    public static readonly Error ActivityTypeCodeRequired =
        Error.Create(
            "Participation.ActivityTypeCode.Required",
            "Activity type code is required.");

    public static Error UnsupportedActivityTypeCode(
        string code)
    {
        return Error.Create(
            "Participation.ActivityTypeCode.Unsupported",
            $"Activity type code '{code}' is not supported.");
    }

    public static Error UnsupportedParticipationCondition(
        string code)
    {
        return Error.Create(
            "Participation.Condition.Unsupported",
            $"Participation condition '{code}' is not supported.");
    }

    public static readonly Error InvalidConditionForStatus =
        Error.Create(
            "Participation.Condition.InvalidForStatus",
            "Participation condition is not valid for the specified status.");

    public static readonly Error ActivityReferenceRequired =
        Error.Create(
            "Participation.ActivityReference.Required",
            "Activity reference is required.");

    public static readonly Error AtmacaCardRequired =
        Error.Create(
            "Participation.AtmacaCard.Required",
            "Atmaca card id is required.");

    public static readonly Error InvalidStatus =
        Error.Create(
            "Participation.Status.Invalid",
            "Participation status is invalid.");

    public static readonly Error ClassificationCorrectionRequired =
        Error.Create(
            "Participation.Classification.CorrectionRequired",
            "An established participation classification cannot be changed through an ordinary classification operation.");

    public static readonly Error ArrivalCannotBeRecordedWhenAbsent =
        Error.Create(
            "Participation.Arrival.CannotBeRecordedWhenAbsent",
            "Arrival cannot be recorded when participation status is absent.");

    public static readonly Error DepartureCannotBeRecordedWhenAbsent =
        Error.Create(
            "Participation.Departure.CannotBeRecordedWhenAbsent",
            "Departure cannot be recorded when participation status is absent.");

    public static readonly Error ArrivalRequiredBeforeDeparture =
        Error.Create(
            "Participation.Departure.ArrivalRequired",
            "Arrival must be recorded before departure.");

    public static readonly Error DepartureCannotBeBeforeArrival =
        Error.Create(
            "Participation.Departure.CannotBeBeforeArrival",
            "Departure cannot be earlier than arrival.");

    public static readonly Error ArrivalCannotBeAfterDeparture =
        Error.Create(
            "Participation.Arrival.CannotBeAfterDeparture",
            "Arrival cannot be later than the recorded departure.");

    public static readonly Error ArrivalCorrectionRequired =
        Error.Create(
            "Participation.Arrival.CorrectionRequired",
            "The recorded arrival cannot be changed through the ordinary arrival recording operation.");

    public static readonly Error DepartureCorrectionRequired =
        Error.Create(
            "Participation.Departure.CorrectionRequired",
            "The recorded departure cannot be changed through the ordinary departure recording operation.");

    public static readonly Error ArrivalNotRecorded =
        Error.Create(
            "Participation.Arrival.NotRecorded",
            "There is no recorded arrival to correct.");

    public static readonly Error DepartureNotRecorded =
        Error.Create(
            "Participation.Departure.NotRecorded",
            "There is no recorded departure to correct.");

    public static readonly Error CorrectionReasonRequired =
        Error.Create(
            "Participation.Correction.ReasonRequired",
            "A correction reason is required.");

    public static readonly Error CorrectionReasonTooLong =
        Error.Create(
            "Participation.Correction.ReasonTooLong",
            "Participation correction reason cannot exceed 250 characters.");

    public static readonly Error NoteTooLong =
        Error.Create(
            "Participation.Note.TooLong",
            "Participation note cannot exceed 250 characters.");
}