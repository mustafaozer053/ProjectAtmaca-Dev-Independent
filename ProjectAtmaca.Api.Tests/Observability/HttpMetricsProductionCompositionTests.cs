using System.Diagnostics.Metrics;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace ProjectAtmaca.Api.Tests.Observability;

public sealed class HttpMetricsProductionCompositionTests
{
    [Fact]
    public async Task ProductionHost_Should_RecordBuiltInHttpRequestDurationMetric()
    {
        // Arrange
        var requestDurationRecorded =
            new TaskCompletionSource<double>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        using MeterListener listener =
            new();

        listener.InstrumentPublished =
            (instrument, publishedListener) =>
            {
                if (
                    instrument.Meter.Name ==
                        "Microsoft.AspNetCore.Hosting"
                    &&
                    instrument.Name ==
                        "http.server.request.duration")
                {
                    publishedListener.EnableMeasurementEvents(
                        instrument);
                }
            };

        listener.SetMeasurementEventCallback<double>(
            (instrument, measurement, tags, state) =>
            {
                if (
                    instrument.Meter.Name ==
                        "Microsoft.AspNetCore.Hosting"
                    &&
                    instrument.Name ==
                        "http.server.request.duration")
                {
                    requestDurationRecorded.TrySetResult(
                        measurement);
                }
            });

        listener.Start();

        using WebApplicationFactory<global::Program> factory =
            new();

        using HttpClient client =
            factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress =
                        new Uri(
                            "https://localhost")
                });

        // Act
        using HttpResponseMessage response =
            await client.GetAsync(
                "/WeatherForecast",
                TestContext.Current.CancellationToken);

        double requestDuration =
            await requestDurationRecorded
                .Task
                .WaitAsync(
                    TimeSpan.FromSeconds(5),
                    TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessStatusCode
            .Should()
            .BeTrue();

        requestDuration
            .Should()
            .BeGreaterThanOrEqualTo(0);
    }
}