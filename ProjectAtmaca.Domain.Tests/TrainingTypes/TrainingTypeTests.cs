using FluentAssertions;

using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.TrainingTypes;

namespace ProjectAtmaca.Domain.Tests.TrainingTypes;

public sealed class TrainingTypeTests
{
    [Fact]
    public void Create_Should_CreateActiveTrainingType()
    {
        TrainingType trainingType = CreateTrainingType();

        trainingType.IsActive.Should().BeTrue();
        trainingType.Code.Value.Should().Be("TACTIC");
        trainingType.Name.Value.Should().Be("Taktik");
        trainingType.DisplayOrder.Should().Be(1);
    }

    [Fact]
    public void Deactivate_Should_PreserveTrainingTypeDefinition()
    {
        TrainingType trainingType = CreateTrainingType();

        trainingType.Deactivate();

        trainingType.IsActive.Should().BeFalse();
        trainingType.Code.Value.Should().Be("TACTIC");
        trainingType.Name.Value.Should().Be("Taktik");
        trainingType.Description.Value.Should().Be("Topla çalışma");
    }

    [Fact]
    public void Activate_Should_RestoreTrainingTypeAvailability()
    {
        TrainingType trainingType = CreateTrainingType();

        trainingType.Deactivate();
        trainingType.Activate();

        trainingType.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_RejectNegativeDisplayOrder()
    {
        Result<TrainingType> result = TrainingType.Create(
            TrainingTypeCode.Create("TACTIC").Value!,
            TrainingTypeName.Create("Taktik").Value!,
            TrainingTypeDescription.Create(null).Value!,
            -1);

        result.Error.Should().Be(TrainingTypeErrors.DisplayOrderInvalid);
    }

    private static TrainingType CreateTrainingType() =>
        TrainingType.Create(
            TrainingTypeCode.Create("TACTIC").Value!,
            TrainingTypeName.Create("Taktik").Value!,
            TrainingTypeDescription.Create("Topla çalışma").Value!,
            1)
        .Value!;
}
