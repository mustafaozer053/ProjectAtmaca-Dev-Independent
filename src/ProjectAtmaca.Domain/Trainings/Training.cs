using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Trainings.DomainEvents;

namespace ProjectAtmaca.Domain.Trainings;

public sealed class Training : AuditableAggregateRoot
{
    private readonly List<TrainingTypeAssignment>
        _trainingTypeAssignments = [];

    public TrainingId TrainingId =>
        TrainingId.From(Id);

    public SeasonOrganization SeasonOrganization { get; private set; }

    public TrainingTitle Title { get; private set; }

    public TrainingDescription Description { get; private set; }

    public TrainingLocation Location { get; private set; }

    public TrainingSchedule Schedule { get; private set; }

    public TrainingStatus Status { get; private set; }

    public IReadOnlyCollection<TrainingTypeAssignment>
        TrainingTypeAssignments =>
            _trainingTypeAssignments.AsReadOnly();

    // Parameterless constructor
    private Training()
    {
        SeasonOrganization = null!;
        Title = null!;
        Description = null!;
        Location = null!;
        Schedule = null!;
    }

    // Constructors
    private Training(
    TrainingId id,
    SeasonOrganization seasonOrganization,
    TrainingTitle title,
    TrainingDescription description,
    TrainingLocation location,
    TrainingSchedule schedule)
    : base(id.Value)
    {
        SeasonOrganization = seasonOrganization;
        Title = title;
        Description = description;
        Location = location;
        Schedule = schedule;
        Status = TrainingStatus.Planned;
    }

    public static Result<Training> Create(
    SeasonOrganization seasonOrganization,
    TrainingTitle title,
    TrainingDescription description,
    TrainingLocation location,
    TrainingSchedule schedule,
    IEnumerable<TrainingTypeAssignment> assignments)
    {
        ArgumentNullException.ThrowIfNull(seasonOrganization);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(assignments);

        List<TrainingTypeAssignment> assignmentList =
            assignments.ToList();

        if (assignmentList.Any(assignment => assignment is null))
        {
            throw new ArgumentException(
                "Training type assignments cannot contain null elements.",
                nameof(assignments));
        }

        Result validationResult =
            ValidateTrainingTypeAssignments(
                schedule,
                assignmentList);

        if (validationResult.IsFailure)
        {
            return Result<Training>.Failure(
                validationResult.Error!);
        }

        var training = new Training(
            TrainingId.New(),
            seasonOrganization,
            title,
            description,
            location,
            schedule);

        training._trainingTypeAssignments.AddRange(
            assignmentList);

        training.RaiseDomainEvent(
            new TrainingPlannedDomainEvent(
                training.TrainingId));

        return Result<Training>.Success(training);
    }

    public void Rename(
    TrainingTitle title)
    {
        // 1. Guard Clause
        ArgumentNullException.ThrowIfNull(title);

        // 2. No-op Check
        if (Title == title)
        {
            return;
        }

        // 3. Domain Rule(s)
        // (gerekiyorsa)

        // 4. State Change
        Title = title;

        // 5. Domain Event
        // (ileride)
    }

    public void ChangeDescription(
    TrainingDescription description)
    {
        ArgumentNullException.ThrowIfNull(description);

        if (Description == description)
        {
            return;
        }

        Description = description;
    }

    public void ChangeLocation(
    TrainingLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (Location == location)
        {
            return;
        }

        Location = location;
    }
    public Result Reschedule(
    TrainingSchedule schedule,
    IEnumerable<TrainingTypeAssignment> assignments)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(assignments);

        List<TrainingTypeAssignment> assignmentList =
            assignments.ToList();

        if (assignmentList.Any(assignment => assignment is null))
        {
            throw new ArgumentException(
                "Training type assignments cannot contain null elements.",
                nameof(assignments));
        }

        Result validationResult =
            ValidateTrainingTypeAssignments(
                schedule,
                assignmentList);

        if (validationResult.IsFailure)
        {
            return Result.Failure(
                validationResult.Error!);
        }

        bool scheduleUnchanged =
            Schedule == schedule;

        bool assignmentsUnchanged =
            HaveSameTrainingTypeAssignments(
                _trainingTypeAssignments,
                assignmentList);

        if (scheduleUnchanged && assignmentsUnchanged)
        {
            return Result.Success();
        }

        Schedule = schedule;

        _trainingTypeAssignments.Clear();
        _trainingTypeAssignments.AddRange(
            assignmentList);

        return Result.Success();
    }
    public Result UpdateTrainingTypeAssignments(
    IEnumerable<TrainingTypeAssignment> assignments)
    {
        ArgumentNullException.ThrowIfNull(assignments);

        List<TrainingTypeAssignment> assignmentList =
            assignments.ToList();

        if (assignmentList.Any(assignment => assignment is null))
        {
            throw new ArgumentException(
                "Training type assignments cannot contain null elements.",
                nameof(assignments));
        }

        Result validationResult =
            ValidateTrainingTypeAssignments(
                Schedule,
                assignmentList);

        if (validationResult.IsFailure)
        {
            return Result.Failure(
                validationResult.Error!);
        }

        if (HaveSameTrainingTypeAssignments(
            _trainingTypeAssignments,
            assignmentList))
        {
            return Result.Success();
        }

        _trainingTypeAssignments.Clear();

        _trainingTypeAssignments.AddRange(
            assignmentList);

        return Result.Success();
    }
    public void Confirm()
    {
        if (Status == TrainingStatus.Cancelled)
        {
            return;
        }

        if (Status == TrainingStatus.Confirmed)
        {
            return;
        }

        Status = TrainingStatus.Confirmed;

        RaiseDomainEvent(
            new TrainingConfirmedDomainEvent(
                TrainingId));
    }

    public Result Cancel()
    {
        if (Status == TrainingStatus.Cancelled)
        {
            return Result.Success();
        }

        Status = TrainingStatus.Cancelled;

        RaiseDomainEvent(
            new TrainingCancelledDomainEvent(
                TrainingId));

        return Result.Success();
    }

    private static bool HaveSameTrainingTypeAssignments(
        IReadOnlyCollection<TrainingTypeAssignment> currentAssignments,
        IReadOnlyCollection<TrainingTypeAssignment> newAssignments)
    {
        if (currentAssignments.Count != newAssignments.Count)
        {
            return false;
        }

        return currentAssignments.All(
            currentAssignment =>
                newAssignments.Any(
                    newAssignment =>
                        newAssignment.TrainingTypeId ==
                            currentAssignment.TrainingTypeId &&
                        newAssignment.Duration ==
                            currentAssignment.Duration));
    }

    private static Result ValidateTrainingTypeAssignments(
        TrainingSchedule schedule,
        IReadOnlyCollection<TrainingTypeAssignment> assignments)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(assignments);

        if (assignments.Count == 0)
        {
            return Result.Failure(
                TrainingErrors.TrainingTypeAssignmentRequired);
        }

        bool hasDuplicates =
            assignments
                .GroupBy(assignment =>
                    assignment.TrainingTypeId)
                .Any(group => group.Count() > 1);

        if (hasDuplicates)
        {
            return Result.Failure(
                TrainingErrors.DuplicateTrainingTypeAssignment);
        }

        int totalAssignedDurationMinutes =
            assignments.Sum(
                assignment =>
                    assignment.Duration.Minutes);

        if (totalAssignedDurationMinutes !=
            schedule.DurationMinutes)
        {
            return Result.Failure(
                TrainingErrors.TrainingTypeDurationMismatch);
        }

        return Result.Success();
    }
}