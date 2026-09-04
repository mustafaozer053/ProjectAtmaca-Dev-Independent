using System.Diagnostics;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace ProjectAtmaca.Api.Tests.Observability;

public sealed class CorrelationProductionCompositionTests
{
    [Fact]
    public void ProductionComposition_Should_IncludeTraceIdAndSpanIdInLoggingScopes()
    {
        // Arrange
        using WebApplicationFactory<global::Program> factory =
            new();

        ActivityTrackingOptions requiredTrackingOptions =
            ActivityTrackingOptions.TraceId
            |
            ActivityTrackingOptions.SpanId;

        // Act
        LoggerFactoryOptions loggerFactoryOptions =
            factory.Services
                .GetRequiredService<
                    IOptions<LoggerFactoryOptions>>()
                .Value;

        ActivityTrackingOptions actualTrackingOptions =
            loggerFactoryOptions.ActivityTrackingOptions
            &
            requiredTrackingOptions;

        // Assert
        actualTrackingOptions
            .Should()
            .Be(requiredTrackingOptions);
    }

    [Fact]
    public void ProductionComposition_Should_EnableConsoleScopesForActivityCorrelation()
    {
        // Arrange
        using WebApplicationFactory<global::Program> factory =
            new();

        // Act
        IOptionsMonitor<SimpleConsoleFormatterOptions>
            formatterOptions =
                factory.Services
                    .GetRequiredService<
                        IOptionsMonitor<
                            SimpleConsoleFormatterOptions>>();

        SimpleConsoleFormatterOptions
            simpleConsoleOptions =
                formatterOptions.CurrentValue;

        // Assert
        simpleConsoleOptions.IncludeScopes
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task ProductionHost_Should_PreserveIncomingTraceId_AndCreateServerSpan()
    {
        // Arrange
        using Activity callerActivity =
            new Activity(
                "ProjectAtmaca.Api.Tests.Caller")
                .SetIdFormat(
                    ActivityIdFormat.W3C)
                .Start();

        var serverActivityStarted =
            new TaskCompletionSource<ActivityContext>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        using ActivityListener listener =
            new()
            {
                ShouldListenTo =
                    source =>
                        source.Name ==
                        "Microsoft.AspNetCore",
                Sample =
                    (ref ActivityCreationOptions<ActivityContext> _) =>
                        ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted =
                    activity =>
                    {
                        if (
                            activity.OperationName ==
                            "Microsoft.AspNetCore.Hosting.HttpRequestIn"
                            && activity.TraceId ==
                            callerActivity.TraceId)
                        {
                            serverActivityStarted
                                .TrySetResult(
                                    activity.Context);
                        }
                    }
            };

        ActivitySource.AddActivityListener(
            listener);

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

        string traceParent =
            $"00-{callerActivity.TraceId}-{callerActivity.SpanId}-01";

        using HttpRequestMessage request =
            new(
                HttpMethod.Get,
                "/WeatherForecast");

        request.Headers
            .TryAddWithoutValidation(
                "traceparent",
                traceParent)
            .Should()
            .BeTrue();

        // Act
        using HttpResponseMessage response =
            await client.SendAsync(
                request,
                TestContext.Current.CancellationToken);

        ActivityContext serverActivity =
            await serverActivityStarted
                .Task
                .WaitAsync(
                    TimeSpan.FromSeconds(5),
                    TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessStatusCode
            .Should()
            .BeTrue();

        serverActivity.TraceId
            .Should()
            .Be(callerActivity.TraceId);

        serverActivity.SpanId
            .Should()
            .NotBe(default);

        serverActivity.SpanId
            .Should()
            .NotBe(callerActivity.SpanId);
    }
}