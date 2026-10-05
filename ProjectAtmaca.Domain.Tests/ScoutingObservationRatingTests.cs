using ProjectAtmaca.Domain.Common.Enums;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Scouting;
using ProjectAtmaca.Domain.Scouting.ValueObjects;

namespace ProjectAtmaca.Domain.Tests.Scouting;

public sealed class ScoutingObservationRatingTests
{
    private static ScoutingCandidate Create(int? rating) => ScoutingCandidate.Create(
        PersonName.Create("Ali Veli").Value!, null, null, BirthDate.Create(new DateTime(2012, 1, 1)), null, null,
        null, null, null, null,
        InitialScoutingSource.Create(InitialScoutingSourceType.Match).Value!,
        DateOnly.FromDateTime(DateTime.Today), ObservationType.Match, "Turnuva", "Kulüp", "U13", null, null, null,
        null, null, null, null, PersonName.Create("Gözlemci Kişi").Value!, Guid.NewGuid(), rating).Value!;

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(null)]
    public void Create_Should_AcceptRatingInRange(int? rating) =>
        Assert.Equal(rating, Create(rating).Observations.Single().Rating);

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Create_Should_RejectRatingOutOfRange(int rating)
    {
        var result = ScoutingCandidate.Create(
            PersonName.Create("Ali Veli").Value!, null, null, BirthDate.Create(new DateTime(2012, 1, 1)), null, null,
            null, null, null, null,
            InitialScoutingSource.Create(InitialScoutingSourceType.Match).Value!,
            DateOnly.FromDateTime(DateTime.Today), ObservationType.Match, null, null, null, null, null, null,
            null, null, null, null, PersonName.Create("Gözlemci Kişi").Value!, Guid.NewGuid(), rating);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void AddObservation_Should_RejectInvalidRating()
    {
        var candidate = Create(5);
        var result = candidate.AddObservation(
            DateOnly.FromDateTime(DateTime.Today), ObservationType.Training, null, null, null, null, null, null,
            null, null, null, null, PersonName.Create("Gözlemci Kişi").Value!, Guid.NewGuid(), 11);

        Assert.True(result.IsFailure);
        Assert.Single(candidate.Observations);
    }

    [Fact]
    public void SetObservationRating_Should_UpdateOrClearRating()
    {
        var candidate = Create(5);
        var id = candidate.Observations.Single().Id;

        Assert.True(candidate.SetObservationRating(id, 8).IsSuccess);
        Assert.Equal(8, candidate.Observations.Single().Rating);
        Assert.True(candidate.SetObservationRating(id, null).IsSuccess);
        Assert.Null(candidate.Observations.Single().Rating);
        Assert.True(candidate.SetObservationRating(id, 11).IsFailure);
    }

    [Fact]
    public void ChangeObservationType_Should_UpdateTypeAndRejectUndefined()
    {
        var candidate = Create(5);
        var id = candidate.Observations.Single().Id;

        Assert.True(candidate.ChangeObservationType(id, ObservationType.Training).IsSuccess);
        Assert.Equal(ObservationType.Training, candidate.Observations.Single().ObservationType);
        Assert.True(candidate.ChangeObservationType(id, (ObservationType)999).IsFailure);
    }
}