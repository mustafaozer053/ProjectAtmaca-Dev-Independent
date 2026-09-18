using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Application.Tests.Persons.RegisterWithAtmacaCard;

public sealed class RegisterPersonWithAtmacaCardAuthorizationTests
{
    [Fact]
    public async Task Execute_Should_ReturnAuthorizationFailure_WithoutStartingRegistration()
    {
        var authorization = new AuthorizationStub(Result.Failure(ActorAuthorizationErrors.Forbidden));
        var gate = new PersonRegistrationAuthorization(authorization);
        var calls = 0;
        using var cancellation = new CancellationTokenSource();

        var result = await gate.ExecuteAsync<int>(_ =>
        {
            calls++;
            throw new InvalidOperationException("Registration must not start.");
        }, cancellation.Token);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(ActorAuthorizationErrors.Forbidden);
        calls.Should().Be(0);
        authorization.Calls.Should().Be(1);
        authorization.Permission!.Code.Should().Be("Persons.RegisterWithAtmacaCard");
        authorization.Token.Should().Be(cancellation.Token);
    }

    [Fact]
    public async Task Execute_Should_AwaitAuthorization_ThenForwardResultAndCancellation()
    {
        var completion = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
        var authorization = new AuthorizationStub(completion.Task);
        var gate = new PersonRegistrationAuthorization(authorization);
        var calls = 0;
        var expected = Result<int>.Success(42);
        using var cancellation = new CancellationTokenSource();
        var pending = gate.ExecuteAsync(token =>
        {
            token.Should().Be(cancellation.Token);
            calls++;
            return Task.FromResult(expected);
        }, cancellation.Token);

        calls.Should().Be(0);
        completion.SetResult(Result.Success());
        (await pending).Should().BeSameAs(expected);
        calls.Should().Be(1);
    }

    [Fact]
    public async Task Execute_Should_ReauthorizeEachAttempt_AndPreserveRegistrationFailure()
    {
        var authorization = new AuthorizationStub(Result.Success());
        var gate = new PersonRegistrationAuthorization(authorization);
        var expected = Result<int>.Failure(Error.Create("Registration.Conflict", "Conflict."));
        (await gate.ExecuteAsync(_ => Task.FromResult(expected), TestContext.Current.CancellationToken)).Should().BeSameAs(expected);
        (await gate.ExecuteAsync(_ => Task.FromResult(expected), TestContext.Current.CancellationToken)).Should().BeSameAs(expected);
        authorization.Calls.Should().Be(2);
    }

    [Fact]
    public async Task Execute_Should_NotStartRegistration_WhenCancelledAfterAuthorization()
    {
        using var cancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new PersonRegistrationAuthorization(new AuthorizationStub(completion.Task));
        var calls = 0;
        var pending = gate.ExecuteAsync(_ =>
        {
            calls++;
            return Task.FromResult(Result<int>.Success(42));
        }, cancellation.Token);
        cancellation.Cancel();
        completion.SetResult(Result.Success());
        Func<Task> action = async () => await pending;
        await action.Should().ThrowAsync<OperationCanceledException>();
        calls.Should().Be(0);
    }

    [Fact]
    public async Task Execute_Should_PropagateRegistrationException()
    {
        var gate = new PersonRegistrationAuthorization(new AuthorizationStub(Result.Success()));
        var expected = new InvalidOperationException("Persistence failed.");
        Func<Task> action = async () => await gate.ExecuteAsync<int>(
            _ => Task.FromException<Result<int>>(expected), TestContext.Current.CancellationToken);
        var assertion = await action.Should().ThrowAsync<InvalidOperationException>();
        assertion.Which.Should().BeSameAs(expected);
    }

    private sealed class AuthorizationStub : IActorAuthorizationService
    {
        private readonly Task<Result> _result;
        public int Calls { get; private set; }
        public Permission? Permission { get; private set; }
        public CancellationToken Token { get; private set; }
        public AuthorizationStub(Result result) : this(Task.FromResult(result)) { }
        public AuthorizationStub(Task<Result> result) => _result = result;
        public Task<Result> AuthorizeAsync(Permission permission, CancellationToken cancellationToken = default)
        {
            Calls++;
            Permission = permission;
            Token = cancellationToken;
            return _result;
        }
    }
}
